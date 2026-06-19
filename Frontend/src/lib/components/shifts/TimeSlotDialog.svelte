<script lang="ts">
	import * as Alert from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Dialog from '$lib/components/ui/dialog/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { Label } from '$lib/components/ui/label/index.js';
	import type { TimeSlotDto } from '$lib/types/shift.types';

	type Props = {
		open: boolean;
		mode: 'add' | 'edit';
		draft: TimeSlotDto | null;
		error: string;
		submitted: boolean;
		isSaving: boolean;
		availableDays: { value: number; label: string }[];
		onSave: (event: SubmitEvent) => void;
		onRequestDelete: () => void;
		onUpdateSlot: (updates: Partial<TimeSlotDto>) => void;
		onAddBreak: () => void;
		onUpdateBreak: (breakId: number, updates: Partial<TimeSlotDto['breaks'][number]>) => void;
		onRemoveBreak: (breakId: number) => void;
		getTimeSlotError: (slot: TimeSlotDto) => string;
		getBreakError: (slot: TimeSlotDto, breakItem: TimeSlotDto['breaks'][number]) => string;
		deleteConfirmOpen: boolean;
		onConfirmDelete: () => void;
		onCancelDelete: () => void;
	};

	let {
		open = $bindable(false),
		mode = 'add',
		draft = null,
		error = '',
		submitted = false,
		isSaving = false,
		availableDays = [],
		onSave,
		onRequestDelete,
		onUpdateSlot,
		onAddBreak,
		onUpdateBreak,
		onRemoveBreak,
		getTimeSlotError,
		getBreakError,
		deleteConfirmOpen = false,
		onConfirmDelete,
		onCancelDelete
	}: Props = $props();
</script>

<Dialog.Root bind:open>
	<Dialog.Content class="sm:max-w-3xl">
		<Dialog.Header>
			<Dialog.Title>
				{mode === 'add' ? 'Add timeslot' : 'Edit timeslot'}
			</Dialog.Title>
			<Dialog.Description>Set the timeslot details and breaks.</Dialog.Description>
		</Dialog.Header>

		{#if error}
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
				<Alert.Description>{error}</Alert.Description>
			</Alert.Root>
		{/if}

		{#if draft}
			<form class="space-y-6" onsubmit={onSave} novalidate>
				<div class="border-border space-y-4 rounded-lg border p-4">
					<div class="flex flex-wrap items-center gap-4">
						<div class="min-w-[200px] space-y-2">
							<Label>Day of week</Label>
							<select
								class="border-input focus-visible:ring-ring h-9 w-full rounded-md border bg-transparent px-2 text-sm shadow-xs focus-visible:ring-1 focus-visible:outline-none"
								value={draft.dayOfWeek}
								onchange={(event) =>
									onUpdateSlot({
										dayOfWeek: Number((event.currentTarget as HTMLSelectElement).value)
									})}
							>
								{#each availableDays as day (day.value)}
									<option value={day.value}>{day.label}</option>
								{/each}
							</select>
						</div>
						<div class="min-w-[160px] space-y-2">
							<Label>Start time</Label>
							<Input
								type="time"
								value={draft.startTime}
								oninput={(event) =>
									onUpdateSlot({
										startTime: (event.currentTarget as HTMLInputElement).value
									})}
							/>
						</div>
						<div class="min-w-[160px] space-y-2">
							<Label>End time</Label>
							<Input
								type="time"
								value={draft.endTime}
								oninput={(event) =>
									onUpdateSlot({
										endTime: (event.currentTarget as HTMLInputElement).value
									})}
							/>
						</div>
					</div>

					{#if getTimeSlotError(draft) !== '' && submitted}
						<span class="error">{getTimeSlotError(draft)}</span>
					{/if}

					<div class="space-y-3">
						<div class="flex items-center justify-between">
							<Label>Breaks</Label>
							<Button type="button" variant="outline" size="sm" onclick={onAddBreak}>
								Add break
							</Button>
						</div>
						{#if draft.breaks.length === 0}
							<p class="text-muted-foreground text-sm">No breaks defined.</p>
						{:else}
							<div class="space-y-3">
								{#each draft.breaks as breakItem (breakItem.id)}
									<div class="flex flex-wrap items-center gap-4">
										<div class="min-w-[160px] space-y-2">
											<Label>Break start</Label>
											<Input
												type="time"
												value={breakItem.startTime}
												oninput={(event) =>
													onUpdateBreak(breakItem.id, {
														startTime: (event.currentTarget as HTMLInputElement).value
													})}
											/>
										</div>
										<div class="min-w-[160px] space-y-2">
											<Label>Break end</Label>
											<Input
												type="time"
												value={breakItem.endTime}
												oninput={(event) =>
													onUpdateBreak(breakItem.id, {
														endTime: (event.currentTarget as HTMLInputElement).value
													})}
											/>
										</div>
										<Button
											type="button"
											variant="ghost"
											onclick={() => onRemoveBreak(breakItem.id)}
										>
											Remove
										</Button>
									</div>
									{#if getBreakError(draft, breakItem) !== '' && submitted}
										<span class="error">{getBreakError(draft, breakItem)}</span>
									{/if}
								{/each}
							</div>
						{/if}
					</div>
				</div>

				<Dialog.Footer>
					<Dialog.Close asChild>
						<Button variant="outline" type="button" disabled={isSaving}>Cancel</Button>
					</Dialog.Close>
					{#if mode === 'edit'}
						<Button
							type="button"
							variant="destructive"
							onclick={onRequestDelete}
							disabled={isSaving}
						>
							Delete timeslot
						</Button>
					{/if}
					<Button type="submit" disabled={isSaving} aria-busy={isSaving}>
						{mode === 'add' ? 'Add timeslot' : 'Save changes'}
					</Button>
				</Dialog.Footer>
			</form>
		{/if}
	</Dialog.Content>
</Dialog.Root>

<Dialog.Root bind:open={deleteConfirmOpen}>
	<Dialog.Content class="sm:max-w-sm">
		<Dialog.Header>
			<Dialog.Title>Delete timeslot?</Dialog.Title>
			<Dialog.Description>This will permanently remove this timeslot.</Dialog.Description>
		</Dialog.Header>
		<Dialog.Footer>
			<Button variant="outline" onclick={onCancelDelete}>Cancel</Button>
			<Button variant="destructive" onclick={onConfirmDelete}>Delete</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
