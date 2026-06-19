using AutoMapper;
using Data;
using Microsoft.EntityFrameworkCore;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Services;

/// <summary>
/// Orchestrates absence related operations, including creating, updating, deleting viewing user/all user absences.
/// </summary>
public class AbsenceService : IAbsenceService
{
    private readonly SchichtpilotDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IWorkScheduleService _workScheduleService;

    public AbsenceService(SchichtpilotDbContext dbContext, IMapper mapper, IEmailService emailService, IWorkScheduleService workScheduleService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _workScheduleService = workScheduleService ?? throw new ArgumentNullException(nameof(workScheduleService));
    }


    /// <summary>
    /// Creates a new absence request.
    /// </summary>
    /// <param name="userId"> The user that created the absence. </param>
    /// <param name="dto"> The absence specifics. </param>
    /// <returns></returns>
    /// <exception cref="UserNotFoundException"> Thrown when the user id does not match a user. </exception>
    /// <exception cref="AlreadyExistsException"> Thrown if the user already created an absence that lies in the start and end date. </exception>
    public async Task CreateAbsenceRequestAsync(long userId, CreateAbsenceDto dto)
    {
        // FR149: User exists (Identity handles lockout, controller auth)
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) { throw new UserNotFoundException("User not found"); }

        // Overlap check
        var overlapping = await _dbContext.Absences.AnyAsync(x => x.UserId == userId &&
            x.Status != nameof(AbsenceStatusEnum.Denied) && dto.StartDate < x.EndDate && dto.EndDate > x.StartDate);
        if (overlapping) { throw new AlreadyExistsException("Overlapping absence exists"); }

        // FR143-144: Pending persist
        var absence = new Absence
        {
            UserId = userId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            AbsenceType = dto.AbsenceType,
            Message = dto.Message,
            // If the person is sick this has to be approved by manager
            Status = AbsenceStatusEnum.Pending.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Absences.Add(absence);
        await _dbContext.SaveChangesAsync();

        if (absence.Status == nameof(AbsenceStatusEnum.Approved))
        {
            await SetWorkSchedulesWithUserAsInvalid(userId, dto.StartDate, dto.EndDate);
        }
        //map tp AbsenceDto and pass User object
        var absenceDto = _mapper.Map<AbsenceDto>(absence);
        //_ = Task.Run(async () =>
        await _emailService.SendNewAbsenceMailToManager(user, absenceDto);
    }

    /// <summary>
    /// Sets the work schedules with the user assigned to it as invalid.
    /// </summary>
    /// <param name="userId"> The user id. </param>
    /// <param name="startDate"> The start date of the absence. </param>
    /// <param name="endDate"> The end date of the absence. </param>
    private async Task SetWorkSchedulesWithUserAsInvalid(long userId, DateTime startDate, DateTime endDate)
    {
        var schedulesWithUser = this._dbContext.ShiftAssignments
            .Include(x => x.UserJobRole)
            .ThenInclude(x => x.User)
            .Include(x => x.WorkSchedule)
            .Where(x => startDate < x.EndTime.Date && x.StartTime.Date < endDate &&
                        userId == x.UserId)
            .ToList()
            .Select(x => x.WorkSchedule);

        foreach (var schedule in schedulesWithUser)
        {
            await this._workScheduleService.SetScheduleOfflineAsync(schedule.Id);
            await this._workScheduleService.SetScheduleAsInvalidAsync(schedule.Id);
        }
    }

    /// <summary>
    /// Deletes an absence created by the user itself.
    /// </summary>
    /// <param name="id"> The absence to be deleted. </param>
    /// <param name="userId"> The user id that created the absence. </param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"> Thrown if the absence could not be found. </exception>
    public async Task DeleteOwnAbsenceAsync(int id, long userId)
    {
        var absence = await _dbContext.Absences
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.Status == nameof(AbsenceStatusEnum.Pending));
        if (absence == null) { throw new NotFoundException("Own pending absence not found"); }

