using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Interfaces;

public interface IShiftService
{
    Task CreateShiftAsync(CreateShiftDto shift);
    
    Task ManageShiftAsync(int shiftId, CreateShiftDto shift);
    Task DeleteShiftAsync(int shiftId);
    Task<QueryableShiftResponse> ViewShiftsAsync(PaginationDto pagination, ShiftFilterDto? filter);
    Task<ShiftDto> GetShiftAsync(int shiftId);
}