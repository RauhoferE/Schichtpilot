using Azure;
using Azure.Communication.Email;
using Schichtpilot.Models.DTOs;
using System.Text;
using Data.Entities;

namespace Schichtpilot.Services;

public class EmailService : IEmailService
{
    private readonly EmailClient _emailClient;

    // private readonly UserManager<User> _userManager;
    private readonly string _senderAddress;
    private readonly string _templatesPath;
    private readonly ILogger<EmailService> _logger;

    // Tuesday–Sunday: Monday is the restaurant's weekly day off
    private static readonly DayOfWeek[] WorkWeek =
    {
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    };

    public EmailService(
        IConfiguration configuration,
       // UserManager<User> userManager,
        ILogger<EmailService> logger)
    {
        // _userManager = userManager;
        _logger = logger;

        var connectionString = configuration["AzureEmail:ConnectionString"]
                               ?? throw new InvalidOperationException("AzureEmail:ConnectionString is missing.");

        _senderAddress = configuration["AzureEmail:SenderAddress"]
                         ?? throw new InvalidOperationException("AzureEmail:SenderAddress is missing.");

        _emailClient = new EmailClient(connectionString);
        _templatesPath = Path.Combine(AppContext.BaseDirectory, "Services" ,"EmailTemplate");
    }

    // All managers notified
    
    public async Task SendNewAbsenceMailToManager(User employee, AbsenceDto absence)
    {
        var testManagers = new List<(string Email, string Name)>
        {
            ("rezaifariba01@gmail.com", "Fariba Rezai"),
            ("manager2@gmail.com", "Manager 2")
        };

        var tasks = testManagers.Select(m =>
        {
            var placeholders = new Dictionary<string, string>
            {
                { "{{ManagerName}}", m.Name },
                { "{{EmployeeName}}", $"{employee.FirstName} {employee.LastName}" },
                { "{{StartDate}}", absence.StartDate.ToString("dd.MM.yyyy") },
                { "{{EndDate}}", absence.EndDate.ToString("dd.MM.yyyy") },
                { "{{AbsenceType}}", absence.AbsenceType.ToString() },
                { "{{Message}}", absence.Message }
            };

            return SendTemplateAsync(m.Email, $"New Absence Request from {employee.FirstName}", "absence.html",
                placeholders);
        });
        await Task.WhenAll(tasks);
    }
    // ──────────────────────────────────────────────────────────────
    // Specific employee
    // ──────────────────────────────────────────────────────────────

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

    public async Task SendScheduleInActiveMail(User employee, WorkScheduleDto schedule)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "{{EmployeeName}}", FullName(employee) },
            { "{{ScheduleName}}", schedule.Name },
            { "{{WeekStart}}", schedule.StartDate.ToString("dd.MM.yyyy") },
            { "{{WeekEnd}}", schedule.EndDate.ToString("dd.MM.yyyy") }
        };

        await SendTemplateAsync(
            employee.Email!,
            "Your Schedule Has Been Deactivated",
            "scheduleInactive.html",
            placeholders);
    }

    public async Task SendUserRegisterMail(User newUser, string temporaryPassword)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "{{FullName}}", FullName(newUser) },
            { "{{Email}}", newUser.Email! },
            { "{{TemporaryPassword}}", temporaryPassword }
        };

        await SendTemplateAsync(
            newUser.Email!,
            "Welcome to Schichtpilot — Your Account is Ready",
            "register.html",
            placeholders);
    }

    public async Task SendScheduleMail(WorkScheduleDto schedule)
    {
        // 🧪 TEMP: hardcoded test recipient — replace with UserManager later
        var testEmployees = new List<(string Email, string Name)>
        {
            ("your-real-email@gmail.com", "Test Employee")
        };

        var shiftTable = BuildShiftTable(schedule);

        var tasks = testEmployees.Select(e =>
        {
            var placeholders = new Dictionary<string, string>
            {
                { "{{EmployeeName}}", e.Name },
                { "{{ScheduleName}}", schedule.Name },
                { "{{WeekStart}}", schedule.StartDate.ToString("dd.MM.yyyy") },
                { "{{WeekEnd}}", schedule.EndDate.ToString("dd.MM.yyyy") },
                { "{{ShiftTable}}", shiftTable }
            };
            return SendTemplateAsync(e.Email, $"Your Schedule for {schedule.StartDate:dd.MM.yyyy}", "schedule.html",
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

        foreach (var day in WorkWeek)
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
    
    private async Task SendTemplateAsync(
        string toEmail,
        string subject,
        string templateFileName,
        Dictionary<string, string> placeholders)
    {
        var filePath = Path.Combine(_templatesPath, templateFileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Email template not found: {filePath}");
        
        // load HTML file from disk
        var html = await File.ReadAllTextAsync(filePath);
        
        //replace placeholder with values
        foreach (var (key, value) in placeholders)
            html = html.Replace(key, value);

        await SendAsync(toEmail, subject, html);
    }
    //communication with Azure
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
            throw;
        }
    }

    private static string FullName(User user) =>
        $"{user.FirstName} {user.LastName}";
}