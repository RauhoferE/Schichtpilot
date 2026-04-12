using Data.Entities;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;

public interface IEmailService
{
    // All managers notified
    Task SendNewAbsenceMailToManager(User employee, AbsenceDto absence);

    // Specific employee
    Task SendApprovalMail(User employee, AbsenceDto absence);
    Task SendRejectionMail(User employee, AbsenceDto absence);
    Task SendScheduleInActiveMail(WorkScheduleDto schedule);
    Task SendUserRegisterMail(User newUser);

    // All employees
    Task SendScheduleMail(WorkScheduleDto schedule);
}