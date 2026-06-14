import { PUBLIC_BASE_URL } from "$env/static/public";
import { del, get, patch, post } from "$lib/api";
import type { GenerateScheduleDto, GetSchedulesRequest, QueryableSchedules, UpdateScheduleRequest, WorkScheduleDto } from "$lib/types/schedule.types";
import qs from "qs";

const controllerURL: string = PUBLIC_BASE_URL + '/api/workschedule';

export function generateSchedule(dto: GenerateScheduleDto): Promise<void>{
    return post<void>(`${controllerURL}/generate`, dto);
}

export function regenerateSchedule(scheduleId: number): Promise<void>{
    return post<void>(`${controllerURL}/generate/${scheduleId}`, undefined);
}

export function publishSchedule(scheduleId: number): Promise<void>{
    return post<void>(`${controllerURL}/publish/${scheduleId}`, undefined);
}

export function deleteSchedule(scheduleId: number): Promise<void>{
    return del<void>(`${controllerURL}/${scheduleId}`, undefined);
}

export function updateSchedule(scheduleId: number, dto: UpdateScheduleRequest): Promise<void>{
    return patch<void>(`${controllerURL}/${scheduleId}`, dto);
}

export function getSchedule(scheduleId: number): Promise<WorkScheduleDto>{
    return get<WorkScheduleDto>(`${controllerURL}/${scheduleId}`);
}

export function getactiveSchedule(startDate: Date): Promise<WorkScheduleDto>{
    const iso = startDate.toISOString().slice(0, 10);
    return get<WorkScheduleDto>(`${controllerURL}/active?startDate=${iso}`);
}
export function getSchedulesRequest(params: GetSchedulesRequest): Promise<QueryableSchedules>{
    return get<QueryableSchedules>(`${controllerURL}/all?${qs.stringify(params)}`);
}

export function setScheduleAsInactive(scheduleId: number): Promise<void>{
    return patch<void>(`${controllerURL}/${scheduleId}/inactive`, undefined);
}

export function setScheduleAsActive(scheduleId: number): Promise<void>{
    return patch<void>(`${controllerURL}/${scheduleId}/active`, undefined);
}