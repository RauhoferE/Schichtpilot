<script lang="ts">
    import * as Alert from '$lib/components/ui/alert/index.js';
    import { Button } from '$lib/components/ui/button/index.js';
    import { Input } from '$lib/components/ui/input/index.js';
    import { Label } from '$lib/components/ui/label/index.js';
    import * as Table from '$lib/components/ui/table/index.js';
    import { getShifts } from '$lib/services/shift.service';
    import { goto } from '$app/navigation';
    import type { QueryableShiftResponse, ShortShiftDto } from '$lib/types/shift.types';
    import CreateShiftDialog from '$lib/components/shifts/CreateShiftDialog/CreateShiftDialog.svelte';

    type ShiftStatus = 'All' | 'InSchedule' | 'NotInSchedule';

    const shiftStatusOptions: { value: ShiftStatus; label: string }[] = [
        { value: 'All', label: 'All' },
        { value: 'InSchedule', label: 'In schedule' },
        { value: 'NotInSchedule', label: 'Not in schedule' }
    ];

    const weekDayOptions = [
        { value: 'Sunday', label: 'Sunday' },
        { value: 'Monday', label: 'Monday' },
        { value: 'Tuesday', label: 'Tuesday' },
        { value: 'Wednesday', label: 'Wednesday' },
        { value: 'Thursday', label: 'Thursday' },
        { value: 'Friday', label: 'Friday' },
        { value: 'Saturday', label: 'Saturday' }
    ];

    let page = $state(1);
    let pageSize = $state(10);
    let selectedWeekDays = $state<string[]>([]);
    let shiftStatusEnum = $state<ShiftStatus>('All');
    let searchstring = $state('');

    let response = $state<QueryableShiftResponse>({ count: 0, shift: [] });
    let isLoading = $state(false);
    let errorMessage = $state('');
    let lastFilterKey = $state('');
    let isCreateOpen = $state(false);

    const totalPages = $derived.by(() => Math.max(1, Math.ceil(response.count / pageSize)));
    const canGoBack = $derived.by(() => page > 1);
    const canGoForward = $derived.by(() => page < totalPages);
    const startIndex = $derived.by(() => (response.count === 0 ? 0 : (page - 1) * pageSize + 1));
    const endIndex = $derived.by(() => Math.min(page * pageSize, response.count));
    const allWeekDaysSelected = $derived.by(() => selectedWeekDays.length === weekDayOptions.length);

    function toggleWeekDay(day: string) {
        if (selectedWeekDays.includes(day)) {
            selectedWeekDays = selectedWeekDays.filter((value) => value !== day);
            return;
        }

        selectedWeekDays = [...selectedWeekDays, day];
    }

    function toggleAllWeekDays() {
        selectedWeekDays = allWeekDaysSelected ? [] : weekDayOptions.map((day) => day.value);
    }

    function handleSearchInput(event: Event) {
        searchstring = (event.currentTarget as HTMLInputElement).value;
    }

    function handleShiftStatusChange(event: Event) {
        shiftStatusEnum = (event.currentTarget as HTMLSelectElement).value as ShiftStatus;
    }

    function handlePageSizeChange(event: Event) {
        pageSize = Number((event.currentTarget as HTMLSelectElement).value);
    }

    function goToPreviousPage() {
        if (page > 1) {
            page = page - 1;
        }
    }

    function goToNextPage() {
        if (page < totalPages) {
            page = page + 1;
        }
    }

    $effect(() => {
        const filterKey = `${pageSize}|${shiftStatusEnum}|${selectedWeekDays.join(',')}|${searchstring.trim()}`;
        if (filterKey !== lastFilterKey) {
            lastFilterKey = filterKey;
            page = 1;
        }
    });

    $effect(() => {
        void loadShifts();
    });

    async function loadShifts() {
        errorMessage = '';
        isLoading = true;

        try {
            const data = await getShifts({
                page,
                pageSize,
                weekDays: selectedWeekDays,
                shiftStatusEnum: 'All',
                searchstring: searchstring.trim()
            });

            response = data;

            if (page > totalPages) {
                page = totalPages;
            }
        } catch {

            response = { count: 0, shift: [] };
            errorMessage = 'Failed to load shifts.';
        }

        isLoading = false;
    }

    function formatShiftId(shift: ShortShiftDto) {
        return `#${shift.id}`;
    }

    async function handleCreated() {
        await loadShifts();
    }
</script>
<svelte:head>
    <title>Shift Management - Schichtpilot</title>
</svelte:head>

