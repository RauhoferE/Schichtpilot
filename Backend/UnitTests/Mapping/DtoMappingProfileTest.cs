using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Data.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Schichtpilot.Mapping;
using Schichtpilot.Models.DTOs;
using Xunit;

namespace UnitTests.Mapping;

public class DtoMappingProfileTest
{
    [Fact]
    public void Configuration_IsValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DtoMappingProfile>(), NullLoggerFactory.Instance);
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_User_To_UserDto_MapsAddressAndJobRoles()
    {
        var mapper = CreateMapper();

        var jobRole = new JobRole
        {
            Id = 7,
            Name = "Cashier",
            Description = "Handles checkout.",
            CreatedOn = DateTime.UtcNow
        };

        var user = new User
        {
            Email = "user@test.com",
            FirstName = "Jane",
            LastName = "Doe",
            City = "Testville",
            PostalCode = 1234,
            StreetAddress = "Main Street 1",
            BirthDate = DateTime.UtcNow.AddYears(-25),
            JobRoles = new HashSet<UserJobRoles>
            {
                new()
                {
                    JobRole = jobRole,
                    JobRoleId = jobRole.Id
                }
            }
        };

        var dto = mapper.Map<UserDto>(user);

        Assert.NotNull(dto.AddressDto);
        Assert.Equal("Testville", dto.AddressDto.City);
        Assert.Equal(1234, dto.AddressDto.PostalCode);
        Assert.Equal("Main Street 1", dto.AddressDto.Street);

        Assert.NotNull(dto.AssignedJobRoles);
        var assigned = Assert.Single(dto.AssignedJobRoles);
        Assert.Equal(jobRole.Id, assigned.Id);
        Assert.Equal(jobRole.Name, assigned.Name);
        Assert.Equal(jobRole.Description, assigned.Description);
    }

    [Fact]
    public void Map_UserDto_To_User_MapsAddressFields()
    {
        var mapper = CreateMapper();

        var dto = new UserDto
        {
            Email = "user@test.com",
            FirstName = "John",
            LastName = "Smith",
            Birthdate = DateTime.UtcNow.AddYears(-30),
            AddressDto = new AddressDto
            {
                Street = "Second Street 5",
                City = "Sampletown",
                PostalCode = 4321
            },
            AssignedJobRoles = new List<JobRoleShortDto>()
        };

        var user = mapper.Map<User>(dto);

        Assert.Equal("Sampletown", user.City);
        Assert.Equal(4321, user.PostalCode);
        Assert.Equal("Second Street 5", user.StreetAddress);
    }

    [Fact]
    public void Map_ShiftRequirement_To_ShiftRequirementDto_MapsJobFields()
    {
        var mapper = CreateMapper();

        var shiftRequirement = new ShiftRequirement
        {
            JobRoleId = 5,
            JobRole = new JobRole
            {
                Id = 5,
                Name = "Stock",
                Description = "Handles stocking.",
                CreatedOn = DateTime.UtcNow
            },
            RequiredStaffCount = 3
        };

        var dto = mapper.Map<ShiftRequirementDto>(shiftRequirement);

        Assert.Equal(5, dto.JobId);
        Assert.Equal("Stock", dto.Name);
        Assert.Equal(3, dto.RequiredStaffCount);
    }

    [Fact]
    public void Map_WorkSchedule_To_WorkScheduleShortDto_MapsShiftCount()
    {
        var mapper = CreateMapper();

        var schedule = new WorkSchedule
        {
            Id = 10,
            Name = "Week 1",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(7),
            IsActive = true,
            IsValid = true,
            Shifts = new HashSet<WorkScheduleShifts>
            {
                new()
                {
                    ShiftId = 1,
                    Shift = new Shift
                    {
                        Id = 1,
                        Name = "Morning",
                        ColorAsHex = "FFFFFF"
                    }
                },
                new()
                {
                    ShiftId = 2,
                    Shift = new Shift
                    {
                        Id = 2,
                        Name = "Evening",
                        ColorAsHex = "000000"
                    }
                }
            }
        };

        var dto = mapper.Map<WorkScheduleShortDto>(schedule);

        Assert.Equal(2, dto.ShiftCount);
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DtoMappingProfile>(), NullLoggerFactory.Instance);
        return config.CreateMapper();
    }
}
