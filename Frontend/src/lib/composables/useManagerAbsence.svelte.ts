import type { AbsenceStatus, AbsenceType } from '$lib/types/absence.types';

// Types 
export type { AbsenceStatus, AbsenceType };

export interface ManagerAbsence {
    id: number;
    userId: number;
    employeeName: string;
    absenceType: AbsenceType;
    startDate: string;
    endDate: string;
    createdAt: string;
    status: AbsenceStatus;
    employeeMessage?: string;
    managerMessage?: string;
}

export interface AbsenceFilters {
    search: string;
    absenceType: AbsenceType | '';
    createdOn: string;
    absenceDate: string;
}

// Labels & styles 
export const ABSENCE_TYPE_LABELS: Record<AbsenceType, string> = {
    Vacation: 'Vacation',
    SickLeave: 'Sick Leave',
    PaidLeave: 'Paid Leave',
    ParentalLeave: 'Parental Leave',
    CareLeave: 'Care Leave',
    EducationalLeave: 'Educational Leave',
    CompensationTime: 'Compensation Time',
    PersonalLeave: 'Personal Leave',
};

export const ABSENCE_TYPES = Object.keys(ABSENCE_TYPE_LABELS) as AbsenceType[];

export const STATUS_STYLES: Record<AbsenceStatus, string> = {
    Pending: 'bg-yellow-100 text-yellow-800 border-yellow-200',
    Approved: 'bg-green-100 text-green-800 border-green-200',
    Denied: 'bg-red-100 text-red-800 border-red-200',
};

export const CALENDAR_DOT: Record<AbsenceStatus, string> = {
    Pending: 'bg-yellow-400',
    Approved: 'bg-green-500',
    Denied: 'bg-red-400',
};

//Helpers
export function fmtDate(iso: string): string {
    const [y, m, d] = iso.slice(0, 10).split('-');
    return `${Number(d)}.${Number(m)}.${y.slice(2)}`;
}

function toDateKey(value: string): string {
    return value.slice(0, 10);
}

function dateRange(start: string, end: string): string[] {
    const dates: string[] = [];
    const cur = new Date(start);
    const last = new Date(end);

    while (cur <= last) {
        dates.push(cur.toISOString().slice(0, 10));
        cur.setDate(cur.getDate() + 1);
    }

    return dates;
}

function normalizeAbsence(raw: any): ManagerAbsence {
    return {
        id: Number(raw.id),
        userId: Number(raw.userId),
        employeeName:
            raw.employeeName ??
            raw.userName ??
            raw.fullName ??
            raw.employee?.fullName ??
            raw.employee?.name ??
            `User ${raw.userId}`,
        absenceType: raw.absenceType,
        startDate: toDateKey(raw.startDate),
        endDate: toDateKey(raw.endDate),
        createdAt: raw.createdAt,
        status: raw.status,
        employeeMessage: raw.message ?? '',
        managerMessage: raw.managerMessage ?? '',
    };
}

