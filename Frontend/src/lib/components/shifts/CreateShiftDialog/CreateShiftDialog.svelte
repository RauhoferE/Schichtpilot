<script lang="ts">
    import * as Alert from '$lib/components/ui/alert/index.js';
    import { Button } from '$lib/components/ui/button/index.js';
    import * as Dialog from '$lib/components/ui/dialog/index.js';
    import { Input } from '$lib/components/ui/input/index.js';
    import { Label } from '$lib/components/ui/label/index.js';
    import { createShift } from '$lib/services/shift.service';
    import { getJobRoles } from '$lib/services/jobRole.service';
    import type { BreakDto, CreateShiftDto, ShiftRequirementDto, TimeSlotDto } from '$lib/types/shift.types';
    import type { JobRoleShortDto } from '$lib/types/jobRole.types';
    import { HttpError } from '$lib/customErrors';

    interface Props {
        open: boolean;
        onCreated?: () => void | Promise<void>;
    }

    let { open = $bindable(false), onCreated }: Props = $props();

    const weekDayOptions = [
        { value: 0, label: 'Sunday' },
        { value: 1, label: 'Monday' },
        { value: 2, label: 'Tuesday' },
        { value: 3, label: 'Wednesday' },
        { value: 4, label: 'Thursday' },
        { value: 5, label: 'Friday' },
        { value: 6, label: 'Saturday' }
    ];

    let name = $state('');
    let description = $state('');
    let colorAsHex = $state('#f59e0b');
    let timeSlots = $state<TimeSlotDto[]>([]);
    let jobRequirements = $state<ShiftRequirementDto[]>([]);

    let errorMessage = $state('');
    let isSaving = $state(false);
    let submitted = $state(false);
    let touched = $state({ name: false, description: false });

    let jobSearch = $state('');
    let jobOptions = $state<JobRoleShortDto[]>([]);
    let isJobLoading = $state(false);

    let nextTimeSlotId = 1;
    let nextBreakId = 1;

    function resetForm() {
        name = '';
        description = '';
        colorAsHex = '#f59e0b';
        timeSlots = [];
        jobRequirements = [];
        errorMessage = '';
        isSaving = false;
        submitted = false;
        touched = { name: false, description: false };
        jobSearch = '';
        jobOptions = [];
        isJobLoading = false;
        nextTimeSlotId = 1;
        nextBreakId = 1;
    }

    $effect(() => {
        if (!open) {
            resetForm();
        }
    });

    function handleBlur(field: 'name' | 'description') {
        touched[field] = true;
    }

    let nameError = $derived.by(() => {
        if (name.trim() === '') {
            return 'Shift name is required';
        }
        return '';
    });

    function timeToMinutes(value: string): number | null {
        const parts = value.split(':');
        if (parts.length !== 2) {
            return null;
        }
        const hours = Number(parts[0]);
        const minutes = Number(parts[1]);
        if (Number.isNaN(hours) || Number.isNaN(minutes)) {
            return null;
        }
        return hours * 60 + minutes;
    }

    function minutesToTime(value: number) {
        const hours = Math.floor(value / 60);
        const minutes = value % 60;
        const paddedHours = hours.toString().padStart(2, '0');
        const paddedMinutes = minutes.toString().padStart(2, '0');
        return `${paddedHours}:${paddedMinutes}`;
    }

    function getTimeSlotError(slot: TimeSlotDto): string {
        const start = timeToMinutes(slot.startTime);
        const end = timeToMinutes(slot.endTime);

        if (start === null || end === null) {
            return 'Start and end time are required.';
        }

        if (start >= end) {
            return 'Start time must be before end time.';
        }

        return '';
    }

    function getBreakError(slot: TimeSlotDto, breakItem: BreakDto): string {
        const slotStart = timeToMinutes(slot.startTime);
        const slotEnd = timeToMinutes(slot.endTime);
        const breakStart = timeToMinutes(breakItem.startTime);
        const breakEnd = timeToMinutes(breakItem.endTime);

        if (breakStart === null || breakEnd === null) {
            return 'Break start and end time are required.';
        }

        if (breakStart >= breakEnd) {
            return 'Break start must be before end time.';
        }

        if (slotStart !== null && breakStart < slotStart) {
            return 'Break must start within the timeslot.';
        }

        if (slotEnd !== null && breakEnd > slotEnd) {
            return 'Break must end within the timeslot.';
        }

        return '';
    }

    function getRequirementError(requirement: ShiftRequirementDto): string {
        if (requirement.requiredStaffCount <= 0) {
            return 'Staff count must be greater than 0.';
        }
        return '';
    }

    function addTimeSlot() {
        timeSlots = [
            ...timeSlots,
            {
                id: nextTimeSlotId++,
                dayOfWeek: 1,
                startTime: '09:00',
                endTime: '17:00',
                breaks: []
            }
        ];
    }

    function removeTimeSlot(slotId: number) {
        timeSlots = timeSlots.filter((slot) => slot.id !== slotId);
    }

    function updateTimeSlot(slotId: number, updates: Partial<TimeSlotDto>) {
        timeSlots = timeSlots.map((slot) => (slot.id === slotId ? { ...slot, ...updates } : slot));
    }

    function addBreak(slotId: number) {
        timeSlots = timeSlots.map((slot) => {
            if (slot.id !== slotId) {
                return slot;
            }

            const slotStart = timeToMinutes(slot.startTime);
            const slotEnd = timeToMinutes(slot.endTime);
            let startTime = slot.startTime;
            let endTime = slot.endTime;

            if (slotStart !== null && slotEnd !== null && slotEnd - slotStart >= 30) {
                startTime = minutesToTime(slotStart + 5);
                endTime = minutesToTime(Math.min(slotStart + 30, slotEnd));
            }

            const nextBreak: BreakDto = {
                id: nextBreakId++,
                startTime,
                endTime
            };

            return { ...slot, breaks: [...slot.breaks, nextBreak] };
        });
    }

    function updateBreak(slotId: number, breakId: number, updates: Partial<BreakDto>) {
        timeSlots = timeSlots.map((slot) => {
            if (slot.id !== slotId) {
                return slot;
            }

            const breaks = slot.breaks.map((breakItem) =>
                breakItem.id === breakId ? { ...breakItem, ...updates } : breakItem
            );

            return { ...slot, breaks };
        });
    }

    function removeBreak(slotId: number, breakId: number) {
        timeSlots = timeSlots.map((slot) => {
            if (slot.id !== slotId) {
                return slot;
            }

            return { ...slot, breaks: slot.breaks.filter((breakItem) => breakItem.id !== breakId) };
        });
    }

    async function loadJobOptions() {
        const query = jobSearch.trim();
        if (query === '') {
            jobOptions = [];
            return;
        }

        isJobLoading = true;
        try {
            const response = await getJobRoles({
                page: 1,
                pageSize: 10,
                searchstring: query
            });
            jobOptions = response.jobRoles;
        } catch {
            jobOptions = [];
        } finally {
            isJobLoading = false;
        }
    }

    function handleJobSearchInput(event: Event) {
        jobSearch = (event.currentTarget as HTMLInputElement).value;
        loadJobOptions();
    }

    function handleJobSelect(event: Event) {
        const select = event.currentTarget as HTMLSelectElement;
        const selectedIds = Array.from(select.selectedOptions).map(option => Number(option.value));
        const selectedJobs = jobOptions.filter(job => selectedIds.includes(job.id));

        selectedJobs.forEach((job) => {
            if (!jobRequirements.some((req) => req.jobId === job.id)) {
                jobRequirements = [
                    ...jobRequirements,
                    { jobId: job.id, name: job.name, requiredStaffCount: 1 }
                ];
            }
        });

        Array.from(select.options).forEach(option => {
            option.selected = false;
        });
    }

    function updateRequirement(jobId: number, updates: Partial<ShiftRequirementDto>) {
        jobRequirements = jobRequirements.map((req) =>
            req.jobId === jobId ? { ...req, ...updates } : req
        );
    }

    function removeRequirement(jobId: number) {
        jobRequirements = jobRequirements.filter((req) => req.jobId !== jobId);
    }

    function isFormValid(): boolean {
        let isValid = true;

        if (nameError !== '') {
            isValid = false;
        }

        const hasTimeSlotErrors = timeSlots.some((slot) => {
            if (getTimeSlotError(slot) !== '') {
                return true;
            }

            return slot.breaks.some((breakItem) => getBreakError(slot, breakItem) !== '');
        });

        if (hasTimeSlotErrors) {
            isValid = false;
        }

        const hasRequirementErrors = jobRequirements.some(
            (req) => getRequirementError(req) !== ''
        );

        if (hasRequirementErrors) {
            isValid = false;
        }

        return isValid;
    }

    async function handleCreateShift(event: SubmitEvent) {
        event.preventDefault();
        errorMessage = '';
        submitted = true;

        if (!isFormValid()) {
            return;
        }

        isSaving = true;
        try {
            const payload: CreateShiftDto = {
                name: name.trim(),
                description: description.trim(),
                colorAsHex,
                timeSlots,
                jobRequirements
            };

            await createShift(payload);
            open = false;
            await onCreated?.();
        } catch (error) {
            errorMessage = 'Failed to create shift.';
            if (error instanceof HttpError) {
                errorMessage = error.Error.message;
            }
        } finally {
            isSaving = false;
        }
    }
