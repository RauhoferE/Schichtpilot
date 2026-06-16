using Azure;
using Azure.Communication.Email;
using Data.Entities;
using Microsoft.Extensions.Options;
using Schichtpilot.Models.DTOs;
using System.Text;
using Core;
using Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Schichtpilot.Interfaces;
using Schichtpilot.Settings;

namespace Schichtpilot.Services;

/// <summary>
/// Orchestrates email related operations.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailClient _emailClient;

    // private readonly UserManager<User> _userManager;
    private readonly string _senderAddress;
    private readonly string _templatesPath;
    private readonly bool _sendMail;
    private readonly ILogger<EmailService> _logger;
    private readonly UserManager<User> _userManager;

    public EmailService(
        IOptions<AzureEmailSettings> emailSettings,
        UserManager<User> userManager,
        ILogger<EmailService> logger, EmailClient emailClient)
    {
        // _userManager = userManager;
        _logger = logger;

        var settings = emailSettings.Value;

        _sendMail = settings.SendMail;

        if (string.IsNullOrEmpty(settings.ConnectionString))
            throw new InvalidOperationException("AzureEmail:ConnectionString is missing.");

        if (string.IsNullOrEmpty(settings.SenderAddress))
            throw new InvalidOperationException("AzureEmail:SenderAddress is missing.");

        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this._emailClient = emailClient;

        //_emailClient = new EmailClient(settings.ConnectionString);
        _senderAddress = settings.SenderAddress;
        _templatesPath = Path.Combine(AppContext.BaseDirectory, "Services", "EmailTemplate");
    }

    // ──────────────────────────────────────────────────────────────
    // All managers notified
    // ──────────────────────────────────────────────────────────────

    // All managers notified
    /// <summary>
    /// Sends a notification to all managers about a newly created absence. 
    /// </summary>
    /// <param name="employee"> The user that created the absence. </param>
    /// <param name="absence"> The specifics about the absence. </param>
    /// <returns></returns>
    public async Task SendNewAbsenceMailToManager(User employee, AbsenceDto absence)
    {
        var managers = await this._userManager.GetUsersInRoleAsync(UserRolesClass.Admin);

        var tasks = managers.Select(m =>
        {
            var placeholders = new Dictionary<string, string>
            {
                { "{{ManagerName}}", $"{m.FirstName} {m.LastName}"},
                { "{{EmployeeName}}", $"{employee.FirstName} {employee.LastName}" },
                { "{{StartDate}}", absence.StartDate.ToString("dd.MM.yyyy") },
                { "{{EndDate}}", absence.EndDate.ToString("dd.MM.yyyy") },
                { "{{AbsenceType}}", absence.AbsenceType.ToString() },
                { "{{Message}}", absence.Message }
            };

            return SendTemplateAsync(
                m.Email, $"New Absence Request from {employee.FirstName}",
                "absence.html",
                placeholders);
        });
        await Task.WhenAll(tasks);
    }
    // ──────────────────────────────────────────────────────────────
    // Specific employee
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a notification to the user that his absence was approved.
    /// </summary>
    /// <param name="employee"> The user that created the absence. </param>
    /// <param name="absence"> The specifics about the absence. </param>
    /// <returns></returns>
    public async Task SendApprovalMail(User employee, AbsenceDto absence)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "{{EmployeeName}}", FullName(employee) },
            { "{{StartDate}}", absence.StartDate.ToString("dd.MM.yyyy") },
            { "{{EndDate}}", absence.EndDate.ToString("dd.MM.yyyy") },
            { "{{ManagerMessage}}", absence.ManagerMessage }
        };

        await SendTemplateAsync(
            employee.Email!,
            "Your Absence Request Has Been Approved ✓",
            "approval.html",
            placeholders);
    }

    /// <summary>
    /// Sends a notification to the user that his absence was rejected.
    /// </summary>
    /// <param name="employee"> The user that created the absence. </param>
    /// <param name="absence"> The specifics about the absence. </param>
    /// <returns></returns>
    public async Task SendRejectionMail(User employee, AbsenceDto absence)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "{{EmployeeName}}", FullName(employee) },
            { "{{StartDate}}", absence.StartDate.ToString("dd.MM.yyyy") },
            { "{{EndDate}}", absence.EndDate.ToString("dd.MM.yyyy") },
            { "{{ManagerMessage}}", absence.ManagerMessage }
        };

        await SendTemplateAsync(
            employee.Email!,
            "Your Absence Request Has Been Declined",
            "rejection.html",
            placeholders);
    }

    /// <summary>
    /// Sends the notification that the given workschedule is now active to all assigned users.
    /// </summary>
    /// <param name="schedule"> The workschedule to be sent. </param>
    /// <returns></returns>
    public async Task SendScheduleInActiveMail(WorkScheduleDto schedule)
    {
        var managers = await this._userManager.GetUsersInRoleAsync(UserRolesClass.Admin);
        var tasks = schedule.AssignedUsers.Select(e =>
        {
            var placeholders = new Dictionary<string, string>
            {
                { "{{EmployeeName}}", $"{e.User.FirstName} {e.User.LastName}" },
                { "{{ScheduleName}}", schedule.Name },
                { "{{WeekStart}}", schedule.StartDate.ToString("dd.MM.yyyy") },
                { "{{WeekEnd}}", schedule.EndDate.ToString("dd.MM.yyyy") }
            };

            return SendTemplateAsync(
                e.User.Email!,
                "Your Schedule Has Been Deactivated",
                "scheduleInactive.html",
                placeholders);
        });

        var managerTasks = managers.Select(e =>
        {
            var placeholders = new Dictionary<string, string>
            {
                { "{{EmployeeName}}", FullName(e) },
                { "{{ScheduleName}}", schedule.Name },
                { "{{WeekStart}}", schedule.StartDate.ToString("dd.MM.yyyy") },
                { "{{WeekEnd}}", schedule.EndDate.ToString("dd.MM.yyyy") }
            };

            return SendTemplateAsync(
                e.Email!,
                "Your Schedule Has Been Deactivated",
                "scheduleInactiveManager.html",
                placeholders);
        });

        await Task.WhenAll(tasks);
        await Task.WhenAll(managerTasks);
    }

    /// <summary>
    /// Sends a notification about the account creation to the user that created the account.
    /// </summary>
    /// <param name="newUser"> The newly created user. </param>
    /// <returns></returns>
    public async Task SendUserRegisterMail(User newUser)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "{{FullName}}", FullName(newUser) },
            { "{{Email}}", newUser.Email! }
        };

        await SendTemplateAsync(
            newUser.Email!,
            "Welcome to Schichtpilot — Your Account is Ready",
            "register.html",
            placeholders);
    }

    // All employees
    /// <summary>
    /// Sends the workschedule to all assigned users.
    /// </summary>
    /// <param name="schedule"> The workschedule to be sent. </param>
    /// <returns></returns>
    public async Task SendScheduleMail(WorkScheduleDto schedule)
    {
        var shiftTable = BuildShiftTable(schedule);

        var tasks = schedule.AssignedUsers.Select(e =>
        {
            var placeholders = new Dictionary<string, string>
            {
                { "{{EmployeeName}}", e.User.LastName },
                { "{{ScheduleName}}", schedule.Name },
                { "{{WeekStart}}", schedule.StartDate.ToString("dd.MM.yyyy") },
                { "{{WeekEnd}}", schedule.EndDate.ToString("dd.MM.yyyy") },
                { "{{ShiftTable}}", shiftTable }
            };

            return SendTemplateAsync(
                e.User.Email, $"Your Schedule for {schedule.StartDate:dd.MM.yyyy}",
                "schedule.html",
                placeholders);
        });

        await Task.WhenAll(tasks);
    }

    // ──────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the Tues–Sun HTML shift table from a WorkScheduleDto.
    /// Flattens all TimeSlots across all Shifts, keyed by DayOfWeek.
    /// Days with no timeslot render as "Day off".
    /// </summary>
    private static string BuildShiftTable(WorkScheduleDto schedule)
    {
        var slotsByDay = schedule.Shifts
            .Where(s => s.TimeSlots != null)
            .SelectMany(s => s.TimeSlots.Select(ts => new
            {
                Day = ts.DayOfWeek,
                ShiftName = s.Name,
                StartTime = ts.StartTime.ToString(@"HH\:mm"),
                EndTime = ts.EndTime.ToString(@"HH\:mm")
            }))
            .GroupBy(ts => ts.Day)
            .ToDictionary(g => g.Key, g => g.First());

        var sb = new StringBuilder();
        sb.Append(@"
            <table style='width:100%;border-collapse:collapse;font-family:DM Sans,sans-serif;font-size:14px;'>
              <thead>
                <tr style='background-color:#1A1A2E;color:#E8C547;'>
                  <th style='padding:10px 14px;text-align:left;border:1px solid #2E2E4E;'>Day</th>
                  <th style='padding:10px 14px;text-align:left;border:1px solid #2E2E4E;'>Shift</th>
                  <th style='padding:10px 14px;text-align:left;border:1px solid #2E2E4E;'>Start</th>
                  <th style='padding:10px 14px;text-align:left;border:1px solid #2E2E4E;'>End</th>
                </tr>
              </thead>
              <tbody>");

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            slotsByDay.TryGetValue(day, out var slot);
            var hasShift = slot is not null;
            var bgColor = hasShift ? "#ffffff" : "#F8F6F1";

            var cells = hasShift
                ? $"<td style='padding:10px 14px;border:1px solid #E8E3D9;'>{slot!.ShiftName}</td>" +
                  $"<td style='padding:10px 14px;border:1px solid #E8E3D9;font-family:monospace;'>{slot.StartTime}</td>" +
                  $"<td style='padding:10px 14px;border:1px solid #E8E3D9;font-family:monospace;'>{slot.EndTime}</td>"
                : "<td colspan='3' style='padding:10px 14px;border:1px solid #E8E3D9;" +
                  "color:#BBB;font-style:italic;text-align:center;'>— Day off —</td>";

            sb.Append($@"
                <tr style='background-color:{bgColor};'>
                  <td style='padding:10px 14px;border:1px solid #E8E3D9;font-weight:600;color:#1A1A2E;'>{day}</td>
                  {cells}
                </tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    /// <summary>
    /// Creates the email from a template and sends it.
    /// </summary>
    /// <param name="toEmail"> The reciver of the email. </param>
    /// <param name="subject"> The subject of the email. </param>
    /// <param name="templateFileName"> The filename of the template to be used. </param>
    /// <param name="placeholders"> The placeholders and the value of the actual data. </param>
    /// <exception cref="FileNotFoundException"> Is thrown when the template couldn't be found. </exception>
    private async Task SendTemplateAsync(
        string toEmail,
        string subject,
        string templateFileName,
        Dictionary<string, string> placeholders)
    {
        var filePath = Path.Combine(_templatesPath, templateFileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Email template not found: {filePath}");

        // Load HTML file from disk
        var html = await File.ReadAllTextAsync(filePath);

        // Replace placeholders with real values
        foreach (var (key, value) in placeholders)
            html = html.Replace(key, value);

        if (_sendMail)
        {
            await SendAsync(toEmail, subject, html);
        }
    }

    // Communication with Azure
    /// <summary>
    /// Sends the email to the given subject.
    /// </summary>
    /// <param name="toEmail"> The receiver of the email. </param>
    /// <param name="subject"> The subject of the mail </param>
    /// <param name="htmlBody"> The email body as HTML. </param>
    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var emailMessage = new Azure.Communication.Email.EmailMessage(
                senderAddress: _senderAddress,
                content: new EmailContent(subject) { Html = htmlBody },
                recipients: new EmailRecipients(new List<EmailAddress>
                {
                    new EmailAddress(toEmail)
                })
            );

            EmailSendOperation operation = await _emailClient.SendAsync(
                WaitUntil.Completed,
                emailMessage
            );

            _logger.LogInformation(
                "Email sent to {Email} | Subject: {Subject} | Operation: {OperationId}",
                toEmail, subject, operation.Id);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex,
                "ACS failed sending to {Email} | Code: {Code} | Message: {Message}",
                toEmail, ex.ErrorCode, ex.Message);
        }
    }

    private static string FullName(User user) =>
        $"{user.FirstName} {user.LastName}";
}