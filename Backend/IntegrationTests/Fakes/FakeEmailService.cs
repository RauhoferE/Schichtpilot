using Data.Entities;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;

namespace IntegrationTests.Fakes;

public class FakeEmailService : IEmailService
{
    public List<(User Employee, AbsenceDto Absence)> ApprovalEmails { get; } = new List<(User Employee, AbsenceDto Absence)>();

    public Task SendNewAbsenceMailToManager(User employee, AbsenceDto absence)
    {
        return Task.CompletedTask;
    }

    public Task SendApprovalMail(User employee, AbsenceDto absence)
    {
        ApprovalEmails.Add((employee, absence));
        return Task.CompletedTask;
    }

    public Task SendRejectionMail(User employee, AbsenceDto absence)
    {
        return Task.CompletedTask;
    }

    public Task SendScheduleInActiveMail(WorkScheduleDto schedule)
    {
        return Task.CompletedTask;
    }

    public Task SendUserRegisterMail(User newUser)
    {
        return Task.CompletedTask;
    }

    public Task SendScheduleMail(WorkScheduleDto schedule)
    {
        return Task.CompletedTask;
    }
}
