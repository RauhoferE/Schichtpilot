using AutoMapper;
using Data;
using Data.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Moq;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;
using Schichtpilot.Services;

namespace UnitTests.Services;

[TestSubject(typeof(WorkScheduleService))]
public class WorkScheduleServiceTest
{
    [Fact]
    public async Task GenerateSchedule_ShiftIdNotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5), // Monday
            EndDate = new DateTime(2026, 1, 11),  // Sunday
            ShiftIds = new List<int> { 999 }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GenerateScheduleAsync(dto));
        Assert.Equal("One or more shifts were not found.", ex.Message);
    }

    [Fact]
    public async Task GenerateSchedule_IntersectingTimeslots_ThrowsException()
    {
        await using var dbContext = CreateDbContext();

        var role = CreateJobRole(1, "Nurse");
        var shift1 = CreateShift(1, "Morning");
        var shift2 = CreateShift(2, "Overlap");

        shift1.Timeslots = new HashSet<Timeslot>
        {
            new()
            {
                Id = 1,
                ShiftId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            }
        };

        shift2.Timeslots = new HashSet<Timeslot>
        {
            new()
            {
                Id = 2,
                ShiftId = 2,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(15, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            }
        };

        shift1.JobRequirements = new HashSet<ShiftRequirement>
        {
            new() { ShiftId = 1, JobRoleId = 1, RequiredStaffCount = 1, JobRole = role, Shift = shift1 }
        };

        shift2.JobRequirements = new HashSet<ShiftRequirement>
        {
            new() { ShiftId = 2, JobRoleId = 1, RequiredStaffCount = 1, JobRole = role, Shift = shift2 }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.AddRange(shift1, shift2);
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        await dbContext.SaveChangesAsync();

        var user = CreateUser(1);
        dbContext.Users.Add(user);
        dbContext.UserJobRoles.Add(new UserJobRoles
        {
            UserId = user.Id,
            JobRoleId = role.Id,
            User = user,
            JobRole = role,
            ShiftAssignments = new HashSet<ShiftAssignment>()
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1, 2 }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GenerateScheduleAsync(dto));
        Assert.Equal("Shifts have intersections with each other.", ex.Message);
    }

    [Fact]
    public async Task GenerateSchedule_WorkPolicyMissing_ThrowsException()
    {
        await using var dbContext = CreateDbContext();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "Morning");
        shift.Timeslots = new HashSet<Timeslot>
        {
            new()
            {
                Id = 1,
                ShiftId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            }
        };
        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new() { ShiftId = 1, JobRoleId = 1, RequiredStaffCount = 1, JobRole = role, Shift = shift }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);

        var user = CreateUser(1);
        dbContext.Users.Add(user);
        dbContext.UserJobRoles.Add(new UserJobRoles
        {
            UserId = user.Id,
            JobRoleId = role.Id,
            User = user,
            JobRole = role,
            ShiftAssignments = new HashSet<ShiftAssignment>()
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1 }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GenerateScheduleAsync(dto));
        Assert.Equal("WorkPolicy not configured.", ex.Message);
    }

    [Fact]
    public async Task GenerateSchedule_NotEnoughStaff_ThrowsException()
    {
        await using var dbContext = CreateDbContext();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "Morning");

        shift.Timeslots = new HashSet<Timeslot>
        {
            new()
            {
                Id = 1,
                ShiftId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            }
        };

        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new() { ShiftId = 1, JobRoleId = 1, RequiredStaffCount = 2, JobRole = role, Shift = shift }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        var user = CreateUser(1);
        dbContext.Users.Add(user);
        dbContext.UserJobRoles.Add(new UserJobRoles
        {
            UserId = user.Id,
            JobRoleId = role.Id,
            User = user,
            JobRole = role,
            ShiftAssignments = new HashSet<ShiftAssignment>()
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1 }
        };

        await Assert.ThrowsAsync<Exception>(() => service.GenerateScheduleAsync(dto));
    }

    [Fact]
    public async Task GenerateSchedule_UserAbsent_ThrowsNotEnoughStaffException()
    {
        await using var dbContext = CreateDbContext();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "Morning");

        shift.Timeslots = new HashSet<Timeslot>
        {
            new()
            {
                Id = 1,
                ShiftId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            }
        };

        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new() { ShiftId = 1, JobRoleId = 1, RequiredStaffCount = 1, JobRole = role, Shift = shift }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        var user = CreateUser(1);
        dbContext.Users.Add(user);
        dbContext.UserJobRoles.Add(new UserJobRoles
        {
            UserId = user.Id,
            JobRoleId = role.Id,
            User = user,
            JobRole = role,
            ShiftAssignments = new HashSet<ShiftAssignment>()
        });

        dbContext.Absences.Add(new Absence
        {
            UserId = user.Id,
            User = user,
            StartDate = new DateTime(2026, 1, 5, 0, 0, 0),
            EndDate = new DateTime(2026, 1, 6, 0, 0, 0),
            AbsenceType = "Vacation",
            Status = AbsenceStatusEnum.Approved.ToString(),
            Message = "",
            ManagerMessage = ""
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1 }
        };

        await Assert.ThrowsAsync<Exception>(() => service.GenerateScheduleAsync(dto));
    }

    [Fact]
    public async Task GenerateSchedule_ConsecutiveHoursExceeded_ThrowsException()
    {
        await using var dbContext = CreateDbContext();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "LongShift");

        shift.Timeslots = new HashSet<Timeslot>
        {
            new()
            {
                Id = 1,
                ShiftId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new()
            {
                Id = 2,
                ShiftId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(12, 0),
                EndTime = new TimeOnly(16, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            }
        };

        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new() { ShiftId = 1, JobRoleId = 1, RequiredStaffCount = 1, JobRole = role, Shift = shift }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 6, // 8h chain should violate
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        var user = CreateUser(1);
        dbContext.Users.Add(user);
        dbContext.UserJobRoles.Add(new UserJobRoles
        {
            UserId = user.Id,
            JobRoleId = role.Id,
            User = user,
            JobRole = role,
            ShiftAssignments = new HashSet<ShiftAssignment>()
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1 }
        };

        await Assert.ThrowsAsync<Exception>(() => service.GenerateScheduleAsync(dto));
    }

    [Fact]
    public async Task GenerateSchedule_ValidInput_CreatesScheduleAndAssignments()
    {
        await using var dbContext = CreateDbContext();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "Morning");

        shift.Timeslots = new HashSet<Timeslot>
        {
            new()
            {
                Id = 1,
                ShiftId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new()
            {
                Id = 2,
                ShiftId = 1,
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            }
        };

        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new() { ShiftId = 1, JobRoleId = 1, RequiredStaffCount = 1, JobRole = role, Shift = shift }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        var user = CreateUser(1);
        dbContext.Users.Add(user);
        dbContext.UserJobRoles.Add(new UserJobRoles
        {
            UserId = user.Id,
            JobRoleId = role.Id,
            User = user,
            JobRole = role,
            ShiftAssignments = new HashSet<ShiftAssignment>()
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1 }
        };

        await service.GenerateScheduleAsync(dto);

        var createdSchedule = await dbContext.WorkSchedules
            .Include(x => x.Shifts)
            .Include(x => x.ShiftAssignments)
            .FirstOrDefaultAsync();

        Assert.NotNull(createdSchedule);
        Assert.Equal("Week 1", createdSchedule.Name);
        Assert.Equal(new DateTime(2026, 1, 5), createdSchedule.StartDate.Date);
        Assert.Equal(new DateTime(2026, 1, 11), createdSchedule.EndDate.Date);
        Assert.True(createdSchedule.IsValid);
        Assert.False(createdSchedule.IsActive);
        Assert.Single(createdSchedule.Shifts);
        Assert.Equal(2, createdSchedule.ShiftAssignments.Count);
    }

    [Fact]
    public async Task GenerateSchedule_WithFiveUsers_TwoShifts_TwoRequirementsEach_AssignsExactRequiredCounts()
    {
        await using var dbContext = CreateDbContext();

        // Roles
        var nurseRole = CreateJobRole(1, "Nurse");
        var doctorRole = CreateJobRole(2, "Doctor");
        dbContext.JobRoles.AddRange(nurseRole, doctorRole);

        // Shift 1 - Monday
        var shift1 = CreateShift(1, "Shift-Monday");
        var shift1Timeslot = new Timeslot
        {
            Id = 101,
            ShiftId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(12, 0),
            Breaks = new HashSet<Data.Entities.Break>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        shift1.Timeslots = new HashSet<Timeslot> { shift1Timeslot };
        shift1.JobRequirements = new HashSet<ShiftRequirement>
    {
        new()
        {
            ShiftId = 1,
            Shift = shift1,
            JobRoleId = nurseRole.Id,
            JobRole = nurseRole,
            RequiredStaffCount = 2 // required by your request
        },
        new()
        {
            ShiftId = 1,
            Shift = shift1,
            JobRoleId = doctorRole.Id,
            JobRole = doctorRole,
            RequiredStaffCount = 1
        }
    };

        // Shift 2 - Wednesday (different day)
        var shift2 = CreateShift(2, "Shift-Wednesday");
        var shift2Timeslot = new Timeslot
        {
            Id = 102,
            ShiftId = 2,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(13, 0),
            Breaks = new HashSet<Data.Entities.Break>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        shift2.Timeslots = new HashSet<Timeslot> { shift2Timeslot };
        shift2.JobRequirements = new HashSet<ShiftRequirement>
    {
        new()
        {
            ShiftId = 2,
            Shift = shift2,
            JobRoleId = nurseRole.Id,
            JobRole = nurseRole,
            RequiredStaffCount = 2 // again >=2 staff requirement
        },
        new()
        {
            ShiftId = 2,
            Shift = shift2,
            JobRoleId = doctorRole.Id,
            JobRole = doctorRole,
            RequiredStaffCount = 1
        }
    };

        dbContext.Shifts.AddRange(shift1, shift2);

        // Work policy
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        // 5 Users total:
        // u1, u2, u3 = nurses
        // u4, u5 = doctors
        var u1 = CreateUser(1);
        var u2 = CreateUser(2);
        var u3 = CreateUser(3);
        var u4 = CreateUser(4);
        var u5 = CreateUser(5);

        dbContext.Users.AddRange(u1, u2, u3, u4, u5);

        dbContext.UserJobRoles.AddRange(
            new UserJobRoles { UserId = u1.Id, User = u1, JobRoleId = nurseRole.Id, JobRole = nurseRole, ShiftAssignments = new HashSet<ShiftAssignment>() },
            new UserJobRoles { UserId = u2.Id, User = u2, JobRoleId = nurseRole.Id, JobRole = nurseRole, ShiftAssignments = new HashSet<ShiftAssignment>() },
            new UserJobRoles { UserId = u3.Id, User = u3, JobRoleId = nurseRole.Id, JobRole = nurseRole, ShiftAssignments = new HashSet<ShiftAssignment>() },
            new UserJobRoles { UserId = u4.Id, User = u4, JobRoleId = doctorRole.Id, JobRole = doctorRole, ShiftAssignments = new HashSet<ShiftAssignment>() },
            new UserJobRoles { UserId = u5.Id, User = u5, JobRoleId = doctorRole.Id, JobRole = doctorRole, ShiftAssignments = new HashSet<ShiftAssignment>() }
        );

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Complex Week",
            StartDate = new DateTime(2026, 1, 5), // Monday
            EndDate = new DateTime(2026, 1, 11),  // Sunday
            ShiftIds = new List<int> { shift1.Id, shift2.Id }
        };

        await service.GenerateScheduleAsync(dto);

        var schedule = await dbContext.WorkSchedules
            .Include(x => x.ShiftAssignments)
            .Include(x => x.Shifts)
            .SingleAsync(x => x.Name == "Complex Week");

        Assert.NotNull(schedule);
        Assert.Equal(2, schedule.Shifts.Count);

        // Strict assertions by Timeslot + JobRole:
        // Monday timeslot (101): Nurse=2, Doctor=1
        // Wednesday timeslot (102): Nurse=2, Doctor=1
        var grouped = schedule.ShiftAssignments
            .GroupBy(a => new { a.TimeslotId, a.JobRoleId })
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(2, grouped[new { TimeslotId = 101, JobRoleId = nurseRole.Id }]);
        Assert.Equal(1, grouped[new { TimeslotId = 101, JobRoleId = doctorRole.Id }]);
        Assert.Equal(2, grouped[new { TimeslotId = 102, JobRoleId = nurseRole.Id }]);
        Assert.Equal(1, grouped[new { TimeslotId = 102, JobRoleId = doctorRole.Id }]);

        // Total expected assignments: 6
        Assert.Equal(6, schedule.ShiftAssignments.Count);

        // Optional sanity: no duplicate same user-role in same timeslot
        var duplicates = schedule.ShiftAssignments
            .GroupBy(a => new { a.TimeslotId, a.UserId, a.JobRoleId })
            .Any(g => g.Count() > 1);

        Assert.False(duplicates);
    }

    [Fact]
    public async Task GenerateSchedule_WhenTwoUsersHavePendingAbsence_PicksOneOfThem()
    {
        await using var dbContext = CreateDbContext();

        var nurseRole = CreateJobRole(20, "Nurse");
        dbContext.JobRoles.Add(nurseRole);

        var shift = CreateShift(20, "TuesdayShift");
        shift.Timeslots = new HashSet<Timeslot>
    {
        new()
        {
            Id = 2001,
            ShiftId = shift.Id,
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(13, 0),
            Breaks = new HashSet<Data.Entities.Break>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        }
    };

        shift.JobRequirements = new HashSet<ShiftRequirement>
    {
        new()
        {
            ShiftId = shift.Id,
            Shift = shift,
            JobRoleId = nurseRole.Id,
            JobRole = nurseRole,
            RequiredStaffCount = 1
        }
    };

        dbContext.Shifts.Add(shift);

        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        var pendingUser1 = CreateUser(201);
        var pendingUser2 = CreateUser(202);

        dbContext.Users.AddRange(pendingUser1, pendingUser2);

        dbContext.UserJobRoles.AddRange(
            new UserJobRoles
            {
                UserId = pendingUser1.Id,
                User = pendingUser1,
                JobRoleId = nurseRole.Id,
                JobRole = nurseRole,
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new UserJobRoles
            {
                UserId = pendingUser2.Id,
                User = pendingUser2,
                JobRoleId = nurseRole.Id,
                JobRole = nurseRole,
                ShiftAssignments = new HashSet<ShiftAssignment>()
            });

        dbContext.Absences.AddRange(
            new Absence
            {
                UserId = pendingUser1.Id,
                User = pendingUser1,
                StartDate = new DateTime(2026, 1, 6, 0, 0, 0), // Tuesday
                EndDate = new DateTime(2026, 1, 7, 0, 0, 0),
                AbsenceType = "Vacation",
                Status = AbsenceStatusEnum.Pending.ToString(),
                Message = "",
                ManagerMessage = ""
            },
            new Absence
            {
                UserId = pendingUser2.Id,
                User = pendingUser2,
                StartDate = new DateTime(2026, 1, 6, 0, 0, 0),
                EndDate = new DateTime(2026, 1, 7, 0, 0, 0),
                AbsenceType = "Vacation",
                Status = AbsenceStatusEnum.Pending.ToString(),
                Message = "",
                ManagerMessage = ""
            });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "PendingOnlyWeek",
            StartDate = new DateTime(2026, 1, 5), // Monday
            EndDate = new DateTime(2026, 1, 11),  // Sunday
            ShiftIds = new List<int> { shift.Id }
        };

        await service.GenerateScheduleAsync(dto);

        var schedule = await dbContext.WorkSchedules
            .Include(s => s.ShiftAssignments)
            .SingleAsync(s => s.Name == "PendingOnlyWeek");

        Assert.Single(schedule.ShiftAssignments);

        var assignment = schedule.ShiftAssignments.Single();
        Assert.True(
            assignment.UserId == pendingUser1.Id || assignment.UserId == pendingUser2.Id,
            "Expected one of the pending-absence users to be assigned."
        );
    }

    [Fact]
    public async Task GenerateSchedule_PrefersUserWithoutPendingAbsence_First()
    {
        await using var dbContext = CreateDbContext();

        var nurseRole = CreateJobRole(10, "Nurse");
        dbContext.JobRoles.Add(nurseRole);

        var shift = CreateShift(10, "MondayShift");
        shift.Timeslots = new HashSet<Timeslot>
    {
        new()
        {
            Id = 1001,
            ShiftId = shift.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(12, 0),
            Breaks = new HashSet<Data.Entities.Break>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        }
    };

        shift.JobRequirements = new HashSet<ShiftRequirement>
    {
        new()
        {
            ShiftId = shift.Id,
            Shift = shift,
            JobRoleId = nurseRole.Id,
            JobRole = nurseRole,
            RequiredStaffCount = 1
        }
    };

        dbContext.Shifts.Add(shift);

        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        var userNoPending = CreateUser(100);
        var userPending = CreateUser(101);

        dbContext.Users.AddRange(userNoPending, userPending);

        dbContext.UserJobRoles.AddRange(
            new UserJobRoles
            {
                UserId = userNoPending.Id,
                User = userNoPending,
                JobRoleId = nurseRole.Id,
                JobRole = nurseRole,
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new UserJobRoles
            {
                UserId = userPending.Id,
                User = userPending,
                JobRoleId = nurseRole.Id,
                JobRole = nurseRole,
                ShiftAssignments = new HashSet<ShiftAssignment>()
            });

        // pending overlap for userPending in same schedule week/time window
        dbContext.Absences.Add(new Absence
        {
            UserId = userPending.Id,
            User = userPending,
            StartDate = new DateTime(2026, 1, 5, 0, 0, 0), // Monday
            EndDate = new DateTime(2026, 1, 6, 0, 0, 0),
            AbsenceType = "Vacation",
            Status = AbsenceStatusEnum.Pending.ToString(),
            Message = "",
            ManagerMessage = ""
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "PendingPriorityWeek1",
            StartDate = new DateTime(2026, 1, 5), // Monday
            EndDate = new DateTime(2026, 1, 11),  // Sunday
            ShiftIds = new List<int> { shift.Id }
        };

        await service.GenerateScheduleAsync(dto);

        var schedule = await dbContext.WorkSchedules
            .Include(s => s.ShiftAssignments)
            .SingleAsync(s => s.Name == "PendingPriorityWeek1");

        Assert.Single(schedule.ShiftAssignments);

        var assignment = schedule.ShiftAssignments.Single();
        Assert.Equal(userNoPending.Id, assignment.UserId); // non-pending must be picked first
    }

    [Fact]
    public async Task GenerateSchedule_OverlappingAssignment_SkipsCandidateAndAssignsNext()
    {
        await using var dbContext = CreateDbContext();

        var role1 = CreateJobRole(1, "Nurse");
        var role2 = CreateJobRole(2, "Doctor");
        dbContext.JobRoles.AddRange(role1, role2);

        var shift = CreateShift(1, "Single");
        shift.Timeslots = new HashSet<Timeslot>
        {
            new()
            {
                Id = 9001,
                ShiftId = shift.Id,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            }
        };

        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new() { ShiftId = shift.Id, Shift = shift, JobRoleId = role1.Id, JobRole = role1, RequiredStaffCount = 1 },
            new() { ShiftId = shift.Id, Shift = shift, JobRoleId = role2.Id, JobRole = role2, RequiredStaffCount = 1 }
        };

        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        var userBoth = CreateUser(1);
        var userRole1 = CreateUser(2);
        var userRole2 = CreateUser(3);

        dbContext.Users.AddRange(userBoth, userRole1, userRole2);

        dbContext.UserJobRoles.AddRange(
            new UserJobRoles { UserId = userBoth.Id, User = userBoth, JobRoleId = role1.Id, JobRole = role1, ShiftAssignments = new HashSet<ShiftAssignment>() },
            new UserJobRoles { UserId = userBoth.Id, User = userBoth, JobRoleId = role2.Id, JobRole = role2, ShiftAssignments = new HashSet<ShiftAssignment>() },
            new UserJobRoles { UserId = userRole1.Id, User = userRole1, JobRoleId = role1.Id, JobRole = role1, ShiftAssignments = new HashSet<ShiftAssignment>() },
            new UserJobRoles { UserId = userRole2.Id, User = userRole2, JobRoleId = role2.Id, JobRole = role2, ShiftAssignments = new HashSet<ShiftAssignment>() }
        );

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "OverlapUserWeek",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { shift.Id }
        };

        await service.GenerateScheduleAsync(dto);

        var schedule = await dbContext.WorkSchedules
            .Include(s => s.ShiftAssignments)
            .SingleAsync(s => s.Name == "OverlapUserWeek");

        Assert.Equal(2, schedule.ShiftAssignments.Count);
        Assert.Contains(schedule.ShiftAssignments, a => a.UserId == userBoth.Id);
        Assert.Equal(1, schedule.ShiftAssignments.Count(a => a.UserId == userBoth.Id));
    }

    [Fact]
    public async Task ViewSchedulesAsync_WithFilter_ReturnsMappedAndFilteredSchedules()
    {
        await using var dbContext = CreateDbContext();

        var schedule1 = new WorkSchedule
        {
            Id = 1,
            Name = "Alpha",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts> { new() { ShiftId = 1 } },
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };
        var schedule2 = new WorkSchedule
        {
            Id = 2,
            Name = "Beta",
            StartDate = new DateTime(2026, 1, 12),
            EndDate = new DateTime(2026, 1, 18),
            IsActive = false,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts> { new() { ShiftId = 2 } },
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };
        var schedule3 = new WorkSchedule
        {
            Id = 3,
            Name = "Gamma",
            StartDate = new DateTime(2026, 1, 19),
            EndDate = new DateTime(2026, 1, 25),
            IsActive = true,
            IsValid = false,
            Shifts = new HashSet<WorkScheduleShifts> { new() { ShiftId = 3 } },
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        dbContext.WorkSchedules.AddRange(schedule1, schedule2, schedule3);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var pagination = new PaginationDto { Page = 1, PageSize = 10 };
        var filter = new ScheduleFilterDot
        {
            Searchstring = "alp",
            Status = ScheduleStatusEnum.Active,
            ShiftIds = new List<int> { 1 }
        };

        var result = await service.ViewSchedulesAsync(pagination, filter);

        Assert.Equal(1, result.Count);
        var list = result.WorkSchedules.ToList();
        Assert.Single(list);
        Assert.Equal("Alpha", list[0].Name);
        Assert.Equal(1, list[0].ShiftCount);
        Assert.True(list[0].IsActive);
        Assert.True(list[0].IsValid);
    }

    [Fact]
    public async Task ViewSchedulesAsync_DateRangeFilters_ByStartDate()
    {
        await using var dbContext = CreateDbContext();

        dbContext.WorkSchedules.AddRange(
            new WorkSchedule
            {
                Id = 101,
                Name = "Early",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 7),
                IsActive = false,
                IsValid = true,
                Shifts = new HashSet<WorkScheduleShifts>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new WorkSchedule
            {
                Id = 102,
                Name = "Mid",
                StartDate = new DateTime(2026, 1, 10),
                EndDate = new DateTime(2026, 1, 16),
                IsActive = false,
                IsValid = true,
                Shifts = new HashSet<WorkScheduleShifts>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new WorkSchedule
            {
                Id = 103,
                Name = "Late",
                StartDate = new DateTime(2026, 1, 20),
                EndDate = new DateTime(2026, 1, 26),
                IsActive = false,
                IsValid = true,
                Shifts = new HashSet<WorkScheduleShifts>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var filter = new ScheduleFilterDot
        {
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 15),
            Status = ScheduleStatusEnum.All,
            ShiftIds = new List<int>()
        };

        var pagination = new PaginationDto { Page = 1, PageSize = 10 };
        var result = await service.ViewSchedulesAsync(pagination, filter);

        Assert.Equal(1, result.Count);
        var list = result.WorkSchedules.ToList();
        Assert.Single(list);
        Assert.Equal("Mid", list[0].Name);
    }

    [Theory]
    [InlineData(ScheduleStatusEnum.Inactive, 2)]
    [InlineData(ScheduleStatusEnum.Valid, 2)]
    [InlineData(ScheduleStatusEnum.Invalid, 2)]
    public async Task ViewSchedulesAsync_StatusFilters_ReturnExpectedCounts(ScheduleStatusEnum status, int expectedCount)
    {
        await using var dbContext = CreateDbContext();

        dbContext.WorkSchedules.AddRange(
            new WorkSchedule
            {
                Id = 201,
                Name = "ActiveValid",
                StartDate = new DateTime(2026, 1, 5),
                EndDate = new DateTime(2026, 1, 11),
                IsActive = true,
                IsValid = true,
                Shifts = new HashSet<WorkScheduleShifts>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new WorkSchedule
            {
                Id = 202,
                Name = "InactiveValid",
                StartDate = new DateTime(2026, 1, 12),
                EndDate = new DateTime(2026, 1, 18),
                IsActive = false,
                IsValid = true,
                Shifts = new HashSet<WorkScheduleShifts>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new WorkSchedule
            {
                Id = 203,
                Name = "ActiveInvalid",
                StartDate = new DateTime(2026, 1, 19),
                EndDate = new DateTime(2026, 1, 25),
                IsActive = true,
                IsValid = false,
                Shifts = new HashSet<WorkScheduleShifts>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new WorkSchedule
            {
                Id = 204,
                Name = "InactiveInvalid",
                StartDate = new DateTime(2026, 1, 26),
                EndDate = new DateTime(2026, 2, 1),
                IsActive = false,
                IsValid = false,
                Shifts = new HashSet<WorkScheduleShifts>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var filter = new ScheduleFilterDot
        {
            Status = status,
            ShiftIds = new List<int>()
        };

        var pagination = new PaginationDto { Page = 1, PageSize = 10 };
        var result = await service.ViewSchedulesAsync(pagination, filter);

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public async Task ViewSchedulesAsync_StatusAll_DoesNotFilter()
    {
        await using var dbContext = CreateDbContext();

        dbContext.WorkSchedules.AddRange(
            new WorkSchedule
            {
                Id = 301,
                Name = "ActiveValid",
                StartDate = new DateTime(2026, 1, 5),
                EndDate = new DateTime(2026, 1, 11),
                IsActive = true,
                IsValid = true,
                Shifts = new HashSet<WorkScheduleShifts>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new WorkSchedule
            {
                Id = 302,
                Name = "InactiveValid",
                StartDate = new DateTime(2026, 1, 12),
                EndDate = new DateTime(2026, 1, 18),
                IsActive = false,
                IsValid = true,
                Shifts = new HashSet<WorkScheduleShifts>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new WorkSchedule
            {
                Id = 303,
                Name = "ActiveInvalid",
                StartDate = new DateTime(2026, 1, 19),
                EndDate = new DateTime(2026, 1, 25),
                IsActive = true,
                IsValid = false,
                Shifts = new HashSet<WorkScheduleShifts>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new WorkSchedule
            {
                Id = 304,
                Name = "InactiveInvalid",
                StartDate = new DateTime(2026, 1, 26),
                EndDate = new DateTime(2026, 2, 1),
                IsActive = false,
                IsValid = false,
                Shifts = new HashSet<WorkScheduleShifts>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var filter = new ScheduleFilterDot
        {
            Status = ScheduleStatusEnum.All,
            ShiftIds = new List<int>()
        };

        var pagination = new PaginationDto { Page = 1, PageSize = 10 };
        var result = await service.ViewSchedulesAsync(pagination, filter);

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task ViewSchedulesAsync_Pagination_ReturnsSecondPage()
    {
        await using var dbContext = CreateDbContext();

        dbContext.WorkSchedules.AddRange(
                    new WorkSchedule
                    {
                        Id = 1,
                        Name = "Page One",
                        StartDate = new DateTime(2026, 1, 5),
                        EndDate = new DateTime(2026, 1, 11),
                        IsActive = false,
                        IsValid = true,
                        Shifts = new HashSet<WorkScheduleShifts>(),
                        ShiftAssignments = new HashSet<ShiftAssignment>()
                    },
                    new WorkSchedule
                    {
                        Id = 2,
                        Name = "Page Two",
                        StartDate = new DateTime(2026, 1, 12),
                        EndDate = new DateTime(2026, 1, 18),
                        IsActive = false,
                        IsValid = true,
                        Shifts = new HashSet<WorkScheduleShifts>(),
                        ShiftAssignments = new HashSet<ShiftAssignment>()
                    });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var pagination = new PaginationDto { Page = 2, PageSize = 1 };
        var result = await service.ViewSchedulesAsync(pagination, null);

        Assert.Equal(2, result.Count);
        Assert.Single(result.WorkSchedules);
        var returnedId = result.WorkSchedules.Single().Id;
        Assert.Contains(returnedId, new[] { 1, 2 });
    }

    [Fact]
    public async Task ViewScheduleAsync_NotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.ViewScheduleAsync(999));
        Assert.Equal("Schedule with id 999 not found.", ex.Message);
    }

    [Fact]
    public async Task ViewScheduleAsync_ReturnsMappedDto()
    {
        await using var dbContext = CreateDbContext();

        var schedule = new WorkSchedule
        {
            Id = 10,
            Name = "Week 10",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            IsActive = false,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };
        dbContext.WorkSchedules.Add(schedule);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = await service.ViewScheduleAsync(schedule.Id);

        Assert.Equal(schedule.Id, dto.Id);
        Assert.Equal(schedule.Name, dto.Name);
        Assert.True(dto.IsValid);
        Assert.False(dto.IsActive);
    }

    [Fact]
    public async Task DeleteScheduleAsync_ActiveSchedule_ThrowsException()
    {
        await using var dbContext = CreateDbContext();

        var schedule = new WorkSchedule
        {
            Id = 20,
            Name = "Active Week",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        dbContext.WorkSchedules.Add(schedule);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.DeleteScheduleAsync(schedule.Id));
        Assert.Equal("Cannot delete active schedule", ex.Message);
    }

    [Fact]
    public async Task DeleteScheduleAsync_NotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.DeleteScheduleAsync(999));
        Assert.Equal("Schedule with id 999 not found.", ex.Message);
    }

    [Fact]
    public async Task DeleteScheduleAsync_InactiveSchedule_RemovesSchedule()
    {
        await using var dbContext = CreateDbContext();

        var schedule = new WorkSchedule
        {
            Id = 21,
            Name = "Draft Week",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            IsActive = false,
            IsValid = false,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        dbContext.WorkSchedules.Add(schedule);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.DeleteScheduleAsync(schedule.Id);

        Assert.Empty(dbContext.WorkSchedules);
    }

    [Fact]
    public async Task SetScheduleActiveAsync_NotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.SetScheduleActiveAsync(999));
        Assert.Equal("Schedule with id 999 not found.", ex.Message);
    }

    [Fact]
    public async Task SetScheduleActiveAsync_InvalidSchedule_ThrowsException()
    {
        await using var dbContext = CreateDbContext();

        var schedule = new WorkSchedule
        {
            Id = 30,
            Name = "Invalid Week",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            IsActive = false,
            IsValid = false,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        dbContext.WorkSchedules.Add(schedule);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.SetScheduleActiveAsync(schedule.Id));
        Assert.Equal("Schedule with id 30 is invalid.", ex.Message);
    }

    [Fact]
    public async Task SetScheduleActiveAsync_OverlappingSchedule_ThrowsException()
    {
        await using var dbContext = CreateDbContext();

        var schedule = new WorkSchedule
        {
            Id = 31,
            Name = "Target Week",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        var overlapping = new WorkSchedule
        {
            Id = 32,
            Name = "Overlap Week",
            StartDate = new DateTime(2026, 1, 8),
            EndDate = new DateTime(2026, 1, 15),
            IsActive = false,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        dbContext.WorkSchedules.AddRange(schedule, overlapping);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.SetScheduleActiveAsync(schedule.Id));
        Assert.Contains("overlapping", ex.Message);
    }

    [Fact]
    public async Task SetScheduleActiveAsync_ValidSchedule_SetsActiveTrue()
    {
        await using var dbContext = CreateDbContext();

        var schedule = new WorkSchedule
        {
            Id = 33,
            Name = "Valid Week",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            IsActive = false,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        dbContext.WorkSchedules.Add(schedule);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.SetScheduleActiveAsync(schedule.Id);

        var updated = await dbContext.WorkSchedules.FirstAsync(x => x.Id == schedule.Id);
        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task SetScheduleOfflineAsync_NotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.SetScheduleOfflineAsync(999));
        Assert.Equal("Schedule with id 999 not found.", ex.Message);
    }

    [Fact]
    public async Task SetScheduleOfflineAsync_SetsActiveFalse()
    {
        await using var dbContext = CreateDbContext();

        var schedule = new WorkSchedule
        {
            Id = 40,
            Name = "Online Week",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        dbContext.WorkSchedules.Add(schedule);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.SetScheduleOfflineAsync(schedule.Id);

        var updated = await dbContext.WorkSchedules.FirstAsync(x => x.Id == schedule.Id);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task ChangeScheduleDateAsync_NotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            service.ChangeScheduleDateAsync(999, new DateTime(2026, 1, 5), new DateTime(2026, 1, 11)));
        Assert.Equal("Schedule with id 999 not found.", ex.Message);
    }

    [Fact]
    public async Task ChangeScheduleDateAsync_UpdatesDatesAndRegeneratesAssignments()
    {
        await using var dbContext = CreateDbContext();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "Morning");
        shift.Timeslots = new HashSet<Timeslot>
        {
            new()
            {
                Id = 5001,
                ShiftId = shift.Id,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            }
        };
        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new() { ShiftId = shift.Id, JobRoleId = role.Id, RequiredStaffCount = 1, JobRole = role, Shift = shift }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        var user = CreateUser(1);
        dbContext.Users.Add(user);
        dbContext.UserJobRoles.Add(new UserJobRoles
        {
            UserId = user.Id,
            JobRoleId = role.Id,
            User = user,
            JobRole = role,
            ShiftAssignments = new HashSet<ShiftAssignment>()
        });

        var schedule = new WorkSchedule
        {
            Id = 50,
            Name = "Change Dates",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            IsActive = false,
            IsValid = false,
            Shifts = new HashSet<WorkScheduleShifts> { new() { ShiftId = shift.Id } },
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        dbContext.WorkSchedules.Add(schedule);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var newStart = new DateTime(2026, 1, 12);
        var newEnd = new DateTime(2026, 1, 18);

        await service.ChangeScheduleDateAsync(schedule.Id, newStart, newEnd);

        var updated = await dbContext.WorkSchedules
            .Include(x => x.ShiftAssignments)
            .FirstAsync(x => x.Id == schedule.Id);

        Assert.Equal(newStart, updated.StartDate);
        Assert.Equal(newEnd, updated.EndDate);
        Assert.True(updated.IsValid);
        Assert.NotEmpty(updated.ShiftAssignments);
    }

    [Fact]
    public async Task ReGenerateScheduleAsync_NotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.ReGenerateScheduleAsync(999));
        Assert.Equal("Schedule with id 999 not found.", ex.Message);
    }

    [Fact]
    public async Task ReGenerateScheduleAsync_ValidSchedule_RecreatesAssignments()
    {
        await using var dbContext = CreateDbContext();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "Morning");
        shift.Timeslots = new HashSet<Timeslot>
        {
            new()
            {
                Id = 7001,
                ShiftId = shift.Id,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new HashSet<Data.Entities.Break>(),
                ShiftAssignments = new HashSet<ShiftAssignment>()
            }
        };
        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new() { ShiftId = shift.Id, JobRoleId = role.Id, RequiredStaffCount = 1, JobRole = role, Shift = shift }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        var user = CreateUser(1);
        dbContext.Users.Add(user);
        dbContext.UserJobRoles.Add(new UserJobRoles
        {
            UserId = user.Id,
            JobRoleId = role.Id,
            User = user,
            JobRole = role,
            ShiftAssignments = new HashSet<ShiftAssignment>()
        });

        var schedule = new WorkSchedule
        {
            Id = 70,
            Name = "Regen Week",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts> { new() { ShiftId = shift.Id, Shift = shift } },
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        dbContext.WorkSchedules.Add(schedule);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.ReGenerateScheduleAsync(schedule.Id);

        var updated = await dbContext.WorkSchedules
            .Include(x => x.ShiftAssignments)
            .FirstAsync(x => x.Id == schedule.Id);

        Assert.True(updated.IsValid);
        Assert.False(updated.IsActive);
        Assert.NotEmpty(updated.ShiftAssignments);
    }

    [Fact]
    public async Task ReGenerateScheduleAsync_RemovesPreviousAssignmentsBeforeRecreating()
    {
        await using var dbContext = CreateDbContext();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "Morning");
        shift.Timeslots = new HashSet<Timeslot>
    {
        new()
        {
            Id = 7101,
            ShiftId = shift.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(12, 0),
            Breaks = new HashSet<Data.Entities.Break>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        }
    };
        shift.JobRequirements = new HashSet<ShiftRequirement>
    {
        new() { ShiftId = shift.Id, JobRoleId = role.Id, RequiredStaffCount = 1, JobRole = role, Shift = shift }
    };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(new WorkPolicy
        {
            MaximumConsecutiveWorkHours = 8,
            RestPeriodInMinutes = 30,
            RestPeriodThresholdInMinutes = 360
        });

        var user1 = CreateUser(1);
        var user2 = CreateUser(2);

        dbContext.Users.AddRange(user1, user2);
        dbContext.UserJobRoles.AddRange(
            new UserJobRoles
            {
                UserId = user1.Id,
                JobRoleId = role.Id,
                User = user1,
                JobRole = role,
                ShiftAssignments = new HashSet<ShiftAssignment>()
            },
            new UserJobRoles
            {
                UserId = user2.Id,
                JobRoleId = role.Id,
                User = user2,
                JobRole = role,
                ShiftAssignments = new HashSet<ShiftAssignment>()
            });

        var schedule = new WorkSchedule
        {
            Id = 71,
            Name = "Regen Replace",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts> { new() { ShiftId = shift.Id, Shift = shift } },
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };

        var oldAssignment = new ShiftAssignment
        {
            WorkSchedule = schedule,
            TimeslotId = 7101,
            UserId = user2.Id,
            JobRoleId = role.Id,
            StartTime = new DateTime(2026, 1, 5, 8, 0, 0),
            EndTime = new DateTime(2026, 1, 5, 12, 0, 0)
        };

        schedule.ShiftAssignments.Add(oldAssignment);

        dbContext.WorkSchedules.Add(schedule);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.ReGenerateScheduleAsync(schedule.Id);

        var updated = await dbContext.WorkSchedules
            .Include(x => x.ShiftAssignments)
            .FirstAsync(x => x.Id == schedule.Id);

        Assert.Single(updated.ShiftAssignments);
        Assert.Equal(user1.Id, updated.ShiftAssignments.Single().UserId);
        Assert.False(updated.IsActive);
        Assert.True(updated.IsValid);
    }

    [Fact]
    public async Task PublishScheduleAsync_ThrowsNotImplementedException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<NotImplementedException>(() => service.PublishScheduleAsync(1));
    }

    private static WorkScheduleService CreateService(SchichtpilotDbContext dbContext, Mock<IMapper>? mapperMock = null)
    {
        var mock = mapperMock ?? CreateMapperMock();
        return new WorkScheduleService(dbContext, mock.Object);
    }

    private static Mock<IMapper> CreateMapperMock()
    {
        var mock = new Mock<IMapper>();

        mock.Setup(m => m.Map<WorkSchedule, WorkScheduleShortDto>(It.IsAny<WorkSchedule>()))
            .Returns((WorkSchedule ws) => new WorkScheduleShortDto
            {
                Id = ws.Id,
                Name = ws.Name,
                StartDate = ws.StartDate,
                EndDate = ws.EndDate,
                IsActive = ws.IsActive,
                IsValid = ws.IsValid,
                ShiftCount = ws.Shifts?.Count ?? 0
            });

        mock.Setup(m => m.Map<WorkSchedule, WorkScheduleDto>(It.IsAny<WorkSchedule>()))
            .Returns((WorkSchedule ws) => new WorkScheduleDto
            {
                Id = ws.Id,
                Name = ws.Name,
                StartDate = ws.StartDate,
                EndDate = ws.EndDate,
                IsActive = ws.IsActive,
                IsValid = ws.IsValid,
                AssignedUsers = new List<AssignedUserDto>(),
                Shifts = new List<ShiftDto>()
            });

        return mock;
    }

    private static SchichtpilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchichtpilotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SchichtpilotDbContext(options);
    }

    private static User CreateUser(long id)
    {
        return new User
        {
            Id = id,
            Email = $"user{id}@test.com",
            UserName = $"user{id}@test.com",
            FirstName = "Test",
            LastName = "User",
            StreetAddress = "Main Street 1",
            City = "Testville",
            PostalCode = 12345,
            BirthDate = new DateTime(1990, 1, 1),
            JobRoles = new HashSet<UserJobRoles>()
        };
    }

    private static JobRole CreateJobRole(int id, string name)
    {
        return new JobRole
        {
            Id = id,
            Name = name,
            Description = $"{name} role",
            CreatedOn = DateTime.UtcNow,
            UsersWithRole = new HashSet<UserJobRoles>(),
            Dependencies = new HashSet<JobRoleDependency>(),
            Prerequisites = new HashSet<JobRoleDependency>()
        };
    }

    private static Shift CreateShift(int id, string name)
    {
        return new Shift
        {
            Id = id,
            Name = name,
            ColorAsHex = "FFAA00",
            Timeslots = new HashSet<Timeslot>(),
            JobRequirements = new HashSet<ShiftRequirement>(),
            ShiftAssignments = new HashSet<WorkScheduleShifts>()
        };
    }
}
