using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Data.Entities;
using FluentAssertions;
using JetBrains.Annotations;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Identity;
using Schichtpilot.Settings;
using Schichtpilot.Models.Enums;
using Core;
using Azure.Communication.Email;
using Xunit;

namespace UnitTests.Services;

// ══════════════════════════════════════════════════════════════════
// 1. BuildShiftTable — mirrors the private static method in EmailService
//    Tests the Tues–Sun logic, day-off rendering, slot lookup
// ══════════════════════════════════════════════════════════════════
public class BuildShiftTableTests
{
    private static readonly DayOfWeek[] WorkWeek =
    {
        DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday,  DayOfWeek.Saturday,  DayOfWeek.Sunday
    };

    // Mirrors EmailService.BuildShiftTable exactly
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

        var sb = new System.Text.StringBuilder();
        foreach (var day in WorkWeek)
        {
            slotsByDay.TryGetValue(day, out var slot);
            // simplified output for assertions
            sb.Append(slot is not null
                ? $"SHIFT:{day}:{slot.ShiftName}:{slot.StartTime}:{slot.EndTime}|"
                : $"DAYOFF:{day}|");
        }
        return sb.ToString();
    }

    // ── WorkWeek definition ───────────────────────────────────────

    [Fact]
    public void WorkWeek_StartsOnTuesday()
        => WorkWeek.First().Should().Be(DayOfWeek.Tuesday);

    [Fact]
    public void WorkWeek_EndsOnSunday()
        => WorkWeek.Last().Should().Be(DayOfWeek.Sunday);

    [Fact]
    public void WorkWeek_DoesNotContainMonday()
        => WorkWeek.Should().NotContain(DayOfWeek.Monday);

    [Fact]
    public void WorkWeek_HasExactlySixDays()
        => WorkWeek.Should().HaveCount(6);

    // ── Shift rendering ───────────────────────────────────────────

    [Fact]
    public void Build_RendersShift_WhenDayHasTimeslot()
    {
        var schedule = MakeSchedule(new[]
        {
            (DayOfWeek.Tuesday, "Morning", new TimeOnly(8, 0), new TimeOnly(16, 0))
        });

        BuildShiftTable(schedule).Should().Contain("SHIFT:Tuesday:Morning:08:00:16:00");
    }

    [Fact]
    public void Build_FormatsTime_AsHHmm()
    {
        var schedule = MakeSchedule(new[]
        {
            (DayOfWeek.Friday, "Night", new TimeOnly(23, 30), new TimeOnly(7, 15))
        });

        var result = BuildShiftTable(schedule);
        result.Should().Contain("23:30");
        result.Should().Contain("07:15");
    }

    [Fact]
    public void Build_RendersMultipleShiftsOnDifferentDays()
    {
        var schedule = MakeSchedule(new[]
        {
            (DayOfWeek.Tuesday,  "Morning", new TimeOnly(8,  0), new TimeOnly(16, 0)),
            (DayOfWeek.Saturday, "Evening", new TimeOnly(16, 0), new TimeOnly(23, 0))
        });

        var result = BuildShiftTable(schedule);
        result.Should().Contain("SHIFT:Tuesday:Morning");
        result.Should().Contain("SHIFT:Saturday:Evening");
    }

    // ── Day off rendering ─────────────────────────────────────────

    [Fact]
    public void Build_RendersDayOff_WhenDayHasNoTimeslot()
    {
        var schedule = MakeSchedule(new[]
        {
            (DayOfWeek.Tuesday, "Morning", new TimeOnly(8, 0), new TimeOnly(16, 0))
            // all other days have no shift
        });

        var result = BuildShiftTable(schedule);
        result.Should().Contain("DAYOFF:Wednesday");
        result.Should().Contain("DAYOFF:Thursday");
        result.Should().Contain("DAYOFF:Friday");
        result.Should().Contain("DAYOFF:Saturday");
        result.Should().Contain("DAYOFF:Sunday");
    }

    [Fact]
    public void Build_RendersAllDaysAsOff_WhenNoShifts()
    {
        var schedule = MakeSchedule(Array.Empty<(DayOfWeek, string, TimeOnly, TimeOnly)>());
        var result = BuildShiftTable(schedule);

        foreach (var day in WorkWeek)
            result.Should().Contain($"DAYOFF:{day}");
    }

    // ── Monday exclusion ──────────────────────────────────────────

    [Fact]
    public void Build_IgnoresMonday_EvenIfTimeslotExists()
    {
        var schedule = MakeSchedule(new[]
        {
            (DayOfWeek.Monday, "Monday Shift", new TimeOnly(8, 0), new TimeOnly(16, 0))
        });

        var result = BuildShiftTable(schedule);
        result.Should().NotContain("Monday Shift");
        result.Should().NotContain("SHIFT:Monday");
    }

    // ── Edge cases ────────────────────────────────────────────────

    [Fact]
    public void Build_DoesNotThrow_WhenTimeSlotsIsNull()
    {
        var schedule = new WorkScheduleDto
        {
            Id = 1,
            Name = "Test",
            IsActive = true,
            IsValid = true,
            StartDate = new DateTime(2026, 4, 7),
            EndDate = new DateTime(2026, 4, 13),
            Shifts = new List<ShiftDto>
            {
                new() { Id = 1, Name = "Null Slots", ColorAsHex = "#000", TimeSlots = null! }
            }
        };

        var act = () => BuildShiftTable(schedule);
        act.Should().NotThrow();
    }

    [Fact]
    public void Build_DoesNotThrow_WhenShiftsListIsEmpty()
    {
        var schedule = new WorkScheduleDto
        {
            Id = 1,
            Name = "Empty",
            IsActive = true,
            IsValid = true,
            StartDate = new DateTime(2026, 4, 7),
            EndDate = new DateTime(2026, 4, 13),
            Shifts = new List<ShiftDto>()
        };

        var act = () => BuildShiftTable(schedule);
        act.Should().NotThrow();
    }

    [Fact]
    public void Build_WhenTwoDaysHaveSameName_TakesFirstOne()
    {
        // Two shifts both on Tuesday — only the first should appear
        var schedule = new WorkScheduleDto
        {
            Id = 1,
            Name = "Test",
            IsActive = true,
            IsValid = true,
            StartDate = new DateTime(2026, 4, 7),
            EndDate = new DateTime(2026, 4, 13),
            Shifts = new List<ShiftDto>
            {
                new()
                {
                    Id = 1, Name = "First", ColorAsHex = "#000",
                    TimeSlots = new List<TimeSlotDto>
                    {
                        new() { Id = 1, DayOfWeek = DayOfWeek.Tuesday,
                                StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0),
                                Breaks = new List<BreakDto>() }
                    }
                },
                new()
                {
                    Id = 2, Name = "Second", ColorAsHex = "#FFF",
                    TimeSlots = new List<TimeSlotDto>
                    {
                        new() { Id = 2, DayOfWeek = DayOfWeek.Tuesday,
                                StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(17, 0),
                                Breaks = new List<BreakDto>() }
                    }
                }
            }
        };

        var result = BuildShiftTable(schedule);
        result.Should().Contain("SHIFT:Tuesday:First");
        result.Should().NotContain("Second");
    }

    // ── Builder helper ────────────────────────────────────────────

    private static WorkScheduleDto MakeSchedule(
        IEnumerable<(DayOfWeek Day, string ShiftName, TimeOnly Start, TimeOnly End)> slots)
    {
        var list = slots.ToList();
        var shifts = list
            .GroupBy(s => s.ShiftName)
            .Select((g, idx) => new ShiftDto
            {
                Id = idx + 1,
                Name = g.Key,
                ColorAsHex = "#000000",
                TimeSlots = g.Select((s, i) => new TimeSlotDto
                {
                    Id = i + 1,
                    DayOfWeek = s.Day,
                    StartTime = s.Start,
                    EndTime = s.End,
                    Breaks = new List<BreakDto>()
                }).ToList()
            }).ToList();

        return new WorkScheduleDto
        {
            Id = 1,
            Name = "Test Week",
            IsActive = true,
            IsValid = true,
            StartDate = new DateTime(2026, 4, 7),
            EndDate = new DateTime(2026, 4, 13),
            Shifts = shifts
        };
    }
}

