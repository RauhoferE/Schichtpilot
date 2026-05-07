import { goto } from '$app/navigation';
import type { AbsenceDto, AbsenceStatus, AbsenceType } from '$lib/types/absence';

// ── Mock data ─────────────────────────────────────────────────────────────────
function getMockAbsences(): AbsenceDto[] {
    const today = new Date();
    const y = today.getFullYear();
    const m = String(today.getMonth() + 1).padStart(2, '0');
    return [
        {
            id: 1, userId: 1,
            startDate: `${y}-${m}-12`, endDate: `${y}-${m}-14`,
            absenceType: 'SickLeave', message: '', status: 'Pending',
            createdAt: `${y}-${m}-10T08:00:00Z`, managerMessage: ''
        },
        {
            id: 2, userId: 1,
            startDate: `${y}-${m}-11`, endDate: `${y}-${m}-11`,
            absenceType: 'CompensationTime', message: '', status: 'Denied',
            createdAt: `${y}-${m}-09T08:00:00Z`, managerMessage: 'Not enough notice'
        },
        {
            id: 3, userId: 1,
            startDate: `${y}-${m}-05`, endDate: `${y}-${m}-10`,
            absenceType: 'Vacation', message: 'Family trip', status: 'Approved',
            createdAt: `${y}-${m}-01T08:00:00Z`, managerMessage: 'Enjoy!'
        },
    ];
}

// ── Labels & styles ───────────────────────────────────────────────────────────
export const ABSENCE_TYPE_LABELS: Record<AbsenceType, string> = {
    Vacation:         'Vacation',
    SickLeave:        'Sick Leave',
    ParentalLeave:    'Parental Leave',
    CompensationTime: 'Compensation Time',
    PersonalLeave:    'Personal Leave',
    Other:            'Other',
};

export const ABSENCE_TYPES = Object.keys(ABSENCE_TYPE_LABELS) as AbsenceType[];

export const STATUS_STYLES: Record<AbsenceStatus, string> = {
    Pending:  'bg-yellow-100 text-yellow-800 border-yellow-200',
    Approved: 'bg-green-100  text-green-800  border-green-200',
    Denied:   'bg-red-100    text-red-800    border-red-200',
};

export const CALENDAR_DOT: Record<AbsenceStatus, string> = {
    Pending:  'bg-yellow-400',
    Approved: 'bg-green-500',
    Denied:   'bg-red-400',
};

// ── Helpers ───────────────────────────────────────────────────────────────────
export function fmtDate(iso: string): string {
    const d = new Date(iso);
    return `${d.getDate()}.${d.getMonth() + 1}.${String(d.getFullYear()).slice(2)}`;
}

export function todayIso(): string {
    return new Date().toISOString().slice(0, 10);
}

function dateRange(start: string, end: string): string[] {
    const dates: string[] = [];
    const cur  = new Date(start);
    const last = new Date(end);
    while (cur <= last) {
        dates.push(cur.toISOString().slice(0, 10));
        cur.setDate(cur.getDate() + 1);
    }
    return dates;
}

