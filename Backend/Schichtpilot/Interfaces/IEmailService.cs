namespace Schichtpilot.Interfaces;

public interface IEmailService 
{
    Task SendNewAbsenceNotificationAsync(int absenceId, string userName);
}