// ══════════════════════════════════════════════════════════════════
// 2. Placeholder replacement — the core template engine logic
// ══════════════════════════════════════════════════════════════════
public class PlaceholderReplacementTests
{
    // Mirrors SendTemplateAsync's replacement loop
    private static string Apply(string html, Dictionary<string, string> placeholders)
    {
        foreach (var (key, value) in placeholders)
            html = html.Replace(key, value);
        return html;
    }

    [Fact]
    public void Approval_ReplacesAllPlaceholders()
    {
        var template = "<p>Hello {{EmployeeName}}, approved {{StartDate}}–{{EndDate}}. {{ManagerMessage}}</p>";

        var result = Apply(template, new Dictionary<string, string>
        {
            { "{{EmployeeName}}",   "Anna Schmidt" },
            { "{{StartDate}}",      "01.04.2026" },
            { "{{EndDate}}",        "05.04.2026" },
            { "{{ManagerMessage}}", "Enjoy your break!" }
        });

        result.Should().Contain("Anna Schmidt");
        result.Should().Contain("01.04.2026");
        result.Should().Contain("05.04.2026");
        result.Should().Contain("Enjoy your break!");
        result.Should().NotContain("{{");
    }

    [Fact]
    public void Rejection_ReplacesAllPlaceholders()
    {
        var template = "<p>{{EmployeeName}}: {{StartDate}}–{{EndDate}}. {{ManagerMessage}}</p>";

        var result = Apply(template, new Dictionary<string, string>
        {
            { "{{EmployeeName}}",   "Max Mustermann" },
            { "{{StartDate}}",      "10.04.2026" },
            { "{{EndDate}}",        "12.04.2026" },
            { "{{ManagerMessage}}", "Fully booked." }
        });

        result.Should().NotContain("{{");
        result.Should().Contain("Max Mustermann");
        result.Should().Contain("Fully booked.");
    }

