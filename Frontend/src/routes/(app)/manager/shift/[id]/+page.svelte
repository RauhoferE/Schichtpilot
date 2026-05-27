<script lang="ts">
	import * as Alert from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { Label } from '$lib/components/ui/label/index.js';
	import * as Dialog from '$lib/components/ui/dialog/index.js';
	import {
		addJobRequirement,
		changeJobRequirementCount,
		getShift,
		removeJobRequirement,
		updateShift
	} from '$lib/services/shift.service';
	import { getJobRoles } from '$lib/services/jobRole.service';
	import type { ShiftDto, TimeSlotDto } from '$lib/types/shift.types';
	import type { JobRoleShortDto } from '$lib/types/jobRole.types';
	import { page } from '$app/stores';
	import { adminGuard } from '../../../../common-guards.ts/manager.guard';
	import { goto } from '$app/navigation';
	import { HttpError } from '$lib/customErrors';

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

	let jobSearch = $state('');
	let jobOptions = $state<JobRoleShortDto[]>([]);
	let isJobLoading = $state(false);
	let selectedJobIds = $state<string[]>([]);
	let newRequirementCount = $state(1);
	let requirementError = $state('');
	let requirementSuccess = $state('');

	let confirmRemoveOpen = $state(false);
	let confirmRemoveJobId = $state<number | null>(null);
	let confirmRemoveJobName = $state('');

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

	const availableJobOptions = $derived.by(() => {
		if (!shift) {
			return [];
		}
		return jobOptions.filter((job) => !shift.jobRequirements.some((req) => req.jobId === job.id));
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
		requirementError = '';
		requirementSuccess = '';

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

	async function loadJobOptions() {
		const query = jobSearch.trim();
		if (query === '') {
			jobOptions = [];
			selectedJobIds = [];
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
		selectedJobIds = [];
		void loadJobOptions();
	}

	async function handleAddRequirement() {
		requirementError = '';
		requirementSuccess = '';
		if (!shift) {
			return;
		}

		if (selectedJobIds.length === 0) {
			requirementError = 'Select at least one job role to add.';
			return;
		}

		const count = Number(newRequirementCount);
		if (!Number.isFinite(count) || count < 1) {
			requirementError = 'Staff count must be at least 1.';
			return;
		}

		const selectedJobIdValues = selectedJobIds
			.map((id) => Number(id))
			.filter((id) => Number.isFinite(id));

		if (selectedJobIdValues.length === 0) {
			requirementError = 'Select a valid job role.';
			return;
		}

		const newJobs = selectedJobIdValues
			.filter((id) => !shift.jobRequirements.some((req) => req.jobId === id))
			.map((id) => jobOptions.find((job) => job.id === id))
			.filter((job): job is JobRoleShortDto => Boolean(job));

		if (newJobs.length === 0) {
			requirementError = 'Selected job role(s) are already required.';
			return;
		}

		try {
			for (const job of newJobs) {
				await addJobRequirement(shiftId, {
					jobId: job.id,
					name: job.name,
					requiredStaffCount: count
				});
			}

			shift = {
				...shift,
				jobRequirements: [
					...shift.jobRequirements,
					...newJobs.map((job) => ({
						jobId: job.id,
						name: job.name,
						requiredStaffCount: count
					}))
				]
			};
			selectedJobIds = [];
			newRequirementCount = 1;
			requirementSuccess = 'Job requirement(s) added.';
		} catch (error){
			requirementError = 'Failed to add job requirement.';
            if (error instanceof HttpError) {
                requirementError = error.Error.message;
            }
		}
	}

	function updateRequirementLocal(jobId: number, requiredStaffCount: number) {
		if (!shift) {
			return;
		}

		shift = {
			...shift,
			jobRequirements: shift.jobRequirements.map((req) =>
				req.jobId === jobId ? { ...req, requiredStaffCount } : req
			)
		};
	}

	function handleRequirementCountInput(jobId: number, event: Event) {
		const rawValue = (event.currentTarget as HTMLInputElement).value;
		const parsedValue = Number(rawValue);
		if (!Number.isFinite(parsedValue)) {
			return;
		}
		updateRequirementLocal(jobId, parsedValue);
	}

	async function handleRequirementCountBlur(jobId: number, event: Event) {
		requirementError = '';
		requirementSuccess = '';
		const rawValue = (event.currentTarget as HTMLInputElement).value;
		const parsedValue = Number(rawValue);
		const normalizedValue = Number.isFinite(parsedValue) && parsedValue >= 1 ? parsedValue : 1;

		updateRequirementLocal(jobId, normalizedValue);

		try {
			await changeJobRequirementCount(shiftId, jobId, normalizedValue);
		} catch {
			requirementError = 'Failed to update job requirement count.';
		}
	}

	function openRemoveRequirement(jobId: number, jobName: string) {
		if (!shift) {
			return;
		}
		requirementError = '';
		requirementSuccess = '';

		if (shift.jobRequirements.length <= 1) {
			requirementError = 'At least one job requirement is required.';
			return;
		}

		confirmRemoveJobId = jobId;
		confirmRemoveJobName = jobName;
		confirmRemoveOpen = true;
	}

	function closeRemoveRequirementDialog() {
		confirmRemoveOpen = false;
		confirmRemoveJobId = null;
		confirmRemoveJobName = '';
	}

	async function confirmRemoveRequirement() {
		if (!shift || confirmRemoveJobId === null) {
			return;
		}
		requirementError = '';
		requirementSuccess = '';

		if (shift.jobRequirements.length <= 1) {
			requirementError = 'At least one job requirement is required.';
			closeRemoveRequirementDialog();
			return;
		}

		try {
			await removeJobRequirement(shiftId, confirmRemoveJobId);
			shift = {
				...shift,
				jobRequirements: shift.jobRequirements.filter((req) => req.jobId !== confirmRemoveJobId)
			};
			requirementSuccess = 'Job requirement removed.';
			
		} catch(error) {
			requirementError = 'Failed to remove job requirement.';

            if (error instanceof HttpError) {
                requirementError = error.Error.message;
            }
		}

        closeRemoveRequirementDialog();
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
			<p class="text-muted-foreground text-sm">Manage shift info, timeslots, and requirements.</p>
		</div>
		<Button variant="outline" onclick={goBack}>Back to shifts</Button>
	</div>

	{#if loadError}
		<Alert.Root variant="destructive">
			<svg
				class="h-4 w-4"
				viewBox="0 0 24 24"
				fill="none"
				stroke="currentColor"
				stroke-width="2"
				aria-hidden="true"
			>
				<circle cx="12" cy="12" r="10" />
				<line x1="12" y1="8" x2="12" y2="12" />
				<line x1="12" y1="16" x2="12.01" y2="16" />
			</svg>
			<Alert.Description>{loadError}</Alert.Description>
		</Alert.Root>
	{/if}

	{#if isLoading}
		<div class="border-border text-muted-foreground rounded-lg border p-6 text-center">
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
					<svg
						class="h-4 w-4"
						viewBox="0 0 24 24"
						fill="none"
						stroke="currentColor"
						stroke-width="2"
						aria-hidden="true"
					>
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
			<div class="space-y-3 lg:col-span-2">
				<h2 class="text-lg font-semibold">Weekly timeslots</h2>
				<div class="border-border rounded-lg border p-4">
					<div
						class="text-muted-foreground grid grid-cols-[60px_repeat(7,minmax(0,1fr))] gap-2 text-xs"
					>
						<div></div>
						{#each weekDayOptions as day (day.value)}
							<div class="text-foreground text-center font-medium">{day.label}</div>
						{/each}
					</div>

					<div class="mt-3 grid grid-cols-[60px_repeat(7,minmax(0,1fr))] gap-2">
						<div class="text-muted-foreground relative text-xs">
							{#each hours as hour (hour)}
								<div class="flex h-10 items-start justify-end pr-2">
									{hour < 24 ? `${hour.toString().padStart(2, '0')}:00` : ''}
								</div>
							{/each}
						</div>

						{#each weekDayOptions as day (day.value)}
							<div
								class="border-border bg-muted/10 relative h-[960px] rounded-md border border-dashed"
							>
								{#each getSlotsForDay(day.value) as slot (slot.id)}
									{#each getSlotSegments(slot) as segment (segment.start)}
										{@const top = minutesToPercent(segment.start)}
										{@const height = Math.max(minutesToPercent(segment.end - segment.start), 1)}
										<div
											class="absolute right-2 left-2 rounded-md shadow-sm"
											style={`top: ${top}%; height: ${height}%; background-color: ${shift.colorAsHex}`}
										></div>
									{/each}

									{#each slot.breaks as breakItem (breakItem.id)}
										{@const breakStart = timeToMinutes(breakItem.startTime)}
										{@const breakEnd = timeToMinutes(breakItem.endTime)}
										{@const breakTop = minutesToPercent(breakStart)}
										{@const breakHeight = Math.max(minutesToPercent(breakEnd - breakStart), 1)}
										<div
											class="text-muted-foreground absolute right-4 left-4 flex items-center justify-center rounded border text-[10px] font-semibold shadow-sm"
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

				{#if requirementError}
					<Alert.Root variant="destructive">
						<svg
							class="h-4 w-4"
							viewBox="0 0 24 24"
							fill="none"
							stroke="currentColor"
							stroke-width="2"
							aria-hidden="true"
						>
							<circle cx="12" cy="12" r="10" />
							<line x1="12" y1="8" x2="12" y2="12" />
							<line x1="12" y1="16" x2="12.01" y2="16" />
						</svg>
						<Alert.Description>{requirementError}</Alert.Description>
					</Alert.Root>
				{/if}

				{#if requirementSuccess}
					<Alert.Root>
						<Alert.Description>{requirementSuccess}</Alert.Description>
					</Alert.Root>
				{/if}

				<div class="space-y-3">
					<div class="border-border space-y-3 rounded-lg border p-4">
						<h3 class="text-sm font-medium">Existing requirements</h3>
						{#if shift.jobRequirements.length === 0}
							<p class="text-muted-foreground text-sm">No job requirements defined.</p>
						{:else}
							<div class="space-y-3">
								{#each shift.jobRequirements as requirement (requirement.jobId)}
									<div
										class="border-border flex flex-wrap items-center gap-4 rounded-lg border p-3"
									>
										<div class="flex-1">
											<span class="text-sm font-medium">{requirement.name}</span>
										</div>
										<div class="min-w-[120px] space-y-2">
											<Label>Staff count</Label>
											<Input
												type="number"
												min="1"
												disabled={isSaving}
												value={requirement.requiredStaffCount}
												oninput={(event) => handleRequirementCountInput(requirement.jobId, event)}
												onchange={(event) => handleRequirementCountBlur(requirement.jobId, event)}
											/>
										</div>
										<Button
											type="button"
											variant="ghost"
											onclick={() => openRemoveRequirement(requirement.jobId, requirement.name)}
											disabled={isSaving || shift.jobRequirements.length <= 1}
											aria-label="Remove requirement"
											class="text-muted-foreground"
										>
											×
										</Button>
									</div>
								{/each}
							</div>
						{/if}

						{#if shift.jobRequirements.length <= 1}
							<p class="text-muted-foreground text-xs">At least one requirement is required.</p>
						{/if}
					</div>

					<div class="border-border space-y-3 rounded-lg border p-4">
						<h3 class="text-sm font-medium">Add new requirement</h3>
						<Input
							type="text"
							placeholder="Search jobs to add..."
							oninput={handleJobSearchInput}
							disabled={isSaving}
							value={jobSearch}
						/>
						<select
							multiple
							class="border-input focus-visible:ring-ring min-h-[120px] w-full rounded-md border bg-transparent px-2 py-2 text-sm shadow-xs focus-visible:ring-1 focus-visible:outline-none"
							bind:value={selectedJobIds}
							disabled={isSaving || isJobLoading}
						>
							{#if jobSearch.trim() === ''}
								<option disabled>Type to search jobs</option>
							{:else if isJobLoading}
								<option disabled>Loading...</option>
							{:else if availableJobOptions.length === 0}
								<option disabled>No matches</option>
							{:else}
								{#each availableJobOptions as job (job.id)}
									<option value={job.id}>{job.name}</option>
								{/each}
							{/if}
						</select>

						<div class="flex flex-wrap items-end gap-3">
							<div class="min-w-[120px] space-y-2">
								<Label>Staff count</Label>
								<Input
									type="number"
									min="1"
									disabled={isSaving}
									value={newRequirementCount}
									oninput={(event) => {
										const value = Number((event.currentTarget as HTMLInputElement).value);
										if (Number.isFinite(value) && value >= 1) {
											newRequirementCount = value;
										}
									}}
								/>
							</div>
							<Button
								type="button"
								variant="outline"
								onclick={handleAddRequirement}
								disabled={isSaving || selectedJobIds.length === 0}
							>
								Add requirement
							</Button>
						</div>
					</div>
				</div>
			</div>
		</div>
	{/if}
</div>

<Dialog.Root bind:open={confirmRemoveOpen}>
	<Dialog.Content class="sm:max-w-sm">
		<Dialog.Header>
			<Dialog.Title>Remove requirement?</Dialog.Title>
			<Dialog.Description>
				This will remove the requirement for “{confirmRemoveJobName}”.
			</Dialog.Description>
		</Dialog.Header>
		<Dialog.Footer>
			<Button variant="outline" onclick={closeRemoveRequirementDialog}>Cancel</Button>
			<Button variant="destructive" onclick={confirmRemoveRequirement}>Remove</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