</script>

<Dialog.Root bind:open>
    <Dialog.Content class="sm:max-w-3xl">
        <Dialog.Header>
            <Dialog.Title>Create shift</Dialog.Title>
            <Dialog.Description>Define shift details, timeslots, and job requirements.</Dialog.Description>
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

        <form class="space-y-6" onsubmit={handleCreateShift} novalidate>
            <div class="grid gap-4 sm:grid-cols-2">
                <div class="space-y-2">
                    <Label for="shiftName">Name</Label>
                    <Input
                        id="shiftName"
                        type="text"
                        placeholder="Lunch shift"
                        onblur={() => handleBlur('name')}
                        disabled={isSaving}
                        bind:value={name}
                    />
                    {#if nameError !== '' && (touched.name || submitted)}
                        <span class="error">{nameError}</span>
                    {/if}
                </div>

                <div class="space-y-2">
                    <Label for="shiftDescription">Description</Label>
                    <Input
                        id="shiftDescription"
                        type="text"
                        placeholder="Short description"
                        onblur={() => handleBlur('description')}
                        disabled={isSaving}
                        bind:value={description}
                    />
                </div>

                <div class="space-y-2">
                    <Label for="shiftColor">Color</Label>
                    <div class="flex items-center gap-2">
                        <Input
                            id="shiftColor"
                            type="color"
                            class="h-9 w-16 p-1"
                            disabled={isSaving}
                            bind:value={colorAsHex}
                        />
                        <Input type="text" readonly value={colorAsHex} />
                    </div>
                </div>
            </div>

            <div class="space-y-3">
                <div class="flex items-center justify-between">
                    <Label>Timeslots</Label>
                    <Button type="button" variant="outline" size="sm" onclick={addTimeSlot}>
                        Add timeslot
                    </Button>
                </div>

                {#if timeSlots.length === 0}
                    <p class="text-sm text-muted-foreground">No timeslots added yet.</p>
                {:else}
                    <div class="space-y-4">
                        {#each timeSlots as slot (slot.id)}
                            <div class="rounded-lg border border-border p-4 space-y-4">
                                <div class="flex flex-wrap items-center gap-4">
                                    <div class="min-w-[200px] space-y-2">
                                        <Label>Day of week</Label>
                                        <select
                                            class="h-9 w-full rounded-md border border-input bg-background px-3 text-sm shadow-xs focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-3"
                                            value={slot.dayOfWeek}
                                            onchange={(event) =>
                                                updateTimeSlot(slot.id, {
                                                    dayOfWeek: Number((event.currentTarget as HTMLSelectElement).value)
                                                })
                                            }
                                        >
                                            {#each weekDayOptions as day(day.value)}
                                                <option value={day.value}>{day.label}</option>
                                            {/each}
                                        </select>
                                    </div>

                                    <div class="min-w-[160px] space-y-2">
                                        <Label>Start time</Label>
                                        <Input
                                            type="time"
                                            value={slot.startTime}
                                            oninput={(event) =>
                                                updateTimeSlot(slot.id, {
                                                    startTime: (event.currentTarget as HTMLInputElement).value
                                                })
                                            }
                                        />
                                    </div>

                                    <div class="min-w-[160px] space-y-2">
                                        <Label>End time</Label>
                                        <Input
                                            type="time"
                                            value={slot.endTime}
                                            oninput={(event) =>
                                                updateTimeSlot(slot.id, {
                                                    endTime: (event.currentTarget as HTMLInputElement).value
                                                })
                                            }
                                        />
                                    </div>

                                    <Button type="button" variant="ghost" onclick={() => removeTimeSlot(slot.id)}>
                                        Remove timeslot
                                    </Button>
                                </div>

                                {#if getTimeSlotError(slot) !== '' && submitted}
                                    <span class="error">{getTimeSlotError(slot)}</span>
                                {/if}

                                <div class="space-y-3">
                                    <div class="flex items-center justify-between">
                                        <Label>Breaks</Label>
                                        <Button type="button" variant="outline" size="sm" onclick={() => addBreak(slot.id)}>
                                            Add break
                                        </Button>
                                    </div>

                                    {#if slot.breaks.length === 0}
                                        <p class="text-sm text-muted-foreground">No breaks added.</p>
                                    {:else}
                                        <div class="space-y-3">
                                            {#each slot.breaks as breakItem (breakItem.id)}
                                                <div class="flex flex-wrap items-center gap-4">
                                                    <div class="min-w-[160px] space-y-2">
                                                        <Label>Break start</Label>
                                                        <Input
                                                            type="time"
                                                            value={breakItem.startTime}
                                                            oninput={(event) =>
                                                                updateBreak(slot.id, breakItem.id, {
                                                                    startTime: (event.currentTarget as HTMLInputElement)
                                                                        .value
                                                                })
                                                            }
                                                        />
                                                    </div>

                                                    <div class="min-w-[160px] space-y-2">
                                                        <Label>Break end</Label>
                                                        <Input
                                                            type="time"
                                                            value={breakItem.endTime}
                                                            oninput={(event) =>
                                                                updateBreak(slot.id, breakItem.id, {
                                                                    endTime: (event.currentTarget as HTMLInputElement)
                                                                        .value
                                                                })
                                                            }
                                                        />
                                                    </div>

                                                    <Button
                                                        type="button"
                                                        variant="ghost"
                                                        onclick={() => removeBreak(slot.id, breakItem.id)}
                                                    >
                                                        Remove break
                                                    </Button>
                                                </div>
                                                {#if getBreakError(slot, breakItem) !== '' && submitted}
                                                    <span class="error">{getBreakError(slot, breakItem)}</span>
                                                {/if}
                                            {/each}
                                        </div>
                                    {/if}
                                </div>
                            </div>
                        {/each}
                    </div>
                {/if}
            </div>

            <div class="space-y-3">
                <Label>Job requirements</Label>
                <Input
                    type="text"
                    placeholder="Search jobs to add..."
                    oninput={handleJobSearchInput}
                    disabled={isSaving}
                    value={jobSearch}
                />
                <select
                    multiple
                    class="min-h-[120px] w-full rounded-md border border-input bg-transparent px-2 py-2 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    onchange={handleJobSelect}
                    disabled={isSaving || isJobLoading}
                >
                    {#if jobSearch.trim() === ''}
                        <option disabled>Type to search jobs</option>
                    {:else if isJobLoading}
                        <option disabled>Loading...</option>
                    {:else if jobOptions.length === 0}
                        <option disabled>No matches</option>
                    {:else}
                        {#each jobOptions as job(job.id)}
                            {#if !jobRequirements.some((req) => req.jobId === job.id)}
                                <option value={job.id}>{job.name}</option>
                            {/if}
                        {/each}
                    {/if}
                </select>

                {#if jobRequirements.length > 0}
                    <div class="space-y-3">
                        {#each jobRequirements as requirement (requirement.jobId)}
                            <div class="flex flex-wrap items-center gap-4">
                                <div class="flex-1">
                                    <span class="text-sm font-medium">{requirement.name}</span>
                                </div>
                                <div class="min-w-[120px] space-y-2">
                                    <Label>Staff count</Label>
                                    <Input
                                        type="number"
                                        min="1"
                                        value={requirement.requiredStaffCount}
                                        oninput={(event) =>
                                            updateRequirement(requirement.jobId, {
                                                requiredStaffCount: Number(
                                                    (event.currentTarget as HTMLInputElement).value
                                                )
                                            })
                                        }
                                    />
                                </div>
                                <Button type="button" variant="ghost" onclick={() => removeRequirement(requirement.jobId)}>
                                    Remove
                                </Button>
                            </div>
                            {#if getRequirementError(requirement) !== '' && submitted}
                                <span class="error">{getRequirementError(requirement)}</span>
                            {/if}
                        {/each}
                    </div>
                {/if}
            </div>

            <Dialog.Footer>
                <Dialog.Close asChild>
                    <Button variant="outline" type="button" disabled={isSaving}>Cancel</Button>
                </Dialog.Close>
                <Button type="submit" disabled={isSaving} aria-busy={isSaving}>
                    Create shift
                </Button>
            </Dialog.Footer>
        </form>
    </Dialog.Content>
</Dialog.Root>
