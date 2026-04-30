using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;

namespace Schichtpilot.Interfaces;

/// <summary>
/// Orchestrates shift related operations including creating, managing, deleting shifts, timeslots for the shifts and job requirements.
/// </summary>
public interface IShiftService
{
    /// <summary>
    /// Creates a new shift.
    /// </summary>
    /// <param name="shift"> The shift to be created. </param>
    /// <returns></returns>
    Task CreateShiftAsync(CreateShiftDto shift);

    /// <summary>
    /// Updates an existing shift with a new name and color.
    /// </summary>
    /// <param name="shiftId"> The shift to be updated. </param>
    /// <param name="shift"> The updated shift data. </param>
    /// <returns></returns>
    Task ManageShiftAsync(int shiftId, EditShiftDto shift);

    /// <summary>
    /// Deletes a timeslot of a shift.
    /// </summary>
    /// <param name="shiftId"> The shift that contains the timeslot. </param>
    /// <param name="timeSlotId"> The timeslot to be deleted. </param>
    /// <returns></returns>
    Task DeleteTimeSlotAsync(int shiftId, int timeSlotId);

    /// <summary>
    /// Adds a new timeslot to the shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be updated. </param>
    /// <param name="timeSlot"> The new timeslot to be added. </param>
    /// <returns></returns>
    Task AddTimeSlotAsync(int shiftId, TimeSlotDto timeSlot);

    /// <summary>
    /// Updates an existing timeslot.
    /// </summary>
    /// <param name="shiftId"> The shift that contains the timeslot. </param>
    /// <param name="timeSlot"> The timeslot to be updated. </param>
    /// <returns></returns>
    Task EditTimeSlotAsync(int shiftId, TimeSlotDto timeSlot);

    /// <summary>
    /// Adds new job requirements for a shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be updated. </param>
    /// <param name="jobRequirement"> The new job requirements. </param>
    /// <returns></returns>
    Task AddJobRequirementAsync(int shiftId, ShiftRequirementDto jobRequirement);

    /// <summary>
    /// Changes the staff amount needed to complete a shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be updated. </param>
    /// <param name="jobRequirementId"> The job to be updated. </param>
    /// <param name="requiredStaffCount"> The staff needed for this particular job. </param>
    /// <returns></returns>
    Task ChangeRequiredStaffAsync(int shiftId, int jobRequirementId, int requiredStaffCount);

    /// <summary>
    /// Deletes a job requirement of a shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be updated. </param>
    /// <param name="jobRequirementId"> The job to be removed from a shift. </param>
    /// <returns></returns>
    Task DeleteJobRequirementAsync(int shiftId, int jobRequirementId);

    /// <summary>
    /// Deletes an existing shift.
    /// </summary>
    /// <param name="shiftId"> The shift to be deleted. </param>
    /// <returns></returns>
    Task DeleteShiftAsync(int shiftId);
    
    /// <summary>
    /// Gets a list of existing shifts.
    /// </summary>
    /// <param name="pagination"> The pagination element. </param>
    /// <param name="filter"> How to sort and filter the shifts. </param>
    /// <returns> Returns the shifts as <see cref="QueryableShiftResponse"/>. </returns>
    Task<QueryableShiftResponse> GetShiftsAsync(PaginationDto pagination, ShiftFilterDto? filter);
    
    /// <summary>
    /// Gets the details of a shift.
    /// </summary>
    /// <param name="shiftId"> The targeted shift. </param>
    /// <returns> Returns the shift as <see cref="ShiftDto"/>. </returns>
    Task<ShiftDto> GetShiftAsync(int shiftId);
}