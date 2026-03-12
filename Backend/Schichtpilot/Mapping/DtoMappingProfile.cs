using AutoMapper;
using Data.Entities;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Mapping;

public class DtoMappingProfile : Profile
{
    public DtoMappingProfile()
    {
        CreateMap<WorkPolicy, CompanyPolicyDto>();
        CreateMap<Absence, AbsenceDto>();
        CreateMap<JobRole, JobRoleDto>()
            .ForMember(dest => dest.DependentOn, opt => opt.MapFrom(src => src.Dependencies.Select(x => x.JobRole)))
            .ForMember(dest => dest.Prerequisites, opt => opt.MapFrom(src => src.Prerequisites.Select(x => x.JobRole)))
            .ForMember(dest => dest.Users, opt => opt.MapFrom(src => src.UsersWithRole.Select(x => x.User)));
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.AddressDto.City, opt => opt.MapFrom(src => src.City))
            .ForMember(dest => dest.AddressDto.PostalCode, opt => opt.MapFrom(src => src.PostalCode))
            .ForMember(dest => dest.AddressDto.Street, opt => opt.MapFrom(src => src.StreetAddress))
            .ForMember(dest => dest.AssignedJobRoles, opt => opt.MapFrom(src => src.JobRoles.Select(x => x.JobRole)));
        CreateMap<UserDto, User>()
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.AddressDto.City))
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.AddressDto.PostalCode))
            .ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.AddressDto.Street));
        CreateMap<JobRole, JobRoleShortDto>();
        
    }
}