<script lang="ts">
    import * as Alert from '$lib/components/ui/alert/index.js';
    import { Button } from '$lib/components/ui/button/index.js';
    import * as Dialog from '$lib/components/ui/dialog/index.js';
    import { Input } from '$lib/components/ui/input/index.js';
    import { Label } from '$lib/components/ui/label/index.js';
    import {
        addJobRoleDependency,
        getJobRoleAsync,
        getJobRoles,
        removeJobRoleDependency,
        updateJobRoleAsync,
    } from '$lib/services/jobRole.service';
    import type { JobRoleDto, JobRoleShortDto } from '$lib/types/jobRole.types';
    import { HttpError } from '$lib/customErrors';

    interface Props {
        open: boolean;
        roleId: number | null;
        onSaved?: () => void | Promise<void>;
    }

    let { open = $bindable(false), roleId, onSaved }: Props = $props();

    let name = $state('');
    let description = $state('');
    let selectedDependencies = $state<JobRoleShortDto[]>([]);
    let initialDependencies = $state<number[]>([]);
    let prerequisites = $state<JobRoleShortDto[]>([]);
    let isLoading = $state(false);
    let errorMessage = $state('');

    let touched = $state({ name: false, description: false, dependencies: false });
    let submitted = $state(false);

    let dependencySearch = $state('');
    let dependencyOptions = $state<JobRoleShortDto[]>([]);
    let isDependencyLoading = $state(false);

    function handleBlur(field: 'name' | 'description' | 'dependencies') {
        touched[field] = true;
    }

    let nameError = $derived.by(() => {
        if (name == '') {
            return 'Job role name is required'
        }

        if (name.length < 2 || name.length > 50) {
            return 'Name must be between 2 and 50 characters'
        }
        return '';
    });

    let descriptionError = $derived.by(() => {
        if (description == '') {
            return 'Description is required'
        }

        if (description.length < 2 || description.length > 50) {
            return 'Description must be between 2 and 50 characters'
        }
        return '';
    });

    let dependenciesError = $derived.by(() => {
        if (new Set(selectedDependencies.map(x => x.id)).size != selectedDependencies.length) {
            return 'Every dependency can only be added once!';
        }
        return '';
    });

    function isFormValid(): boolean {
        let isValid = true;

        if (nameError != '') {
            isValid = false;
        }

        if (descriptionError != '') {
            isValid = false;
        }

        if (dependenciesError != '') {
            isValid = false;
        }

        return isValid;
    }

    async function loadRoleDetails(id: number) {
        isLoading = true;
        errorMessage = '';
        try {
            const role: JobRoleDto = await getJobRoleAsync(id);
            name = role.name;
            description = role.description;
            selectedDependencies = role.dependentOn.map(dep => ({
                id: dep.id,
                name: dep.name,
                description: dep.description,
            }));
            initialDependencies = role.dependentOn.map(dep => dep.id);
            prerequisites = role.prerequisites.map(dep => ({
                id: dep.id,
                name: dep.name,
                description: dep.description,
            }));
        } catch (error) {
            errorMessage = 'Failed to load job role.';
            if (error instanceof (HttpError)) {
                errorMessage = error.Error.message;
            }
        } finally {
            isLoading = false;
        }
    }

    $effect(() => {
        if (open && roleId != null) {
            loadRoleDetails(roleId);
        }

        if (!open) {
            dependencySearch = '';
            dependencyOptions = [];
            touched = { name: false, description: false, dependencies: false };
            submitted = false;
        }
    });

    async function loadDependencyOptions() {
        const query = dependencySearch.trim();
        if (query === '') {
            dependencyOptions = [];
            return;
        }

        isDependencyLoading = true;
        try {
            const response = await getJobRoles({
                page: 1,
                pageSize: 20,
                searchstring: query,
            });
            dependencyOptions = roleId == null
                ? response.jobRoles
                : response.jobRoles.filter(role => role.id !== roleId);
        } catch {
            dependencyOptions = [];
        } finally {
            isDependencyLoading = false;
        }
    }

    function handleDependencySearchInput(event: Event) {
        dependencySearch = (event.currentTarget as HTMLInputElement).value;
        loadDependencyOptions();
    }

    function handleDependencyChange(event: Event) {
        const select = event.currentTarget as HTMLSelectElement;
        const selectedIds = Array.from(select.selectedOptions).map(option => Number(option.value));
        const newlySelected = dependencyOptions.filter(role => selectedIds.includes(role.id));

        newlySelected.forEach(role => {
            if (!selectedDependencies.some(existing => existing.id === role.id)) {
                selectedDependencies = [...selectedDependencies, role];
            }
        });

        Array.from(select.options).forEach(option => {
            option.selected = false;
        });
    }

    function removeDependency(id: number) {
        selectedDependencies = selectedDependencies.filter(role => role.id !== id);
    }

    async function handleSave(event: SubmitEvent) {
        event.preventDefault();
        errorMessage = '';
        submitted = true;

        if (!isFormValid() || roleId == null) {
            return;
        }

        isLoading = true;
        try {
            await updateJobRoleAsync(roleId, { name, description });

            const nextIds = selectedDependencies.map(role => role.id);
            const toAdd = nextIds.filter(id => !initialDependencies.includes(id));
            const toRemove = initialDependencies.filter(id => !nextIds.includes(id));

            await Promise.all([
                ...toAdd.map(depId => addJobRoleDependency(roleId, depId)),
                ...toRemove.map(depId => removeJobRoleDependency(roleId, depId)),
            ]);

            open = false;
            await onSaved?.();
        } catch (error) {
            errorMessage = 'Failed to update job role.';
            if (error instanceof (HttpError)) {
                errorMessage = error.Error.message;
            }
        } finally {
            isLoading = false;
        }
    }
