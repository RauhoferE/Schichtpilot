namespace Schichtpilot.Interfaces;

public interface IEmailService 
{
    Task SendNewAbsenceMailToManager(int absenceId, string userName);
    Task SendApprovalMail(string email, object data);
    Task SendRejectionMail(string email, object data);
    Task SendSchedule(string email, object data);
    Task SendScheduleInActiveMail(string email, object data);
    Task SendUserRegisterMail(string email, object data);
}
