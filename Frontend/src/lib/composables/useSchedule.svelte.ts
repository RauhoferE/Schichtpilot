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
    return `${String(date.getDate()).padStart(2, '0')}.${String(date.getMonth() + 1).padStart(2, '0')}.${String(date.getFullYear()).slice(2)}`;
}

function addDays(date: Date, n: number): Date {
    const d = new Date(date);
    d.setDate(d.getDate() + n);
    return d;
}

function toIso(date: Date): string {
    return date.toISOString().slice(0, 10);
}

// ── Map WorkScheduleDto → Week 

function mapScheduleToWeek(schedule: WorkScheduleDto, monday: Date): Week {
    const end = addDays(monday, 6);
    const label = `${fmtShort(monday)} – ${fmtShort(end)}`;

    // Group assignedUsers by user name
    const userMap = new Map<string, ShiftCode[]>();

    for (const assignment of schedule.assignedUsers) {
        const name = `${assignment.user.firstName} ${assignment.user.lastName}`;
        if (!userMap.has(name)) {
            userMap.set(name, [null, null, null, null, null, null, null]);
        }

        // Get day index (0=Mon ... 6=Sun)
        const assignDate = new Date(assignment.startTime);
        const jsDay = assignDate.getDay(); // 0=Sun, 1=Mon...
        const dayIndex = jsDay === 0 ? 6 : jsDay - 1; // convert to Mon=0

        // Find which shift this assignment belongs to
        const shift = schedule.shifts.find(s =>
            s.timeSlots?.some(ts => {
                const slotStart = new Date(assignment.startTime);
                const hour = slotStart.getHours();
                // Map shift name to ShiftCode
                return true;
            })
        );

        // Map shift name to ShiftCode
        let code: ShiftCode = null;
        if (shift) {
            const name_lower = shift.name.toLowerCase();
            if (name_lower.includes('lunch')) code = 'L';
            else if (name_lower.includes('evening')) code = 'E';
            else if (name_lower.includes('mixed')) code = 'M';
            else if (name_lower.includes('closed')) code = 'X';
            else code = 'E'; // fallback
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
        label: `${fmtShort(monday)} – ${fmtShort(end)}`,
        startDate: toIso(monday),
        staff: []
    };
}

// ── State ─────────────────────────────────────────────────────────────────────

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
            const isoDate = monday.toISOString().slice(0, 10); // "2026-06-15"
            scheduleData = await getactiveSchedule(new Date(isoDate + 'T12:00:00'));
        } catch {
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