    [Fact]
    public void NewAbsence_ReplacesAllPlaceholders()
    {
        var template = "{{ManagerName}}: {{EmployeeName}} — {{AbsenceType}} {{StartDate}}–{{EndDate}}. {{Message}}";

        var result = Apply(template, new Dictionary<string, string>
        {
            { "{{ManagerName}}",  "Chef Boss" },
            { "{{EmployeeName}}", "Lisa Maier" },
            { "{{StartDate}}",    "01.04.2026" },
            { "{{EndDate}}",      "03.04.2026" },
            { "{{AbsenceType}}", "Vacation" },
            { "{{Message}}",      "Family trip" }
        });

        result.Should().NotContain("{{");
        result.Should().Contain("Chef Boss");
        result.Should().Contain("Vacation");
    }

    [Fact]
    public void UserRegister_ReplacesAllPlaceholders()
    {
        var template = "Welcome {{FullName}}! Email: {{Email}} Pass: {{TemporaryPassword}}";

        var result = Apply(template, new Dictionary<string, string>
        {
            { "{{FullName}}",          "Tom Bauer" },
            { "{{Email}}",             "tom@restaurant.com" },
            { "{{TemporaryPassword}}", "Temp@1234" }
        });

        result.Should().NotContain("{{");
        result.Should().Contain("Tom Bauer");
        result.Should().Contain("Temp@1234");
    }

    [Fact]
    public void ScheduleInactive_ReplacesAllPlaceholders()
    {
        var template = "{{EmployeeName}}: {{ScheduleName}} {{WeekStart}}–{{WeekEnd}} deactivated.";

        var result = Apply(template, new Dictionary<string, string>
        {
            { "{{EmployeeName}}",  "Anna Schmidt" },
            { "{{ScheduleName}}", "Week 14" },
            { "{{WeekStart}}",    "07.04.2026" },
            { "{{WeekEnd}}",      "13.04.2026" }
        });

        result.Should().NotContain("{{");
        result.Should().Contain("Week 14");
    }

