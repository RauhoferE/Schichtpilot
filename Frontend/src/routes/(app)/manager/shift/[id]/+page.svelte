<script lang="ts">
    import * as Alert from '$lib/components/ui/alert/index.js';
    import { Button } from '$lib/components/ui/button/index.js';
    import { Input } from '$lib/components/ui/input/index.js';
    import { Label } from '$lib/components/ui/label/index.js';
    import { getShift, updateShift } from '$lib/services/shift.service';
    import type { ShiftDto, TimeSlotDto } from '$lib/types/shift.types';
    import { page } from '$app/stores';
    import { adminGuard } from '../../../../common-guards.ts/manager.guard';
    import { goto } from '$app/navigation';

    const weekDayOptions = [
        { value: 1, label: 'Monday' },
        { value: 2, label: 'Tuesday' },
        { value: 3, label: 'Wednesday' },
        { value: 4, label: 'Thursday' },
        { value: 5, label: 'Friday' },
        { value: 6, label: 'Saturday' },
        { value: 0, label: 'Sunday' }
    ];

    let shift = $state<ShiftDto | null>(null);
    let isLoading = $state(false);
    let loadError = $state('');
    let saveError = $state('');
    let saveSuccess = $state('');
    let isSaving = $state(false);

    let name = $state('');
    let description = $state('');
    let colorAsHex = $state('#000000');

    const shiftId = $derived.by(() => Number($page.params.id));

    $effect(() => {
        adminGuard($page.url);
    });

    $effect(() => {
        void loadShift();
    });

    let nameError = $derived.by(() => {
        if (name.trim() === '') {
            return 'Shift name is required';
        }
        return '';
    });

    async function loadShift() {
        const id = shiftId;
        if (!Number.isFinite(id)) {
            loadError = 'Invalid shift id.';
            shift = null;
            return;
        }

        loadError = '';
        isLoading = true;
        saveSuccess = '';

        try {
            const data = await getShift(id);
            shift = data;
            name = data.name;
            description = data.description;
            colorAsHex = data.colorAsHex;
        } catch {
            shift = null;
            loadError = 'Failed to load shift.';
        } finally {
            isLoading = false;
        }
    }

    function getSlotsForDay(day: number): TimeSlotDto[] {
        if (!shift) {
            return [];
        }

        return shift.timeSlots
            .filter((slot) => slot.dayOfWeek === day)
            .sort((a, b) => a.startTime.localeCompare(b.startTime));
    }

    function timeToMinutes(value: string): number {
        const [hours, minutes] = value.split(':').map(Number);
        return (hours || 0) * 60 + (minutes || 0);
    }

    function minutesToPercent(minutes: number): number {
        return (minutes / (24 * 60)) * 100;
    }

    function getSlotSegments(slot: TimeSlotDto): { start: number; end: number }[] {
        const start = timeToMinutes(slot.startTime);
        const end = timeToMinutes(slot.endTime);
        const breaks = [...slot.breaks]
            .map((breakItem) => ({
                start: timeToMinutes(breakItem.startTime),
                end: timeToMinutes(breakItem.endTime)
            }))
            .filter((breakItem) => breakItem.start < breakItem.end)
            .sort((a, b) => a.start - b.start)
            .filter((breakItem) => breakItem.end > start && breakItem.start < end);

        const segments: { start: number; end: number }[] = [];
        let cursor = start;

        for (const breakItem of breaks) {
            const clampedStart = Math.max(breakItem.start, start);
            const clampedEnd = Math.min(breakItem.end, end);

            if (clampedStart > cursor) {
                segments.push({ start: cursor, end: clampedStart });
            }

            cursor = Math.max(cursor, clampedEnd);
        }

        if (cursor < end) {
            segments.push({ start: cursor, end });
        }

        return segments;
    }

    const hours = Array.from({ length: 25 }, (_, index) => index);

    async function handleSave(event: SubmitEvent) {
        event.preventDefault();
        if (!shift) {
            return;
        }

        if (nameError !== '') {
            return;
        }

        saveError = '';
        saveSuccess = '';
        isSaving = true;

        try {
            await updateShift(shiftId, {
                name: name?.trim(),
                description: description?.trim(),
                colorAsHex  
            });
            
            shift = {
                ...shift,
                name: name?.trim(),
                description: description?.trim(),
                colorAsHex
            };

            saveSuccess = 'Shift updated.';
        } catch {
            saveError = 'Failed to update shift.';
        } finally {
            isSaving = false;
        }
    }

    function goBack() {
        goto('/manager/shifts');
    }
</script>

