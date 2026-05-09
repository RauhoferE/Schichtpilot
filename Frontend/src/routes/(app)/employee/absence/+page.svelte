<script lang="ts">
    import * as Dialog from '$lib/components/ui/dialog/index.js';
    import * as Table  from '$lib/components/ui/table/index.js';
    import * as Alert  from '$lib/components/ui/alert/index.js';
    import { Button }  from '$lib/components/ui/button/index.js';
    import { Input }   from '$lib/components/ui/input/index.js';
    import { Label }   from '$lib/components/ui/label/index.js';
    import {
        createAbsenceState,
        ABSENCE_TYPE_LABELS,
        ABSENCE_TYPES,
        STATUS_STYLES,
        CALENDAR_DOT,
        fmtDate,
        todayIso,
    } from '$lib/composables/useAbsence.svelte';

    const MONTH_NAMES = [
        'January','February','March','April','May','June',
        'July','August','September','October','November','December'
    ];
    const DAY_HEADERS = ['Su','Mo','Tu','We','Th','Fr','Sa'];

    const s = createAbsenceState();

    // Build calendar grid for current month
    function buildCalendarGrid(year: number, month: number) {
        const totalDays  = s.daysInMonth(year, month);
        const startDay   = s.firstDayOfMonth(year, month);
        const cells: Array<{ day: number | null; dateStr: string | null }> = [];
        for (let i = 0; i < startDay; i++) cells.push({ day: null, dateStr: null });
        for (let d = 1; d <= totalDays; d++) {
            const dateStr = `${year}-${String(month + 1).padStart(2,'0')}-${String(d).padStart(2,'0')}`;
            cells.push({ day: d, dateStr });
        }
        return cells;
    }

    // Is this dateStr part of the selected absence?
    function isSelectedDay(dateStr: string | null): boolean {
        if (!dateStr || !s.selectedAbsence) return false;
        const d = new Date(dateStr);
        return d >= new Date(s.selectedAbsence.startDate) && d <= new Date(s.selectedAbsence.endDate);
    }
</script>

<svelte:head><title>Absence — SchichtPilot</title></svelte:head>

