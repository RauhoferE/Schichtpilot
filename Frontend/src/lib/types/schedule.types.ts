// ── Schedule types ────────────────────────────────────────────────────────────

import type { JobRoleShortDto } from "./jobRole.types";
import type { ShiftDto, TimeSlotDto } from "./shift.types";
import type { UserDto } from "./user.types";

// Shift codes used in the schedule grid
// L = Lunch, E = Evening, M = Mixed, V = Day off/Vacation, null = not assigned
export type ShiftCode = 'L' | 'E' | 'M' | 'V' | 'X' | null;

// A single day entry for one staff member
export interface DaySchedule {
    date: string;   // YYYY-MM-DD
    shift: ShiftCode;
}

// One staff member's full week
export interface StaffSchedule {
    name: string;
    days: DaySchedule[]; // always 7 entries, Mon–Sun
}

// A full week of schedule data for all staff
export interface Week {
    label: string;      // e.g. "03.05.25 – 09.05.25"
    startDate: string;  // YYYY-MM-DD (always a Monday)
    staff: StaffSchedule[];
}

// Metadata for displaying a shift code in the UI
export interface ShiftMeta {
    label: string;  // e.g. "Lunch Shift"
    hours: string;  // e.g. "10:00–17:00"
    bg: string;     // tailwind bg class
    text: string;   // tailwind text class
}

export interface GenerateScheduleDto{
    name: string,
    startDate: Date,
    endDate: Date,
    shiftIds: number[]
}

export interface UpdateScheduleRequest{
    startTime: Date,
    endTime: Date
}

export interface GetSchedulesRequest{
    page: number,
    pageSize: number,
    startDate: Date,
    endDate: Date,
    searchstring: string,
    shiftIds: number[],
    status: string
}

export interface WorkScheduleDto{
    id: number,
    name: string,
    startDate: Date,
    endDate: Date,
    isActive: boolean,
    isValid: boolean,
    assignedUsers: AssignedUserDto[],
    shifts: ShiftDto[]
}

export interface AssignedUserDto{
    timeSlot: TimeSlotDto,
    user: UserDto,
    jobRole: JobRoleShortDto,
    startTime: Date,
    endTime: Date
}

export interface QueryableSchedules{
    count: number,
    workSchedules: WorkScheduleShortDto[]
}

export interface WorkScheduleShortDto{
    id: number,
    name: string,
    startDate: Date,
    endDate: Date,
    isActive: boolean,
    isValid: boolean,
    shiftCount: number
}