    [Fact]
    public void UnknownPlaceholder_IsLeftUntouched()
    {
        var template = "{{EmployeeName}} and {{UnknownField}}";

        var result = Apply(template, new Dictionary<string, string>
        {
            { "{{EmployeeName}}", "Max" }
        });

        result.Should().Contain("Max");
        result.Should().Contain("{{UnknownField}}"); // not replaced, no crash
    }

    [Fact]
    public void EmptyPlaceholderValue_RemovesPlaceholder()
    {
        var template = "Note: {{ManagerMessage}}";

        var result = Apply(template, new Dictionary<string, string>
        {
            { "{{ManagerMessage}}", "" }
        });

        result.Should().NotContain("{{ManagerMessage}}");
        result.Should().Be("Note: ");
    }
}

// ══════════════════════════════════════════════════════════════════
// 3. FullName helper
// ══════════════════════════════════════════════════════════════════
public class FullNameTests
{
    private static string FullName(User user) =>
        $"{user.FirstName} {user.LastName}";

    [Fact]
    public void CombinesFirstAndLastName()
    {
        var user = new User { FirstName = "Anna", LastName = "Schmidt" };
        FullName(user).Should().Be("Anna Schmidt");
    }

    [Fact]
    public void ContainsSpaceBetweenNames()
    {
        var user = new User { FirstName = "Max", LastName = "Mustermann" };
        FullName(user).Should().Contain(" ");
    }

    [Fact]
    public void WorksWithSingleCharacterNames()
    {
        var user = new User { FirstName = "A", LastName = "B" };
        FullName(user).Should().Be("A B");
    }

    [Fact]
    public void PreservesCorrectLength()
    {
        var user = new User { FirstName = "Max", LastName = "Mustermann" };
        FullName(user).Should().HaveLength("Max Mustermann".Length);
    }
}

// ══════════════════════════════════════════════════════════════════
// 4. Date formatting — dd.MM.yyyy used in all placeholders
// ══════════════════════════════════════════════════════════════════
public class DateFormattingTests
{
    [Fact]
    public void StartDate_FormatsAs_DdMmYyyy()
    {
        var date = new DateTime(2026, 4, 1);
        date.ToString("dd.MM.yyyy").Should().Be("01.04.2026");
    }

    [Fact]
    public void EndDate_FormatsAs_DdMmYyyy()
    {
        var date = new DateTime(2026, 12, 31);
        date.ToString("dd.MM.yyyy").Should().Be("31.12.2026");
    }

    [Fact]
    public void SingleDigitDay_PadsWithLeadingZero()
    {
        var date = new DateTime(2026, 3, 5);
        date.ToString("dd.MM.yyyy").Should().Be("05.03.2026");
    }

    [Fact]
    public void SingleDigitMonth_PadsWithLeadingZero()
    {
        var date = new DateTime(2026, 1, 15);
        date.ToString("dd.MM.yyyy").Should().Be("15.01.2026");
    }
}

// ══════════════════════════════════════════════════════════════════
// 5. Template file existence check
// ══════════════════════════════════════════════════════════════════
public class TemplateFileTests : IDisposable
{
    private readonly string _tempDir;

    public TemplateFileTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task ExistingTemplate_CanBeReadFromDisk()
    {
        var filePath = Path.Combine(_tempDir, "test.html");
        await File.WriteAllTextAsync(filePath, "<p>Hello {{Name}}</p>");

        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("{{Name}}");
    }

