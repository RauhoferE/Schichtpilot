<script lang="ts">
    import { onMount } from 'svelte';
    import { Button } from '$lib/components/ui/button';
    import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '$lib/components/ui/card';
    import * as Alert from '$lib/components/ui/alert';
    import * as Table from '$lib/components/ui/table/index.js';
    import { getSchedulesRequest, getSchedule, generateSchedule, deleteSchedule, publishSchedule, setScheduleAsActive, setScheduleAsInactive } from '$lib/services/workschedule.service';
    import { getShifts } from '$lib/services/shift.service';
    import type { WorkScheduleShortDto, WorkScheduleDto } from '$lib/types/schedule.types';
    import type { ShortShiftDto } from '$lib/types/shift.types';
    
    //State
    let schedules: WorkScheduleShortDto[] = $state([]);
    let selectedSchedule = $state<WorkScheduleDto | null>(null);
    let availableShifts: ShortShiftDto[] = $state([]);
    let loading = $state(false);
    let errorMessage = $state('');
    let successMessage = $state('');

    // Calendar
    let currentYear = $state(new Date().getFullYear());
    let currentMonth = $state(new Date().getMonth()); // 0-indexed

    // Create dialog
    let showCreateDialog = $state(false);
    let newScheduleName = $state('');
    let newStartDate = $state('');
    let newEndDate = $state('');
    let selectedShiftIds = $state<number[]>([]);
    let creating = $state(false);

    const monthNames = ['January','February','March','April','May','June',
        'July','August','September','October','November','December'];
    const dayNames = ['Su','Mo','Tu','We','Th','Fr','Sa'];

    // Calendar helpers
    function getDaysInMonth(year: number, month: number): (number | null)[] {
        const firstDay = new Date(year, month, 1).getDay();
        const daysInMonth = new Date(year, month + 1, 0).getDate();
        const days: (number | null)[] = [];
        for (let i = 0; i < firstDay; i++) days.push(null);
        for (let d = 1; d <= daysInMonth; d++) days.push(d);
        return days;
    }

    function toDateString(year: number, month: number, day: number): string {
        return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    }
    
    function getScheduleForDate(dateStr: string): WorkScheduleShortDto | undefined {
        return schedules.find(s => {
            const start = new Date(new Date(s.startDate).toDateString());
            const end = new Date(new Date(s.endDate).toDateString());
            const d = new Date(new Date(dateStr).toDateString());
            return d >= start && d <= end;
        });
    }

    /*
    function isSelectedDate(dateStr: string): boolean {
        if (!selectedSchedule) return false;
        const start = new Date(selectedSchedule.startDate);
        const end = new Date(selectedSchedule.endDate);
        const d = new Date(dateStr);
        return d >= start && d <= end;
    }*/
    function isSelectedDate(dateStr: string): boolean {
        if (!selectedSchedule) return false;
        const start = new Date(new Date(selectedSchedule.startDate).toDateString());
        const end = new Date(new Date(selectedSchedule.endDate).toDateString());
        const d = new Date(new Date(dateStr).toDateString());
        return d >= start && d <= end;
    }

    function prevMonth() {
        if (currentMonth === 0) { currentMonth = 11; currentYear--; }
        else currentMonth--;
    }

    function nextMonth() {
        if (currentMonth === 11) { currentMonth = 0; currentYear++; }
        else currentMonth++;
    }

    async function onDayClick(day: number | null) {
        if (!day) return;
        const dateStr = toDateString(currentYear, currentMonth, day);
        const found = getScheduleForDate(dateStr);
        if (found) {
            loading = true;
            try {
                selectedSchedule = await getSchedule(found.id);
            } catch {
                errorMessage = 'Failed to load schedule details.';
            } finally {
                loading = false;
            }
        }
    }

    //Load Schedule
    async function loadSchedules() {
        loading = true;
        errorMessage = '';
        try {
            const result = await getSchedulesRequest({
                page: 1,
                pageSize: 100,
                startDate: new Date(currentYear, currentMonth, 1),
                endDate: new Date(currentYear, currentMonth + 2, 0),
                searchstring: '',
                shiftIds: [],
                status: 'All'  // leer → 'All'
            });
            schedules = result.workSchedules;
        } catch {
            errorMessage = 'Failed to load schedules.';
        } finally {
            loading = false;
        }
    }
    
    //Load Shifts
    async function loadShifts() {
        try {
            const result = await getShifts({
                page: 1,
                pageSize: 100,
                weekDays: [],
                shiftStatusEnum: 'All',  // leer → 'All'
                searchstring: ''
            });
            availableShifts = result.shift;
        } catch {
            errorMessage = 'Failed to load shifts.';
        }
    }

    // Create Schedule
    async function handleCreate() {
        if (!newScheduleName || !newStartDate || !newEndDate || selectedShiftIds.length === 0) {
            errorMessage = 'Please fill in all fields and select at least one shift.';
            return;
        }
        creating = true;
        errorMessage = '';
        try {
            await generateSchedule({
                name: newScheduleName,
                startDate: new Date(newStartDate + 'T12:00:00'),  // Mittag statt Mitternacht
                endDate: new Date(newEndDate + 'T12:00:00'),
                shiftIds: selectedShiftIds
            });
            
            successMessage = 'Work schedule created successfully.';
            showCreateDialog = false;
            newScheduleName = '';
            newStartDate = '';
            newEndDate = '';
            selectedShiftIds = [];
            await loadSchedules();
        } catch {
            errorMessage = 'Failed to create work schedule.';
        } finally {
            creating = false;
        }
    }

    function toggleShift(id: number) {
        if (selectedShiftIds.includes(id)) {
            selectedShiftIds = selectedShiftIds.filter(s => s !== id);
        } else {
            selectedShiftIds = [...selectedShiftIds, id];
        }
    }

    //Format helper
    function formatTime(date: Date | string): string {
        const d = new Date(date);
        return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
    }

    function formatDate(date: Date | string): string {
        const d = new Date(date);
        return `${String(d.getDate()).padStart(2, '0')}.${String(d.getMonth() + 1).padStart(2, '0')}.${String(d.getFullYear()).slice(2)}`;
    }

    onMount(async () => {
        await Promise.all([loadSchedules(), loadShifts()]);
    });

    $effect(() => {
        currentMonth;
        currentYear;
        loadSchedules();
    });
