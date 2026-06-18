using Data.Entities;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Controllers;

using Microsoft.AspNetCore.Mvc;
using Models.DTOs;

[ApiController]
[Route("api/test-email")]
public class TestEmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public TestEmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    // Test: absence notification to all managers
    [HttpPost("absence-to-manager")]
    public async Task<IActionResult> TestAbsenceToManager([FromQuery] string toEmail)
    {
        var fakeEmployee = new User
        {
            FirstName = "Max",
            LastName = "Mustermann",
            Email = toEmail
        };

        var fakeAbsence = new AbsenceDto
        {
            StartDate = DateTime.Now.AddDays(3),
            EndDate = DateTime.Now.AddDays(7),
            AbsenceType = AbsenceTypeEnum.Vacation,
            Message = "Family holiday, planned in advance."
        };

        await _emailService.SendNewAbsenceMailToManager(fakeEmployee, fakeAbsence);
        return Ok("Absence notification sent to all managers.");
    }

    // Test: approval email to a specific address
    [HttpPost("approval")]
    public async Task<IActionResult> TestApproval([FromQuery] string toEmail)
    {
        var fakeEmployee = new User
        {
            FirstName = "Anna",
            LastName = "Schmidt",
            Email = toEmail
        };

        var fakeAbsence = new AbsenceDto
        {
            StartDate = DateTime.Now.AddDays(3),
            EndDate = DateTime.Now.AddDays(7),
            ManagerMessage = "Approved — enjoy your time off!"
        };

        await _emailService.SendApprovalMail(fakeEmployee, fakeAbsence);
        return Ok($"Approval email sent to {toEmail}.");
    }

    // Test: rejection email
    [HttpPost("rejection")]
    public async Task<IActionResult> TestRejection([FromQuery] string toEmail)
    {
        var fakeEmployee = new User
        {
            FirstName = "Anna",
            LastName = "Schmidt",
            Email = toEmail
        };

        var fakeAbsence = new AbsenceDto
        {
            StartDate = DateTime.Now.AddDays(3),
            EndDate = DateTime.Now.AddDays(7),
            ManagerMessage = "Unfortunately we are fully booked that week."
        };

        await _emailService.SendRejectionMail(fakeEmployee, fakeAbsence);
        return Ok($"Rejection email sent to {toEmail}.");
    }

    // Test: welcome email for new user
    [HttpPost("register")]
    public async Task<IActionResult> TestRegister([FromQuery] string toEmail)
    {
        var fakeUser = new User
        {
            FirstName = "Lisa",
            LastName = "Maier",
            Email = toEmail
        };

        await _emailService.SendUserRegisterMail(fakeUser);
        return Ok($"Registration email sent to {toEmail}.");
    }

    // Test: schedule published — sends to ALL employees in the system
    [HttpPost("schedule")]
    public async Task<IActionResult> TestSchedule()
    {
        var fakeSchedule = new WorkScheduleDto
        {
            Name = "Week 14 — Kitchen",
            StartDate = new DateTime(2026, 4, 7), // Tuesday
            EndDate = new DateTime(2026, 4, 13), // Sunday
            IsActive = true,
            IsValid = true,
            Shifts = new List<ShiftDto>
            {
                new ShiftDto
                {
                    Id = 1, Name = "Morning", ColorAsHex = "#FFD700",
                    TimeSlots = new List<TimeSlotDto>
                    {
                        new()
                        {
                            Id = 1, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(8, 0),
                            EndTime = new TimeOnly(16, 0), Breaks = []
                        },
                        new()
                        {
                            Id = 2, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(8, 0),
                            EndTime = new TimeOnly(16, 0), Breaks = []
                        },
                        new()
                        {
                            Id = 3, DayOfWeek = DayOfWeek.Friday, StartTime = new TimeOnly(8, 0),
                            EndTime = new TimeOnly(16, 0), Breaks = []
                        }
                    }
                },
                new ShiftDto
                {
                    Id = 2, Name = "Evening", ColorAsHex = "#4A90D9",
                    TimeSlots = new List<TimeSlotDto>
                    {
                        new()
                        {
                            Id = 4, DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeOnly(16, 0),
                            EndTime = new TimeOnly(23, 0), Breaks = []
                        },
                        new()
                        {
                            Id = 5, DayOfWeek = DayOfWeek.Saturday, StartTime = new TimeOnly(16, 0),
                            EndTime = new TimeOnly(23, 0), Breaks = []
                        },
                        new()
                        {
                            Id = 6, DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(16, 0),
                            EndTime = new TimeOnly(23, 0), Breaks = []
                        }
                    }
                }
            }
        };

        await _emailService.SendScheduleMail(fakeSchedule);
        return Ok("Schedule email sent to all employees.");
    }

    // Test: schedule deactivated for a specific employee
    [HttpPost("schedule-inactive")]
    public async Task<IActionResult> TestScheduleInactive([FromQuery] string toEmail)
    {
        var fakeEmployee = new User
        {
            FirstName = "Tom",
            LastName = "Bauer",
            Email = toEmail
        };

        var fakeSchedule = new WorkScheduleDto
        {
            Name = "Week 14 — Kitchen",
            StartDate = new DateTime(2026, 4, 7),
            EndDate = new DateTime(2026, 4, 13),
            IsActive = false,
            IsValid = false
        };

        await _emailService.SendScheduleInActiveMail(fakeSchedule);
        return Ok($"Schedule deactivation email sent to {toEmail}.");
    }
}