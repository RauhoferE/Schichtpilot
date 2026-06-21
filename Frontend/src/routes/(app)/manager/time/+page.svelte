<script lang="ts">
    import { onMount } from 'svelte';
    import { Button } from '$lib/components/ui/button';
    import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '$lib/components/ui/card';
    import * as Alert from '$lib/components/ui/alert';
    import * as Table from '$lib/components/ui/table/index.js';
    import {
        getSchedulesRequest,
        getSchedule,
        generateSchedule,
        regenerateSchedule,
        deleteSchedule,
        publishSchedule,
        setScheduleAsActive,
        setScheduleAsInactive
    } from '$lib/services/workschedule.service';
    import { getShifts } from '$lib/services/shift.service';
    import type { WorkScheduleShortDto, WorkScheduleDto } from '$lib/types/schedule.types';
    import type { ShortShiftDto } from '$lib/types/shift.types';
    import { HttpError } from '$lib/customErrors';

    type NotificationType = 'success' | 'error';

    type NotificationState = {
        type: NotificationType;
        message: string;
    } | null;

    let schedules: WorkScheduleShortDto[] = $state([]);
    let selectedSchedule = $state<WorkScheduleDto | null>(null);
    let availableShifts: ShortShiftDto[] = $state([]);
    let loading = $state(false);

    let notification = $state<NotificationState>(null);

    let currentYear = $state(new Date().getFullYear());
    let currentMonth = $state(new Date().getMonth());

    let showCreateDialog = $state(false);
    let newScheduleName = $state('');
    let newWeekStart = $state<Date | null>(null);
    let newWeekEnd = $state<Date | null>(null);
    let selectedShiftIds = $state<number[]>([]);
    let creating = $state(false);
    let createError = $state('');

    const monthNames = ['January','February','March','April','May','June',
        'July','August','September','October','November','December'];
    const dayNames = ['Su','Mo','Tu','We','Th','Fr','Sa'];

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

    function isSelectedDate(dateStr: string): boolean {
        if (!selectedSchedule) return false;
        const start = new Date(new Date(selectedSchedule.startDate).toDateString());
        const end = new Date(new Date(selectedSchedule.endDate).toDateString());
        const d = new Date(new Date(dateStr).toDateString());
        return d >= start && d <= end;
    }

    function getMonday(date: Date): Date {
        const result = new Date(date);
        const day = result.getDay();
        const diff = day === 0 ? -6 : 1 - day;
        result.setDate(result.getDate() + diff);
        result.setHours(0, 0, 0, 0);
        return result;
    }

    function getWeekRange(date: Date): { start: Date; end: Date } {
        const start = getMonday(date);
        const end = new Date(start);
        end.setDate(start.getDate() + 6);
        return { start, end };
    }

    function getIsoWeekNumber(date: Date): number {
        const target = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
        const dayNumber = target.getUTCDay() || 7;
        target.setUTCDate(target.getUTCDate() + 4 - dayNumber);
        const yearStart = new Date(Date.UTC(target.getUTCFullYear(), 0, 1));
        return Math.ceil((((target.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
    }

    function toInputDateString(date: Date): string {
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
    }

    function prevMonth() {
        if (currentMonth === 0) {
            currentMonth = 11;
            currentYear--;
        } else {
            currentMonth--;
        }
    }

    function nextMonth() {
        if (currentMonth === 11) {
            currentMonth = 0;
            currentYear++;
        } else {
            currentMonth++;
        }
    }

    function canActivate(schedule: WorkScheduleDto | null): boolean {
        return !!schedule && schedule.assignedUsers.length > 0;
    }

    async function onDayClick(day: number | null) {
        if (!day) return;

        clearNotification();

        const dateStr = toDateString(currentYear, currentMonth, day);
        const found = getScheduleForDate(dateStr);

        if (found) {
            loading = true;
            try {
                selectedSchedule = await getSchedule(found.id);
            } catch (error) {
                notifyError(getErrorMessage(error, 'Failed to load schedule details.'));
            } finally {
                loading = false;
            }
            return;
        }

        openCreateDialogForWeek(new Date(currentYear, currentMonth, day));
    }

    async function loadSchedules() {
        loading = true;
        try {
            const result = await getSchedulesRequest({
                page: 1,
                pageSize: 100,
                startDate: new Date(currentYear, currentMonth, 1),
                endDate: new Date(currentYear, currentMonth + 2, 0),
                searchstring: '',
                shiftIds: [],
                status: 'All'
            });
            schedules = result.workSchedules;
        } catch (error) {
            notifyError(getErrorMessage(error, 'Failed to load schedules.'));
        } finally {
            loading = false;
        }
    }

    async function loadShifts() {
        try {
            const result = await getShifts({
                page: 1,
                pageSize: 100,
                weekDays: [],
                shiftStatusEnum: 'All',
                searchstring: ''
            });
            availableShifts = result.shift;
        } catch (error) {
            notifyError(getErrorMessage(error, 'Failed to load shifts.'));
        }
    }

    function openCreateDialogForWeek(date: Date) {
        const { start, end } = getWeekRange(date);
        newWeekStart = start;
        newWeekEnd = end;
        newScheduleName = `KW ${getIsoWeekNumber(start)}`;
        selectedShiftIds = [];
        createError = '';
        showCreateDialog = true;
    }

    function handleCreateButtonClick() {
        openCreateDialogForWeek(new Date());
    }

    async function handleCreate() {
        createError = '';

        if (!newScheduleName.trim()) {
            createError = 'Please enter a name for the work schedule.';
            return;
        }

        if (!newWeekStart || !newWeekEnd) {
            createError = 'Please select a week in the calendar first.';
            return;
        }

        if (selectedShiftIds.length === 0) {
            createError = 'Please select at least one shift.';
            return;
        }

        creating = true;
        try {
            await generateSchedule({
                name: newScheduleName.trim(),
                startDate: new Date(`${toInputDateString(newWeekStart)}T12:00:00`),
                endDate: new Date(`${toInputDateString(newWeekEnd)}T12:00:00`),
                shiftIds: selectedShiftIds
            });

            notifySuccess('Work schedule created successfully.');
            showCreateDialog = false;
            newScheduleName = '';
            newWeekStart = null;
            newWeekEnd = null;
            selectedShiftIds = [];
            await loadSchedules();
        } catch (error) {
            createError = getErrorMessage(error, 'Failed to create work schedule.');
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

    function formatTime(date: Date | string): string {
        const d = new Date(date);
        return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
    }

    function formatDate(date: Date | string): string {
        const d = new Date(date);
        return `${String(d.getDate()).padStart(2, '0')}.${String(d.getMonth() + 1).padStart(2, '0')}.${String(d.getFullYear()).slice(2)}`;
    }

    async function activateSelectedSchedule() {
        if (!selectedSchedule) return;
        if (!canActivate(selectedSchedule)) {
            notifyError('This schedule cannot be activated because no staff is assigned.');
            return;
        }

        try {
            await setScheduleAsActive(selectedSchedule.id);
            selectedSchedule = await getSchedule(selectedSchedule.id);
            await loadSchedules();
            notifySuccess('Schedule activated.');
        } catch (error) {
            notifyError(getErrorMessage(error, 'Failed to activate schedule.'));
        }
    }

    async function deactivateSelectedSchedule() {
        if (!selectedSchedule) return;

        try {
            await setScheduleAsInactive(selectedSchedule.id);
            selectedSchedule = await getSchedule(selectedSchedule.id);
            await loadSchedules();
            notifySuccess('Schedule deactivated.');
        } catch (error) {
            notifyError(getErrorMessage(error, 'Failed to deactivate schedule.'));
        }
    }

    async function publishSelectedSchedule() {
        if (!selectedSchedule) return;

        const confirmed = window.confirm(`Publish "${selectedSchedule.name}"? Staff will be notified by email.`);
        if (!confirmed) return;

        try {
            await publishSchedule(selectedSchedule.id);
            selectedSchedule = await getSchedule(selectedSchedule.id);
            await loadSchedules();
            notifySuccess('Schedule published. Staff notified by email.');
        } catch (error) {
            notifyError(getErrorMessage(error, 'Failed to publish schedule.'));
        }
    }

    async function regenerateSelectedSchedule() {
        if (!selectedSchedule) return;

        const confirmed = window.confirm(
            `Regenerate "${selectedSchedule.name}"? This replaces all current assignments based on the latest shifts/timeslots, and deactivates the schedule if it is currently active.`
        );
        if (!confirmed) return;

        try {
            await regenerateSchedule(selectedSchedule.id);
            selectedSchedule = await getSchedule(selectedSchedule.id);
            await loadSchedules();
            notifySuccess('Schedule regenerated.');
        } catch (error) {
            notifyError(getErrorMessage(error, 'Failed to regenerate schedule.'));
        }
    }

    async function deleteSelectedSchedule() {
        if (!selectedSchedule) return;

        const scheduleId = selectedSchedule.id;
        const scheduleName = selectedSchedule.name;

        const confirmed = window.confirm(`Delete schedule "${scheduleName}"? This cannot be undone.`);
        if (!confirmed) return;

        try {
            await deleteSchedule(scheduleId);

            schedules = schedules.filter(s => s.id !== scheduleId);
            selectedSchedule = null;

            notifySuccess(`Schedule "${scheduleName}" deleted.`);
            await loadSchedules();
        } catch (error) {
            notifyError(getErrorMessage(error, 'Failed to delete schedule.'));
        }
    }

    onMount(async () => {
        await Promise.all([loadSchedules(), loadShifts()]);
    });

    $effect(() => {
        currentMonth;
        currentYear;
        loadSchedules();
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
    <title>Work Schedule — Schichtpilot</title>
</svelte:head>

<div class="space-y-4">
    <div class="flex items-center justify-between">
        <div>
            <h1 class="text-xl font-semibold">Work Schedule</h1>
            <p class="text-sm text-muted-foreground">View and manage work schedules.</p>
        </div>
        <Button onclick={handleCreateButtonClick}>
            Create new work schedule
        </Button>
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

    <div class="flex gap-6 items-start">
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
                                 'hover:bg-muted cursor-pointer text-muted-foreground'}"
                                    onclick={() => onDayClick(day)}
                            >
                                {day}
                            </button>
                        {/if}
                    {/each}
                </div>

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

        <div class="flex-1 space-y-4">
            {#if !selectedSchedule}
                <Card>
                    <CardContent class="py-10 text-center text-muted-foreground text-sm">
                        Select a week in the calendar to view the schedule, or click an empty day to create one.
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
                                    <Button
                                            size="sm"
                                            disabled={!canActivate(selectedSchedule)}
                                            onclick={activateSelectedSchedule}
                                    >
                                        Activate
                                    </Button>
                                {:else}
                                    <Button
                                            size="sm"
                                            variant="outline"
                                            onclick={deactivateSelectedSchedule}
                                    >
                                        Deactivate
                                    </Button>
                                {/if}

                                {#if selectedSchedule.isActive}
                                    <Button size="sm" onclick={publishSelectedSchedule}>
                                        Publish
                                    </Button>
                                {/if}

                                <Button
                                        size="sm"
                                        variant="outline"
                                        onclick={regenerateSelectedSchedule}
                                >
                                    Regenerate
                                </Button>

                                <Button
                                        size="sm"
                                        variant="destructive"
                                        onclick={deleteSelectedSchedule}
                                >
                                    Delete
                                </Button>
                            </div>
                        </div>
                    </CardHeader>

                    <CardContent>
                        {#if selectedSchedule.assignedUsers.length === 0}
                            <div class="space-y-2">
                                <p class="text-sm text-muted-foreground">No staff assigned to this schedule.</p>
                                <p class="text-sm text-amber-600">
                                    This schedule cannot be activated until staff has been assigned.
                                </p>
                            </div>
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

{#if showCreateDialog}
    <div class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
        <Card class="w-full max-w-md">
            <CardHeader>
                <CardTitle>Create new Work Schedule</CardTitle>
                <CardDescription>Pick a week in the calendar, then select the shifts to apply.</CardDescription>
            </CardHeader>

            <CardContent class="space-y-4">
                {#if createError}
                    <Alert.Root variant="destructive">
                        <Alert.Title>Error</Alert.Title>
                        <Alert.Description>{createError}</Alert.Description>
                    </Alert.Root>
                {/if}

                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Calendar week</label>
                    {#if newWeekStart && newWeekEnd}
                        <p class="text-sm">
                            KW {getIsoWeekNumber(newWeekStart)} · {formatDate(newWeekStart)} – {formatDate(newWeekEnd)}
                        </p>
                    {/if}
                </div>

                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Name</label>
                    <input
                            class="border rounded-md px-3 py-2 text-sm"
                            placeholder="e.g. KW 25"
                            bind:value={newScheduleName}
                    />
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
                <Button
                        variant="outline"
                        onclick={() => {
                        showCreateDialog = false;
                        createError = '';
                    }}
                        disabled={creating}
                >
                    Cancel
                </Button>

                <Button onclick={handleCreate} disabled={creating}>
                    {creating ? 'Creating...' : 'Create'}
                </Button>
            </div>
        </Card>
    </div>
{/if}