<div class="space-y-6">
    <div class="flex flex-col gap-4 sm:flex-row sm:flex-wrap sm:items-start sm:justify-between sm:gap-6">
        <div class="flex flex-col gap-4 sm:flex-row sm:flex-wrap sm:gap-6">
            <div class="w-full space-y-2 sm:min-w-[220px] sm:w-auto">
                <Label for="shift-search">Search</Label>
                <Input
                        id="shift-search"
                        type="text"
                        placeholder="Search by name"
                        bind:value={searchstring}
                        oninput={handleSearchInput}
                />
            </div>

            <div class="w-full space-y-2 sm:min-w-[200px] sm:w-auto">
                <Label for="shift-status">Shift status</Label>
                <select
                        id="shift-status"
                        class="h-9 w-full rounded-md border border-input bg-background px-3 text-sm shadow-xs focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-3"
                        bind:value={shiftStatusEnum}
                        onchange={handleShiftStatusChange}
                >
                    {#each shiftStatusOptions as option(option.value)}
                        <option value={option.value}>{option.label}</option>
                    {/each}
                </select>
            </div>

            <div class="w-full space-y-2 sm:min-w-[220px] sm:w-auto">
                <Label for="page-size">Page size</Label>
                <select
                        id="page-size"
                        class="h-9 w-full rounded-md border border-input bg-background px-3 text-sm shadow-xs focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-3"
                        bind:value={pageSize}
                        onchange={handlePageSizeChange}
                >
                    <option value={5}>5</option>
                    <option value={10}>10</option>
                    <option value={20}>20</option>
                    <option value={50}>50</option>
                </select>
            </div>

            <div class="w-full space-y-2 sm:min-w-[260px] sm:w-auto">
                <div class="flex items-center justify-between gap-2">
                    <Label>Weekdays</Label>
                    <button
                            type="button"
                            class="text-xs font-medium text-primary hover:underline"
                            onclick={toggleAllWeekDays}
                    >
                        {allWeekDaysSelected ? 'Deselect all' : 'Select all'}
                    </button>
                </div>
                <div class="flex flex-wrap gap-3">
                    {#each weekDayOptions as day(day.value)}
                        <label class="flex items-center gap-2 text-sm text-muted-foreground">
                            <input
                                    type="checkbox"
                                    class="h-4 w-4 rounded border-input text-primary"
                                    checked={selectedWeekDays.includes(day.value)}
                                    onchange={() => toggleWeekDay(day.value)}
                            />
                            <span class="text-foreground">{day.label}</span>
                        </label>
                    {/each}
                </div>
            </div>
        </div>
        <div class="flex w-full justify-end sm:w-auto">
            <Button onclick={() => (isCreateOpen = true)}>Create shift</Button>
        </div>
    </div>

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

    <div class="overflow-x-auto rounded-lg border border-border">
        <Table.Root>
            <Table.Header>
                <Table.Row>
                    <Table.Head class="w-[140px]">Shift ID</Table.Head>
                    <Table.Head>Name</Table.Head>
                    <Table.Head class="w-[160px]">Color</Table.Head>
                </Table.Row>
            </Table.Header>
            <Table.Body>
                {#if isLoading}
                    <Table.Row>
                        <Table.Cell colspan={3} class="py-8 text-center text-muted-foreground">
                            Loading shifts...
                        </Table.Cell>
                    </Table.Row>
                {:else if response.shift.length === 0}
                    <Table.Row>
                        <Table.Cell colspan={3} class="py-8 text-center text-muted-foreground">
                            No shifts match your filters.
                        </Table.Cell>
                    </Table.Row>
                {:else}
                    {#each response.shift as shift (shift.id)}
                        <Table.Row
                                class="cursor-pointer"
                                onclick={() => goto(`/manager/shift/${shift.id}`)}
                        >
                            <Table.Cell>{formatShiftId(shift)}</Table.Cell>
                            <Table.Cell class="font-medium">{shift.name}</Table.Cell>
                            <Table.Cell>
                                <div class="flex items-center gap-2">
                                    <span
                                            class="h-4 w-4 rounded-full border border-border"
                                            style={`background-color: ${shift.colorAsHex}`}
                                    ></span>
                                    <span class="text-sm text-muted-foreground">{shift.colorAsHex}</span>
                                </div>
                            </Table.Cell>
                        </Table.Row>
                    {/each}
                {/if}
            </Table.Body>
        </Table.Root>
    </div>

    <div class="flex flex-wrap items-center justify-between gap-4">
        <div class="text-sm text-muted-foreground">
            Showing {startIndex} - {endIndex} of {response.count}
        </div>

        <div class="flex items-center gap-2">
            <Button variant="outline" size="sm" onclick={goToPreviousPage} disabled={!canGoBack}>
                Previous
            </Button>
            <span class="text-sm text-muted-foreground">Page {page} of {totalPages}</span>
            <Button variant="outline" size="sm" onclick={goToNextPage} disabled={!canGoForward}>
                Next
            </Button>
        </div>
    </div>

    <CreateShiftDialog bind:open={isCreateOpen} onCreated={handleCreated} />
</div>