using Data.Entities;
using Schichtpilot.Models.DTOs;

public interface IEmailService
{
    // All managers notified
    Task SendNewAbsenceMailToManager(User employee, AbsenceDto absence);

    // Specific employee
    Task SendApprovalMail(User employee, AbsenceDto absence);
    Task SendRejectionMail(User employee, AbsenceDto absence);
    Task SendScheduleInActiveMail(User employee, WorkScheduleDto schedule);
    Task SendUserRegisterMail(User newUser, string temporaryPassword);

    // All employees
    Task SendScheduleMail(WorkScheduleDto schedule); 
}