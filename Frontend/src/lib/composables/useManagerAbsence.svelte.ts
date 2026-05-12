import type { AbsenceStatus, AbsenceType } from '$lib/types/absence.types';

// ── Types ─────────────────────────────────────────────────────────────────────
export type { AbsenceStatus, AbsenceType };

export interface ManagerAbsence {
    id:               string;
    employeeName:     string;
    absenceType:      AbsenceType;
    startDate:        string;
    endDate:          string;
    createdAt:        string;
    status:           AbsenceStatus;
    employeeMessage?: string;
    managerMessage?:  string;
}

export interface AbsenceFilters {
    search:      string;
    absenceType: AbsenceType | '';
    createdOn:   string;
    absenceDate: string;
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
    const [y, m, d] = iso.slice(0, 10).split('-');
    return `${Number(d)}.${Number(m)}.${y.slice(2)}`;
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

// ── Mock data ─────────────────────────────────────────────────────────────────
function getMockAbsences(): ManagerAbsence[] {
    const today = new Date();
    const y = today.getFullYear();
    const m = String(today.getMonth() + 1).padStart(2, '0');
    const todayIso = today.toISOString().slice(0, 10);

    return [
        {
            id: '1', employeeName: 'Hannah G.',
            absenceType: 'SickLeave',
            startDate: `${y}-${m}-12`, endDate: `${y}-${m}-14`,
            createdAt: todayIso,
            status: 'Pending', employeeMessage: 'Doctor confirmed.'
        },
        {
            id: '2', employeeName: 'Hannah G.',
            absenceType: 'CompensationTime',
            startDate: `${y}-${m}-11`, endDate: `${y}-${m}-11`,
            createdAt: todayIso,
            status: 'Denied', employeeMessage: '', managerMessage: 'Not enough notice.'
        },
        {
            id: '3', employeeName: 'Hannah G.',
            absenceType: 'Vacation',
            startDate: `${y}-${m}-05`, endDate: `${y}-${m}-10`,
            createdAt: todayIso,
            status: 'Approved', employeeMessage: 'Family trip.', managerMessage: 'Enjoy!'
        },
        {
            id: '4', employeeName: 'Max M.',
            absenceType: 'Vacation',
            startDate: `${y}-${m}-18`, endDate: `${y}-${m}-22`,
            createdAt: todayIso,
            status: 'Pending', employeeMessage: ''
        },
        {
            id: '5', employeeName: 'Max M.',
            absenceType: 'SickLeave',
            startDate: `${y}-${m}-24`, endDate: `${y}-${m}-26`,
            createdAt: todayIso,
            status: 'Approved', employeeMessage: ''
        },
        {
            id: '6', employeeName: 'Sara K.',
            absenceType: 'Other',
            startDate: `${y}-${m}-28`, endDate: `${y}-${m}-28`,
            createdAt: todayIso,
            status: 'Pending', employeeMessage: 'Personal appointment.'
        },
    ];
}

const MOCK_MODE = true;

// ── Composable ────────────────────────────────────────────────────────────────
export function createManagerAbsenceState() {
    // ── Data ──
    let allAbsences    = $state<ManagerAbsence[]>([]);
    let isLoading      = $state(false);
    let fetchError     = $state<string | null>(null);
    let successMessage = $state('');

    // ── Selection ──
    let selectedId = $state<string | null>(null);

    // ── Calendar ──
    let calendarYear  = $state(new Date().getFullYear());
    let calendarMonth = $state(new Date().getMonth());

    // ── Filters ──
    let filters = $state<AbsenceFilters>({
        search:      '',
        absenceType: '',
        createdOn:   '',
        absenceDate: '',
    });

    // ── Confirm dialog ──
    let confirmDialog = $state<{
        open:    boolean;
        id:      string;
        action:  'Approved' | 'Denied';
        message: string;
    }>({
        open: false, id: '', action: 'Approved', message: ''
    });
    let confirmDialogError = $state('');

    // ── Derived ──
    let filteredAbsences = $derived.by(() => {
        return allAbsences.filter(a => {
            if (filters.search) {
                const q = filters.search.toLowerCase();
                if (!a.employeeName.toLowerCase().includes(q)) return false;
            }
            if (filters.absenceType && a.absenceType !== filters.absenceType) return false;
            if (filters.createdOn   && !a.createdAt.startsWith(filters.createdOn)) return false;
            if (filters.absenceDate && (a.endDate < filters.absenceDate || a.startDate > filters.absenceDate)) return false;
            return true;
        });
    });

    let selectedAbsence = $derived(allAbsences.find(a => a.id === selectedId) ?? null);

    // calendar dots follow filtered results
    let calendarMap = $derived.by(() => {
        const map = new Map<string, AbsenceStatus>();
        for (const a of filteredAbsences) {
            for (const d of dateRange(a.startDate, a.endDate)) {
                const existing = map.get(d);
                if (!existing || a.status === 'Approved') map.set(d, a.status);
            }
        }
        return map;
    });

    let hasActiveFilters = $derived(
        !!filters.search      ||
        !!filters.absenceType ||
        !!filters.createdOn   ||
        !!filters.absenceDate
    );

    // ── Load ──
    async function loadAbsences() {
        isLoading   = true;
        fetchError  = null;
        allAbsences = [];

        try {
            if (MOCK_MODE) {
                await new Promise(r => setTimeout(r, 400));
                allAbsences = getMockAbsences();
                return;
            }

            const response = await fetch('/api/absences', { credentials: 'include' });
            if (!response.ok) throw new Error('Server error');
            allAbsences = await response.json();
        } catch {
            fetchError  = 'Failed to load absence requests. Please try again.';
            allAbsences = [];
        } finally {
            isLoading = false;
        }
    }

    // ── Calendar helpers ──
    function prevMonth() {
        if (calendarMonth === 0) { calendarMonth = 11; calendarYear--; }
        else calendarMonth--;
    }
    function nextMonth() {
        if (calendarMonth === 11) { calendarMonth = 0; calendarYear++; }
        else calendarMonth++;
    }
    function daysInMonth(year: number, month: number)    { return new Date(year, month + 1, 0).getDate(); }
    function firstDayOfMonth(year: number, month: number){ return new Date(year, month, 1).getDay(); }

    function selectDay(dateStr: string) {
        const hit = filteredAbsences.find(a => {
            const d = new Date(dateStr);
            return d >= new Date(a.startDate) && d <= new Date(a.endDate);
        });
        selectedId = hit?.id ?? null;
    }

    function selectAbsence(id: string) {
        selectedId = selectedId === id ? null : id;
        if (selectedId !== null) {
            const a = allAbsences.find(x => x.id === id);
            if (a) {
                const d = new Date(a.startDate);
                calendarYear  = d.getFullYear();
                calendarMonth = d.getMonth();
            }
        }
    }

    // ── Filters ──
    function clearFilters() {
        filters = { search: '', absenceType: '', createdOn: '', absenceDate: '' };
    }

    // ── Confirm dialog ──
    function openConfirm(id: string, action: 'Approved' | 'Denied') {
        confirmDialog      = { open: true, id, action, message: '' };
        confirmDialogError = '';
    }

    function closeConfirm() {
        confirmDialog      = { ...confirmDialog, open: false };
        confirmDialogError = '';
    }

    async function applyDecision() {
        const { id, action, message } = confirmDialog;

        // message is mandatory when denying
        if (action === 'Denied' && !message.trim()) {
            confirmDialogError = 'A reason is required when denying an absence.';
            return;
        }

        confirmDialogError = '';
        closeConfirm();

        try {
            if (MOCK_MODE) {
                allAbsences = allAbsences.map(a =>
                    a.id === id ? { ...a, status: action, managerMessage: message.trim() } : a
                );
            } else {
                const response = await fetch(`/api/absences/${id}/status`, {
                    method: 'PATCH',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ status: action, managerMessage: message.trim() }),
                });
                if (!response.ok) throw new Error('Server error');
                allAbsences = allAbsences.map(a =>
                    a.id === id ? { ...a, status: action, managerMessage: message.trim() } : a
                );
            }

            successMessage = action === 'Approved' ? 'Absence approved.' : 'Absence denied.';
            setTimeout(() => { successMessage = ''; }, 3000);
        } catch {
            fetchError = 'Failed to update absence status. Please try again.';
        }
    }

    // kick off on mount
    loadAbsences();

    return {
        get allAbsences()        { return allAbsences; },
        get filteredAbsences()   { return filteredAbsences; },
        get isLoading()          { return isLoading; },
        get fetchError()         { return fetchError; },
        get successMessage()     { return successMessage; },
        get selectedId()         { return selectedId; },
        get selectedAbsence()    { return selectedAbsence; },
        get calendarYear()       { return calendarYear; },
        get calendarMonth()      { return calendarMonth; },
        get calendarMap()        { return calendarMap; },
        get filters()            { return filters; },
        get hasActiveFilters()   { return hasActiveFilters; },
        get confirmDialog()      { return confirmDialog; },
        get confirmDialogError() { return confirmDialogError; },
        set filters(v)           { filters = v; },
        prevMonth, nextMonth,
        daysInMonth, firstDayOfMonth,
        selectDay, selectAbsence,
        clearFilters, loadAbsences,
        openConfirm, closeConfirm, applyDecision,
    };
}