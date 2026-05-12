export interface Holidays{
    holidays: Date[]
}

export interface CompanyPolicyDto{
    minimumRestPeriodInMinutes: number,
    restPeriodThresholdInMinutes: number,
    maximumConsecutiveWorkHoursPerDay: number,
    maximumConsecutiveWorkHoursPerWeek: number

}