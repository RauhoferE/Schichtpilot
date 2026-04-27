using Data.Entities;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Interfaces;

/// <summary>
/// Orchestrates email related operations.
/// </summary>
public interface IEmailService
{
    // All managers notified
    /// <summary>
    /// Sends a notification to all managers about a newly created absence. 
    /// </summary>
    /// <param name="employee"> The user that created the absence. </param>
    /// <param name="absence"> The specifics about the absence. </param>
    /// <returns></returns>
    Task SendNewAbsenceMailToManager(User employee, AbsenceDto absence);

    // Specific employee

    /// <summary>
    /// Sends a notification to the user that his absence was approved.
    /// </summary>
    /// <param name="employee"> The user that created the absence. </param>
    /// <param name="absence"> The specifics about the absence. </param>
    /// <returns></returns>
    Task SendApprovalMail(User employee, AbsenceDto absence);

    /// <summary>
    /// Sends a notification to the user that his absence was rejected.
    /// </summary>
    /// <param name="employee"> The user that created the absence. </param>
    /// <param name="absence"> The specifics about the absence. </param>
    /// <returns></returns>
    Task SendRejectionMail(User employee, AbsenceDto absence);

    /// <summary>
    /// Sends the notification that the given workschedule is now active to all assigned users.
    /// </summary>
    /// <param name="schedule"> The workschedule to be sent. </param>
    /// <returns></returns>
    Task SendScheduleInActiveMail(WorkScheduleDto schedule);

    /// <summary>
    /// Sends a notification about the account creation to the user that created the account.
    /// </summary>
    /// <param name="newUser"> The newly created user. </param>
    /// <returns></returns>
    Task SendUserRegisterMail(User newUser);

    // All employees
    /// <summary>
    /// Sends the workschedule to all assigned users.
    /// </summary>
    /// <param name="schedule"> The workschedule to be sent. </param>
    /// <returns></returns>
    Task SendScheduleMail(WorkScheduleDto schedule);
}