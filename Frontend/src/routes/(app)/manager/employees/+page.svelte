<script lang="ts">
    import { onMount } from 'svelte';
    import { Button } from '$lib/components/ui/button';
    import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '$lib/components/ui/card';
    import * as Alert from '$lib/components/ui/alert';
    import * as Table from '$lib/components/ui/table/index.js';
    import { HttpError } from '$lib/customErrors';
    import RefreshCw from 'lucide-svelte/icons/refresh-cw';

    import { createUser, getUsers } from '$lib/services/user.service';
    import { addUserToRole, getJobRoles, removeUserToRole } from '$lib/services/jobRole.service';
    
    import type { CreateUserRequest, UserDto } from '$lib/types/user.types';
    import type { JobRoleShortDto } from '$lib/types/jobRole.types';

    type NotificationType = 'success' | 'error';

    type NotificationState = {
        type: NotificationType;
        message: string;
    } | null;

    type UserWithId = UserDto & {
        id: number;
    };

    type CreateEmployeeForm = {
        firstName: string;
        lastName: string;
        email: string;
        password: string;
        birthDate: string;
        street: string;
        city: string;
        postalCode: string;
        jobRoleId: string;
    };

    let employees = $state<UserWithId[]>([]);
    let jobRoles = $state<JobRoleShortDto[]>([]);
    let loading = $state(false);
    let creating = $state(false);
    let savingRoleForUserId = $state<number | null>(null);

    let notification = $state<NotificationState>(null);
    let createError = $state('');

    let createForm = $state<CreateEmployeeForm>({
        firstName: '',
        lastName: '',
        email: '',
        password: '',
        birthDate: '',
        street: '',
        city: '',
        postalCode: '',
        jobRoleId: ''
    });

    let selectedRoles = $state<Record<number, string>>({});

    function notify(type: NotificationType, message: string) {
        notification = { type, message };
    }

    function notifySuccess(message: string) {
        notify('success', message);
    }

    function notifyError(message: string) {
        notify('error', message);
    }

    function clearNotification() {
        notification = null;
    }

    function getErrorMessage(error: unknown, fallback: string): string {
        if (error instanceof HttpError) {
            return error.Error.message;
        }
        return fallback;
    }

    function getCurrentRole(user: UserWithId): JobRoleShortDto | null {
        return user.assignedJobRoles?.[0] ?? null;
    }

    function formatCurrentRole(user: UserWithId): string {
        return getCurrentRole(user)?.name ?? 'No role assigned';
    }

    function resetCreateForm() {
        createForm = {
            firstName: '',
            lastName: '',
            email: '',
            password: '',
            birthDate: '',
            street: '',
            city: '',
            postalCode: '',
            jobRoleId: ''
        };
        createError = '';
    }

    async function loadEmployees() {
        loading = true;
        try {
            const result = await getUsers({
                page: 1,
                pageSize: 100,
                sortProperty: 'Id',
                accountStatus: 'Ok',
                ascending: true,
                jobFilters: [],
                searchstring: ''
            });

            console.log('getUsers result:', result);
            employees = result.users as UserWithId[];

            selectedRoles = {};
            for (const employee of employees) {
                const currentRole = getCurrentRole(employee);
                selectedRoles[employee.id] = currentRole ? currentRole.id.toString() : '';
            }
        } catch (error) {
            console.error('getUsers failed:', error);
            notifyError(getErrorMessage(error, 'Failed to load employees.'));
        } finally {
            loading = false;
        }
    }

    async function loadJobRoles() {
        try {
            const result = await getJobRoles({
                page: 1,
                pageSize: 100,
                searchstring: ''
            });

            jobRoles = result.jobRoles;
        } catch (error) {
            notifyError(getErrorMessage(error, 'Failed to load job roles.'));
        }
    }

    async function handleCreateEmployee() {
        createError = '';

        if (!createForm.firstName.trim()) {
            createError = 'Please enter a first name.';
            return;
        }

        if (!createForm.lastName.trim()) {
            createError = 'Please enter a last name.';
            return;
        }

        if (!createForm.email.trim()) {
            createError = 'Please enter an email address.';
            return;
        }

        if (!createForm.password.trim()) {
            createError = 'Please enter a password.';
            return;
        }

        if (!createForm.birthDate) {
            createError = 'Please select a birth date.';
            return;
        }

        if (!createForm.street.trim()) {
            createError = 'Please enter a street.';
            return;
        }

        if (!createForm.city.trim()) {
            createError = 'Please enter a city.';
            return;
        }

        if (!createForm.postalCode) {
            createError = 'Please enter a postal code.';
            return;
        }

        if (!createForm.jobRoleId) {
            createError = 'Please select a job role.';
            return;
        }

        const dto: CreateUserRequest = {
            email: createForm.email.trim(),
            firstName: createForm.firstName.trim(),
            lastName: createForm.lastName.trim(),
            password: createForm.password,
            birthDate: new Date(createForm.birthDate),
            addressDto: {
                street: createForm.street.trim(),
                city: createForm.city.trim(),
                postalCode: Number(createForm.postalCode)
            }
        };

        creating = true;
        try {
            await createUser(dto);
            await loadEmployees();

            const createdUser = employees.find(
                (user) => user.email.toLowerCase() === createForm.email.trim().toLowerCase()
            );

            if (!createdUser) {
                notifySuccess('Employee created, but role could not be assigned automatically.');
                resetCreateForm();
                return;
            }

            await addUserToRole(Number(createForm.jobRoleId), createdUser.id);

            notifySuccess('Employee created and role assigned successfully.');
            resetCreateForm();
            await loadEmployees();
        } catch (error) {
            createError = getErrorMessage(error, 'Failed to create employee.');
        } finally {
            creating = false;
        }
    }

    async function handleSaveRole(user: UserWithId) {
        const selectedRoleId = selectedRoles[user.id];

        if (!selectedRoleId) {
            notifyError('Please select a job role first.');
            return;
        }

        const currentRole = getCurrentRole(user);
        const nextRoleId = Number(selectedRoleId);

        if (currentRole?.id === nextRoleId) {
            notifySuccess('This employee already has that role.');
            return;
        }

        savingRoleForUserId = user.id;
        try {
            if (currentRole) {
                await removeUserToRole(currentRole.id, user.id);
            }

            await addUserToRole(nextRoleId, user.id);

            notifySuccess('Job role updated successfully.');
            await loadEmployees();
        } catch (error) {
            notifyError(getErrorMessage(error, 'Failed to update job role.'));
        } finally {
            savingRoleForUserId = null;
        }
    }

    onMount(async () => {
        await Promise.all([loadEmployees(), loadJobRoles()]);
    });

    $effect(() => {
        if (!notification) return;

        const timeoutId = setTimeout(() => {
            clearNotification();
        }, 5000);

        return () => clearTimeout(timeoutId);
    });
