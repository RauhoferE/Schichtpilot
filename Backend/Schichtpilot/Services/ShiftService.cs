using AutoMapper;
using Data;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Services;

public class ShiftService : IShiftService
{
    public ShiftService(SchichtpilotDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    private readonly SchichtpilotDbContext _dbContext;
    
    private readonly IMapper _mapper;
    
    public Task CreateShiftAsync(CreateShiftDto shift)
    {
        throw new NotImplementedException();
    }

    public Task ManageShiftAsync(int shiftId, CreateShiftDto shift)
    {
        throw new NotImplementedException();
    }

    public Task DeleteShiftAsync(int shiftId)
    {
        throw new NotImplementedException();
    }

    public Task<QueryableShiftResponse> ViewShiftsAsync(PaginationDto pagination, ShiftFilterDto? filter)
    {
        throw new NotImplementedException();
    }

    public Task<ShiftDto> GetShiftAsync(int shiftId)
    {
        throw new NotImplementedException();
    }
}