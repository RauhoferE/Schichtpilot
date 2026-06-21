import { get, post } from '$lib/api';
import type { AbsenceDto, AbsenceType } from '$lib/types/absence.types';

const controllerURL = '/api/absence';

export interface CreateAbsenceDto {
    absenceType: AbsenceType;
    startDate: string;
    endDate: string;
    message: string;
}

interface UserAbsencesApiResponse {
    count?: number;
    items?: AbsenceDto[];
    data?: AbsenceDto[];
    absences?: AbsenceDto[];
}

function normalizeAbsencesResponse(response: UserAbsencesApiResponse | AbsenceDto[] | null | undefined): AbsenceDto[] {
    if (!response) {
        return [];
    }

    if (Array.isArray(response)) {
        return response;
    }

    return response.items ?? response.data ?? response.absences ?? [];
}

export async function getUserAbsences(page = 1, pageSize = 100): Promise<AbsenceDto[]> {
    const response = await get<UserAbsencesApiResponse>(`${controllerURL}/user?page=${page}&pageSize=${pageSize}`);
    return normalizeAbsencesResponse(response);
}

export async function createAbsence(payload: CreateAbsenceDto): Promise<void> {
    await post<unknown>(controllerURL, payload);
}