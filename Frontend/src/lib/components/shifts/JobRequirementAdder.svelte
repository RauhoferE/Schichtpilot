<script lang="ts">
	import { Button } from '$lib/components/ui/button/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { Label } from '$lib/components/ui/label/index.js';
	import type { JobRoleShortDto } from '$lib/types/jobRole.types';

	type Props = {
		title: string;
		jobSearch: string;
		isJobLoading: boolean;
		availableJobOptions: JobRoleShortDto[];
		selectedJobIds: string[];
		newRequirementCount: number;
		isSaving: boolean;
		onSearchInput: (event: Event) => void;
		onAdd: () => void;
	};

	let {
		title = 'Add requirement',
		jobSearch = '',
		isJobLoading = false,
		availableJobOptions = [],
		selectedJobIds = $bindable([]),
		newRequirementCount = $bindable(1),
		isSaving = false,
		onSearchInput,
		onAdd
	}: Props = $props();
</script>

<div class="border-border space-y-3 rounded-lg border p-4">
	<h3 class="text-sm font-medium">{title}</h3>
	<Input
		type="text"
		placeholder="Search jobs to add..."
		oninput={onSearchInput}
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
		<Button type="button" variant="outline" onclick={onAdd} disabled={isSaving || selectedJobIds.length === 0}>
			Add requirement
		</Button>
	</div>
</div>
