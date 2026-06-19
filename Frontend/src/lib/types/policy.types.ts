export interface Holidays{
    holidays: Date[]
}

export interface CompanyPolicyDto{
    minimumRestPeriodInMinutes: number,
    restPeriodThresholdInMinutes: number,
    maximumConsecutiveWorkHoursPerDay: number,
    maximumConsecutiveWorkHoursPerWeek: number

}

export interface HolidaysDto {
    holidays: string[];
}

export interface CompanyPolicyDto {
    minimumRestPeriodInMinutes: number;
    restPeriodThresholdInMinutes: number;
    maximumConsecutiveWorkHoursPerDay: number;
    maximumConsecutiveWorkHoursPerWeek: number;
}

export const DEFAULT_COMPANY_POLICY: CompanyPolicyDto = {
    minimumRestPeriodInMinutes: 30,
    restPeriodThresholdInMinutes: 360,
    maximumConsecutiveWorkHoursPerDay: 8,
    maximumConsecutiveWorkHoursPerWeek: 40
};

export const DEFAULT_HOLIDAYS: HolidaysDto = {
    holidays: []
};
