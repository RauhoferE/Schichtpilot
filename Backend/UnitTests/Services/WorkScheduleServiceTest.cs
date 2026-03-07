using Data;
using Data.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.Enums;

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

        var service = new WorkScheduleService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5), // Monday
            EndDate = new DateTime(2026, 1, 11),  // Sunday
            ShiftIds = new List<int> { 999 }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GenerateSchedule(dto));
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

        var service = new WorkScheduleService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1, 2 }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GenerateSchedule(dto));
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

        var service = new WorkScheduleService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1 }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GenerateSchedule(dto));
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

        var service = new WorkScheduleService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1 }
        };

        await Assert.ThrowsAsync<Exception>(() => service.GenerateSchedule(dto));
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

        var service = new WorkScheduleService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1 }
        };

        await Assert.ThrowsAsync<Exception>(() => service.GenerateSchedule(dto));
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

        var service = new WorkScheduleService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1 }
        };

        await Assert.ThrowsAsync<Exception>(() => service.GenerateSchedule(dto));
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

        var service = new WorkScheduleService(dbContext);

        var dto = new GenerateScheduleDto
        {
            Name = "Week 1",
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 11),
            ShiftIds = new List<int> { 1 }
        };

        await service.GenerateSchedule(dto);

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

    var service = new WorkScheduleService(dbContext);

    var dto = new GenerateScheduleDto
    {
        Name = "Complex Week",
        StartDate = new DateTime(2026, 1, 5), // Monday
        EndDate = new DateTime(2026, 1, 11),  // Sunday
        ShiftIds = new List<int> { shift1.Id, shift2.Id }
    };

    await service.GenerateSchedule(dto);

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

    var service = new WorkScheduleService(dbContext);

    var dto = new GenerateScheduleDto
    {
        Name = "PendingOnlyWeek",
        StartDate = new DateTime(2026, 1, 5), // Monday
        EndDate = new DateTime(2026, 1, 11),  // Sunday
        ShiftIds = new List<int> { shift.Id }
    };

    await service.GenerateSchedule(dto);

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

    var service = new WorkScheduleService(dbContext);

    var dto = new GenerateScheduleDto
    {
        Name = "PendingPriorityWeek1",
        StartDate = new DateTime(2026, 1, 5), // Monday
        EndDate = new DateTime(2026, 1, 11),  // Sunday
        ShiftIds = new List<int> { shift.Id }
    };

    await service.GenerateSchedule(dto);

    var schedule = await dbContext.WorkSchedules
        .Include(s => s.ShiftAssignments)
        .SingleAsync(s => s.Name == "PendingPriorityWeek1");

    Assert.Single(schedule.ShiftAssignments);

    var assignment = schedule.ShiftAssignments.Single();
    Assert.Equal(userNoPending.Id, assignment.UserId); // non-pending must be picked first
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
