<script lang="ts">
    import * as Alert from '$lib/components/ui/alert/index.js';
    import { Button } from '$lib/components/ui/button/index.js';
    import * as Dialog from '$lib/components/ui/dialog/index.js';
    import { Input } from '$lib/components/ui/input/index.js';
    import { Label } from '$lib/components/ui/label/index.js';
    import { createJobRoleRequest, getJobRoles } from '$lib/services/jobRole.service';
    import type { JobRoleShortDto } from '$lib/types/jobRole.types';
    import { HttpError } from '$lib/customErrors';

    interface Props {
        open: boolean;
        onCreated?: () => void | Promise<void>;
    }

    let { open = $bindable(false), onCreated }: Props = $props();

    let addName = $state('');
    let addDescription = $state('');
    let selectedDependencies = $state<JobRoleShortDto[]>([]);
    let addErrorMessage = $state('');
    let addSubmitted = $state(false);
    let addTouched = $state({ name: false, description: false, dependencies: false });
    let isCreating = $state(false);

    let dependencySearch = $state('');
    let dependencyOptions = $state<JobRoleShortDto[]>([]);
    let isDependencyLoading = $state(false);

    function handleAddBlur(field: 'name' | 'description' | 'dependencies') {
        addTouched[field] = true;
    }

    let addNameError = $derived.by(() => {
        if (addName == '') {
            return 'Job role name is required'
        }

        if (addName.length < 2 || addName.length > 50) {
            return 'Name must be between 2 and 50 characters'
        }
        return '';
    });

    let addDescriptionError = $derived.by(() => {
                if (addDescription == '') {
            return 'Description is required'
        }

        if (addDescription.length < 2 || addDescription.length > 50) {
            return 'Description must be between 2 and 50 characters'
        }

        return '';
    });

    let addDependenciesError = $derived.by(() => {
        if (new Set(selectedDependencies.map(x => x.id)).size != selectedDependencies.length) {
            return 'Every dependency can only be added once!';
        }

        return '';
    });

    function isAddFormValid(): boolean {
        let isValid = true;

        if (addNameError != '') {
            isValid = false;
        }

        if (addDescriptionError != '') {
            isValid = false;
        }

        if (addDependenciesError != '') {
            isValid = false;
        }

        return isValid;
    }

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
            dependencyOptions = response.jobRoles;
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

    async function handleCreateJobRole(event: SubmitEvent) {
        event.preventDefault();
        addErrorMessage = '';
        addSubmitted = true;

        if (!isAddFormValid()) {
            return;
        }

        isCreating = true;
        try {
            await createJobRoleRequest({
                name: addName,
                description: addDescription,
                DependentOnJobRoleIds: selectedDependencies.map(role => role.id),
            });
            open = false;
            addName = '';
            addDescription = '';
            selectedDependencies = [];
            addSubmitted = false;
            addTouched = { name: false, description: false, dependencies: false };
            dependencySearch = '';
            dependencyOptions = [];
            await onCreated?.();
        } catch (error) {
            addErrorMessage = 'Failed to create job role. Please try again.';
            if (error instanceof (HttpError)) {
                addErrorMessage = error.Error.message;
            }
        } finally {
            isCreating = false;
        }
    }
</script>

<Dialog.Root bind:open>
    <Dialog.Content class="sm:max-w-lg">
        <Dialog.Header>
            <Dialog.Title>Add job role</Dialog.Title>
            <Dialog.Description>Create a new job role and its dependencies.</Dialog.Description>
        </Dialog.Header>

        {#if addErrorMessage}
            <Alert.Root variant="destructive">
                <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                    <circle cx="12" cy="12" r="10" />
                    <line x1="12" y1="8" x2="12" y2="12" />
                    <line x1="12" y1="16" x2="12.01" y2="16" />
                </svg>
                <Alert.Description>{addErrorMessage}</Alert.Description>
            </Alert.Root>
        {/if}

        <form class="space-y-4" onsubmit={handleCreateJobRole} novalidate>
            <div class="space-y-2">
                <Label for="jobRoleName">Name</Label>
                <Input
                    id="jobRoleName"
                    type="text"
                    placeholder="Front Desk"
                    required
                    onblur={() => handleAddBlur('name')}
                    disabled={isCreating}
                    bind:value={addName}
                />
                {#if addNameError != '' && (addTouched.name || addSubmitted)}
                    <span class="error">{addNameError}</span>
                {/if}
            </div>

            <div class="space-y-2">
                <Label for="jobRoleDescription">Description</Label>
                <Input
                    id="jobRoleDescription"
                    type="text"
                    placeholder="Short description"
                    onblur={() => handleAddBlur('description')}
                    disabled={isCreating}
                    bind:value={addDescription}
                />
                {#if addDescriptionError != '' && (addTouched.description || addSubmitted)}
                    <span class="error">{addDescriptionError}</span>
                {/if}
            </div>

            <div class="space-y-2">
                <Label for="jobRoleDependencies">Dependencies</Label>
                <Input
                    id="jobRoleDependencySearch"
                    type="text"
                    placeholder="Search job roles to add..."
                    oninput={handleDependencySearchInput}
                    disabled={isCreating}
                    value={dependencySearch}
                />
                <select
                    id="jobRoleDependencies"
                    multiple
                    class="min-h-[120px] w-full rounded-md border border-input bg-transparent px-2 py-2 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    onchange={handleDependencyChange}
                    onblur={() => handleAddBlur('dependencies')}
                    disabled={isCreating || isDependencyLoading}
                >
                    {#if dependencySearch.trim() === ''}
                        <option disabled>Use the field above to search for roles</option>
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
                {#if addDependenciesError != '' && (addTouched.dependencies || addSubmitted)}
                    <span class="error">{addDependenciesError}</span>
                {/if}
            </div>

            <Dialog.Footer>
                <Dialog.Close asChild>
                    <Button variant="outline" type="button" disabled={isCreating}>Cancel</Button>
                </Dialog.Close>
                <Button type="submit" disabled={isCreating} aria-busy={isCreating}>
                    Create
                </Button>
            </Dialog.Footer>
        </form>
    </Dialog.Content>
</Dialog.Root>
