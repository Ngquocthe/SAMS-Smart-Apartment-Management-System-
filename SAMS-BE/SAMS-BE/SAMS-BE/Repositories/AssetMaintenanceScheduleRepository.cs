using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories;

public class AssetMaintenanceScheduleRepository : IAssetMaintenanceScheduleRepository
{
    private readonly BuildingManagementContext _context;

    public AssetMaintenanceScheduleRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AssetMaintenanceSchedule>> GetAllSchedulesAsync()
    {
        return await _context.AssetMaintenanceSchedules
            .Include(s => s.Asset)
                .ThenInclude(a => a.Category)
            .Include(s => s.CreatedByUser)
            .Include(s => s.AssetMaintenanceHistories)
            .Where(s => s.Status != "DONE" && s.Status != "CANCELLED")
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<AssetMaintenanceSchedule?> GetScheduleByIdAsync(Guid scheduleId)
    {
        return await _context.AssetMaintenanceSchedules
            .Include(s => s.Asset)
                .ThenInclude(a => a.Category)
            .Include(s => s.CreatedByUser)
            .Include(s => s.AssetMaintenanceHistories)
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
    }

    public async Task<IEnumerable<AssetMaintenanceSchedule>> GetSchedulesByAssetIdAsync(Guid assetId)
    {
        return await _context.AssetMaintenanceSchedules
            .Include(s => s.Asset)
                .ThenInclude(a => a.Category)
            .Include(s => s.CreatedByUser)
            .Include(s => s.AssetMaintenanceHistories)
            .Where(s => s.AssetId == assetId && s.Status != "DONE" && s.Status != "CANCELLED")
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<AssetMaintenanceSchedule>> GetSchedulesByStatusAsync(string status)
    {
        return await _context.AssetMaintenanceSchedules
            .Include(s => s.Asset)
                .ThenInclude(a => a.Category)
            .Include(s => s.CreatedByUser)
            .Include(s => s.AssetMaintenanceHistories)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();
    }


    public async Task<IEnumerable<AssetMaintenanceSchedule>> GetSchedulesDueForReminderAsync(DateOnly today)
    {
        return await _context.AssetMaintenanceSchedules
            .Include(s => s.Asset)
                .ThenInclude(a => a.Category)
            .Include(s => s.CreatedByUser)
            .Include(s => s.AssetMaintenanceHistories)
            .Where(s => s.Status == "SCHEDULED" &&
                       s.StartDate >= today &&
                       s.StartDate <= today.AddDays(s.ReminderDays))
            .ToListAsync();
    }

    public async Task<IEnumerable<AssetMaintenanceSchedule>> GetSchedulesDueForMaintenanceAsync(DateOnly today)
    {
        return await _context.AssetMaintenanceSchedules
            .Include(s => s.Asset)
                .ThenInclude(a => a.Category)
            .Include(s => s.CreatedByUser)
            .Include(s => s.AssetMaintenanceHistories)
            .Where(s => (s.Status == "SCHEDULED" || s.Status == "IN_PROGRESS") &&
                       s.EndDate <= today)
            .ToListAsync();
    }

    public async Task<AssetMaintenanceSchedule> CreateScheduleAsync(AssetMaintenanceSchedule schedule)
    {
        if (schedule.ScheduleId == Guid.Empty)
        {
            schedule.ScheduleId = Guid.NewGuid();
        }
        
        if (schedule.CreatedAt == default(DateTime))
        {
            schedule.CreatedAt = DateTime.UtcNow.AddHours(7);
        }
        
        schedule.Status = schedule.Status ?? "SCHEDULED";
        
        var startTimeValue = schedule.StartTime;
        var endTimeValue = schedule.EndTime;
        
        _context.AssetMaintenanceSchedules.Add(schedule);
        
        var entry = _context.Entry(schedule);
        
        if (startTimeValue.HasValue)
        {
            entry.Property(nameof(AssetMaintenanceSchedule.StartTime)).CurrentValue = startTimeValue.Value;
            entry.Property(nameof(AssetMaintenanceSchedule.StartTime)).IsModified = true;
        }
        
        if (endTimeValue.HasValue)
        {
            entry.Property(nameof(AssetMaintenanceSchedule.EndTime)).CurrentValue = endTimeValue.Value;
            entry.Property(nameof(AssetMaintenanceSchedule.EndTime)).IsModified = true;
        }
        
        await _context.SaveChangesAsync();
        
        return schedule;
    }

    public async Task<AssetMaintenanceSchedule?> UpdateScheduleAsync(AssetMaintenanceSchedule schedule)
    {
        var existingSchedule = await _context.AssetMaintenanceSchedules
            .FirstOrDefaultAsync(s => s.ScheduleId == schedule.ScheduleId);
            
        if (existingSchedule == null)
        {
            return null;
        }

        existingSchedule.StartDate = schedule.StartDate;
        existingSchedule.EndDate = schedule.EndDate;
        existingSchedule.AssetId = schedule.AssetId;
        existingSchedule.ReminderDays = schedule.ReminderDays;
        existingSchedule.Description = schedule.Description;
        existingSchedule.Status = schedule.Status;
        existingSchedule.RecurrenceType = schedule.RecurrenceType;
        existingSchedule.RecurrenceInterval = schedule.RecurrenceInterval;
        
        // Update scheduled dates
        existingSchedule.ScheduledStartDate = schedule.ScheduledStartDate;
        existingSchedule.ScheduledEndDate = schedule.ScheduledEndDate;
        
        // Update actual dates
        existingSchedule.ActualStartDate = schedule.ActualStartDate;
        existingSchedule.ActualEndDate = schedule.ActualEndDate;
        
        // Update completion info
        existingSchedule.CompletionNotes = schedule.CompletionNotes;
        existingSchedule.CompletedBy = schedule.CompletedBy;
        existingSchedule.CompletedAt = schedule.CompletedAt;
        
        var entry = _context.Entry(existingSchedule);
        entry.Property(nameof(AssetMaintenanceSchedule.StartTime)).CurrentValue = schedule.StartTime;
        entry.Property(nameof(AssetMaintenanceSchedule.StartTime)).IsModified = true;
        entry.Property(nameof(AssetMaintenanceSchedule.EndTime)).CurrentValue = schedule.EndTime;
        entry.Property(nameof(AssetMaintenanceSchedule.EndTime)).IsModified = true;

        await _context.SaveChangesAsync();
        return existingSchedule;
    }

    public async Task<bool> DeleteScheduleAsync(Guid scheduleId)
    {
        var schedule = await _context.AssetMaintenanceSchedules.FindAsync(scheduleId);
        if (schedule == null)
        {
            return false;
        }

        // Xóa các announcements liên quan trước (để tránh foreign key constraint)
        var relatedAnnouncements = await _context.Announcements
            .Where(a => a.ScheduleId == scheduleId)
            .ToListAsync();
        
        if (relatedAnnouncements.Any())
        {
            _context.Announcements.RemoveRange(relatedAnnouncements);
        }

        // Sau đó mới xóa schedule
        _context.AssetMaintenanceSchedules.Remove(schedule);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AssetMaintenanceSchedule>> GetOverlappingSchedulesAsync(Guid assetId, DateOnly startDate, DateOnly endDate, TimeOnly? startTime, TimeOnly? endTime, Guid? excludeScheduleId = null)
    {
        var query = _context.AssetMaintenanceSchedules
            .Include(s => s.Asset)
            .Include(s => s.CreatedByUser)
            .Include(s => s.AssetMaintenanceHistories)
            .Where(s => s.AssetId == assetId && 
                       s.Status != "DONE" && 
                       s.Status != "CANCELLED");

        if (excludeScheduleId.HasValue)
        {
            query = query.Where(s => s.ScheduleId != excludeScheduleId.Value);
        }

        var schedules = await query.ToListAsync();
        
        var overlapping = new List<AssetMaintenanceSchedule>();
        
        foreach (var schedule in schedules)
        {
            bool overlaps = false;
            
            if (startTime.HasValue && endTime.HasValue && schedule.StartTime.HasValue && schedule.EndTime.HasValue)
            {
                var newStart = startDate.ToDateTime(startTime.Value);
                var newEnd = endDate.ToDateTime(endTime.Value);
                var existingStart = schedule.StartDate.ToDateTime(schedule.StartTime.Value);
                var existingEnd = schedule.EndDate.ToDateTime(schedule.EndTime.Value);
                
                overlaps = newStart < existingEnd && newEnd > existingStart;
            }
            else if (!startTime.HasValue && !endTime.HasValue && !schedule.StartTime.HasValue && !schedule.EndTime.HasValue)
            {
                overlaps = startDate <= schedule.EndDate && endDate >= schedule.StartDate;
            }
            else
            {
                var newStart = startDate.ToDateTime(startTime ?? TimeOnly.MinValue);
                var newEnd = endDate.ToDateTime(endTime ?? new TimeOnly(23, 59, 59));
                var existingStart = schedule.StartDate.ToDateTime(schedule.StartTime ?? TimeOnly.MinValue);
                var existingEnd = schedule.EndDate.ToDateTime(schedule.EndTime ?? new TimeOnly(23, 59, 59));
                
                overlaps = newStart < existingEnd && newEnd > existingStart;
            }
            
            if (overlaps)
            {
                overlapping.Add(schedule);
            }
        }
        
        return overlapping;
    }

    public async Task<bool> IsAssetUnderMaintenanceAsync(Guid assetId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var now = DateTime.UtcNow.AddHours(7);
        
        var activeSchedules = await _context.AssetMaintenanceSchedules
            .Where(s => s.AssetId == assetId && 
                       (s.Status == "SCHEDULED" || s.Status == "IN_PROGRESS"))
            .ToListAsync();
        
        foreach (var schedule in activeSchedules)
        {
            DateTime startDateTime;
            DateTime endDateTime;
            
            if (schedule.StartTime.HasValue && schedule.EndTime.HasValue)
            {
                startDateTime = schedule.StartDate.ToDateTime(schedule.StartTime.Value);
                endDateTime = schedule.EndDate.ToDateTime(schedule.EndTime.Value);
            }
            else
            {
                startDateTime = schedule.StartDate.ToDateTime(TimeOnly.MinValue);
                endDateTime = schedule.EndDate.ToDateTime(new TimeOnly(23, 59, 59));
            }
            
            if (now >= startDateTime && now <= endDateTime)
            {
                return true;
            }
        }
        
        return false;
    }

    public async Task<Dictionary<Guid, AssetMaintenanceSchedule>> GetActiveMaintenanceSchedulesByAssetIdsAsync(IEnumerable<Guid> assetIds)
    {
        var ids = assetIds?.Distinct().ToList() ?? new List<Guid>();
        if (!ids.Any())
        {
            return new Dictionary<Guid, AssetMaintenanceSchedule>();
        }

        var now = DateTime.UtcNow.AddHours(7);

        var schedules = await _context.AssetMaintenanceSchedules
            .Where(s => ids.Contains(s.AssetId) &&
                       (s.Status == "SCHEDULED" || s.Status == "IN_PROGRESS"))
            .ToListAsync();

        var lookup = new Dictionary<Guid, AssetMaintenanceSchedule>();

        foreach (var schedule in schedules)
        {
            DateTime startDateTime;
            DateTime endDateTime;

            if (schedule.StartTime.HasValue && schedule.EndTime.HasValue)
            {
                startDateTime = schedule.StartDate.ToDateTime(schedule.StartTime.Value);
                endDateTime = schedule.EndDate.ToDateTime(schedule.EndTime.Value);
            }
            else
            {
                startDateTime = schedule.StartDate.ToDateTime(TimeOnly.MinValue);
                endDateTime = schedule.EndDate.ToDateTime(new TimeOnly(23, 59, 59));
            }

            if (now >= startDateTime && now <= endDateTime)
            {
                if (!lookup.TryGetValue(schedule.AssetId, out var existing))
                {
                    lookup[schedule.AssetId] = schedule;
                }
                else
                {
                    var existingStart = existing.StartTime.HasValue
                        ? existing.StartDate.ToDateTime(existing.StartTime.Value)
                        : existing.StartDate.ToDateTime(TimeOnly.MinValue);

                    if (startDateTime < existingStart)
                    {
                        lookup[schedule.AssetId] = schedule;
                    }
                }
            }
        }

        return lookup;
    }

    public async Task<IEnumerable<AssetMaintenanceSchedule>> SearchSchedulesAsync(string? searchTerm, Guid? assetId, string? status, DateOnly? startDateFrom, DateOnly? startDateTo)
    {
        var query = _context.AssetMaintenanceSchedules
            .Include(s => s.Asset)
                .ThenInclude(a => a.Category)
            .Include(s => s.CreatedByUser)
            .Include(s => s.AssetMaintenanceHistories)
            .AsQueryable();
        
        if (assetId.HasValue)
        {
            query = query.Where(s => s.AssetId == assetId.Value);
        }
        
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(s => s.Status == status);
        }
        else
        {
            query = query.Where(s => s.Status != "DONE" && s.Status != "CANCELLED");
        }
        
        if (startDateFrom.HasValue)
        {
            query = query.Where(s => s.StartDate >= startDateFrom.Value);
        }
        
        if (startDateTo.HasValue)
        {
            query = query.Where(s => s.StartDate <= startDateTo.Value);
        }
        
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(s => 
                s.Description != null && s.Description.ToLower().Contains(searchLower) ||
                s.Asset.Name.ToLower().Contains(searchLower) ||
                s.Asset.Code.ToLower().Contains(searchLower));
        }
        
        return await query.OrderByDescending(s => s.StartDate).ToListAsync();
    }
}

