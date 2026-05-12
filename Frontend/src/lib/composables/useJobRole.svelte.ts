import { CreateJobRoleDto, EditJobRoleDto, GetJobRoleRequest, type JobRoleDto, type QueryableJobRoleResponse } from "$lib/types/jobRole";


// ── Mock data ─────────────────────────────────────────────────────────────────
function getMockJobRoles(): JobRoleDto[] {

    return [
        {
            id: 1,
            name: 'Cook',
            description: 'Cook',
            users: [
                {
                    firstName: 'Henry',
                    lastName: 'Dog',
                    addressDto: {} as any,
                    assignedJobRoles: [],
                    birthDate: new Date(),
                    email: 'henry.dog@fresh.at'
                }
            ],
            dependentOn: [],
            prerequisites: []
        },
        {
            id: 2,
            name: 'KitchenHelp',
            description: 'Kitchen Help',
            users: [
                {
                    firstName: 'John',
                    lastName: 'Dog',
                    addressDto: {} as any,
                    assignedJobRoles: [],
                    birthDate: new Date(),
                    email: 'john.dog@fresh.at'
                }
            ],
            dependentOn: [],
            prerequisites: []
        },
        {
            id: 3,
            name: 'Waiter',
            description: 'Waiter',
            users: [
                {
                    firstName: 'Sally',
                    lastName: 'Mesa',
                    addressDto: {} as any,
                    assignedJobRoles: [],
                    birthDate: new Date(),
                    email: 'sally.messa@fresh.at'
                }
            ],
            dependentOn: [],
            prerequisites: []
        }
    ];
}

function getMockQueryableResponse(): QueryableJobRoleResponse{
    const roles = getMockJobRoles();
    return {
        count: roles.length,
        jobRoles: roles
    }
}

const MOCK_MODE = true;

// ── Composable ────────────────────────────────────────────────────────────────
export function createJobRole() {
    // ── Data ──
    let allJobRoles    = $state<JobRoleDto[]>([]);
    let isLoading      = $state(false);
    let fetchError     = $state<string | null>(null);
    let successMessage = $state('');

    // ── Selection ──
    let selectedId = $state<number | null>(null);

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

    let selectedJobRole = $derived(allJobRoles.find(a => a.id === selectedId) ?? null);

    // ── Load ──
    async function loadJobRoles() {
        isLoading   = true;
        fetchError  = null;
        allJobRoles = [];

        try {
            if (MOCK_MODE) {
                await new Promise(r => setTimeout(r, 400));
                allJobRoles = getMockJobRoles();
                return;
            }

            const response = await fetch('/api/absences', { credentials: 'include' });
            if (!response.ok) throw new Error('Server error');
            allJobRoles = await response.json();
        } catch {
            fetchError  = 'Failed to load absence requests. Please try again.';
            allJobRoles = [];
        } finally {
            isLoading = false;
        }
    }

    function selectAbsence(id: string) {
        selectedId = selectedId === id ? null : id;
        if (selectedId !== null) {
            const a = allJobRoles.find(x => x.id === id);
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
                allJobRoles = allJobRoles.map(a =>
                    a.id === id ? { ...a, status: action, managerMessage: message.trim() } : a
                );
            } else {
                const response = await fetch(`/api/absences/${id}/status`, {
                    method: 'PATCH',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ status: action, managerMessage: message.trim() }),
                });
                if (!response.ok) throw new Error('Server error');
                allJobRoles = allJobRoles.map(a =>
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
    loadJobRoles();

    return {
        get allAbsences()        { return allJobRoles; },
        get filteredAbsences()   { return filteredAbsences; },
        get isLoading()          { return isLoading; },
        get fetchError()         { return fetchError; },
        get successMessage()     { return successMessage; },
        get selectedId()         { return selectedId; },
        get selectedAbsence()    { return selectedJobRole; },
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
        clearFilters, loadAbsences: loadJobRoles,
        openConfirm, closeConfirm, applyDecision,
    };
}