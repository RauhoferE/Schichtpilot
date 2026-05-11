<script lang="ts">
    import * as Dialog from '$lib/components/ui/dialog/index.js';
    import * as Table  from '$lib/components/ui/table/index.js';
    import * as Alert  from '$lib/components/ui/alert/index.js';
    import { Button }  from '$lib/components/ui/button/index.js';
    import { Input }   from '$lib/components/ui/input/index.js';
    import { Label }   from '$lib/components/ui/label/index.js';
    import {
        createManagerAbsenceState,
        ABSENCE_TYPE_LABELS,
        ABSENCE_TYPES,
        STATUS_STYLES,
        CALENDAR_DOT,
        fmtDate,
    } from '$lib/composables/useManagerAbsence.svelte';

    const MONTH_NAMES = [
        'January','February','March','April','May','June',
        'July','August','September','October','November','December'
    ];
    const DAY_HEADERS = ['Su','Mo','Tu','We','Th','Fr','Sa'];

    const s = createManagerAbsenceState();

    function buildCalendarGrid(year: number, month: number) {
        const totalDays = s.daysInMonth(year, month);
        const startDay  = s.firstDayOfMonth(year, month);
        const cells: Array<{ day: number | null; dateStr: string | null }> = [];
        for (let i = 0; i < startDay; i++) cells.push({ day: null, dateStr: null });
        for (let d = 1; d <= totalDays; d++) {
            const dateStr = `${year}-${String(month + 1).padStart(2,'0')}-${String(d).padStart(2,'0')}`;
            cells.push({ day: d, dateStr });
        }
        return cells;
    }

    function isSelectedDay(dateStr: string | null): boolean {
        if (!dateStr || !s.selectedAbsence) return false;
        const d = new Date(dateStr);
        return d >= new Date(s.selectedAbsence.startDate) && d <= new Date(s.selectedAbsence.endDate);
    }
</script>

<svelte:head><title>Absence Management — SchichtPilot</title></svelte:head>

