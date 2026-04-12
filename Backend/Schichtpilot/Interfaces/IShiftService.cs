using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Interfaces;

public interface IShiftService
{
    Task CreateShiftAsync(CreateShiftDto shift);

    Task ManageShiftAsync(int shiftId, EditShiftDto shift);

    Task DeleteTimeSlotAsync(int shiftId, int timeSlotId);

    Task AddTimeSlotAsync(int shiftId, TimeSlotDto timeSlot);

    Task EditTimeSlotAsync(int shiftId, TimeSlotDto timeSlot);

    Task AddJobRequirementAsync(int shiftId, ShiftRequirementDto jobRequirement);

    Task ChangeRequiredStaffAsync(int shiftId, int jobRequirementId, int requiredStaffCount);

    Task DeleteJobRequirementAsync(int shiftId, int jobRequirementId);

    Task DeleteShiftAsync(int shiftId);
    Task<QueryableShiftResponse> GetShiftsAsync(PaginationDto pagination, ShiftFilterDto? filter);
    Task<ShiftDto> GetShiftAsync(int shiftId);
}