<div class="space-y-4">

    <!-- Page header -->
    <div class="flex items-center justify-between">
        <h1 class="text-xl font-semibold">My Absence</h1>
        <Button onclick={() => s.openDialog()}>+ Add new absence</Button>
    </div>
    
    <!-- Success toast: shown for 3s after a request is submitted -->
    {#if s.successMessage}
        <Alert.Root class="border-green-500 bg-green-50 text-green-800">
            <svg class="h-4 w-4 text-green-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                <path d="M20 6 9 17l-5-5"/>
            </svg>
            <Alert.Description class="text-green-800">{s.successMessage}</Alert.Description>
        </Alert.Root>
    {/if}
    
    <!-- Main layout: calendar (left) + table (right) -->
    <div class="grid grid-cols-1 md:grid-cols-[280px_1fr] gap-6 items-start">

        <!-- ── Calendar ── -->
        <div class="rounded-lg border bg-card p-4 space-y-3">

            <!-- Month navigation -->
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

            <!-- Day headers -->
            <div class="grid grid-cols-7 text-center">
                {#each DAY_HEADERS as h}
                    <span class="text-xs text-muted-foreground font-medium py-1">{h}</span>
                {/each}
            </div>

            <!-- Day cells -->
            <div class="grid grid-cols-7 gap-y-1 text-center">
                {#each buildCalendarGrid(s.calendarYear, s.calendarMonth) as cell}
                    {#if cell.day === null}
                        <span></span>
                    {:else}
                        {@const status = cell.dateStr ? s.calendarMap.get(cell.dateStr) : undefined}
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

            <!-- Legend -->
            <div class="border-t pt-3 space-y-1.5">
                {#each [['Approved','bg-green-500'],['Denied','bg-red-400'],['Pending','bg-yellow-400']] as [label, cls]}
                    <div class="flex items-center gap-2 text-xs text-muted-foreground">
                        <span class="w-3 h-3 rounded-sm {cls}"></span>
                        {label} absence
                    </div>
                {/each}
            </div>
        </div>

        <!-- ── Absence table ── -->
        <div class="rounded-lg border bg-card overflow-hidden">
            <Table.Root>
                <Table.Header>
                    <Table.Row>
                        <Table.Head>Type</Table.Head>
                        <Table.Head>From – To</Table.Head>
                        <Table.Head>Approval</Table.Head>
                        <Table.Head class="hidden md:table-cell">Manager note</Table.Head>
                    </Table.Row>
                </Table.Header>
                <Table.Body>
                    {#each s.absences as absence (absence.id)}
                        {@const isSelected = s.selectedId === absence.id}
                        <Table.Row
                                onclick={() => s.selectAbsence(absence.id)}
                                class="cursor-pointer transition-colors
                                {isSelected ? 'bg-primary/10 hover:bg-primary/15' : 'hover:bg-muted/50'}"
                                aria-selected={isSelected}
                        >
                            <Table.Cell class="font-medium">
                                {ABSENCE_TYPE_LABELS[absence.absenceType]}
                            </Table.Cell>
                            <Table.Cell class="text-sm tabular-nums text-muted-foreground">
                                {fmtDate(absence.startDate)}
                                {#if absence.startDate !== absence.endDate}
                                    – {fmtDate(absence.endDate)}
                                {/if}
                            </Table.Cell>
                            <Table.Cell>
                                <span class="inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold
                                    {STATUS_STYLES[absence.status]}">
                                    {absence.status}
                                </span>
                            </Table.Cell>
                            <Table.Cell class="hidden md:table-cell text-sm text-muted-foreground max-w-[200px] truncate">
                                {absence.managerMessage || '—'}
                            </Table.Cell>
                        </Table.Row>
                    {/each}

                    {#if s.absences.length === 0}
                        <Table.Row>
                            <Table.Cell colspan={4} class="text-center py-12 text-muted-foreground">
                                No absence requests yet.
                            </Table.Cell>
                        </Table.Row>
                    {/if}
                </Table.Body>
            </Table.Root>
        </div>
    </div>
</div>

<!-- ── Add Absence Dialog ── -->
<Dialog.Root bind:open={s.dialogOpen}>
    <Dialog.Content class="sm:max-w-md">
        <Dialog.Header>
            <Dialog.Title>New Absence Request</Dialog.Title>
            <Dialog.Description>
                Submit a new absence. Your manager will review and approve or deny it.
            </Dialog.Description>
        </Dialog.Header>

        {#if s.errorMessage}
            <Alert.Root variant="destructive">
                <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                    <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <Alert.Description>{s.errorMessage}</Alert.Description>
            </Alert.Root>
        {/if}

        <div class="space-y-4 py-2">
            <!-- Type -->
            <div class="space-y-2">
                <Label for="absenceType">Type</Label>
                <select
                        id="absenceType"
                        class="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                        bind:value={s.form.absenceType}
                >
                    {#each ABSENCE_TYPES as t}
                        <option value={t}>{ABSENCE_TYPE_LABELS[t]}</option>
                    {/each}
                </select>
            </div>

            <!-- Dates  //date starts with today, past is not possible to book todayiso=min -->
            <div class="grid grid-cols-2 gap-4">
                <div class="space-y-2">
                    <Label for="startDate">Start Date</Label>
                    <Input id="startDate" type="date" min={todayIso()} bind:value={s.form.startDate} /> 
                </div>
                <div class="space-y-2">
                    <Label for="endDate">End Date</Label>
                    <Input id="endDate" type="date" min={todayIso()} bind:value={s.form.endDate} />
                </div>
            </div>

            <!-- Optional message -->
            <div class="space-y-2">
                <Label for="message">Message <span class="text-muted-foreground">(optional)</span></Label>
                <textarea
                        id="message"
                        rows={3}
                        maxlength={250}
                        placeholder="Any additional information for your manager…"
                        class="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs resize-none focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                        bind:value={s.form.message}
                ></textarea>
            </div>
        </div>

        <Dialog.Footer>
            <Dialog.Close>
                <Button variant="outline">Cancel</Button>
            </Dialog.Close>
            <Button onclick={s.submitAbsence} disabled={s.isLoading} aria-busy={s.isLoading}>
                {#if s.isLoading}
                    <span class="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden="true"></span>
                    Submitting…
                {:else}
                    Save request
                {/if}
            </Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>