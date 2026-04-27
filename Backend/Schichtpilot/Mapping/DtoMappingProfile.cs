using AutoMapper;
using Data.Entities;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;
using Schichtpilot.Models.Requests;

namespace Schichtpilot.Mapping;

/// <summary>
/// Maps entities to dtos, dtos to entities and dtos to other dtos.
/// </summary>
public class DtoMappingProfile : Profile
{
    public DtoMappingProfile()
    {
        CreateMap<WorkPolicy, CompanyPolicyDto>();
        CreateMap<Absence, AbsenceDto>();
        CreateMap<JobRole, JobRoleDto>()
            .ForMember(dest => dest.DependentOn, opt => opt.MapFrom(src => src.Dependencies.Select(x => x.JobRole).ToList()))
            .ForMember(dest => dest.Prerequisites, opt => opt.MapFrom(src => src.Prerequisites.Select(x => x.JobRole).ToList()))
            .ForMember(dest => dest.Users, opt => opt.MapFrom(src => src.UsersWithRole.Select(x => x.User)));
        CreateMap<User, UserDto>(MemberList.Destination)
            .ForPath(dest => dest.AddressDto.City, opt => opt.MapFrom(src => src.City))
            .ForPath(dest => dest.AddressDto.PostalCode, opt => opt.MapFrom(src => src.PostalCode))
            .ForPath(dest => dest.AddressDto.Street, opt => opt.MapFrom(src => src.StreetAddress))
            .ForMember(dest => dest.AssignedJobRoles, opt => opt.MapFrom(src => src.JobRoles.Select(x => x.JobRole)));
        CreateMap<UserDto, User>(MemberList.None)
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.AddressDto.City))
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.AddressDto.PostalCode))
            .ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.AddressDto.Street))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email));
        CreateMap<JobRole, JobRoleShortDto>();

        CreateMap<Timeslot, TimeSlotDto>();
        CreateMap<Break, BreakDto>();
        CreateMap<Shift, ShiftDto>()
            .ForMember(dest => dest.TimeSlots, opt => opt.MapFrom(src => src.Timeslots));
        CreateMap<ShiftRequirement, ShiftRequirementDto>()
            .ForMember(dest => dest.JobId, opt => opt.MapFrom(src => src.JobRoleId))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.JobRole.Name));
        CreateMap<Shift, ShortShiftDto>();
        CreateMap<WorkSchedule, WorkScheduleShortDto>()
            .ForMember(dest => dest.ShiftCount, opt => opt.MapFrom(src => src.Shifts.Count));
        CreateMap<WorkSchedule, WorkScheduleDto>()
            .ForMember(dest => dest.Shifts, opt => opt.MapFrom(src => src.Shifts.Select(x => x.Shift)))
            .ForMember(dest => dest.AssignedUsers, opt => opt.MapFrom(src => src.ShiftAssignments));
        CreateMap<ShiftAssignment, AssignedUserDto>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.UserJobRole.User))
            .ForMember(dest => dest.JobRole, opt => opt.MapFrom(src => src.UserJobRole.JobRole));

        CreateMap<GetAbsencesRequest, PaginationDto>();
        CreateMap<GetAbsencesRequest, AbsenceFilterDto>();
        CreateMap<GetJobRolesRequest, PaginationDto>();
        CreateMap<GetShiftsRequest, PaginationDto>();
        CreateMap<GetShiftsRequest, ShiftFilterDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.ShiftStatusEnum));
        CreateMap<CreateUserRequest, UserDto>(MemberList.None);
        CreateMap<GetUsersRequest, PaginationDto>();
        CreateMap<GetUsersRequest, UserSortingDto>();
        CreateMap<GetUsersRequest, UserFilterDto>();
        CreateMap<GetSchedulesRequest, PaginationDto>();
        CreateMap<GetSchedulesRequest, ScheduleFilterDot>();
    }
}