export type AbsenceStatus = 'Pending' | 'Approved' | 'Denied';

export type AbsenceType =
    |'Vacation'
    | 'SickLeave'
    | 'PaidLeave'
    | 'ParentalLeave'
    | 'CareLeave'
    | 'EducationalLeave'
    | 'CompensationTime'
    | 'PersonalLeave';

export interface AbsenceDto {
    id:             number;
    userId:         number;
    startDate:      string;   // ISO "YYYY-MM-DD"
    endDate:        string;
    absenceType:    AbsenceType;
    message:        string;
    status:         AbsenceStatus;
    createdAt:      string;
    managerMessage: string;
}

export interface CreateAbsenceDto {
    absenceType: AbsenceType;
    startDate:   string;
    endDate:     string;
    message:     string;
}