<div class="space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
            <h1 class="text-2xl font-semibold">Shift details</h1>
            <p class="text-sm text-muted-foreground">Manage shift info, timeslots, and requirements.</p>
        </div>
        <Button variant="outline" onclick={goBack}>Back to shifts</Button>
    </div>

    {#if loadError}
        <Alert.Root variant="destructive">
            <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
            <Alert.Description>{loadError}</Alert.Description>
        </Alert.Root>
    {/if}

    {#if isLoading}
        <div class="rounded-lg border border-border p-6 text-center text-muted-foreground">
            Loading shift...
        </div>
    {:else if shift}
        <form class="space-y-6" onsubmit={handleSave}>
            <div class="grid gap-4 lg:grid-cols-3">
                <div class="space-y-2">
                    <Label for="shiftName">Name</Label>
                    <Input id="shiftName" type="text" bind:value={name} disabled={isSaving} />
                    {#if nameError !== ''}
                        <span class="error">{nameError}</span>
                    {/if}
                </div>
                <div class="space-y-2 lg:col-span-2">
                    <Label for="shiftDescription">Description</Label>
                    <Input id="shiftDescription" type="text" bind:value={description} disabled={isSaving} />
                </div>
                <div class="space-y-2">
                    <Label for="shiftColor">Color</Label>
                    <div class="flex items-center gap-2">
                        <Input
                            id="shiftColor"
                            type="color"
                            class="h-9 w-16 p-1"
                            bind:value={colorAsHex}
                            disabled={isSaving}
                        />
                        <Input type="text" readonly value={colorAsHex} />
                    </div>
                </div>
            </div>

            {#if saveError}
                <Alert.Root variant="destructive">
                    <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                        <circle cx="12" cy="12" r="10" />
                        <line x1="12" y1="8" x2="12" y2="12" />
                        <line x1="12" y1="16" x2="12.01" y2="16" />
                    </svg>
                    <Alert.Description>{saveError}</Alert.Description>
                </Alert.Root>
            {/if}

            {#if saveSuccess}
                <Alert.Root>
                    <Alert.Description>{saveSuccess}</Alert.Description>
                </Alert.Root>
            {/if}

            <div class="flex justify-end">
                <Button type="submit" disabled={isSaving}>Save changes</Button>
            </div>
        </form>

        <div class="grid gap-4 lg:grid-cols-3">
            <div class="lg:col-span-2 space-y-3">
                <h2 class="text-lg font-semibold">Weekly timeslots</h2>
                <div class="rounded-lg border border-border p-4">
                    <div class="grid grid-cols-[60px_repeat(7,minmax(0,1fr))] gap-2 text-xs text-muted-foreground">
                        <div></div>
                        {#each weekDayOptions as day (day.value)}
                            <div class="text-center font-medium text-foreground">{day.label}</div>
                        {/each}
                    </div>

                    <div class="mt-3 grid grid-cols-[60px_repeat(7,minmax(0,1fr))] gap-2">
                        <div class="relative text-xs text-muted-foreground">
                            {#each hours as hour (hour)}
                                <div class="h-10 flex items-start justify-end pr-2">
                                    {hour < 24 ? `${hour.toString().padStart(2, '0')}:00` : ''}
                                </div>
                            {/each}
                        </div>

                        {#each weekDayOptions as day (day.value)}
                            <div class="relative h-[960px] rounded-md border border-dashed border-border bg-muted/10">
                                {#each getSlotsForDay(day.value) as slot (slot.id)}
                                    {#each getSlotSegments(slot) as segment (segment.start)}
                                        {@const top = minutesToPercent(segment.start)}
                                        {@const height = Math.max(minutesToPercent(segment.end - segment.start), 1)}
                                        <div
                                            class="absolute left-2 right-2 rounded-md shadow-sm"
                                            style={`top: ${top}%; height: ${height}%; background-color: ${shift.colorAsHex}`}
                                        ></div>
                                    {/each}

                                    {#each slot.breaks as breakItem (breakItem.id)}
                                        {@const breakStart = timeToMinutes(breakItem.startTime)}
                                        {@const breakEnd = timeToMinutes(breakItem.endTime)}
                                        {@const breakTop = minutesToPercent(breakStart)}
                                        {@const breakHeight = Math.max(minutesToPercent(breakEnd - breakStart), 1)}
                                        <div
                                            class="absolute left-4 right-4 rounded border shadow-sm flex items-center justify-center text-[10px] font-semibold text-muted-foreground"
                                            style={`top: ${breakTop}%; height: ${breakHeight}%; background-color: rgba(15,23,42,0.15); border-color: ${shift.colorAsHex}`}
                                        >
                                            Break
                                        </div>
                                    {/each}
                                {/each}
                            </div>
                        {/each}
                    </div>
                </div>
            </div>

            <div class="space-y-3">
                <h2 class="text-lg font-semibold">Job requirements</h2>
                <div class="rounded-lg border border-border p-4 space-y-2">
                    {#if shift.jobRequirements.length === 0}
                        <p class="text-sm text-muted-foreground">No job requirements defined.</p>
                    {:else}
                        {#each shift.jobRequirements as requirement (requirement.jobId)}
                            <div class="flex items-center justify-between text-sm">
                                <span class="font-medium">{requirement.name}</span>
                                <span class="text-muted-foreground">x{requirement.requiredStaffCount}</span>
                            </div>
                        {/each}
                    {/if}
                </div>
            </div>
        </div>
    {/if}
</div>