//Composable
export function createManagerAbsenceState() {
    let allAbsences = $state<ManagerAbsence[]>([]);
    let isLoading = $state(false);
    let fetchError = $state<string | null>(null);
    let successMessage = $state('');

    let selectedId = $state<number | null>(null);

    let calendarYear = $state(new Date().getFullYear());
    let calendarMonth = $state(new Date().getMonth());

    let filters = $state<AbsenceFilters>({
        search: '',
        absenceType: '',
        createdOn: '',
        absenceDate: '',
    });

    let confirmDialog = $state<{
        open: boolean;
        id: number | null;
        action: 'Approved' | 'Denied';
        message: string;
    }>({
        open: false,
        id: null,
        action: 'Approved',
        message: ''
    });

    let confirmDialogError = $state('');

    let filteredAbsences = $derived.by(() => {
        return allAbsences.filter(a => {
            if (filters.search) {
                const q = filters.search.toLowerCase();
                if (!a.employeeName.toLowerCase().includes(q)) return false;
            }

            if (filters.absenceType && a.absenceType !== filters.absenceType) return false;

            if (filters.createdOn && toDateKey(a.createdAt) !== filters.createdOn) return false;

            if (filters.absenceDate && (a.endDate < filters.absenceDate || a.startDate > filters.absenceDate)) return false;

            return true;
        });
    });

    let selectedAbsence = $derived(allAbsences.find(a => a.id === selectedId) ?? null);

    let calendarMap = $derived.by(() => {
        const map = new Map<string, AbsenceStatus>();
        for (const a of filteredAbsences) {
            for (const d of dateRange(a.startDate, a.endDate)) {
                const existing = map.get(d);
                if (!existing || a.status === 'Approved') {
                    map.set(d, a.status);
                }
            }
        }
        return map;
    });

    let hasActiveFilters = $derived(
        !!filters.search ||
        !!filters.absenceType ||
        !!filters.createdOn ||
        !!filters.absenceDate
    );

    async function loadAbsences() {
        isLoading = true;
        fetchError = null;
        allAbsences = [];

        try {
            const PAGE_SIZE = 100;
            const response = await fetch(`http://localhost:5000/api/absence/all?page=1&pageSize=${PAGE_SIZE}`, {
                method: 'GET',
                credentials: 'include',
            });

            if (!response.ok) {
                let message = 'Failed to load absence requests. Please try again.';
                try {
                    const data = await response.json();
                    if (data?.message) {
                        message = data.message;
                    }
                } catch {
                }
                throw new Error(message);
            }

            const data = await response.json();
            const items = data.items ?? data.data ?? data.absences ?? [];
            allAbsences = items.map(normalizeAbsence);

            if (allAbsences.length > 0 && selectedId === null) {
                selectedId = allAbsences[0].id;
            }
        } catch (e: unknown) {
            fetchError = e instanceof Error ? e.message : 'Failed to load absence requests. Please try again.';
            allAbsences = [];
        } finally {
            isLoading = false;
        }
    }

    function prevMonth() {
        if (calendarMonth === 0) {
            calendarMonth = 11;
            calendarYear--;
        } else {
            calendarMonth--;
        }
    }

    function nextMonth() {
        if (calendarMonth === 11) {
            calendarMonth = 0;
            calendarYear++;
        } else {
            calendarMonth++;
        }
    }

    function daysInMonth(year: number, month: number) {
        return new Date(year, month + 1, 0).getDate();
    }

    function firstDayOfMonth(year: number, month: number) {
        return new Date(year, month, 1).getDay();
    }

    function selectDay(dateStr: string) {
        const hit = filteredAbsences.find(a => dateStr >= a.startDate && dateStr <= a.endDate);
        selectedId = hit?.id ?? null;
    }

    function selectAbsence(id: number) {
        selectedId = selectedId === id ? null : id;

        if (selectedId !== null) {
            const a = allAbsences.find(x => x.id === id);
            if (a) {
                const d = new Date(a.startDate);
                calendarYear = d.getFullYear();
                calendarMonth = d.getMonth();
            }
        }
    }

    function clearFilters() {
        filters = { search: '', absenceType: '', createdOn: '', absenceDate: '' };
    }

    function openConfirm(id: number, action: 'Approved' | 'Denied') {
        confirmDialog = { open: true, id, action, message: '' };
        confirmDialogError = '';
    }

    function closeConfirm() {
        confirmDialog = { open: false, id: null, action: 'Approved', message: '' };
        confirmDialogError = '';
    }

    async function applyDecision() {
        const { id, action, message } = confirmDialog;

        if (!id) {
            confirmDialogError = 'No absence selected.';
            return;
        }

        if (action === 'Denied' && message.trim().length < 3) {
            confirmDialogError = 'A reason is required when denying an absence.';
            return;
        }

        if (message.length > 250) {
            confirmDialogError = 'Message must be 250 characters or fewer.';
            return;
        }

        confirmDialogError = '';
        isLoading = true;

        try {
            const response = await fetch(`http://localhost:5000/api/absence/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({
                    status: action,
                    managerMessage: message.trim()
                }),
            });

            if (!response.ok) {
                let error = 'Failed to update absence status. Please try again.';
                try {
                    const data = await response.json();
                    if (data?.message) {
                        error = data.message;
                    } else if (data?.errorStates?.length) {
                        error = data.errorStates.map((x: { message: string }) => x.message).join(' ');
                    }
                } catch {
                }
                throw new Error(error);
            }

            closeConfirm();
            successMessage = action === 'Approved' ? 'Absence approved.' : 'Absence denied.';
            await loadAbsences();
            setTimeout(() => { successMessage = ''; }, 3000);
        } catch (e: unknown) {
            confirmDialogError = e instanceof Error
                ? e.message
                : 'Failed to update absence status. Please try again.';
        } finally {
            isLoading = false;
        }
    }

    return {
        get allAbsences() { return allAbsences; },
        get filteredAbsences() { return filteredAbsences; },
        get isLoading() { return isLoading; },
        get fetchError() { return fetchError; },
        get successMessage() { return successMessage; },
        get selectedId() { return selectedId; },
        get selectedAbsence() { return selectedAbsence; },
        get calendarYear() { return calendarYear; },
        get calendarMonth() { return calendarMonth; },
        get calendarMap() { return calendarMap; },
        get filters() { return filters; },
        get hasActiveFilters() { return hasActiveFilters; },
        get confirmDialog() { return confirmDialog; },
        get confirmDialogError() { return confirmDialogError; },
        set filters(v) { filters = v; },
        prevMonth,
        nextMonth,
        daysInMonth,
        firstDayOfMonth,
        selectDay,
        selectAbsence,
        clearFilters,
        loadAbsences,
        openConfirm,
        closeConfirm,
        applyDecision,
    };
}