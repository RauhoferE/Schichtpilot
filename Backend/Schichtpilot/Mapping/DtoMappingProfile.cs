using AutoMapper;
using Data.Entities;
using Schichtpilot.Models.DTOs;

namespace Schichtpilot.Mapping;

public class DtoMappingProfile : Profile
{
    public DtoMappingProfile()
    {
        CreateMap<WorkPolicy, CompanyPolicyDto>();
    }
}