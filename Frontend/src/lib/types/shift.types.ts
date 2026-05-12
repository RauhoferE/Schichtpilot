export interface CreateShiftDto{
    name: string,
    colorAsHex: string,
    timeSlots: TimeSlotDto[],
    jobRequirements: ShiftRequirementDto[]
}

export interface TimeSlotDto{
    id: number,
    dayOfWeek: number,
      startTime: string,
    endTime: string,
    breaks: BreakDto[]

}

export interface BreakDto{
    id: number,
    startTime: string,
    endTime: string
}

export interface ShiftRequirementDto{
    jobId: number,
    name: string,
    requiredStaffCount: number
}

export interface ShiftDto{
    name: string,
    colorAsHex: string,
    timeSlots: TimeSlotDto[],
    jobRequirements: ShiftRequirementDto[]
}

export interface ShortShiftDto{
    id: number,
    name: string,
    colorAsHex: string
}

export interface QueryableShiftResponse{
    count: number,
    shift: ShortShiftDto[]
}

export interface GetShiftsRequest{
    page: number,
    pageSize: number,
    weekDays: string[],
    shiftStatusEnum: string,
    searchstring: string,
}

export interface EditShiftDto{
    name: string,
    colorAsHex: string
}