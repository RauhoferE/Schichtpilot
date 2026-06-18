using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace IntegrationTests.Fakes;

public class FakeShiftService : IShiftService
{
    public int? LastChangeRequiredStaffCount { get; private set; }
    public int? LastChangeRequiredStaffShiftId { get; private set; }
    public int? LastChangeRequiredStaffJobId { get; private set; }

    public Task CreateShiftAsync(CreateShiftDto shift)
    {
        return Task.CompletedTask;
    }

    public Task ManageShiftAsync(int shiftId, EditShiftDto shift)
    {
        return Task.CompletedTask;
    }

    public Task DeleteTimeSlotAsync(int shiftId, int timeSlotId)
    {
        return Task.CompletedTask;
    }

    public Task AddTimeSlotAsync(int shiftId, TimeSlotDto timeSlot)
    {
        return Task.CompletedTask;
    }

    public Task EditTimeSlotAsync(int shiftId, TimeSlotDto timeSlot)
    {
        return Task.CompletedTask;
    }

    public Task AddJobRequirementAsync(int shiftId, ShiftRequirementDto jobRequirement)
    {
        return Task.CompletedTask;
    }

    public Task ChangeRequiredStaffAsync(int shiftId, int jobRequirementId, int requiredStaffCount)
    {
        LastChangeRequiredStaffShiftId = shiftId;
        LastChangeRequiredStaffJobId = jobRequirementId;
        LastChangeRequiredStaffCount = requiredStaffCount;
        return Task.CompletedTask;
    }

    public Task DeleteJobRequirementAsync(int shiftId, int jobRequirementId)
    {
        return Task.CompletedTask;
    }

    public Task DeleteShiftAsync(int shiftId)
    {
        return Task.CompletedTask;
    }

    public Task<QueryableShiftResponse> GetShiftsAsync(PaginationDto pagination, ShiftFilterDto? filter)
    {
        return Task.FromResult(new QueryableShiftResponse());
    }

    public Task<ShiftDto> GetShiftAsync(int shiftId)
    {
        return Task.FromResult(new ShiftDto(){Id = shiftId, Name = "test", ColorAsHex = "FFFFFF"});
    }
}
