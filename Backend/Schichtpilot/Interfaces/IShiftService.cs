using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Interfaces;

public interface IShiftService
{
    Task CreateShiftAsync(CreateShiftDto shift);
    
    Task ManageShiftAsync(int shiftId, EditShiftDto shift);
    Task ManageTimeSlots(int shiftId, List<TimeSlotDto> slots);
    
    Task ManageJobRequirements(int shiftId, List<ShiftRequirementDto> requirements);
    
    Task DeleteShiftAsync(int shiftId);
    Task<QueryableShiftResponse> ViewShiftsAsync(PaginationDto pagination, ShiftFilterDto? filter);
    Task<ShiftDto> GetShiftAsync(int shiftId);
}