</script>

<Dialog.Root bind:open>
    <Dialog.Content class="sm:max-w-lg">
        <Dialog.Header>
            <Dialog.Title>Edit job role</Dialog.Title>
            <Dialog.Description>Update details and dependencies.</Dialog.Description>
        </Dialog.Header>

        {#if errorMessage}
            <Alert.Root variant="destructive">
                <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                    <circle cx="12" cy="12" r="10" />
                    <line x1="12" y1="8" x2="12" y2="12" />
                    <line x1="12" y1="16" x2="12.01" y2="16" />
                </svg>
                <Alert.Description>{errorMessage}</Alert.Description>
            </Alert.Root>
        {/if}

        <form class="space-y-4" onsubmit={handleSave} novalidate>
            <div class="space-y-2">
                <Label for="editJobRoleName">Name</Label>
                <Input
                    id="editJobRoleName"
                    type="text"
                    placeholder="Front Desk"
                    required
                    onblur={() => handleBlur('name')}
                    disabled={isLoading}
                    bind:value={name}
                />
                {#if nameError != '' && (touched.name || submitted)}
                    <span class="error">{nameError}</span>
                {/if}
            </div>

            <div class="space-y-2">
                <Label for="editJobRoleDescription">Description</Label>
                <Input
                    id="editJobRoleDescription"
                    type="text"
                    placeholder="Short description"
                    onblur={() => handleBlur('description')}
                    disabled={isLoading}
                    bind:value={description}
                />
                {#if descriptionError != '' && (touched.description || submitted)}
                    <span class="error">{descriptionError}</span>
                {/if}
            </div>

            <div class="space-y-2">
                <Label for="editJobRoleDependencies">Dependencies</Label>
                <Input
                    id="editJobRoleDependencySearch"
                    type="text"
                    placeholder="Search job roles to add..."
                    oninput={handleDependencySearchInput}
                    disabled={isLoading}
                    value={dependencySearch}
                />
                <select
                    id="editJobRoleDependencies"
                    multiple
                    class="min-h-[120px] w-full rounded-md border border-input bg-transparent px-2 py-2 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    onchange={handleDependencyChange}
                    onblur={() => handleBlur('dependencies')}
                    disabled={isLoading || isDependencyLoading}
                >
                    {#if dependencySearch.trim() === ''}
                        <option disabled>Type to search job roles</option>
                    {:else if isDependencyLoading}
                        <option disabled>Loading...</option>
                    {:else if dependencyOptions.length === 0}
                        <option disabled>No matches</option>
                    {:else}
                        {#each dependencyOptions as role}
                            <option value={role.id}>
                                {role.name}
                            </option>
                        {/each}
                    {/if}
                </select>

                {#if selectedDependencies.length > 0}
                    <div class="flex flex-wrap gap-2">
                        {#each selectedDependencies as role}
                            <span class="inline-flex items-center gap-2 rounded-full border px-3 py-1 text-xs">
                                {role.name}
                                <button
                                    type="button"
                                    class="text-muted-foreground hover:text-foreground"
                                    aria-label={`Remove ${role.name}`}
                                    onclick={() => removeDependency(role.id)}
                                >
                                    ✕
                                </button>
                            </span>
                        {/each}
                    </div>
                {/if}

                {#if dependenciesError != '' && (touched.dependencies || submitted)}
                    <span class="error">{dependenciesError}</span>
                {/if}
                <p class="text-xs text-muted-foreground">Hold Ctrl (Windows) or Cmd (Mac) to select multiple.</p>
            </div>

            <div class="space-y-2">
                <Label>Prerequisites</Label>
                {#if prerequisites.length === 0}
                    <p class="text-sm text-muted-foreground">No prerequisites.</p>
                {:else}
                    <div class="flex flex-wrap gap-2">
                        {#each prerequisites as role}
                            <span class="inline-flex items-center rounded-full border px-3 py-1 text-xs">
                                {role.name}
                            </span>
                        {/each}
                    </div>
                {/if}
            </div>

            <Dialog.Footer>
                <Dialog.Close asChild>
                    <Button variant="outline" type="button" disabled={isLoading}>Cancel</Button>
                </Dialog.Close>
                <Button type="submit" disabled={isLoading} aria-busy={isLoading}>
                    Save changes
                </Button>
            </Dialog.Footer>
        </form>
    </Dialog.Content>
</Dialog.Root>