</script>

<svelte:head>
    <title>Time Management — Schichtpilot</title>
</svelte:head>

<div class="space-y-4">

    <!-- Header -->
    <div class="flex items-center justify-between">
        <div>
            <h1 class="text-xl font-semibold">Time Management</h1>
            <p class="text-sm text-muted-foreground">View and manage work schedules.</p>
        </div>
        <Button onclick={() => showCreateDialog = true}>
            Create new work schedule
        </Button>
    </div>

    {#if errorMessage}
        <Alert.Root variant="destructive">
            <Alert.Title>Error</Alert.Title>
            <Alert.Description>{errorMessage}</Alert.Description>
        </Alert.Root>
    {/if}

    {#if successMessage}
        <Alert.Root class="border-2 border-green-200 bg-green-50 text-green-900">
            <Alert.Title class="font-semibold text-green-800">Success</Alert.Title>
            <Alert.Description class="text-green-700">{successMessage}</Alert.Description>
        </Alert.Root>
    {/if}

    <div class="flex gap-6 items-start">

        <!-- KALENDER -->
        <Card class="w-72 shrink-0">
            <CardHeader class="pb-2">
                <div class="flex items-center justify-between">
                    <Button variant="ghost" size="sm" onclick={prevMonth}>‹</Button>
                    <span class="font-semibold text-sm">{monthNames[currentMonth]} {currentYear}</span>
                    <Button variant="ghost" size="sm" onclick={nextMonth}>›</Button>
                </div>
            </CardHeader>
            <CardContent>
                <div class="grid grid-cols-7 gap-1 text-center text-xs text-muted-foreground mb-1">
                    {#each dayNames as d}
                        <span>{d}</span>
                    {/each}
                </div>
                <div class="grid grid-cols-7 gap-1 text-center text-xs">
                    {#each getDaysInMonth(currentYear, currentMonth) as day}
                        {#if day === null}
                            <span></span>
                        {:else}
                            {@const dateStr = toDateString(currentYear, currentMonth, day)}
                            {@const hasSchedule = !!getScheduleForDate(dateStr)}
                            {@const isSelected = isSelectedDate(dateStr)}
                            <button
                                    class="rounded-md py-1 w-full transition-colors
                                    {isSelected ? 'bg-amber-400 text-white font-bold' :
                                     hasSchedule ? 'bg-amber-100 text-amber-800 cursor-pointer hover:bg-amber-200' :
                                     'hover:bg-muted cursor-default text-muted-foreground'}"
                                    onclick={() => onDayClick(day)}
                            >
                                {day}
                            </button>
                        {/if}
                    {/each}
                </div>

                <!-- Legende -->
                <div class="mt-3 flex flex-col gap-1 text-xs text-muted-foreground">
                    <div class="flex items-center gap-2">
                        <span class="w-3 h-3 rounded bg-amber-400 inline-block"></span>
                        <span>Selected schedule</span>
                    </div>
                    <div class="flex items-center gap-2">
                        <span class="w-3 h-3 rounded bg-amber-100 inline-block"></span>
                        <span>Has schedule</span>
                    </div>
                </div>
            </CardContent>
        </Card>

        <!-- RECHTE SEITE -->
        <div class="flex-1 space-y-4">
            {#if !selectedSchedule}
                <Card>
                    <CardContent class="py-10 text-center text-muted-foreground text-sm">
                        Select a week in the calendar to view the schedule.
                    </CardContent>
                </Card>
            {:else}
                <Card>
                    <CardHeader>
                        <div class="flex items-center justify-between">
                            <div>
                                <CardTitle class="text-amber-400">{selectedSchedule.name}</CardTitle>
                                <CardDescription>
                                    {formatDate(selectedSchedule.startDate)} – {formatDate(selectedSchedule.endDate)}
                                    · {selectedSchedule.isActive ? '✅ Active' : '⏸ Inactive'}
                                </CardDescription>
                            </div>
                            <div class="flex gap-2">
                                {#if !selectedSchedule.isActive}
                                    <Button size="sm" onclick={async () => {
                                try {
                                    await setScheduleAsActive(selectedSchedule!.id);
                                    selectedSchedule = await getSchedule(selectedSchedule!.id);
                                    await loadSchedules();
                                    successMessage = 'Schedule activated.';
                                } catch {
                                    errorMessage = 'Failed to activate schedule.';
                                }
                            }}>
                                        Activate
                                    </Button>
                                {:else}
                                    <Button size="sm" variant="outline" onclick={async () => {
                                try {
                                    await setScheduleAsInactive(selectedSchedule!.id);
                                    selectedSchedule = await getSchedule(selectedSchedule!.id);
                                    await loadSchedules();
                                    successMessage = 'Schedule deactivated.';
                                } catch {
                                    errorMessage = 'Failed to deactivate schedule.';
                                }
                            }}>
                                        Deactivate
                                    </Button>
                                {/if}
                                <Button size="sm" variant="destructive" onclick={async () => {
                            const confirmed = window.confirm(`Delete schedule "${selectedSchedule!.name}"? This cannot be undone.`);
                            if (!confirmed) return;
                            try {
                                await deleteSchedule(selectedSchedule!.id);
                                selectedSchedule = null;
                                await loadSchedules();
                                successMessage = 'Schedule deleted.';
                            } catch {
                                errorMessage = 'Failed to delete schedule.';
                            }
                        }}>
                                    Delete
                                </Button>
                            </div>
                        </div>
                    </CardHeader>
                    <CardContent>
                        {#if selectedSchedule.assignedUsers.length === 0}
                            <p class="text-sm text-muted-foreground">No staff assigned to this schedule.</p>
                        {:else}
                            <Table.Root>
                                <Table.Header>
                                    <Table.Row>
                                        <Table.Head>Staff</Table.Head>
                                        <Table.Head>Shift</Table.Head>
                                        <Table.Head>Role</Table.Head>
                                        <Table.Head>Working Hours</Table.Head>
                                    </Table.Row>
                                </Table.Header>
                                <Table.Body>
                                    {#each selectedSchedule.assignedUsers as assignment}
                                        <Table.Row>
                                            <Table.Cell class="font-medium">
                                                {assignment.user.firstName} {assignment.user.lastName}
                                            </Table.Cell>
                                            <Table.Cell>
                                                {assignment.timeSlot?.dayOfWeek ?? '—'}
                                            </Table.Cell>
                                            <Table.Cell class="text-muted-foreground">
                                                {assignment.jobRole?.name ?? '—'}
                                            </Table.Cell>
                                            <Table.Cell>
                                                {formatTime(assignment.startTime)} – {formatTime(assignment.endTime)}
                                            </Table.Cell>
                                        </Table.Row>
                                    {/each}
                                </Table.Body>
                            </Table.Root>
                        {/if}
                    </CardContent>
                </Card>
            {/if}
        </div>
    </div>
</div>

<!-- CREATE DIALOG -->
{#if showCreateDialog}
    <div class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
        <Card class="w-full max-w-md">
            <CardHeader>
                <CardTitle>Create new Work Schedule</CardTitle>
                <CardDescription>Enter the details for the new work schedule.</CardDescription>
            </CardHeader>
            <CardContent class="space-y-4">
                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Name</label>
                    <input
                            class="border rounded-md px-3 py-2 text-sm"
                            placeholder="e.g. Week 23"
                            bind:value={newScheduleName}
                    />
                </div>
                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Start Date</label>
                    <input type="date" class="border rounded-md px-3 py-2 text-sm" bind:value={newStartDate} />
                </div>
                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">End Date</label>
                    <input type="date" class="border rounded-md px-3 py-2 text-sm" bind:value={newEndDate} />
                </div>
                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Shifts</label>
                    {#if availableShifts.length === 0}
                        <p class="text-sm text-muted-foreground">No shifts available.</p>
                    {:else}
                        <div class="flex flex-col gap-2">
                            {#each availableShifts as shift}
                                <label class="flex items-center gap-2 text-sm cursor-pointer">
                                    <input
                                            type="checkbox"
                                            checked={selectedShiftIds.includes(shift.id)}
                                            onchange={() => toggleShift(shift.id)}
                                    />
                                    <span
                                            class="w-3 h-3 rounded-full inline-block"
                                            style="background-color: #{shift.colorAsHex}"
                                    ></span>
                                    {shift.name}
                                </label>
                            {/each}
                        </div>
                    {/if}
                </div>
            </CardContent>
            <div class="flex justify-end gap-3 p-6 pt-0">
                <Button variant="outline" onclick={() => showCreateDialog = false} disabled={creating}>
                    Cancel
                </Button>
                <Button onclick={handleCreate} disabled={creating}>
                    {creating ? 'Creating...' : 'Create'}
                </Button>
            </div>
        </Card>
    </div>
{/if}