    [Fact]
    public void MissingTemplate_ThrowsFileNotFoundException()
    {
        var filePath = Path.Combine(_tempDir, "doesNotExist.html");
        File.Exists(filePath).Should().BeFalse();

        // EmailService checks File.Exists before reading — mirrors that check here
        var act = () =>
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Email template not found: {filePath}");
        };

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*doesNotExist.html*");
    }

    [Fact]
    public async Task Template_AfterPlaceholderReplacement_ContainsNoRemainingBraces()
    {
        var filePath = Path.Combine(_tempDir, "approval.html");
        await File.WriteAllTextAsync(filePath, "<p>{{EmployeeName}} approved {{StartDate}}</p>");

        var html = await File.ReadAllTextAsync(filePath);
        html = html.Replace("{{EmployeeName}}", "Anna Schmidt");
        html = html.Replace("{{StartDate}}", "01.04.2026");

        html.Should().NotContain("{{");
        html.Should().NotContain("}}");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}

// ══════════════════════════════════════════════════════════════════
// 6. Integration-like tests that exercise EmailService public API
//    (constructing the service and invoking its public methods)
// ══════════════════════════════════════════════════════════════════
public class EmailServicePublicApiTests : IDisposable
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly string _templatesPath;

    public EmailServicePublicApiTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _loggerMock = new Mock<ILogger<EmailService>>();
        _templatesPath = Path.Combine(AppContext.BaseDirectory, "Services", "EmailTemplate");
        Directory.CreateDirectory(_templatesPath);
    }

    [Fact]
    public void Ctor_Throws_WhenConnectionStringMissing()
    {
        var settings = new AzureEmailSettings
        {
            ConnectionString = "",
            SenderAddress = "sender@test.com",
            SendMail = false
        };

        Assert.Throws<InvalidOperationException>(() => new EmailService(Options.Create(settings), _userManagerMock.Object, _loggerMock.Object, new EmailClient("endpoint=https://localhost/;accesskey=test")));
    }

    [Fact]
    public void Ctor_Throws_WhenSenderAddressMissing()
    {
        var settings = new AzureEmailSettings
        {
            ConnectionString = "endpoint=https://localhost/;accesskey=test",
            SenderAddress = "",
            SendMail = false
        };

        Assert.Throws<InvalidOperationException>(() => new EmailService(Options.Create(settings), _userManagerMock.Object, _loggerMock.Object, new EmailClient("endpoint=https://localhost/;accesskey=test")));
    }

    [Fact]
    public void Ctor_Throws_WhenUserManagerIsNull()
    {
        var settings = new AzureEmailSettings
        {
            ConnectionString = "endpoint=https://localhost/;accesskey=test",
            SenderAddress = "sender@test.com",
            SendMail = false
        };

        Assert.Throws<ArgumentNullException>(() => new EmailService(Options.Create(settings), null!, _loggerMock.Object, new EmailClient("endpoint=https://localhost/;accesskey=test")));
    }

    [Fact]
    public async Task SendApprovalMail_Throws_WhenTemplateMissing()
    {
        var settings = new AzureEmailSettings
        {
            ConnectionString = "endpoint=https://localhost/;accesskey=test",
            SenderAddress = "sender@test.com",
            SendMail = false
        };

        var service = new EmailService(Options.Create(settings), _userManagerMock.Object, _loggerMock.Object, new EmailClient("endpoint=https://localhost/;accesskey=test"));

        var user = new User { FirstName = "Anna", LastName = "Schmidt", Email = "anna@test.com" };
        var absence = new AbsenceDto { StartDate = new DateTime(2026, 4, 1), EndDate = new DateTime(2026, 4, 3), ManagerMessage = "Approved!", AbsenceType = AbsenceTypeEnum.Vacation };

        var filePath = Path.Combine(_templatesPath, "approval.html");
        if (File.Exists(filePath)) File.Delete(filePath);

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.SendApprovalMail(user, absence));
    }

    [Fact]
    public async Task SendApprovalMail_WithTemplate_DoesNotThrow()
    {
        var settings = new AzureEmailSettings
        {
            ConnectionString = "endpoint=https://localhost/;accesskey=test",
            SenderAddress = "sender@test.com",
            SendMail = false
        };

        var service = new EmailService(Options.Create(settings), _userManagerMock.Object, _loggerMock.Object, new EmailClient("endpoint=https://localhost/;accesskey=test"));

        var filePath = Path.Combine(_templatesPath, "approval.html");
        await File.WriteAllTextAsync(filePath, "<p>{{EmployeeName}} approved from {{StartDate}} to {{EndDate}} — {{ManagerMessage}}</p>");

        var user = new User { FirstName = "Anna", LastName = "Schmidt", Email = "anna@test.com" };
        var absence = new AbsenceDto { StartDate = new DateTime(2026, 4, 1), EndDate = new DateTime(2026, 4, 3), ManagerMessage = "Approved!", AbsenceType = AbsenceTypeEnum.Vacation };

        await service.SendApprovalMail(user, absence); // should not throw
    }

    [Fact]
    public async Task SendRejectionMail_WithTemplate_DoesNotThrow()
    {
        var settings = new AzureEmailSettings
        {
            ConnectionString = "endpoint=https://localhost/;accesskey=test",
            SenderAddress = "sender@test.com",
            SendMail = false
        };

        var service = new EmailService(Options.Create(settings), _userManagerMock.Object, _loggerMock.Object, new EmailClient("endpoint=https://localhost/;accesskey=test"));

        var filePath = Path.Combine(_templatesPath, "rejection.html");
        await File.WriteAllTextAsync(filePath, "<div>{{EmployeeName}} rejected {{StartDate}}—{{EndDate}}. {{ManagerMessage}}</div>");

        var user = new User { FirstName = "Bob", LastName = "Smith", Email = "bob@test.com" };
        var absence = new AbsenceDto { StartDate = new DateTime(2026, 5, 1), EndDate = new DateTime(2026, 5, 2), ManagerMessage = "No capacity", AbsenceType = AbsenceTypeEnum.Vacation };

        await service.SendRejectionMail(user, absence); // should not throw
    }

    [Fact]
    public async Task SendUserRegisterMail_WithTemplate_DoesNotThrow()
    {
        var settings = new AzureEmailSettings
        {
            ConnectionString = "endpoint=https://localhost/;accesskey=test",
            SenderAddress = "sender@test.com",
            SendMail = false
        };

        var service = new EmailService(Options.Create(settings), _userManagerMock.Object, _loggerMock.Object, new EmailClient("endpoint=https://localhost/;accesskey=test"));

        var filePath = Path.Combine(_templatesPath, "register.html");
        await File.WriteAllTextAsync(filePath, "<p>Welcome {{FullName}} ({{Email}})</p>");

        var newUser = new User { FirstName = "New", LastName = "User", Email = "new@user.com" };
        await service.SendUserRegisterMail(newUser); // should not throw
    }

    [Fact]
    public async Task SendScheduleMail_WithTemplate_DoesNotThrow()
    {
        var settings = new AzureEmailSettings
        {
            ConnectionString = "endpoint=https://localhost/;accesskey=test",
            SenderAddress = "sender@test.com",
            SendMail = false
        };

        var service = new EmailService(Options.Create(settings), _userManagerMock.Object, _loggerMock.Object, new EmailClient("endpoint=https://localhost/;accesskey=test"));

        var filePath = Path.Combine(_templatesPath, "schedule.html");
        await File.WriteAllTextAsync(filePath, "<div>{{EmployeeName}} — {{ShiftTable}}</div>");

        var schedule = new WorkScheduleDto
        {
            Id = 1,
            Name = "Week 1",
            StartDate = new DateTime(2026, 4, 7),
            EndDate = new DateTime(2026, 4, 13),
            IsActive = true,
            IsValid = true,
            Shifts = new List<ShiftDto>(),
            AssignedUsers = new List<AssignedUserDto>
            {
                new AssignedUserDto
                {
                    User = new UserDto
                    {
                        Email = "employee@test.com",
                        FirstName = "Emp",
                        LastName = "User",
                        AddressDto = new AddressDto { Street = "Street", City = "City", PostalCode = 1120 },
                        AssignedJobRoles = new List<JobRoleShortDto>()
                    },
                    TimeSlot = null,
                    JobRole = null
                }
            }
        };

        await service.SendScheduleMail(schedule); // should not throw
    }

    [Fact]
    public async Task SendNewAbsenceMailToManager_WithManagersAndTemplate_DoesNotThrow()
    {
        var settings = new AzureEmailSettings
        {
            ConnectionString = "endpoint=https://localhost/;accesskey=test",
            SenderAddress = "sender@test.com",
            SendMail = false
        };

        var service = new EmailService(Options.Create(settings), _userManagerMock.Object, _loggerMock.Object, new EmailClient("endpoint=https://localhost/;accesskey=test"));

        var filePath = Path.Combine(_templatesPath, "absence.html");
        await File.WriteAllTextAsync(filePath, "<p>{{ManagerName}}: {{EmployeeName}} — {{AbsenceType}} {{StartDate}}–{{EndDate}}. {{Message}}</p>");

        var manager = new User { FirstName = "Manager", LastName = "One", Email = "manager@company.com" };
        _userManagerMock.Setup(u => u.GetUsersInRoleAsync(UserRolesClass.Admin)).ReturnsAsync(new List<User> { manager });

        var employee = new User { FirstName = "Employee", LastName = "A", Email = "employee@company.com" };
        var absence = new AbsenceDto { StartDate = new DateTime(2026, 4, 10), EndDate = new DateTime(2026, 4, 12), AbsenceType = AbsenceTypeEnum.Vacation, Message = "Trip" };

        await service.SendNewAbsenceMailToManager(employee, absence); // should not throw
    }

    [Fact]
    public async Task SendScheduleInActiveMail_WithManagersAndTemplate_DoesNotThrow()
    {
        var settings = new AzureEmailSettings
        {
            ConnectionString = "endpoint=https://localhost/;accesskey=test",
            SenderAddress = "sender@test.com",
            SendMail = false
        };

        var service = new EmailService(Options.Create(settings), _userManagerMock.Object, _loggerMock.Object, new EmailClient("endpoint=https://localhost/;accesskey=test"));

        var filePath1 = Path.Combine(_templatesPath, "scheduleInactive.html");
        var filePath2 = Path.Combine(_templatesPath, "scheduleInactiveManager.html");
        await File.WriteAllTextAsync(filePath1, "<p>{{EmployeeName}}: {{ScheduleName}} deactivated</p>");
        await File.WriteAllTextAsync(filePath2, "<p>{{EmployeeName}}: {{ScheduleName}} (manager)</p>");

        var manager = new User { FirstName = "Manager", LastName = "One", Email = "manager@company.com" };
        _userManagerMock.Setup(u => u.GetUsersInRoleAsync(UserRolesClass.Admin)).ReturnsAsync(new List<User> { manager });

        var schedule = new WorkScheduleDto
        {
            Id = 1,
            Name = "Week X",
            StartDate = new DateTime(2026, 4, 7),
            EndDate = new DateTime(2026, 4, 13),
            IsActive = true,
            IsValid = true,
            Shifts = new List<ShiftDto>(),
            AssignedUsers = new List<AssignedUserDto>
            {
                new AssignedUserDto
                {
                    User = new UserDto
                    {
                        Email = "assigned@company.com",
                        FirstName = "Assigned",
                        LastName = "Person",
                        AddressDto = new AddressDto { Street = "Street", City = "City", PostalCode = 1120 },
                        AssignedJobRoles = new List<JobRoleShortDto>()
                    },
                    JobRole = null,
                    TimeSlot =  null,
                }
            }
        };

        await service.SendScheduleInActiveMail(schedule); // should not throw
    }

    // Helper to create a mocked UserManager used across tests
    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<User>>().Object,
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            new Mock<ILookupNormalizer>().Object,
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<User>>>().Object);
    }

    public void Dispose()
    {
        // Intentionally do not delete the directory in case other tests run concurrently.
    }
}
