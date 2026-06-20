<script lang="ts">
    import { onMount } from 'svelte';
    import { Button } from '$lib/components/ui/button';
    import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '$lib/components/ui/card';
    import * as Alert from '$lib/components/ui/alert';
    import * as Table from '$lib/components/ui/table/index.js';
    import { Checkbox } from '$lib/components/ui/checkbox';
    import { HttpError } from '$lib/customErrors';
    import RefreshCw from 'lucide-svelte/icons/refresh-cw';
    import ChevronDown from 'lucide-svelte/icons/chevron-down';
    import ChevronUp from 'lucide-svelte/icons/chevron-up';

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
        jobRoleIds: string[];
    };

    let employees = $state<UserWithId[]>([]);
    let jobRoles = $state<JobRoleShortDto[]>([]);
    let loading = $state(false);
    let creating = $state(false);
    let savingRoleForUserId = $state<number | null>(null);
    let expandedEmployeeId = $state<number | null>(null);

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
        jobRoleIds: []
    });

    let selectedRoles = $state<Record<number, string[]>>({});

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

    function getCurrentRoles(user: UserWithId): JobRoleShortDto[] {
        return user.assignedJobRoles ?? [];
    }

    function formatCurrentRoles(user: UserWithId): string {
        const roles = getCurrentRoles(user);
        if (roles.length === 0) return 'No roles assigned';
        return roles.map((role) => role.name).join(', ');
    }

    function formatBirthDate(user: UserWithId): string {
        const raw = (user as any).birthDate ?? (user as any).birthdate;
        if (!raw) return '—';

        const date = raw instanceof Date ? raw : new Date(raw);
        if (Number.isNaN(date.getTime())) return '—';

        return date.toLocaleDateString();
    }

    function formatAddress(user: UserWithId): string {
        const address = user.addressDto;
        if (!address) return '—';
        return `${address.street}, ${address.postalCode} ${address.city}`;
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
            jobRoleIds: []
        };
        createError = '';
    }

    function toggleExpandedEmployee(userId: number) {
        expandedEmployeeId = expandedEmployeeId === userId ? null : userId;
    }

    function isRoleSelected(userId: number, roleId: number): boolean {
        return selectedRoles[userId]?.includes(roleId.toString()) ?? false;
    }

    function toggleRoleSelection(userId: number, roleId: string, checked: boolean | 'indeterminate') {
        const current = selectedRoles[userId] ?? [];
        const isChecked = checked === true;

        if (isChecked) {
            if (!current.includes(roleId)) {
                selectedRoles = {
                    ...selectedRoles,
                    [userId]: [...current, roleId]
                };
            }
            return;
        }

        selectedRoles = {
            ...selectedRoles,
            [userId]: current.filter((id) => id !== roleId)
        };
    }

    function toggleCreateRoleSelection(roleId: string, checked: boolean | 'indeterminate') {
        const isChecked = checked === true;

        if (isChecked) {
            if (!createForm.jobRoleIds.includes(roleId)) {
                createForm = {
                    ...createForm,
                    jobRoleIds: [...createForm.jobRoleIds, roleId]
                };
            }
            return;
        }

        createForm = {
            ...createForm,
            jobRoleIds: createForm.jobRoleIds.filter((id) => id !== roleId)
        };
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

            employees = result.users as UserWithId[];

            const nextSelectedRoles: Record<number, string[]> = {};
            for (const employee of employees) {
                nextSelectedRoles[employee.id] = (employee.assignedJobRoles ?? []).map((role) =>
                    role.id.toString()
                );
            }
            selectedRoles = nextSelectedRoles;
        } catch (error) {
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

        if (createForm.jobRoleIds.length === 0) {
            createError = 'Please select at least one job role.';
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
                notifySuccess('Employee created, but roles could not be assigned automatically.');
                resetCreateForm();
                return;
            }

            for (const roleId of createForm.jobRoleIds) {
                await addUserToRole(Number(roleId), createdUser.id);
            }

            notifySuccess('Employee created and roles assigned successfully.');
            resetCreateForm();
            await loadEmployees();
        } catch (error) {
            createError = getErrorMessage(error, 'Failed to create employee.');
        } finally {
            creating = false;
        }
    }

    async function handleSaveRoles(user: UserWithId) {
        const selectedRoleIds = selectedRoles[user.id] ?? [];
        const currentRoleIds = (user.assignedJobRoles ?? []).map((role) => role.id.toString());

        const rolesToAdd = selectedRoleIds.filter((id) => !currentRoleIds.includes(id));
        const rolesToRemove = currentRoleIds.filter((id) => !selectedRoleIds.includes(id));

        if (rolesToAdd.length === 0 && rolesToRemove.length === 0) {
            notifySuccess('No role changes detected.');
            return;
        }

        savingRoleForUserId = user.id;
        try {
            for (const roleId of rolesToRemove) {
                await removeUserToRole(Number(roleId), user.id);
            }

            for (const roleId of rolesToAdd) {
                await addUserToRole(Number(roleId), user.id);
            }

            notifySuccess('Job roles updated successfully.');
            await loadEmployees();
        } catch (error) {
            notifyError(getErrorMessage(error, 'Failed to update job roles.'));
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
            Create employees, view details, and assign multiple job roles for work schedule generation.
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
                Add a new employee and assign one or more job roles immediately.
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

                <div class="flex flex-col gap-2 md:col-span-2">
                    <label class="text-sm font-medium">Job roles</label>
                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 rounded-md border p-3">
                        {#each jobRoles as role}
                            <label class="flex items-center gap-2 text-sm">
                                <Checkbox
                                        checked={createForm.jobRoleIds.includes(role.id.toString())}
                                        onCheckedChange={(checked) =>
                                        toggleCreateRoleSelection(role.id.toString(), checked)
                                    }
                                />
                                <span>{role.name}</span>
                            </label>
                        {/each}
                    </div>
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
                <CardTitle>Employee management</CardTitle>
                <CardDescription>
                    Click an employee to view more details and update multiple job roles.
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
                <div class="overflow-x-auto">
                    <Table.Root>
                        <Table.Header>
                            <Table.Row>
                                <Table.Head class="w-[60px]">Details</Table.Head>
                                <Table.Head>Name</Table.Head>
                                <Table.Head>Email</Table.Head>
                                <Table.Head>Current roles</Table.Head>
                                <Table.Head class="w-[140px]">Action</Table.Head>
                            </Table.Row>
                        </Table.Header>

                        <Table.Body>
                            {#each employees as employee}
                                <Table.Row class="align-top">
                                    <Table.Cell>
                                        <Button
                                                variant="ghost"
                                                size="sm"
                                                aria-expanded={expandedEmployeeId === employee.id}
                                                aria-label={expandedEmployeeId === employee.id
                                                ? `Hide details for ${employee.firstName} ${employee.lastName}`
                                                : `Show details for ${employee.firstName} ${employee.lastName}`}
                                                onclick={() => toggleExpandedEmployee(employee.id)}
                                        >
                                            {#if expandedEmployeeId === employee.id}
                                                <ChevronUp class="h-4 w-4" />
                                            {:else}
                                                <ChevronDown class="h-4 w-4" />
                                            {/if}
                                        </Button>
                                    </Table.Cell>

                                    <Table.Cell class="font-medium">
                                        <button
                                                type="button"
                                                class="text-left hover:underline"
                                                aria-expanded={expandedEmployeeId === employee.id}
                                                onclick={() => toggleExpandedEmployee(employee.id)}
                                        >
                                            {employee.firstName} {employee.lastName}
                                        </button>
                                    </Table.Cell>

                                    <Table.Cell>{employee.email}</Table.Cell>

                                    <Table.Cell class="text-muted-foreground">
                                        {formatCurrentRoles(employee)}
                                    </Table.Cell>

                                    <Table.Cell>
                                        <Button
                                                size="sm"
                                                onclick={() => handleSaveRoles(employee)}
                                                disabled={savingRoleForUserId === employee.id}
                                        >
                                            {savingRoleForUserId === employee.id ? 'Saving...' : 'Save roles'}
                                        </Button>
                                    </Table.Cell>
                                </Table.Row>

                                {#if expandedEmployeeId === employee.id}
                                    <Table.Row>
                                        <Table.Cell colspan={5} class="bg-muted/30">
                                            <div class="space-y-4 rounded-md border bg-background p-4">
                                                <div class="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
                                                    <div>
                                                        <p class="font-medium">Email</p>
                                                        <p class="text-muted-foreground">{employee.email}</p>
                                                    </div>

                                                    <div>
                                                        <p class="font-medium">Birth date</p>
                                                        <p class="text-muted-foreground">{formatBirthDate(employee)}</p>
                                                    </div>

                                                    <div class="md:col-span-2">
                                                        <p class="font-medium">Address</p>
                                                        <p class="text-muted-foreground">{formatAddress(employee)}</p>
                                                    </div>

                                                    <div class="md:col-span-2">
                                                        <p class="font-medium mb-2">Assigned roles</p>
                                                        {#if getCurrentRoles(employee).length === 0}
                                                            <p class="text-muted-foreground">No roles assigned.</p>
                                                        {:else}
                                                            <div class="flex flex-wrap gap-2">
                                                                {#each getCurrentRoles(employee) as role}
                                                                    <span class="rounded-full border px-2 py-1 text-xs">
                                                                        {role.name}
                                                                    </span>
                                                                {/each}
                                                            </div>
                                                        {/if}
                                                    </div>
                                                </div>

                                                <div class="space-y-2">
                                                    <p class="text-sm font-medium">Edit job roles</p>
                                                    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 rounded-md border p-3">
                                                        {#each jobRoles as role}
                                                            <label class="flex items-center gap-2 text-sm">
                                                                <Checkbox
                                                                        checked={isRoleSelected(employee.id, role.id)}
                                                                        onCheckedChange={(checked) =>
                                                                        toggleRoleSelection(
                                                                            employee.id,
                                                                            role.id.toString(),
                                                                            checked
                                                                        )}
                                                                />
                                                                <span>{role.name}</span>
                                                            </label>
                                                        {/each}
                                                    </div>
                                                </div>
                                            </div>
                                        </Table.Cell>
                                    </Table.Row>
                                {/if}
                            {/each}
                        </Table.Body>
                    </Table.Root>
                </div>
            {/if}
        </CardContent>
    </Card>
</div>