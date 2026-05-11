<script lang="ts">
    import { Button } from '$lib/components/ui/button/index.js';
    import {
        createScheduleState,
        SHIFT_META,
        DAY_HEADERS,
    } from '$lib/composables/useSchedule.svelte';

    const s = createScheduleState();

    let exportOpen = $state(false);

    function toggleExport() {
        exportOpen = !exportOpen;
    }

    function closeExport() {
        exportOpen = false;
    }

    import jsPDF from 'jspdf';
    import autoTable from 'jspdf-autotable';

    function exportPdf() {
        closeExport();
        const doc = new jsPDF();

        autoTable(doc, {
            head: [['Staff', ...DAY_HEADERS]],
            body: s.week.staff.map(member => [
                member.name,
                ...member.days.map(d => d.shift ?? '—')
            ]),
            headStyles: { fillColor: [30, 30, 30] },
        });

        doc.save(`schedule-${s.week.label}.pdf`);
    }
    

    function exportExcel() {
        closeExport();
        const rows = [
            ['Staff', ...DAY_HEADERS],
            ...s.week.staff.map(member => [
                member.name,
                ...member.days.map(d => d.shift ?? '—')
            ])
        ];
        const csvContent = rows.map(r => r.join(',')).join('\n');
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `schedule-${s.week.label}.csv`;
        a.click();
        URL.revokeObjectURL(url);
    }

    function printSchedule() {
        window.print();
    }
</script>

<svelte:head>
    <title>Overview — SchichtPilot</title>
    <style>
        @media print {
            body * { visibility: hidden; }
            #schedule-grid, #schedule-grid * { visibility: visible; }
            #schedule-grid { position: absolute; top: 0; left: 0; width: 100%; }
        }
    </style>
</svelte:head>

<div class="space-y-4">

    <!-- ── Page header ── -->
    <div class="flex items-center justify-between">
        <h1 class="text-xl font-semibold">Overview</h1>
        <div class="flex gap-2">

            <!-- Export dropdown -->
            <div class="relative">
                <Button variant="outline" onclick={toggleExport}>
                    <svg class="w-4 h-4 mr-2" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                        <polyline points="7 10 12 15 17 10"/>
                        <line x1="12" y1="15" x2="12" y2="3"/>
                    </svg>
                    Export
                    <svg class="w-3 h-3 ml-2" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="m6 9 6 6 6-6"/>
                    </svg>
                </Button>

                {#if exportOpen}
                    <!-- Backdrop to close on outside click -->
                    <button
                            class="fixed inset-0 z-10"
                            onclick={closeExport}
                            aria-label="Close menu"
                    ></button>

                    <div class="absolute right-0 mt-1 w-48 rounded-md border bg-card shadow-md z-20">
                        <button
                                onclick={exportPdf}
                                class="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-muted transition-colors rounded-t-md"
                        >
                            <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                                <polyline points="14 2 14 8 20 8"/>
                            </svg>
                            Export as PDF
                        </button>
                        <button
                                onclick={exportExcel}
                                class="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-muted transition-colors rounded-b-md"
                        >
                            <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                                <polyline points="14 2 14 8 20 8"/>
                                <line x1="8" y1="13" x2="16" y2="13"/>
                                <line x1="8" y1="17" x2="16" y2="17"/>
                            </svg>
                            Export as Excel (.csv)
                        </button>
                    </div>
                {/if}
            </div>

            <!-- Print button -->
            <Button onclick={printSchedule}>
                <svg class="w-4 h-4 mr-2" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="6 9 6 2 18 2 18 9"/>
                    <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/>
                    <rect x="6" y="14" width="12" height="8"/>
                </svg>
                Print
            </Button>

        </div>
    </div>

    <!-- ── Legend ── -->
    <div class="flex flex-wrap gap-3 rounded-lg border bg-card p-3">
        {#each Object.entries(SHIFT_META) as [code, meta]}
            <div class="flex items-center gap-2 text-sm">
                <span class="w-6 h-6 rounded flex items-center justify-center text-xs font-bold {meta.bg} {meta.text}">
                    {code}
                </span>
                <span class="text-muted-foreground">
                    {meta.label}{meta.hours ? ` ${meta.hours}` : ''}
                </span>
            </div>
        {/each}
        <div class="flex items-center gap-2 text-sm">
            <span class="w-6 h-6 rounded border border-border bg-white flex items-center justify-center text-xs text-muted-foreground">
                —
            </span>
            <span class="text-muted-foreground">Not assigned</span>
        </div>
    </div>

    <!-- ── Week navigation + grid ── -->
    <div id="schedule-grid" class="rounded-lg border bg-card overflow-hidden">

        <!-- Week header + navigation -->
        <div class="flex items-center justify-between px-4 py-3 border-b bg-muted/30">
            <button
                    onclick={s.prevWeek}
                    class="p-1.5 rounded hover:bg-muted transition-colors"
                    aria-label="Previous week"
            >
                <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="m15 18-6-6 6-6"/>
                </svg>
            </button>

            <span class="text-sm font-semibold">{s.week.label}</span>

            <button
                    onclick={s.nextWeek}
                    class="p-1.5 rounded hover:bg-muted transition-colors"
                    aria-label="Next week"
            >
                <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="m9 18 6-6-6-6"/>
                </svg>
            </button>
        </div>

        <!-- Schedule table -->
        <div class="overflow-x-auto">
            <table class="w-full text-sm">
                <thead>
                <tr class="border-b bg-muted/20">
                    <th class="text-left px-4 py-2 font-semibold w-28">Staff</th>
                    {#each DAY_HEADERS as day}
                        <th class="text-center px-2 py-2 font-semibold">{day}</th>
                    {/each}
                </tr>
                </thead>
                <tbody>
                {#each s.week.staff as member}
                    <tr class="border-b last:border-0 hover:bg-muted/20 transition-colors">
                        <td class="px-4 py-2 font-medium">{member.name}</td>
                        {#each member.days as day}
                            <td class="px-2 py-2 text-center">
                                {#if day.shift}
                                        <span class="inline-flex items-center justify-center w-8 h-8 rounded text-xs font-bold
                                            {SHIFT_META[day.shift].bg} {SHIFT_META[day.shift].text}">
                                            {day.shift}
                                        </span>
                                {:else}
                                        <span class="inline-flex items-center justify-center w-8 h-8 rounded border border-dashed border-border text-muted-foreground text-xs">
                                            —
                                        </span>
                                {/if}
                            </td>
                        {/each}
                    </tr>
                {/each}
                </tbody>
            </table>
        </div>
    </div>

</div>