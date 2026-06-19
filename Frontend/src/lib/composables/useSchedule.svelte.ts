import type { ShiftCode, StaffSchedule, Week, WorkScheduleDto } from '$lib/types/schedule.types';
import { getactiveSchedule } from '$lib/services/workschedule.service';

export const DAY_HEADERS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

export const SHIFT_META: Record<NonNullable<ShiftCode>, { label: string; hours: string; bg: string; text: string }> = {
    L: { label: 'Lunch Shift',   hours: '10:00–17:00', bg: 'bg-yellow-200', text: 'text-yellow-900' },
    E: { label: 'Evening Shift', hours: '16:00–23:00', bg: 'bg-blue-200',   text: 'text-blue-900'   },
    M: { label: 'Mixed Shift',   hours: '13:00–20:00', bg: 'bg-purple-200', text: 'text-purple-900' },
    V: { label: 'Day off',       hours: '',            bg: 'bg-gray-200',   text: 'text-gray-600'   },
    X: { label: 'Closed',        hours: '',            bg: 'bg-green-200',  text: ''                },
};

// ── Helpers

function mondayOf(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = day === 0 ? -6 : 1 - day;
    d.setDate(d.getDate() + diff);
    d.setHours(0, 0, 0, 0);
    return d;
}

function fmtShort(date: Date): string {
    return `${String(date.getDate()).padStart(2, '0')}.${String(date.getMonth() + 1).padStart(2, '0')}.${date.getFullYear()}`;
}

function addDays(date: Date, n: number): Date {
    const d = new Date(date);
    d.setDate(d.getDate() + n);
    return d;
}

// Zeitzonen-sicher: baut "YYYY-MM-DD" aus den LOKALEN Datumsteilen,
// statt toISOString() zu verwenden (das konvertiert nach UTC und
// verschiebt das Datum je nach Zeitzone um einen Tag).
function toIso(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

function getIsoWeekNumber(date: Date): number {
    const target = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    const dayNumber = target.getUTCDay() || 7;
    target.setUTCDate(target.getUTCDate() + 4 - dayNumber);
    const yearStart = new Date(Date.UTC(target.getUTCFullYear(), 0, 1));
    return Math.ceil((((target.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
}

function buildLabel(monday: Date, end: Date): string {
    return `KW ${getIsoWeekNumber(monday)}: ${fmtShort(monday)}-${fmtShort(end)}`;
}

// ── Map WorkScheduleDto → Week

function mapScheduleToWeek(schedule: WorkScheduleDto, monday: Date): Week {
    const end = addDays(monday, 6);
    const label = buildLabel(monday, end);

    const userMap = new Map<string, ShiftCode[]>();

    for (const assignment of schedule.assignedUsers) {
        const name = `${assignment.user.firstName} ${assignment.user.lastName}`;
        if (!userMap.has(name)) {
            userMap.set(name, [null, null, null, null, null, null, null]);
        }

        const assignDate = new Date(assignment.startTime);
        const jsDay = assignDate.getDay(); // 0=Sun, 1=Mon...
        const dayIndex = jsDay === 0 ? 6 : jsDay - 1; // convert to Mon=0

        // Exaktes Matching über die Timeslot-ID statt über Uhrzeiten raten
        const shift = schedule.shifts.find(s =>
            s.timeSlots?.some(ts => ts.id === assignment.timeSlot.id)
        );

        let code: ShiftCode = null;
        if (shift) {
            const name_lower = shift.name.toLowerCase();
            if (name_lower.includes('lunch')) code = 'L';
            else if (name_lower.includes('evening')) code = 'E';
            else if (name_lower.includes('mixed')) code = 'M';
            else if (name_lower.includes('closed')) code = 'X';
            else code = 'E';
        }

        userMap.get(name)![dayIndex] = code;
    }

    const staff: StaffSchedule[] = Array.from(userMap.entries()).map(([name, shifts]) => ({
        name,
        days: shifts.map((shift, i) => ({
            date: toIso(addDays(monday, i)),
            shift,
        })),
    }));

    return { label, startDate: toIso(monday), staff };
}

function buildEmptyWeek(monday: Date): Week {
    const end = addDays(monday, 6);
    return {
        label: buildLabel(monday, end),
        startDate: toIso(monday),
        staff: []
    };
}

// state

export function createScheduleState() {
    let currentMonday = $state(mondayOf(new Date()));
    let scheduleData = $state<WorkScheduleDto | null>(null);
    let loading = $state(false);
    let errorMessage = $state('');

    const week = $derived(
        scheduleData
            ? mapScheduleToWeek(scheduleData, currentMonday)
            : buildEmptyWeek(currentMonday)
    );

    async function loadWeek(monday: Date) {
        loading = true;
        errorMessage = '';
        try {
            const isoDate = toIso(monday);
            scheduleData = await getactiveSchedule(new Date(isoDate + 'T12:00:00'));
        } catch (e) {
            console.log('loadWeek error:', e);
            scheduleData = null;
            errorMessage = 'No schedule found for this week.';
        } finally {
            loading = false;
        }
    }

    async function prevWeek() {
        currentMonday = addDays(currentMonday, -7);
        await loadWeek(currentMonday);
    }

    async function nextWeek() {
        currentMonday = addDays(currentMonday, 7);
        await loadWeek(currentMonday);
    }

    function downloadPdf() {
        window.print();
    }

    return {
        get week()          { return week; },
        get currentMonday() { return currentMonday; },
        get loading()       { return loading; },
        get errorMessage()  { return errorMessage; },
        prevWeek,
        nextWeek,
        downloadPdf,
        loadWeek,
    };
}