// ── Absence list state (calendar + table + dialog) ────────────────────────────
export function createAbsenceState() {
    const now = new Date();

    // ── Data ──
    let absences     = $state<AbsenceDto[]>(getMockAbsences());
    let selectedId   = $state<number | null>(null);
    let isLoading    = $state(false);
    let errorMessage = $state('');

    // ── Calendar ──
    let calendarYear  = $state(now.getFullYear());
    let calendarMonth = $state(now.getMonth());

    const calendarMap = $derived.by(() => {
        const map = new Map<string, AbsenceStatus>();
        for (const a of absences) {
            for (const d of dateRange(a.startDate, a.endDate)) {
                if (!map.has(d) || a.status === 'Approved') map.set(d, a.status);
            }
        }
        return map;
    });

    const selectedAbsence = $derived(absences.find(a => a.id === selectedId) ?? null);

    // ── Dialog / form ──
    let dialogOpen = $state(false);
    let form = $state({
        absenceType: 'Vacation' as AbsenceType,
        startDate: '',
        endDate: '',
        message: '',
    });

    // ── Calendar helpers ──
    function prevMonth() {
        if (calendarMonth === 0) { calendarMonth = 11; calendarYear--; }
        else calendarMonth--;
    }
    function nextMonth() {
        if (calendarMonth === 11) { calendarMonth = 0; calendarYear++; }
        else calendarMonth++;
    }
    function daysInMonth(year: number, month: number) {
        return new Date(year, month + 1, 0).getDate();
    }
    function firstDayOfMonth(year: number, month: number) {
        return new Date(year, month, 1).getDay();
    }
    function selectDay(dateStr: string) {
        const match = absences.find(a => {
            const d = new Date(dateStr);
            return d >= new Date(a.startDate) && d <= new Date(a.endDate);
        });
        selectedId = match?.id ?? null;
    }
    function selectAbsence(id: number) {
        selectedId = selectedId === id ? null : id;
        if (selectedId !== null) {
            const a = absences.find(x => x.id === id);
            if (a) {
                const d = new Date(a.startDate);
                calendarYear  = d.getFullYear();
                calendarMonth = d.getMonth();
            }
        }
    }

    // ── Dialog actions ──
    function openDialog() {
        form = { absenceType: 'Vacation', startDate: '', endDate: '', message: '' };
        errorMessage = '';
        dialogOpen = true;
    }

    async function submitAbsence() {
        errorMessage = '';

        if (!form.startDate || !form.endDate) {
            errorMessage = 'Please select both a start and end date.';
            return;
        }
        const today = todayIso();
        if (form.startDate < today) {
            errorMessage = 'Start date cannot be in the past.';
            return;
        }
        if (form.endDate < today) {
            errorMessage = 'End date cannot be in the past.';
            return;
        }
        if (form.endDate < form.startDate) {
            errorMessage = 'End date must be on or after the start date.';
            return;
        }
        if (form.message.length > 250) {
            errorMessage = 'Message must be 250 characters or fewer.';
            return;
        }

        isLoading = true;
        try {
            // TODO: swap mock for real API call
            // const res = await fetch('/api/employee/absences', {
            //     method: 'POST',
            //     headers: { 'Content-Type': 'application/json' },
            //     body: JSON.stringify(form),
            //     credentials: 'include',
            // });
            // if (!res.ok) throw new Error((await res.json()).message);
            // const newAbsence: AbsenceDto = await res.json();

            await new Promise(r => setTimeout(r, 600)); // simulate network
            const newAbsence: AbsenceDto = {
                id: Date.now(),
                userId: 1,
                absenceType: form.absenceType,
                startDate: form.startDate,
                endDate: form.endDate,
                message: form.message,
                status: 'Pending',
                createdAt: new Date().toISOString(),
                managerMessage: '',
            };

            absences = [newAbsence, ...absences];
            dialogOpen = false;
            form = { absenceType: 'Vacation', startDate: '', endDate: '', message: '' };
        } catch (e: unknown) {
            errorMessage = e instanceof Error ? e.message : 'Failed to submit. Please try again.';
        } finally {
            isLoading = false;
        }
    }

    function addAbsence(a: AbsenceDto) {
        absences = [a, ...absences];
    }

    return {
        // data
        get absences()        { return absences; },
        get selectedId()      { return selectedId; },
        get selectedAbsence() { return selectedAbsence; },
        get isLoading()       { return isLoading; },
        get errorMessage()    { return errorMessage; },

        // calendar
        get calendarYear()    { return calendarYear; },
        get calendarMonth()   { return calendarMonth; },
        get calendarMap()     { return calendarMap; },
        daysInMonth, firstDayOfMonth,
        prevMonth, nextMonth,
        selectDay, selectAbsence,

        // dialog
        get dialogOpen()           { return dialogOpen; },
        set dialogOpen(v: boolean) { dialogOpen = v; },
        get form()                 { return form; },
        openDialog, submitAbsence, addAbsence,
    };
}