</script>

<svelte:head>
    <title>Employees — Schichtpilot</title>
</svelte:head>

<div class="space-y-4">
    <div>
        <h1 class="text-xl font-semibold">Employees</h1>
        <p class="text-sm text-muted-foreground">
            Create employees and assign job roles for work schedule generation.
        </p>
    </div>

    {#if notification}
        <Alert.Root
                variant={notification.type === 'error' ? 'destructive' : undefined}
                class={notification.type === 'success'
                ? 'border-2 border-green-200 bg-green-50 text-green-900'
                : ''}
        >
            <Alert.Title class={notification.type === 'success' ? 'font-semibold text-green-800' : ''}>
                {notification.type === 'success' ? 'Success' : 'Error'}
            </Alert.Title>
            <Alert.Description class={notification.type === 'success' ? 'text-green-700' : ''}>
                {notification.message}
            </Alert.Description>
        </Alert.Root>
    {/if}

    <Card>
        <CardHeader>
            <CardTitle>Create employee</CardTitle>
            <CardDescription>
                Add a new employee and assign a job role immediately.
            </CardDescription>
        </CardHeader>

        <CardContent class="space-y-4">
            {#if createError}
                <Alert.Root variant="destructive">
                    <Alert.Title>Error</Alert.Title>
                    <Alert.Description>{createError}</Alert.Description>
                </Alert.Root>
            {/if}

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">First name</label>
                    <input class="border rounded-md px-3 py-2 text-sm" bind:value={createForm.firstName} />
                </div>

                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Last name</label>
                    <input class="border rounded-md px-3 py-2 text-sm" bind:value={createForm.lastName} />
                </div>

                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Email</label>
                    <input class="border rounded-md px-3 py-2 text-sm" type="email" bind:value={createForm.email} />
                </div>

                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Password</label>
                    <input class="border rounded-md px-3 py-2 text-sm" type="password" bind:value={createForm.password} />
                </div>

                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Birth date</label>
                    <input class="border rounded-md px-3 py-2 text-sm" type="date" bind:value={createForm.birthDate} />
                </div>

                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Street</label>
                    <input class="border rounded-md px-3 py-2 text-sm" bind:value={createForm.street} />
                </div>

                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">City</label>
                    <input class="border rounded-md px-3 py-2 text-sm" bind:value={createForm.city} />
                </div>

                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Postal code</label>
                    <input class="border rounded-md px-3 py-2 text-sm" type="number" bind:value={createForm.postalCode} />
                </div>

                <div class="flex flex-col gap-1 md:col-span-2">
                    <label class="text-sm font-medium">Job role</label>
                    <select class="border rounded-md px-3 py-2 text-sm bg-background" bind:value={createForm.jobRoleId}>
                        <option value="">Select a role</option>
                        {#each jobRoles as role}
                            <option value={role.id.toString()}>{role.name}</option>
                        {/each}
                    </select>
                </div>
            </div>

            <div class="flex justify-end">
                <Button onclick={handleCreateEmployee} disabled={creating}>
                    {creating ? 'Creating...' : 'Create employee'}
                </Button>
            </div>
        </CardContent>
    </Card>

    <Card>
        <CardHeader class="flex flex-row items-center justify-between gap-4">
            <div>
               <CardTitle>Assign job roles</CardTitle>
                <CardDescription>
                    Update roles for employees already stored in the database.
                </CardDescription>

            </div>
            <Button variant="outline" size="sm" onclick={loadEmployees} disabled={loading}>
                <RefreshCw class="h-4 w-4 {loading ? 'animate-spin' : ''}" />
                Refresh
            </Button>
        </CardHeader>
        <CardContent>
            {#if loading}
                <p class="text-sm text-muted-foreground">Loading employees...</p>
            {:else if employees.length === 0}
                <p class="text-sm text-muted-foreground">No employees found.</p>
            {:else}
                <Table.Root>
                    <Table.Header>
                        <Table.Row>
                            <Table.Head>Name</Table.Head>
                            <Table.Head>Email</Table.Head>
                            <Table.Head>Current role</Table.Head>
                            <Table.Head>New role</Table.Head>
                            <Table.Head>Action</Table.Head>
                        </Table.Row>
                    </Table.Header>

                    <Table.Body>
                        {#each employees as employee}
                            <Table.Row>
                              
                                <Table.Cell class="font-medium">
                                    {employee.firstName} {employee.lastName}
                                </Table.Cell>

                                <Table.Cell>{employee.email}</Table.Cell>

                                <Table.Cell class="text-muted-foreground">
                                    {formatCurrentRole(employee)}
                                </Table.Cell>

                                <Table.Cell>
                                    <select
                                            class="border rounded-md px-3 py-2 text-sm bg-background min-w-[180px]"
                                            bind:value={selectedRoles[employee.id]}
                                    >
                                        <option value="">Select a role</option>
                                        {#each jobRoles as role}
                                            <option value={role.id.toString()}>{role.name}</option>
                                        {/each}
                                    </select>
                                </Table.Cell>

                                <Table.Cell>
                                    <Button
                                            size="sm"
                                            onclick={() => handleSaveRole(employee)}
                                            disabled={savingRoleForUserId === employee.id}
                                    >
                                        {savingRoleForUserId === employee.id ? 'Saving...' : 'Save'}
                                    </Button>
                                </Table.Cell>
                            </Table.Row>
                        {/each}
                    </Table.Body>
                </Table.Root>
            {/if}
        </CardContent>
    </Card>
</div>