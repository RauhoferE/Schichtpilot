using System.ComponentModel.DataAnnotations;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Schichtpilot.Exceptions;
using Schichtpilot.Interfaces;
using Schichtpilot.Models.DTOs;
using Schichtpilot.Models.Responses;
using Schichtpilot.Models.Enums;

namespace Schichtpilot.Services;

public class AbsenceService : IAbsenceService
{
    private readonly SchichtpilotDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;

    public AbsenceService(SchichtpilotDbContext dbContext, IMapper mapper, IEmailService emailService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    public async Task CreateAbsenceRequestAsync(CreateAbsenceDto dto, long userId)
    {
        // FR141: Dates + type (DTO required validates)
        if (dto.StartDate == default || dto.EndDate == default)
            throw new ValidationException("Valid dates and type required");

        // FR142: Not past/valid
        if (dto.StartDate.Date < DateTime.UtcNow.Date || dto.EndDate.Date < dto.StartDate.Date)
            throw new ValidationException("Dates must be future and End >= Start");

        // FR149: User exists (Identity handles lockout, controller auth)
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) throw new UserNotFoundException("User not found");

        // Overlap check
        var overlapping = await _dbContext.Absences.AnyAsync(x => x.UserId == userId &&
            x.Status != nameof(AbsenceStatusEnum.Denied) && dto.StartDate < x.EndDate && dto.EndDate > x.StartDate);
        if (overlapping) throw new AlreadyExistsException("Overlapping absence exists");

        // FR143-144: Pending persist
        var absence = new Absence
        {
            UserId = userId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            AbsenceType = dto.AbsenceType,
            Message = dto.Message,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Absences.Add(absence);
        try
        {
            await _dbContext.SaveChangesAsync();
            // FR145: Notify (uniform sick/other)
            await _emailService.SendNewAbsenceNotificationAsync(absence.Id, user.FirstName + " " + user.LastName);
        }
        catch (Exception emailEx)
        {
            // ROLLBACK absence if email fails
            _dbContext.Absences.Remove(absence);
            await _dbContext.SaveChangesAsync();
    
            throw new EmailNotificationException("Failed to notify managers", emailEx);
        } // FR146
    }

    public async Task DeleteOwnAbsenceAsync(int id, long userId)
    {
        var absence = await _dbContext.Absences
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.Status == nameof(AbsenceStatusEnum.Pending));
        if (absence == null) throw new NotFoundException("Own pending absence not found");

        _dbContext.Absences.Remove(absence);
         await _dbContext.SaveChangesAsync();
    }
    
    public async Task<QueryableAbsenceResponse> ViewOwnAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter, long userId)
    {
        IQueryable<Absence> query = _dbContext.Absences
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt) // FR158
            .Include(x => x.User);

        query = await FilterAbsencesAsync(query, filter);

        var count = await query.CountAsync();
        var pagedQuery = query.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize);
        var dtos = await pagedQuery.ProjectTo<AbsenceDto>(_mapper.ConfigurationProvider).ToListAsync();

        return new QueryableAbsenceResponse { Count = count, Absences = dtos };
    }

    public async Task<QueryableAbsenceResponse> ViewAllAbsencesAsync(PaginationDto pagination, AbsenceFilterDto? filter)
    {
        IQueryable<Absence> query = _dbContext.Absences
            .OrderByDescending(x => x.CreatedAt)
            .Include(x => x.User);

        query = await FilterAbsencesAsync(query, filter);

        var count = await query.CountAsync();
        var pagedQuery = query.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize);
        var dtos = await pagedQuery.ProjectTo<AbsenceDto>(_mapper.ConfigurationProvider).ToListAsync();

        return new QueryableAbsenceResponse { Count = count, Absences = dtos };
    }

    public async Task<AbsenceDto> GetAbsenceDetailAsync(int id)
    {
     
        var absence = await _dbContext.Absences.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (absence == null) throw new NotFoundException("Absence not found");
        return _mapper.Map<AbsenceDto>(absence);
    }

    public async Task UpdateAbsenceStatusAsync(int id, StatusUpdateDto dto)
    {
        var absence = await _dbContext.Absences.FirstOrDefaultAsync(x => x.Id == id && x.Status == nameof(AbsenceStatusEnum.Pending));
        if (absence == null) throw new NotFoundException("Pending absence not found");

        if (dto.Status == nameof(AbsenceStatusEnum.Denied) && string.IsNullOrEmpty(dto.ManagerMessage))
            throw new ValidationException("Denial requires message");

        absence.Status = dto.Status;
        absence.ManagerMessage = dto.ManagerMessage;
        await _dbContext.SaveChangesAsync(); 
    }

    private Task<IQueryable<Absence>> FilterAbsencesAsync(IQueryable<Absence> query, AbsenceFilterDto? filter)
    {
        if (filter == null) return Task.FromResult(query);

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
            query = query.Where(x => filter.Status.Select(s => s.ToString()).Contains(x.Status));

        if (filter.AbsenceType?.Any() == true)
            query = query.Where(x => filter.AbsenceType.Select(t => t.ToString()).Contains(x.AbsenceType));

        if (filter.CreatedFrom.HasValue) query = query.Where(x => x.CreatedAt >= filter.CreatedFrom.Value);
        if (filter.CreatedTo.HasValue) query = query.Where(x => x.CreatedAt <= filter.CreatedTo.Value);
        if (filter.StartDateFrom.HasValue) query = query.Where(x => x.StartDate >= filter.StartDateFrom.Value);
        if (filter.StartDateTo.HasValue) query = query.Where(x => x.StartDate <= filter.StartDateTo.Value);

        return Task.FromResult(query);
    }
}
