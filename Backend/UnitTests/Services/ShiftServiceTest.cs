using AutoMapper;
using Data;
using Data.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Moq;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;
using Schichtpilot.Services;

namespace UnitTests.Services;

[TestSubject(typeof(ShiftService))]
public class ShiftServiceTest
{
    [Fact]
    public async Task CreateShiftAsync_DuplicateName_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        dbContext.Shifts.Add(CreateShift(1, "Morning"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var dto = new CreateShiftDto
        {
            Name = "Morning",
            ColorAsHex = "FF0000",
            TimeSlots = new List<TimeSlotDto>
            {
                CreateTimeSlotDto(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0))
            },
            JobRequirements = new List<ShiftRequirementDto>()
        };

        await Assert.ThrowsAsync<Exception>(() => service.CreateShiftAsync(dto));
    }

    [Fact]
    public async Task CreateShiftAsync_MissingJobRole_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var service = CreateService(dbContext, mapperMock);

        var dto = new CreateShiftDto
        {
            Name = "Day",
            ColorAsHex = "00FF00",
            TimeSlots = new List<TimeSlotDto>
            {
                CreateTimeSlotDto(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(16, 0))
            },
            JobRequirements = new List<ShiftRequirementDto>
            {
                new ShiftRequirementDto { JobId = 99, Name = "Nurse", RequiredStaffCount = 2 }
            }
        };

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateShiftAsync(dto));
    }

    [Fact]
    public async Task CreateShiftAsync_MissingPrerequisites_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var roleA = CreateJobRole(1, "RoleA");
        var roleB = CreateJobRole(2, "RoleB");
        dbContext.JobRoles.AddRange(roleA, roleB);

        dbContext.JobRoleDependencies.Add(new JobRoleDependency
        {
            DependencyJobRoleId = roleB.Id,
            JobRoleId = roleA.Id,
            Dependency = roleB,
            JobRole = roleA
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var dto = new CreateShiftDto
        {
            Name = "Evening",
            ColorAsHex = "0000FF",
            TimeSlots = new List<TimeSlotDto>
            {
                CreateTimeSlotDto(DayOfWeek.Tuesday, new TimeOnly(16, 0), new TimeOnly(20, 0))
            },
            JobRequirements = new List<ShiftRequirementDto>
            {
                new ShiftRequirementDto { JobId = roleA.Id, Name = roleA.Name, RequiredStaffCount = 1 }
            }
        };

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateShiftAsync(dto));
    }

    [Fact]
    public async Task CreateShiftAsync_Success_CreatesShiftWithTimeslotsAndRequirements()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var role = CreateJobRole(1, "Nurse");
        dbContext.JobRoles.Add(role);
        dbContext.WorkPolicies.Add(CreateWorkPolicy());
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var dto = new CreateShiftDto
        {
            Name = "Night",
            ColorAsHex = "123456",
            TimeSlots = new List<TimeSlotDto>
            {
                CreateTimeSlotDto(DayOfWeek.Wednesday, new TimeOnly(20, 0), new TimeOnly(23, 0))
            },
            JobRequirements = new List<ShiftRequirementDto>
            {
                new ShiftRequirementDto { JobId = role.Id, Name = role.Name, RequiredStaffCount = 3 }
            }
        };

        await service.CreateShiftAsync(dto);

        var created = await dbContext.Shifts
            .Include(x => x.Timeslots)
            .Include(x => x.JobRequirements)
            .FirstOrDefaultAsync(x => x.Name == "Night");

        Assert.NotNull(created);
        Assert.Single(created.Timeslots);
        Assert.Single(created.JobRequirements);
        Assert.Equal("123456", created.ColorAsHex);
    }

    [Fact]
    public async Task CreateShiftAsync_NotEnoughBreaks_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var role = CreateJobRole(1, "Nurse");
        dbContext.JobRoles.Add(role);
        dbContext.WorkPolicies.Add(CreateWorkPolicy(restPeriodMinutes: 30, restPeriodThresholdMinutes: 240));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var dto = new CreateShiftDto
        {
            Name = "LongShift",
            ColorAsHex = "999999",
            TimeSlots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(14, 0),
                    Breaks = new List<BreakDto>
                    {
                        new BreakDto
                        {
                            StartTime = new TimeOnly(10, 0),
                            EndTime = new TimeOnly(10, 15)
                        }
                    }
                }
            },
            JobRequirements = new List<ShiftRequirementDto>
            {
                new ShiftRequirementDto { JobId = role.Id, Name = role.Name, RequiredStaffCount = 1 }
            }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateShiftAsync(dto));
        Assert.Equal("Not enough breaks added in the shifts", ex.Message);
    }

    [Fact]
    public async Task ManageShiftAsync_NameConflict_ThrowsAlreadyExistsException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        dbContext.Shifts.Add(CreateShift(1, "ShiftA"));
        dbContext.Shifts.Add(CreateShift(2, "ShiftB"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var dto = new EditShiftDto { Name = "ShiftB", ColorAsHex = "FFFFFF" };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => service.ManageShiftAsync(1, dto));
    }

    [Fact]
    public async Task ManageShiftAsync_ShiftNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var service = CreateService(dbContext, mapperMock);

        var dto = new EditShiftDto { Name = "Missing", ColorAsHex = "FFFFFF" };

        await Assert.ThrowsAsync<NotFoundException>(() => service.ManageShiftAsync(123, dto));
    }

    [Fact]
    public async Task ManageShiftAsync_Success_UpdatesFields()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        dbContext.Shifts.Add(CreateShift(1, "Old"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);
        var dto = new EditShiftDto { Name = "New", ColorAsHex = "ABCDEF" };

        await service.ManageShiftAsync(1, dto);

        var updated = await dbContext.Shifts.FirstAsync(x => x.Id == 1);
        Assert.Equal("New", updated.Name);
        Assert.Equal("ABCDEF", updated.ColorAsHex);
    }

    [Fact]
    public async Task AddTimeSlotAsync_ShiftNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var service = CreateService(dbContext, mapperMock);

        var timeSlot = CreateTimeSlotDto(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(10, 0));

        await Assert.ThrowsAsync<NotFoundException>(() => service.AddTimeSlotAsync(1, timeSlot));
    }

    [Fact]
    public async Task AddTimeSlotAsync_OverlappingTimeSlot_ThrowsAlreadyExistsException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var shift = CreateShift(1, "Overlap");
        shift.Timeslots = new HashSet<Timeslot>
        {
            CreateTimeSlotEntity(1, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0))
        };
        dbContext.Shifts.Add(shift);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var timeSlot = CreateTimeSlotDto(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(14, 0));

        await Assert.ThrowsAsync<AlreadyExistsException>(() => service.AddTimeSlotAsync(1, timeSlot));
    }

    [Fact]
    public async Task AddTimeSlotAsync_Success_AddsTimeSlot()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var shift = CreateShift(1, "AddTime");
        shift.Timeslots = new HashSet<Timeslot>();
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(CreateWorkPolicy());
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var timeSlot = CreateTimeSlotDto(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(11, 0));

        await service.AddTimeSlotAsync(1, timeSlot);

        var updated = await dbContext.Shifts.Include(x => x.Timeslots).FirstAsync(x => x.Id == 1);
        Assert.Single(updated.Timeslots);
    }

    [Fact]
    public async Task AddTimeSlotAsync_WhenShiftUsedInSchedule_UpdatesSchedule()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var scheduleServiceMock = new Mock<IWorkScheduleService>();

        var shift = CreateShift(1, "AddTimeSchedule");
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(CreateWorkPolicy());

        var schedule = CreateWorkSchedule(10);
        var scheduleShift = CreateWorkScheduleShift(schedule, shift);
        schedule.Shifts.Add(scheduleShift);

        dbContext.WorkSchedules.Add(schedule);
        dbContext.WorkScheduleShifts.Add(scheduleShift);
        await dbContext.SaveChangesAsync();

        scheduleServiceMock
            .Setup(x => x.SetScheduleOfflineAsync(schedule.Id))
            .Returns(Task.CompletedTask);
        scheduleServiceMock
            .Setup(x => x.SetScheduleAsInvalidAsync(schedule.Id))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, mapperMock, scheduleServiceMock);

        var timeSlot = CreateTimeSlotDto(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(11, 0));

        await service.AddTimeSlotAsync(shift.Id, timeSlot);

        scheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(schedule.Id), Times.Once);
        scheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(schedule.Id), Times.Once);
    }

    [Fact]
    public async Task AddTimeSlotAsync_NotEnoughBreaks_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var shift = CreateShift(1, "AddTimeBreakFail");
        shift.Timeslots = new HashSet<Timeslot>();
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(CreateWorkPolicy(restPeriodMinutes: 30, restPeriodThresholdMinutes: 240));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var timeSlot = new TimeSlotDto
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(14, 0),
            Breaks = new List<BreakDto>
            {
                new BreakDto
                {
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(10, 15)
                }
            }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.AddTimeSlotAsync(1, timeSlot));
        Assert.Equal("Not enough breaks added in the shifts", ex.Message);
    }

    [Fact]
    public async Task EditTimeSlotAsync_ShiftNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var service = CreateService(dbContext, mapperMock);

        var timeSlot = CreateTimeSlotDto(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(10, 0));
        timeSlot.Id = 1;

        await Assert.ThrowsAsync<NotFoundException>(() => service.EditTimeSlotAsync(1, timeSlot));
    }

    [Fact]
    public async Task EditTimeSlotAsync_TimeSlotNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var shift = CreateShift(1, "EditTime");
        shift.Timeslots = new HashSet<Timeslot>();
        dbContext.Shifts.Add(shift);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var timeSlot = CreateTimeSlotDto(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(10, 0));
        timeSlot.Id = 999;

        await Assert.ThrowsAsync<NotFoundException>(() => service.EditTimeSlotAsync(1, timeSlot));
    }

    [Fact]
    public async Task EditTimeSlotAsync_OverlappingTimeSlot_ThrowsAlreadyExistsException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var shift = CreateShift(1, "EditOverlap");
        shift.Timeslots = new HashSet<Timeslot>
        {
            CreateTimeSlotEntity(1, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(10, 0)),
            CreateTimeSlotEntity(2, DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(14, 0))
        };
        dbContext.Shifts.Add(shift);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var timeSlot = CreateTimeSlotDto(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0));
        timeSlot.Id = 2;

        await Assert.ThrowsAsync<AlreadyExistsException>(() => service.EditTimeSlotAsync(1, timeSlot));
    }

    [Fact]
    public async Task EditTimeSlotAsync_Success_UpdatesTimeSlot()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var shift = CreateShift(1, "EditSuccess");
        shift.Timeslots = new HashSet<Timeslot>
        {
            CreateTimeSlotEntity(1, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(10, 0))
        };
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(CreateWorkPolicy());
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var timeSlot = CreateTimeSlotDto(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(11, 0));
        timeSlot.Id = 1;

        await service.EditTimeSlotAsync(1, timeSlot);

        var updated = await dbContext.Shifts.Include(x => x.Timeslots).FirstAsync(x => x.Id == 1);
        var updatedSlot = updated.Timeslots.First();

        Assert.Equal(DayOfWeek.Tuesday, updatedSlot.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), updatedSlot.StartTime);
        Assert.Equal(new TimeOnly(11, 0), updatedSlot.EndTime);
    }

    [Fact]
    public async Task EditTimeSlotAsync_WhenShiftUsedInSchedule_UpdatesSchedule()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var scheduleServiceMock = new Mock<IWorkScheduleService>();

        var shift = CreateShift(1, "EditSchedule");
        shift.Timeslots = new HashSet<Timeslot>
        {
            CreateTimeSlotEntity(1, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(10, 0))
        };
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(CreateWorkPolicy());

        var schedule = CreateWorkSchedule(10);
        var scheduleShift = CreateWorkScheduleShift(schedule, shift);
        schedule.Shifts.Add(scheduleShift);

        dbContext.WorkSchedules.Add(schedule);
        dbContext.WorkScheduleShifts.Add(scheduleShift);
        await dbContext.SaveChangesAsync();

        scheduleServiceMock
            .Setup(x => x.SetScheduleOfflineAsync(schedule.Id))
            .Returns(Task.CompletedTask);
        scheduleServiceMock
            .Setup(x => x.SetScheduleAsInvalidAsync(schedule.Id))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, mapperMock, scheduleServiceMock);

        var timeSlot = CreateTimeSlotDto(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(11, 0));
        timeSlot.Id = 1;

        await service.EditTimeSlotAsync(shift.Id, timeSlot);

        scheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(schedule.Id), Times.Once);
        scheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(schedule.Id), Times.Once);
    }

    [Fact]
    public async Task EditTimeSlotAsync_NotEnoughBreaks_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var shift = CreateShift(1, "EditBreakFail");
        shift.Timeslots = new HashSet<Timeslot>
        {
            new Timeslot
            {
                Id = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                Breaks = new HashSet<Break>
                {
                    new Break
                    {
                        StartTime = new TimeOnly(10, 0),
                        EndTime = new TimeOnly(10, 30)
                    }
                }
            }
        };
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(CreateWorkPolicy(restPeriodMinutes: 30, restPeriodThresholdMinutes: 240));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var timeSlot = new TimeSlotDto
        {
            Id = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(14, 0),
            Breaks = new List<BreakDto>
            {
                new BreakDto
                {
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(10, 15)
                }
            }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.EditTimeSlotAsync(1, timeSlot));
        Assert.Equal("Not enough breaks added in the shifts", ex.Message);
    }

    [Fact]
    public async Task DeleteTimeSlotAsync_ShiftNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var service = CreateService(dbContext, mapperMock);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteTimeSlotAsync(1, 1));
    }

    [Fact]
    public async Task DeleteTimeSlotAsync_TimeSlotNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        dbContext.Shifts.Add(CreateShift(1, "DeleteTime"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteTimeSlotAsync(1, 999));
    }

    [Fact]
    public async Task DeleteTimeSlotAsync_Success_RemovesTimeSlot()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var shift = CreateShift(1, "DeleteTimeSuccess");
        shift.Timeslots = new HashSet<Timeslot>
        {
            CreateTimeSlotEntity(1, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(10, 0))
        };
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(CreateWorkPolicy());
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        await service.DeleteTimeSlotAsync(1, 1);

        var updated = await dbContext.Shifts.Include(x => x.Timeslots).FirstAsync(x => x.Id == 1);
        Assert.Empty(updated.Timeslots);
    }

    [Fact]
    public async Task DeleteTimeSlotAsync_WhenShiftUsedInSchedule_UpdatesSchedule()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var scheduleServiceMock = new Mock<IWorkScheduleService>();

        var shift = CreateShift(1, "DeleteTimeSchedule");
        shift.Timeslots = new HashSet<Timeslot>
        {
            CreateTimeSlotEntity(1, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(10, 0)),
            CreateTimeSlotEntity(2, DayOfWeek.Tuesday, new TimeOnly(8, 0), new TimeOnly(10, 0))
        };
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(CreateWorkPolicy());

        var schedule = CreateWorkSchedule(10);
        var scheduleShift = CreateWorkScheduleShift(schedule, shift);
        schedule.Shifts.Add(scheduleShift);

        dbContext.WorkSchedules.Add(schedule);
        dbContext.WorkScheduleShifts.Add(scheduleShift);
        await dbContext.SaveChangesAsync();

        scheduleServiceMock
            .Setup(x => x.SetScheduleOfflineAsync(schedule.Id))
            .Returns(Task.CompletedTask);
        scheduleServiceMock
            .Setup(x => x.SetScheduleAsInvalidAsync(schedule.Id))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, mapperMock, scheduleServiceMock);

        await service.DeleteTimeSlotAsync(shift.Id, 1);

        scheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(schedule.Id), Times.Once);
        scheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(schedule.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteTimeSlotAsync_NotEnoughBreaks_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var shift = CreateShift(1, "DeleteBreakFail");
        shift.Timeslots = new HashSet<Timeslot>
        {
            new Timeslot
            {
                Id = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0),
                Breaks = new HashSet<Break>()
            },
            new Timeslot
            {
                Id = 2,
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(14, 0),
                Breaks = new HashSet<Break>
                {
                    new Break
                    {
                        StartTime = new TimeOnly(10, 0),
                        EndTime = new TimeOnly(10, 15)
                    }
                }
            }
        };
        dbContext.Shifts.Add(shift);
        dbContext.WorkPolicies.Add(CreateWorkPolicy(restPeriodMinutes: 30, restPeriodThresholdMinutes: 240));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.DeleteTimeSlotAsync(1, 1));
        Assert.Equal("Not enough breaks added in the shifts", ex.Message);
    }

    [Fact]
    public async Task AddJobRequirementAsync_ShiftNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var service = CreateService(dbContext, mapperMock);

        var requirement = new ShiftRequirementDto { JobId = 1, Name = "Nurse", RequiredStaffCount = 2 };

        await Assert.ThrowsAsync<NotFoundException>(() => service.AddJobRequirementAsync(1, requirement));
    }

    [Fact]
    public async Task AddJobRequirementAsync_AlreadyExists_ThrowsAlreadyExistsException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "Req");
        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new ShiftRequirement
            {
                JobRoleId = role.Id,
                JobRole = role,
                RequiredStaffCount = 1
            }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var requirement = new ShiftRequirementDto { JobId = role.Id, Name = role.Name, RequiredStaffCount = 2 };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => service.AddJobRequirementAsync(1, requirement));
    }

    [Fact]
    public async Task AddJobRequirementAsync_JobRoleNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        dbContext.Shifts.Add(CreateShift(1, "ReqMissingRole"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var requirement = new ShiftRequirementDto { JobId = 1, Name = "Missing", RequiredStaffCount = 2 };

        await Assert.ThrowsAsync<NotFoundException>(() => service.AddJobRequirementAsync(1, requirement));
    }

    [Fact]
    public async Task AddJobRequirementAsync_Success_AddsRequirement()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var role = CreateJobRole(1, "Nurse");
        dbContext.JobRoles.Add(role);

        var shift = CreateShift(1, "ReqAdd");
        shift.JobRequirements = new HashSet<ShiftRequirement>();
        dbContext.Shifts.Add(shift);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var requirement = new ShiftRequirementDto { JobId = role.Id, Name = role.Name, RequiredStaffCount = 2 };

        await service.AddJobRequirementAsync(1, requirement);

        var updated = await dbContext.Shifts.Include(x => x.JobRequirements).FirstAsync(x => x.Id == 1);
        Assert.Single(updated.JobRequirements);
        Assert.Equal(2, updated.JobRequirements.First().RequiredStaffCount);
    }

    [Fact]
    public async Task AddJobRequirementAsync_WhenShiftUsedInSchedule_UpdatesSchedule()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var scheduleServiceMock = new Mock<IWorkScheduleService>();

        var role = CreateJobRole(1, "Nurse");
        dbContext.JobRoles.Add(role);

        var shift = CreateShift(1, "ReqAddSchedule");
        shift.JobRequirements = new HashSet<ShiftRequirement>();
        dbContext.Shifts.Add(shift);

        var schedule = CreateWorkSchedule(10);
        var scheduleShift = CreateWorkScheduleShift(schedule, shift);
        schedule.Shifts.Add(scheduleShift);

        dbContext.WorkSchedules.Add(schedule);
        dbContext.WorkScheduleShifts.Add(scheduleShift);
        await dbContext.SaveChangesAsync();

        scheduleServiceMock
            .Setup(x => x.SetScheduleOfflineAsync(schedule.Id))
            .Returns(Task.CompletedTask);
        scheduleServiceMock
            .Setup(x => x.SetScheduleAsInvalidAsync(schedule.Id))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, mapperMock, scheduleServiceMock);

        var requirement = new ShiftRequirementDto { JobId = role.Id, Name = role.Name, RequiredStaffCount = 2 };

        await service.AddJobRequirementAsync(shift.Id, requirement);

        scheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(schedule.Id), Times.Once);
        scheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(schedule.Id), Times.Once);
    }

    [Fact]
    public async Task ChangeRequiredStaffAsync_ShiftNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var service = CreateService(dbContext, mapperMock);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ChangeRequiredStaffAsync(1, 1, 5));
    }

    [Fact]
    public async Task ChangeRequiredStaffAsync_RequirementNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        dbContext.Shifts.Add(CreateShift(1, "ReqChange"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ChangeRequiredStaffAsync(1, 99, 5));
    }

    [Fact]
    public async Task ChangeRequiredStaffAsync_Success_UpdatesRequirement()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "ReqChangeSuccess");
        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new ShiftRequirement
            {
                Id = 1,
                JobRoleId = role.Id,
                JobRole = role,
                RequiredStaffCount = 2
            }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        await service.ChangeRequiredStaffAsync(1, 1, 5);

        var updated = await dbContext.Shifts.Include(x => x.JobRequirements).FirstAsync(x => x.Id == 1);
        Assert.Equal(5, updated.JobRequirements.First().RequiredStaffCount);
    }

    [Fact]
    public async Task ChangeRequiredStaffAsync_WhenShiftUsedInSchedule_UpdatesSchedule()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var scheduleServiceMock = new Mock<IWorkScheduleService>();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "ReqChangeSchedule");
        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new ShiftRequirement
            {
                Id = 1,
                JobRoleId = role.Id,
                JobRole = role,
                RequiredStaffCount = 2
            }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);

        var schedule = CreateWorkSchedule(10);
        var scheduleShift = CreateWorkScheduleShift(schedule, shift);
        schedule.Shifts.Add(scheduleShift);

        dbContext.WorkSchedules.Add(schedule);
        dbContext.WorkScheduleShifts.Add(scheduleShift);
        await dbContext.SaveChangesAsync();

        scheduleServiceMock
            .Setup(x => x.SetScheduleOfflineAsync(schedule.Id))
            .Returns(Task.CompletedTask);
        scheduleServiceMock
            .Setup(x => x.SetScheduleAsInvalidAsync(schedule.Id))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, mapperMock, scheduleServiceMock);

        await service.ChangeRequiredStaffAsync(shift.Id, 1, 5);

        scheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(schedule.Id), Times.Once);
        scheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(schedule.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteJobRequirementAsync_ShiftNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var service = CreateService(dbContext, mapperMock);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteJobRequirementAsync(1, 1));
    }

    [Fact]
    public async Task DeleteJobRequirementAsync_RequirementNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        dbContext.Shifts.Add(CreateShift(1, "ReqDelete"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteJobRequirementAsync(1, 999));
    }

    [Fact]
    public async Task DeleteJobRequirementAsync_Success_RemovesRequirement()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "ReqDeleteSuccess");
        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new ShiftRequirement
            {
                Id = 1,
                JobRoleId = role.Id,
                JobRole = role,
                RequiredStaffCount = 1
            }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        await service.DeleteJobRequirementAsync(1, 1);

        var updated = await dbContext.Shifts.Include(x => x.JobRequirements).FirstAsync(x => x.Id == 1);
        Assert.Empty(updated.JobRequirements);
    }

    [Fact]
    public async Task DeleteJobRequirementAsync_WhenShiftUsedInSchedule_UpdatesSchedule()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var scheduleServiceMock = new Mock<IWorkScheduleService>();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "ReqDeleteSchedule");
        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new ShiftRequirement
            {
                Id = 1,
                JobRoleId = role.Id,
                JobRole = role,
                RequiredStaffCount = 1
            }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);

        var schedule = CreateWorkSchedule(10);
        var scheduleShift = CreateWorkScheduleShift(schedule, shift);
        schedule.Shifts.Add(scheduleShift);

        dbContext.WorkSchedules.Add(schedule);
        dbContext.WorkScheduleShifts.Add(scheduleShift);
        await dbContext.SaveChangesAsync();

        scheduleServiceMock
            .Setup(x => x.SetScheduleOfflineAsync(schedule.Id))
            .Returns(Task.CompletedTask);
        scheduleServiceMock
            .Setup(x => x.SetScheduleAsInvalidAsync(schedule.Id))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, mapperMock, scheduleServiceMock);

        await service.DeleteJobRequirementAsync(shift.Id, 1);

        scheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(schedule.Id), Times.Once);
        scheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(schedule.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteShiftAsync_ShiftNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var service = CreateService(dbContext, mapperMock);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteShiftAsync(1));
    }

    [Fact]
    public async Task DeleteShiftAsync_Success_RemovesShift()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        dbContext.Shifts.Add(CreateShift(1, "DeleteShift"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        await service.DeleteShiftAsync(1);

        Assert.False(dbContext.Shifts.Any(x => x.Id == 1));
    }

    [Fact]
    public async Task DeleteShiftAsync_WhenShiftUsedInSchedule_UpdatesSchedule()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();
        var scheduleServiceMock = new Mock<IWorkScheduleService>();

        var shift = CreateShift(1, "DeleteShiftSchedule");
        dbContext.Shifts.Add(shift);

        var schedule = CreateWorkSchedule(10);
        var scheduleShift = CreateWorkScheduleShift(schedule, shift);
        schedule.Shifts.Add(scheduleShift);

        dbContext.WorkSchedules.Add(schedule);
        dbContext.WorkScheduleShifts.Add(scheduleShift);
        await dbContext.SaveChangesAsync();

        scheduleServiceMock
            .Setup(x => x.SetScheduleOfflineAsync(schedule.Id))
            .Returns(Task.CompletedTask);
        scheduleServiceMock
            .Setup(x => x.SetScheduleAsInvalidAsync(schedule.Id))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, mapperMock, scheduleServiceMock);

        await service.DeleteShiftAsync(shift.Id);

        scheduleServiceMock.Verify(x => x.SetScheduleOfflineAsync(schedule.Id), Times.Once);
        scheduleServiceMock.Verify(x => x.SetScheduleAsInvalidAsync(schedule.Id), Times.Once);
    }

    [Fact]
    public async Task ViewShiftsAsync_FilterBySearchAndWeekdays_ReturnsFiltered()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var shiftA = CreateShift(1, "Morning");
        shiftA.Timeslots = new HashSet<Timeslot>
        {
            CreateTimeSlotEntity(1, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0))
        };

        var shiftB = CreateShift(2, "Night");
        shiftB.Timeslots = new HashSet<Timeslot>
        {
            CreateTimeSlotEntity(2, DayOfWeek.Tuesday, new TimeOnly(20, 0), new TimeOnly(23, 0))
        };

        dbContext.Shifts.AddRange(shiftA, shiftB);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var filter = new ShiftFilterDto
        {
            Searchstring = "morn",
            WeekDays = new List<DayOfWeek> { DayOfWeek.Monday },
            Status = ShiftStatusEnum.All
        };

        var result = await service.GetShiftsAsync(new PaginationDto { Page = 1, PageSize = 10 }, filter);

        var shifts = result.Shift.ToList();
        Assert.Equal(1, result.Count);
        Assert.Single(shifts);
        Assert.Equal("Morning", shifts[0].Name);
    }

    [Fact]
    public async Task GetShiftAsync_ShiftNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var service = CreateService(dbContext, mapperMock);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetShiftAsync(999));
    }

    [Fact]
    public async Task GetShiftAsync_Success_ReturnsMappedShift()
    {
        await using var dbContext = CreateDbContext();
        var mapperMock = CreateMapperMock();

        var role = CreateJobRole(1, "Nurse");
        var shift = CreateShift(1, "Mapped");
        shift.Timeslots = new HashSet<Timeslot>
        {
            CreateTimeSlotEntity(1, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0))
        };
        shift.JobRequirements = new HashSet<ShiftRequirement>
        {
            new ShiftRequirement
            {
                Id = 1,
                JobRoleId = role.Id,
                JobRole = role,
                RequiredStaffCount = 2
            }
        };

        dbContext.JobRoles.Add(role);
        dbContext.Shifts.Add(shift);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, mapperMock);

        var result = await service.GetShiftAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Mapped", result.Name);
        Assert.Single(result.TimeSlots);
        Assert.Single(result.JobRequirements);
    }

    private static ShiftService CreateService(
        SchichtpilotDbContext dbContext,
        Mock<IMapper> mapperMock,
        Mock<IWorkScheduleService>? scheduleServiceMock = null)
    {
        var scheduleService = scheduleServiceMock ?? new Mock<IWorkScheduleService>();
        return new ShiftService(dbContext, mapperMock.Object, scheduleService.Object);
    }

    private static Mock<IMapper> CreateMapperMock()
    {
        var mapperMock = new Mock<IMapper>();

        mapperMock
            .Setup(mapper => mapper.Map<Shift, ShortShiftDto>(It.IsAny<Shift>()))
            .Returns((Shift shift) => new ShortShiftDto
            {
                Id = shift.Id,
                Name = shift.Name,
                ColorAsHex = shift.ColorAsHex
            });

        mapperMock
            .Setup(mapper => mapper.Map<Shift, ShiftDto>(It.IsAny<Shift>()))
            .Returns((Shift shift) => new ShiftDto
            {
                Id = shift.Id,
                Name = shift.Name,
                ColorAsHex = shift.ColorAsHex,
                TimeSlots = shift.Timeslots?.Select(x => new TimeSlotDto
                {
                    Id = x.Id,
                    DayOfWeek = x.DayOfWeek,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    Breaks = x.Breaks?.Select(b => new BreakDto
                    {
                        Id = b.Id,
                        StartTime = b.StartTime,
                        EndTime = b.EndTime
                    }).ToList() ?? new List<BreakDto>()
                }).ToList() ?? new List<TimeSlotDto>(),
                JobRequirements = shift.JobRequirements?.Select(x => new ShiftRequirementDto
                {
                    JobId = x.JobRoleId,
                    Name = x.JobRole?.Name ?? string.Empty,
                    RequiredStaffCount = x.RequiredStaffCount
                }).ToList() ?? new List<ShiftRequirementDto>()
            });

        mapperMock
            .Setup(mapper => mapper.Map<Timeslot, TimeSlotDto>(It.IsAny<Timeslot>()))
            .Returns((Timeslot timeslot) => new TimeSlotDto
            {
                Id = timeslot.Id,
                DayOfWeek = timeslot.DayOfWeek,
                StartTime = timeslot.StartTime,
                EndTime = timeslot.EndTime,
                Breaks = timeslot.Breaks?.Select(b => new BreakDto
                {
                    Id = b.Id,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime
                }).ToList() ?? new List<BreakDto>()
            });

        return mapperMock;
    }

    private static SchichtpilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchichtpilotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SchichtpilotDbContext(options);
    }

    private static WorkPolicy CreateWorkPolicy(int restPeriodMinutes = 30, int restPeriodThresholdMinutes = 240)
    {
        return new WorkPolicy
        {
            MaximumConsecutiveWorkHoursPerDay = 8,
            MinimumRestPeriodInMinutes = restPeriodMinutes,
            RestPeriodThresholdInMinutes = restPeriodThresholdMinutes
        };
    }

    private static Shift CreateShift(int id, string name)
    {
        return new Shift
        {
            Id = id,
            Name = name,
            ColorAsHex = "111111",
            Timeslots = new HashSet<Timeslot>(),
            JobRequirements = new HashSet<ShiftRequirement>()
        };
    }

    private static WorkSchedule CreateWorkSchedule(int id)
    {
        return new WorkSchedule
        {
            Id = id,
            Name = $"Schedule {id}",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 7),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>(),
            ShiftAssignments = new HashSet<ShiftAssignment>()
        };
    }

    private static WorkScheduleShifts CreateWorkScheduleShift(WorkSchedule schedule, Shift shift)
    {
        return new WorkScheduleShifts
        {
            WorkScheduleId = schedule.Id,
            WorkSchedule = schedule,
            ShiftId = shift.Id,
            Shift = shift
        };
    }

    private static TimeSlotDto CreateTimeSlotDto(DayOfWeek day, TimeOnly start, TimeOnly end)
    {
        return new TimeSlotDto
        {
            DayOfWeek = day,
            StartTime = start,
            EndTime = end,
            Breaks = new List<BreakDto>()
        };
    }

    private static Timeslot CreateTimeSlotEntity(int id, DayOfWeek day, TimeOnly start, TimeOnly end)
    {
        return new Timeslot
        {
            Id = id,
            DayOfWeek = day,
            StartTime = start,
            EndTime = end,
            Breaks = new HashSet<Break>()
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
}
