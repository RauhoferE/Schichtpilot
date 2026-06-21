import { get, put } from '$lib/api';
import type { AbsenceStatus, AbsenceType } from '$lib/types/absence.types';

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

export interface UpdateAbsenceStatusDto {
    status: 'Approved' | 'Denied';
    managerMessage: string;
}

interface ManagerAbsenceListResponse {
    count?: number;
    items?: unknown[];
    data?: unknown[];
    absences?: unknown[];
}

const controllerURL = '/api/absence';

function toDateKey(value: string | null | undefined): string {
    if (!value) return '';
    return value.slice(0, 10);
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
        absenceType: raw.absenceType as AbsenceType,
        startDate: toDateKey(raw.startDate),
        endDate: toDateKey(raw.endDate),
        createdAt: raw.createdAt ?? '',
        status: raw.status as AbsenceStatus,
        employeeMessage: raw.message ?? '',
        managerMessage: raw.managerMessage ?? '',
    };
}

function normalizeAbsenceListResponse(response: ManagerAbsenceListResponse | unknown[] | null | undefined): ManagerAbsence[] {
    if (!response) {
        return [];
    }

    if (Array.isArray(response)) {
        return response.map(normalizeAbsence);
    }

    const items = response.items ?? response.data ?? response.absences ?? [];
    return items.map(normalizeAbsence);
}

export async function getAllAbsences(page = 1, pageSize = 100): Promise<ManagerAbsence[]> {
    const response = await get<ManagerAbsenceListResponse>(`${controllerURL}/all?page=${page}&pageSize=${pageSize}`);
    return normalizeAbsenceListResponse(response);
}

export async function updateAbsenceStatus(id: number, payload: UpdateAbsenceStatusDto): Promise<void> {
    await put<unknown>(`${controllerURL}/${id}`, payload);
}