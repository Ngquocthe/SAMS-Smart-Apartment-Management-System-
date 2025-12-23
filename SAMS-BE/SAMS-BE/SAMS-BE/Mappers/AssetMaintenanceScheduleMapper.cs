using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class AssetMaintenanceScheduleMapper
{
    /// <summary>
    /// Map từ AssetMaintenanceSchedule entity sang AssetMaintenanceScheduleDto
    /// </summary>
    public static AssetMaintenanceScheduleDto ToDto(this AssetMaintenanceSchedule entity)
    {
        return new AssetMaintenanceScheduleDto
        {
            ScheduleId = entity.ScheduleId,
            AssetId = entity.AssetId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            ReminderDays = entity.ReminderDays,
            Description = entity.Description,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            Status = entity.Status,
            RecurrenceType = entity.RecurrenceType,
            RecurrenceInterval = entity.RecurrenceInterval,
            ScheduledStartDate = entity.ScheduledStartDate,
            ScheduledEndDate = entity.ScheduledEndDate,
            ActualStartDate = entity.ActualStartDate,
            ActualEndDate = entity.ActualEndDate,
            CompletionNotes = entity.CompletionNotes,
            CompletedBy = entity.CompletedBy,
            CompletedAt = entity.CompletedAt,
            Asset = entity.Asset?.ToDto(),
            CreatedByUserName = entity.CreatedByUser != null 
                ? $"{entity.CreatedByUser.FirstName} {entity.CreatedByUser.LastName}" 
                : null,
            CompletedByUserName = entity.CompletedByUser != null 
                ? $"{entity.CompletedByUser.FirstName} {entity.CompletedByUser.LastName}" 
                : null,
            Tickets = null,
            MaintenanceHistories = entity.AssetMaintenanceHistories?.Any() == true
                ? entity.AssetMaintenanceHistories.Select(h => h.ToDto()).ToList()
                : null
        };
    }

    /// <summary>
    /// Map từ CreateAssetMaintenanceScheduleDto sang AssetMaintenanceSchedule entity
    /// </summary>
    public static AssetMaintenanceSchedule ToEntity(this CreateAssetMaintenanceScheduleDto dto)
    {
        return new AssetMaintenanceSchedule
        {
            ScheduleId = Guid.NewGuid(),
            AssetId = dto.AssetId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            ReminderDays = dto.ReminderDays,
            Description = dto.Description,
            Status = dto.Status ?? "SCHEDULED",
            RecurrenceType = dto.RecurrenceType,
            RecurrenceInterval = dto.RecurrenceInterval,
            ScheduledStartDate = dto.ScheduledStartDate ?? dto.StartDate,
            ScheduledEndDate = dto.ScheduledEndDate ?? dto.EndDate
        };
    }

    /// <summary>
    /// Map từ UpdateAssetMaintenanceScheduleDto sang AssetMaintenanceSchedule entity
    /// </summary>
    public static AssetMaintenanceSchedule ToEntity(this UpdateAssetMaintenanceScheduleDto dto, Guid scheduleId, AssetMaintenanceSchedule existing)
    {
        if (dto.AssetId.HasValue)
        {
            existing.AssetId = dto.AssetId.Value;
        }
        if (dto.StartDate.HasValue)
        {
            existing.StartDate = dto.StartDate.Value;
        }
        if (dto.EndDate.HasValue)
        {
            existing.EndDate = dto.EndDate.Value;
        }
        // Xử lý StartTime và EndTime: nếu có giá trị trong DTO thì cập nhật
        if (dto.StartTime.HasValue)
        {
            existing.StartTime = dto.StartTime.Value;
        }
        if (dto.EndTime.HasValue)
        {
            existing.EndTime = dto.EndTime.Value;
        }
        if (dto.ReminderDays.HasValue)
        {
            existing.ReminderDays = dto.ReminderDays.Value;
        }
        if (dto.Description != null)
        {
            existing.Description = dto.Description;
        }
        if (dto.Status != null)
        {
            existing.Status = dto.Status;
        }
        if (dto.RecurrenceType != null)
        {
            existing.RecurrenceType = dto.RecurrenceType;
        }
        if (dto.RecurrenceInterval.HasValue)
        {
            existing.RecurrenceInterval = dto.RecurrenceInterval.Value;
        }
        if (dto.CompletionNotes != null)
        {
            existing.CompletionNotes = dto.CompletionNotes;
        }
        if (dto.ActualEndDate.HasValue)
        {
            existing.ActualEndDate = dto.ActualEndDate.Value;
        }
        
        return existing;
    }

    /// <summary>
    /// Map collection từ AssetMaintenanceSchedule entities sang AssetMaintenanceScheduleDto list
    /// </summary>
    public static IEnumerable<AssetMaintenanceScheduleDto> ToDto(this IEnumerable<AssetMaintenanceSchedule> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}

