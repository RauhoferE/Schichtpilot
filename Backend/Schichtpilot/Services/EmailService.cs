using Schichtpilot.Interfaces;

namespace Schichtpilot.Services;

public class EmailService : IEmailService 
{
    public Task SendNewAbsenceNotificationAsync(int absenceId, string userName)
    {
        // TODO: Real SMTP/sendgrid
        Console.WriteLine($"Email: New absence {absenceId} for {userName}");
        return Task.CompletedTask;
    }
}
