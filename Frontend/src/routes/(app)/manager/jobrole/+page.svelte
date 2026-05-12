<script lang="ts">
    import { onMount } from 'svelte';
    import * as Table from '$lib/components/ui/table/index.js';
    import * as Alert from '$lib/components/ui/alert/index.js';
    import { Button } from '$lib/components/ui/button/index.js';
    import { Input } from '$lib/components/ui/input/index.js';
    import { getJobRoles } from '$lib/services/jobRole.service';
    import type { JobRoleShortDto } from '$lib/types/jobRole.types';

    let jobRoles: JobRoleShortDto[] = [];
    let totalCount = 0;
    let page = 1;
    let pageSize = 10;
    let searchstring = '';
    let isLoading = false;
    let errorMessage = '';
    let initialized = false;

    const pageSizes = [5, 10, 20, 50];

    const totalPages = () => Math.max(1, Math.ceil(totalCount / pageSize));

    async function loadJobRoles() {
        isLoading = true;
        errorMessage = '';
        try {
            const response = await getJobRoles({ page, pageSize, searchstring });
            jobRoles = response.jobRoles;
            totalCount = response.count;
            const maxPage = totalPages();
            if (page > maxPage) {
                page = maxPage;
                await loadJobRoles();
            }
        } catch (error) {
            errorMessage = 'Failed to load job roles. Please try again.';
        } finally {
            isLoading = false;
        }
    }

    function applySearch() {
        page = 1;
        loadJobRoles();
    }

    function changePage(nextPage: number) {
        page = nextPage;
        loadJobRoles();
    }

    function changePageSize(size: number) {
        pageSize = size;
        page = 1;
        loadJobRoles();
    }

    onMount(async () => {
        initialized = true;
        await loadJobRoles();
    });

    $: if (initialized) {
        const maxPage = totalPages();
        if (page > maxPage) page = maxPage;
    }
</script>

<svelte:head><title>Job Roles — SchichtPilot</title></svelte:head>

<div class="space-y-4">
    <div class="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div class="space-y-1">
            <h1 class="text-xl font-semibold">Job Roles</h1>
            <p class="text-sm text-muted-foreground">View and assign job roles to users.</p>
        </div>
        <div class="flex flex-col gap-2 sm:flex-row sm:items-center">
            <div class="relative">
                <Input
                    placeholder="Search job roles..."
                    bind:value={searchstring}
                    on:keydown={(event) => event.key === 'Enter' && applySearch()}
                />
            </div>
            <Button on:click={applySearch} disabled={isLoading} aria-busy={isLoading}>
                Search
            </Button>
        </div>
    </div>

    {#if errorMessage}
        <Alert.Root variant="destructive">
            <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                <circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
            <Alert.Description>{errorMessage}</Alert.Description>
        </Alert.Root>
    {/if}

    <div class="rounded-lg border bg-card overflow-hidden">
        <Table.Root>
            <Table.Header>
                <Table.Row>
                    <Table.Head class="w-[100px]">ID</Table.Head>
                    <Table.Head>Name</Table.Head>
                    <Table.Head>Description</Table.Head>
                </Table.Row>
            </Table.Header>
            <Table.Body>
                {#if isLoading}
                    <Table.Row>
                        <Table.Cell colspan={3} class="py-10 text-center text-muted-foreground">
                            Loading job roles...
                        </Table.Cell>
                    </Table.Row>
                {:else}
                    {#each jobRoles as role (role.id)}
                        <Table.Row>
                            <Table.Cell class="font-medium">{role.id}</Table.Cell>
                            <Table.Cell class="font-medium">{role.name}</Table.Cell>
                            <Table.Cell class="text-sm text-muted-foreground">{role.description || '—'}</Table.Cell>
                        </Table.Row>
                    {/each}

                    {#if jobRoles.length === 0}
                        <Table.Row>
                            <Table.Cell colspan={3} class="py-10 text-center text-muted-foreground">
                                No job roles found.
                            </Table.Cell>
                        </Table.Row>
                    {/if}
                {/if}
            </Table.Body>
        </Table.Root>
    </div>

    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div class="text-sm text-muted-foreground">
            {#if totalCount === 0}
                Showing 0 job roles
            {:else}
                {@const start = (page - 1) * pageSize + 1}
                {@const end = Math.min(page * pageSize, totalCount)}
                Showing {start}–{end} of {totalCount}
            {/if}
        </div>

        <div class="flex flex-wrap items-center gap-2">
            <label class="text-sm text-muted-foreground">Rows per page</label>
            <select
                class="h-9 rounded-md border border-input bg-transparent px-2 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                bind:value={pageSize}
                on:change={(event) => changePageSize(Number((event.currentTarget as HTMLSelectElement).value))}
            >
                {#each pageSizes as size}
                    <option value={size}>{size}</option>
                {/each}
            </select>

            <Button
                variant="outline"
                on:click={() => changePage(page - 1)}
                disabled={page <= 1 || isLoading}
            >
                Previous
            </Button>
            <div class="text-sm text-muted-foreground">Page {page} of {totalPages()}</div>
            <Button
                variant="outline"
                on:click={() => changePage(page + 1)}
                disabled={page >= totalPages() || isLoading}
            >
                Next
            </Button>
        </div>
    </div>
</div>
