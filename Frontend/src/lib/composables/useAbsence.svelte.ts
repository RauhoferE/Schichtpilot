import type { AbsenceDto, AbsenceStatus, AbsenceType } from '$lib/types/absence.types';
import { createAbsence, getUserAbsences } from '$lib/services/absence.service';

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

// Helpers
export function fmtDate(iso: string): string {
    const d = new Date(iso);
    return `${d.getDate()}.${d.getMonth() + 1}.${String(d.getFullYear()).slice(2)}`;
}

export function todayIso(): string {
    return new Date().toISOString().slice(0, 10);
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

function validateAbsenceForm(form: {
    absenceType: AbsenceType;
    startDate: string;
    endDate: string;
    message: string;
}): string | null {
    if (!form.startDate || !form.endDate) {
        return 'Please select both a start and end date.';
    }

    const today = todayIso();

    if (form.startDate < today) {
        return 'Start date cannot be in the past.';
    }

    if (form.endDate < today) {
        return 'End date cannot be in the past.';
    }

    if (form.endDate < form.startDate) {
        return 'End date must be on or after the start date.';
    }

    if (form.message.trim().length < 3) {
        return 'Message must be at least 3 characters long.';
    }

    if (form.message.length > 250) {
        return 'Message must be 250 characters or fewer.';
    }

    return null;
}

// Absence list state (calendar + table + dialog)
export function createAbsenceState() {
    const now = new Date();

    let absences = $state<AbsenceDto[]>([]);
    let selectedId = $state<number | null>(null);
    let isLoading = $state(false);
    let errorMessage = $state('');
    let successMessage = $state('');

    let calendarYear = $state(now.getFullYear());
    let calendarMonth = $state(now.getMonth());

    const calendarMap = $derived.by(() => {
        const map = new Map<string, AbsenceStatus>();

        for (const a of absences) {
            for (const d of dateRange(a.startDate, a.endDate)) {
                if (!map.has(d) || a.status === 'Approved') {
                    map.set(d, a.status);
                }
            }
        }

        return map;
    });

    const selectedAbsence = $derived(absences.find(a => a.id === selectedId) ?? null);

    let dialogOpen = $state(false);
    let form = $state({
        absenceType: 'Vacation' as AbsenceType,
        startDate: '',
        endDate: '',
        message: '',
    });

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
                calendarYear = d.getFullYear();
                calendarMonth = d.getMonth();
            }
        }
    }

    async function loadAbsences() {
        isLoading = true;
        errorMessage = '';

        try {
            absences = await getUserAbsences(1, 100);

            if (absences.length > 0 && selectedId === null) {
                selectedId = absences[0].id;
            }
        } catch (e: unknown) {
            errorMessage = e instanceof Error ? e.message : 'Failed to load absences.';
        } finally {
            isLoading = false;
        }
    }

    function openDialog() {
        form = {
            absenceType: 'Vacation',
            startDate: '',
            endDate: '',
            message: '',
        };
        errorMessage = '';
        successMessage = '';
        dialogOpen = true;
    }

    async function submitAbsence() {
        errorMessage = '';

        const validationError = validateAbsenceForm(form);
        if (validationError) {
            errorMessage = validationError;
            return;
        }

        isLoading = true;

        try {
            await createAbsence({
                absenceType: form.absenceType,
                startDate: form.startDate,
                endDate: form.endDate,
                message: form.message
            });

            dialogOpen = false;
            form = {
                absenceType: 'Vacation',
                startDate: '',
                endDate: '',
                message: '',
            };

            successMessage = 'Your absence request has been submitted successfully!';

            await loadAbsences();

            setTimeout(() => {
                successMessage = '';
            }, 3000);
        } catch (e: unknown) {
            errorMessage = e instanceof Error ? e.message : 'Failed to submit. Please try again.';
        } finally {
            isLoading = false;
        }
    }

    return {
        get absences() { return absences; },
        get selectedId() { return selectedId; },
        get selectedAbsence() { return selectedAbsence; },
        get isLoading() { return isLoading; },
        get errorMessage() { return errorMessage; },
        get successMessage() { return successMessage; },

        get calendarYear() { return calendarYear; },
        get calendarMonth() { return calendarMonth; },
        get calendarMap() { return calendarMap; },
        daysInMonth,
        firstDayOfMonth,
        prevMonth,
        nextMonth,
        selectDay,
        selectAbsence,

        get dialogOpen() { return dialogOpen; },
        set dialogOpen(v: boolean) { dialogOpen = v; },
        get form() { return form; },
        openDialog,
        submitAbsence,
        loadAbsences,
    };
}