        _dbContext.Absences.Remove(absence);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Returns all user created absences.
    /// </summary>
    /// <param name="pagination"> The pagination element. </param>
    /// <param name="filter"> How to filter the available absences. </param>
    /// <param name="userId"> The user that created the absences. </param>
    /// <returns> Return the absences as <see cref="QueryableAbsenceResponse"/>. </returns>
    public async Task<QueryableAbsenceResponse> ViewUserAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter, long userId)
    {
        IQueryable<Absence> query = _dbContext.Absences
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt) // FR158
            .Include(x => x.User);

        query = await FilterAbsencesAsync(query, filter);

        var count = await query.CountAsync();
        var pagedQuery = query.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize);
        var absences = pagedQuery.Select(x => this._mapper.Map<AbsenceDto>(x));
        //var dtos = await pagedQuery.ProjectTo<AbsenceDto>(_mapper.ConfigurationProvider).ToListAsync();

        return new QueryableAbsenceResponse { Count = count, Absences = absences.ToList() };
    }

    /// <summary>
    /// Returns all created absences from all users.
    /// </summary>
    /// <param name="pagination"> The pagination element. </param>
    /// <param name="filter"> How to filter the available absences. </param>
    /// <returns> Return the absences as <see cref="QueryableAbsenceResponse"/>. </returns>
    public async Task<QueryableManagerAbsenceResponse> ViewAllAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter)
    {
        IQueryable<Absence> query = _dbContext.Absences
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt);

        query = await FilterAbsencesAsync(query, filter);

        var count = await query.CountAsync();

        var absences = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(a =>
                this._mapper.Map<ManagerAbsenceDto>(a))
            .ToListAsync();

        return new QueryableManagerAbsenceResponse
        {
            Count = count,
            Absences = absences
        };
    }

    /// <summary>
    /// Gets more details about a specific absence.
    /// </summary>
    /// <param name="id"> The id of the absence. </param>
    /// <returns> Returns the absence as a <see cref="AbsenceDto"/>. </returns>
    /// <exception cref="NotFoundException"> Thrown when the absence could not be found. </exception>
    public async Task<AbsenceDto> GetAbsenceDetailAsync(int id)
    {
        var absence = await _dbContext.Absences.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (absence == null) { throw new NotFoundException("Absence not found"); }
        return _mapper.Map<AbsenceDto>(absence);
    }

    /// <summary>
    /// Updates a specific absence.
    /// </summary>
    /// <param name="id"> The id of the absence to update. </param>
    /// <param name="dto"> The new status for the absence. </param>
    /// <returns></returns>
    public async Task UpdateAbsenceStatusAsync(int id, StatusUpdateDto dto)
    {
        var absence = await _dbContext.Absences
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == nameof(AbsenceStatusEnum.Pending));

        if (absence == null)
        {
            throw new NotFoundException("Pending absence not found");
        }

        absence.Status = dto.Status;
        absence.ManagerMessage = dto.ManagerMessage;
        await _dbContext.SaveChangesAsync();

        if (dto.Status == nameof(AbsenceStatusEnum.Approved))
        {
            await SetWorkSchedulesWithUserAsInvalid(absence.UserId, absence.StartDate, absence.EndDate);
        }

        // Send email
        var absenceDto = _mapper.Map<AbsenceDto>(absence);
        if (dto.Status == nameof(AbsenceStatusEnum.Approved))
        {
            _ = Task.Run(async () =>
                await _emailService.SendApprovalMail(absence.User, absenceDto));
        }

        if (dto.Status == nameof(AbsenceStatusEnum.Denied))
        {
            _ = Task.Run(async () =>
                await _emailService.SendRejectionMail(absence.User, absenceDto));
        }

    }

    /// <summary>
    /// Filters the given absences.
    /// </summary>
    /// <param name="query"> The absences to be filtered. </param>
    /// <param name="filter"> How the absences should be filtered. </param>
    /// <returns> Returns the absences as <see cref="IQueryable"/>. </returns>
    private static Task<IQueryable<Absence>> FilterAbsencesAsync(IQueryable<Absence> query, AbsenceFilterDto? filter)
    {
        if (filter == null) { return Task.FromResult(query); }

        // FR148: Date/emp/type search (UserName via FirstName/LastName/Email)
        if (!string.IsNullOrEmpty(filter.Searchstring))
        {
            var search = filter.Searchstring.ToLower();
            query = query.Where(x => x.AbsenceType.ToLower().Contains(search) ||
                x.User.FirstName.ToLower().Contains(search) ||
                x.User.LastName.ToLower().Contains(search) ||
                x.User.Email!.ToLower().Contains(search));
        }

        if (filter.Status?.Any() == true)
        { query = query.Where(x => filter.Status.Select(s => s.ToString()).Contains(x.Status)); }

        if (filter.AbsenceType?.Any() == true)
        {
            query = query.Where(x => filter.AbsenceType.Select(t => t.ToString()).Contains(x.AbsenceType));
        }

        if (filter.StartDateFrom.HasValue) { query = query.Where(x => x.StartDate >= filter.StartDateFrom.Value); }
        if (filter.StartDateTo.HasValue) { query = query.Where(x => x.StartDate <= filter.StartDateTo.Value); }

        return Task.FromResult(query);
    }
}