<div class="space-y-4">

    <!-- Page header -->
    <div class="flex items-center justify-between">
        <h1 class="text-xl font-semibold">Absence Management</h1>
        <Button variant="outline" onclick={s.loadAbsences} disabled={s.isLoading}>
            <svg class="w-4 h-4 mr-2 {s.isLoading ? 'animate-spin' : ''}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
            </svg>
            Refresh
        </Button>
    </div>

    <!-- Success toast -->
    {#if s.successMessage}
        <Alert.Root class="border-green-500 bg-green-50 text-green-800">
            <svg class="h-4 w-4 text-green-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M20 6 9 17l-5-5"/>
            </svg>
            <Alert.Description class="text-green-800">{s.successMessage}</Alert.Description>
        </Alert.Root>
    {/if}

    <!-- Fetch error -->
    {#if s.fetchError}
        <Alert.Root variant="destructive">
            <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="8" x2="12" y2="12"/>
                <line x1="12" y1="16" x2="12.01" y2="16"/>
            </svg>
            <Alert.Description>{s.fetchError}</Alert.Description>
        </Alert.Root>
    {/if}

    {#if !s.fetchError}

        <!-- ── Filter bar: 4 fields in one row ── -->
        <div class="rounded-lg border bg-card p-4 space-y-3">
            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">

                <!-- 1. Employee name search -->
                <div class="space-y-1">
                    <Label class="text-xs text-muted-foreground">Employee</Label>
                    <Input
                            placeholder="Search by name…"
                            bind:value={s.filters.search}
                    />
                </div>

                <!-- 2. Absence type -->
                <div class="space-y-1">
                    <Label class="text-xs text-muted-foreground">Type</Label>
                    <select
                            class="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                            bind:value={s.filters.absenceType}
                    >
                        <option value="">All types</option>
                        {#each ABSENCE_TYPES as t}
                            <option value={t}>{ABSENCE_TYPE_LABELS[t]}</option>
                        {/each}
                    </select>
                </div>

                <!-- 3. Absence date: show rows where this date falls inside the absence range -->
                <div class="space-y-1">
                    <Label class="text-xs text-muted-foreground">Absence date</Label>
                    <Input type="date" bind:value={s.filters.absenceDate} />
                </div>

                <!-- 4. Created on: show rows created on this exact date -->
                <div class="space-y-1">
                    <Label class="text-xs text-muted-foreground">Created on</Label>
                    <Input type="date" bind:value={s.filters.createdOn} />
                </div>

            </div>

            <!-- Clear filters button, only visible when at least one filter is active -->
            {#if s.hasActiveFilters}
                <div class="flex justify-end">
                    <Button variant="ghost" class="text-muted-foreground" onclick={s.clearFilters}>
                        <svg class="w-4 h-4 mr-1" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M18 6 6 18M6 6l12 12"/>
                        </svg>
                        Clear filters
                    </Button>
                </div>
            {/if}
        </div>

        <!-- ── Main layout: calendar + table ── -->
        <div class="grid grid-cols-1 md:grid-cols-[280px_1fr] gap-6 items-start">

            <!-- Calendar -->
            <div class="rounded-lg border bg-card p-4 space-y-3">
                <div class="flex items-center justify-between">
                    <button onclick={s.prevMonth} aria-label="Previous month"
                            class="p-1 rounded hover:bg-muted transition-colors">
                        <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="m15 18-6-6 6-6"/></svg>
                    </button>
                    <span class="text-sm font-semibold">
                        {MONTH_NAMES[s.calendarMonth]} {s.calendarYear}
                    </span>
                    <button onclick={s.nextMonth} aria-label="Next month"
                            class="p-1 rounded hover:bg-muted transition-colors">
                        <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="m9 18 6-6-6-6"/></svg>
                    </button>
                </div>

                <div class="grid grid-cols-7 text-center">
                    {#each DAY_HEADERS as h}
                        <span class="text-xs text-muted-foreground font-medium py-1">{h}</span>
                    {/each}
                </div>

                <div class="grid grid-cols-7 gap-y-1 text-center">
                    {#each buildCalendarGrid(s.calendarYear, s.calendarMonth) as cell}
                        {#if cell.day === null}
                            <span></span>
                        {:else}
                            {@const status   = cell.dateStr ? s.calendarMap.get(cell.dateStr) : undefined}
                            {@const selected = isSelectedDay(cell.dateStr)}
                            <button
                                    onclick={() => cell.dateStr && s.selectDay(cell.dateStr)}
                                    class="relative mx-auto w-8 h-8 rounded-full text-xs flex items-center justify-center transition-colors
                                    {selected ? 'bg-primary text-primary-foreground font-semibold' : 'hover:bg-muted'}
                                    {status && !selected ? 'font-medium' : ''}"
                                    aria-label={cell.dateStr ?? ''}
                            >
                                {cell.day}
                                {#if status && !selected}
                                    <span class="absolute bottom-0.5 left-1/2 -translate-x-1/2 w-1.5 h-1.5 rounded-full {CALENDAR_DOT[status]}"></span>
                                {/if}
                            </button>
                        {/if}
                    {/each}
                </div>

                <div class="border-t pt-3 space-y-1.5">
                    {#each [['Approved','bg-green-500'],['Denied','bg-red-400'],['Pending','bg-yellow-400']] as [label, cls]}
                        <div class="flex items-center gap-2 text-xs text-muted-foreground">
                            <span class="w-3 h-3 rounded-sm {cls}"></span>
                            {label} absence
                        </div>
                    {/each}
                </div>
            </div>

            <!-- Table -->
            <div class="rounded-lg border bg-card overflow-hidden">
                {#if s.isLoading}
                    <div class="p-8 flex flex-col items-center gap-3 text-muted-foreground">
                        <svg class="w-6 h-6 animate-spin" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
                        </svg>
                        <span class="text-sm">Loading absence requests…</span>
                    </div>
                {:else}
                    <Table.Root>
                        <Table.Header>
                            <Table.Row>
                                <Table.Head>Status</Table.Head>
                                <Table.Head>Type</Table.Head>
                                <Table.Head>Person</Table.Head>
                                <Table.Head>Absence dates</Table.Head>
                                <Table.Head class="hidden lg:table-cell">Created</Table.Head>
                                <Table.Head class="hidden lg:table-cell">Note</Table.Head>
                                <Table.Head class="w-20">Actions</Table.Head>
                            </Table.Row>
                        </Table.Header>
                        <Table.Body>
                            {#each s.filteredAbsences as absence (absence.id)}
                                {@const isSelected = s.selectedId === absence.id}
                                <Table.Row
                                        onclick={() => s.selectAbsence(absence.id)}
                                        class="cursor-pointer transition-colors
                                        {isSelected ? 'bg-primary/10 hover:bg-primary/15' : 'hover:bg-muted/50'}"
                                        aria-selected={isSelected}
                                >
                                    <Table.Cell>
                                        <span class="inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold
                                            {STATUS_STYLES[absence.status]}">
                                            {absence.status}
                                        </span>
                                    </Table.Cell>
                                    <Table.Cell class="font-medium">
                                        {ABSENCE_TYPE_LABELS[absence.absenceType]}
                                    </Table.Cell>
                                    <Table.Cell class="text-sm text-muted-foreground">
                                        {absence.employeeName}
                                    </Table.Cell>
                                    <Table.Cell class="text-sm tabular-nums text-muted-foreground">
                                        {fmtDate(absence.startDate)}
                                        {#if absence.startDate !== absence.endDate}
                                            – {fmtDate(absence.endDate)}
                                        {/if}
                                    </Table.Cell>
                                    <Table.Cell class="hidden lg:table-cell text-sm text-muted-foreground">
                                        {fmtDate(absence.createdAt)}
                                    </Table.Cell>
                                    <Table.Cell class="hidden lg:table-cell text-sm text-muted-foreground max-w-40 truncate">
                                        {absence.managerMessage || '—'}
                                    </Table.Cell>
                                    <Table.Cell>
                                      <div class="flex items-center gap-1" onclick={(e) => e.stopPropagation()}>
                                           <!--   Approve -->
                                            <button
                                                    onclick={() => s.openConfirm(absence.id, 'Approved')}
                                                    disabled={absence.status === 'Approved'}
                                                    title="Approve"
                                                    class="p-1.5 rounded transition-colors
                                                    {absence.status === 'Approved'
                                                        ? 'text-green-500 cursor-default'
                                                        : 'hover:bg-green-100 text-muted-foreground hover:text-green-600'}"
                                            >
                                                <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                                                    <path d="M20 6 9 17l-5-5"/>
                                                </svg>
                                            </button>
                                            <!-- Deny -->
                                            <button
                                                    onclick={() => s.openConfirm(absence.id, 'Denied')}
                                                    disabled={absence.status === 'Denied'}
                                                    title="Deny"
                                                    class="p-1.5 rounded transition-colors
                                                    {absence.status === 'Denied'
                                                        ? 'text-red-400 cursor-default'
                                                        : 'hover:bg-red-100 text-muted-foreground hover:text-red-500'}"
                                            >
                                                <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                                                    <path d="M18 6 6 18M6 6l12 12"/>
                                                </svg>
                                            </button>
                                       </div>
                                    </Table.Cell>
                                </Table.Row>
                            {/each}

                            {#if s.filteredAbsences.length === 0}
                                <Table.Row>
                                    <Table.Cell colspan={7} class="text-center py-12 text-muted-foreground">
                                        {s.hasActiveFilters
                                            ? 'No absences match your filters.'
                                            : 'No absence requests yet.'}
                                    </Table.Cell>
                                </Table.Row>
                            {/if}
                        </Table.Body>
                    </Table.Root>

                    {#if s.filteredAbsences.length > 0}
                        <div class="px-4 py-2 border-t text-xs text-muted-foreground">
                            Showing {s.filteredAbsences.length} of {s.allAbsences.length} requests
                        </div>
                    {/if}
                {/if}
            </div>
        </div>
    {/if}
</div>

<!-- ── Confirm dialog (Approve / Deny) ── -->
<Dialog.Root bind:open={s.confirmDialog.open}>
    <Dialog.Content class="sm:max-w-sm">
        <Dialog.Header>
            <Dialog.Title>
                {s.confirmDialog.action === 'Approved' ? 'Approve absence?' : 'Deny absence?'}
            </Dialog.Title>
            <Dialog.Description>
                {s.confirmDialog.action === 'Approved'
                    ? 'You can optionally leave a message for the employee.'
                    : 'You must provide a reason for denying this request.'}
            </Dialog.Description>
        </Dialog.Header>

        <!-- Message field: optional for Approve, mandatory for Deny -->
        <div class="space-y-2 py-2">
            <Label for="managerMessage">
                Message
                {#if s.confirmDialog.action === 'Denied'}
                    <span class="text-destructive ml-0.5">*</span>
                {:else}
                    <span class="text-muted-foreground text-xs ml-1">(optional)</span>
                {/if}
            </Label>
            <textarea
                    id="managerMessage"
                    rows={3}
                    maxlength={250}
                    placeholder={s.confirmDialog.action === 'Denied'
                    ? 'Reason for denial…'
                    : 'Any message for the employee…'}
                    class="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs resize-none focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring
                    {s.confirmDialogError ? 'border-destructive focus-visible:ring-destructive' : ''}"
                    bind:value={s.confirmDialog.message}
            ></textarea>
            <!-- Validation error, only shown when Deny is submitted without a message -->
            {#if s.confirmDialogError}
                <p class="text-xs text-destructive">{s.confirmDialogError}</p>
            {/if}
        </div>

        <Dialog.Footer>
            <Button variant="outline" onclick={s.closeConfirm}>Cancel</Button>
            <Button
                    variant={s.confirmDialog.action === 'Approved' ? 'default' : 'destructive'}
                    onclick={s.applyDecision}
            >
                {s.confirmDialog.action === 'Approved' ? 'Approve' : 'Deny'}
            </Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>