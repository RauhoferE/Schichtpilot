import { goto } from '$app/navigation';
import type { AbsenceDto, AbsenceStatus, AbsenceType, CreateAbsenceDto } from '$lib/types/absence';

// ── Mock data ────────────
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

// ── Labels & styles ────────────────────────────────────────────────────────────
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

// ── Helpers ────────────────────────────────────────────────────────────────────
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

// ── Absence list state (calendar + table view) ─────────────────────────────────
export function createAbsenceState() {
    const now = new Date();
    let absences      = $state<AbsenceDto[]>(getMockAbsences());
    let selectedId    = $state<number | null>(null);
    let isLoading     = $state(false);
    let errorMessage  = $state('');

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

    // Called from /absence/new after a successful submit to add to the list
    function addAbsence(a: AbsenceDto) {
        absences = [a, ...absences];
    }

    return {
        get absences()        { return absences; },
        get selectedId()      { return selectedId; },
        get selectedAbsence() { return selectedAbsence; },
        get isLoading()       { return isLoading; },
        get errorMessage()    { return errorMessage; },
        get calendarYear()    { return calendarYear; },
        get calendarMonth()   { return calendarMonth; },
        get calendarMap()     { return calendarMap; },
        daysInMonth, firstDayOfMonth,
        prevMonth, nextMonth,
        selectDay, selectAbsence, addAbsence,
    };
}

// ── New absence form state (/absence/new) ──────────────────────────────────────
export function createNewAbsenceState() {
    let absenceType  = $state<AbsenceType>('Vacation');
    let startDate    = $state('');
    let endDate      = $state('');
    let message      = $state('');
    let isLoading    = $state(false);
    let errorMessage = $state('');
    let submitted    = $state(false); // success state

    // Auto-set endDate when startDate is picked and endDate is empty/before start
    function onStartDateChange() {
        if (!endDate || endDate < startDate) endDate = startDate;
    }

    // ── Validation ──
    function validate(): string | null {
        if (!absenceType)       return 'Please select an absence type.';
        if (!startDate)         return 'Please select a start date.';
        if (!endDate)           return 'Please select an end date.';

        const today = todayIso();
        if (startDate < today)  return 'Start date cannot be in the past.';
        if (endDate < today)    return 'End date cannot be in the past.';
        if (endDate < startDate) return 'End date must be on or after start date.';
        if (message.length > 250) return 'Message must be 250 characters or fewer.';
        return null;
    }

    const isValid    = $derived(validate() === null);
    const dayCount   = $derived.by(() => {
        if (!startDate || !endDate) return 0;
        const diff = new Date(endDate).getTime() - new Date(startDate).getTime();
        return Math.floor(diff / 86400000) + 1;
    });

    async function handleSubmit(event: SubmitEvent) {
        event.preventDefault();
        errorMessage = '';
        const err = validate();
        if (err) { errorMessage = err; return; }

        isLoading = true;
        try {
            // TODO: swap mock for real API
            // const res = await fetch('/api/employee/absences', {
            //   method: 'POST',
            //   headers: { 'Content-Type': 'application/json' },
            //   body: JSON.stringify({ absenceType, startDate, endDate, message }),
            //   credentials: 'include',
            // });
            // if (!res.ok) throw new Error((await res.json()).message);

            await new Promise(r => setTimeout(r, 600)); // simulate network
            submitted = true;

            // Auto-redirect after 2.5s so user sees success
            setTimeout(() => goto('/employee/absence'), 2500);
        } catch (e: unknown) {
            errorMessage = e instanceof Error ? e.message : 'Failed to submit. Please try again.';
        } finally {
            isLoading = false;
        }
    }

    return {
        get absenceType()           { return absenceType; },
        set absenceType(v: AbsenceType) { absenceType = v; },
        get startDate()             { return startDate; },
        set startDate(v: string)    { startDate = v; onStartDateChange(); },
        get endDate()               { return endDate; },
        set endDate(v: string)      { endDate = v; },
        get message()               { return message; },
        set message(v: string)      { message = v; },
        get isLoading()             { return isLoading; },
        get errorMessage()          { return errorMessage; },
        get submitted()             { return submitted; },
        get isValid()               { return isValid; },
        get dayCount()              { return dayCount; },
        handleSubmit,
    };
}