using Schichtpilot.Interfaces;

namespace Schichtpilot.Services;

public class EmailService : IEmailService 
{
  public Task SendNewAbsenceMailToManager(int absenceId, string userName)
    {
        // TODO: Real SMTP/sendgrid
        Console.WriteLine($"Email: New absence {absenceId} for {userName}");
        return Task.CompletedTask;
    }

    public Task SendApprovalMail(string email, object data)
    {
        //TODO
        return Task.CompletedTask;   
    }

    public Task SendRejectionMail(string email, object data)
    {
        //TODO
        return Task.CompletedTask;
    }

    public Task SendSchedule(string email, object data)
    {
        //TODO
        return Task.CompletedTask;
    }

    public Task SendScheduleInActiveMail(string email, object data)
    {
        //TODO
        return Task.CompletedTask;
    }

    public Task SendUserRegisterMail(string email, object data)
    {
        //TODO
        return Task.CompletedTask;
    }
}
