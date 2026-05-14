import { PUBLIC_BASE_URL } from "$env/static/public";
import { del, get, patch, post, put } from "$lib/api";
import type { CreateShiftDto, EditShiftDto, GetShiftsRequest, QueryableShiftResponse, ShiftDto, ShiftRequirementDto, TimeSlotDto } from "$lib/types/shift.types";

const controllerURL: string = PUBLIC_BASE_URL + '/api/shift';

export function createShift(dto: CreateShiftDto): Promise<void>{
    return post<void>(`${controllerURL}`, dto);
}

export function getShift(shiftId: number): Promise<ShiftDto>{
    return get<ShiftDto>(`${controllerURL}/${shiftId}`);
}

export function getShifts(params: GetShiftsRequest): Promise<QueryableShiftResponse>{
    return get<QueryableShiftResponse>(`${controllerURL}/all?${new URLSearchParams(params as any).toString()}`);
}

export function updateShift(shiftId: number, dto: EditShiftDto): Promise<void>{
    return put<void>(`${controllerURL}/${shiftId}`, dto);
}

export function deleteShift(shiftId: number): Promise<void>{
    return del<void>(`${controllerURL}/${shiftId}`, undefined);
}

export function deleteTimeslot(shiftId: number, timeslotId: number): Promise<void>{
    return del<void>(`${controllerURL}/${shiftId}/timeslot/${timeslotId}`, undefined);
}

export function addTimeslot(shiftId: number, dto: TimeSlotDto): Promise<void>{
    return post<void>(`${controllerURL}/${shiftId}/timeslot`, dto);
}

export function updateTimeslot(shiftId: number, timeslotId: number, dto: TimeSlotDto): Promise<void>{
    return put<void>(`${controllerURL}/${shiftId}/timeslot/${timeslotId}`, dto);
}

export function addJobRequirement(shiftId: number, dto: ShiftRequirementDto): Promise<void>{
    return post<void>(`${controllerURL}/${shiftId}/job`, dto);
}

export function changeJobRequirementCount(shiftId: number, jobId: number, newCount: number): Promise<void>{
    return patch<void>(`${controllerURL}/${shiftId}/job/${jobId}?staffCount=${newCount}`, undefined);
}

export function removeJobRequirement(shiftId: number, jobId: number): Promise<void>{
    return del<void>(`${controllerURL}/${shiftId}/job/${jobId}`, undefined);
}