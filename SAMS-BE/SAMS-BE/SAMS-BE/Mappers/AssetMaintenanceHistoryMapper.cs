using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class AssetMaintenanceHistoryMapper
{
    /// <summary>
    /// Map từ AssetMaintenanceHistory entity sang AssetMaintenanceHistoryDto
    /// </summary>
    public static AssetMaintenanceHistoryDto ToDto(this AssetMaintenanceHistory entity)
    {
        AssetMaintenanceScheduleDto? scheduleDto = null;
        if (entity.Schedule != null)
        {
            scheduleDto = new AssetMaintenanceScheduleDto
            {
                ScheduleId = entity.Schedule.ScheduleId,
                AssetId = entity.Schedule.AssetId,
                StartDate = entity.Schedule.StartDate,
                EndDate = entity.Schedule.EndDate,
                StartTime = entity.Schedule.StartTime,
                EndTime = entity.Schedule.EndTime,
                ReminderDays = entity.Schedule.ReminderDays,
                Description = entity.Schedule.Description,
                CreatedBy = entity.Schedule.CreatedBy,
                CreatedAt = entity.Schedule.CreatedAt,
                Status = entity.Schedule.Status,
                RecurrenceType = entity.Schedule.RecurrenceType,
                RecurrenceInterval = entity.Schedule.RecurrenceInterval,
                Asset = entity.Schedule.Asset?.ToDto(),
                CreatedByUserName = entity.Schedule.CreatedByUser != null
                    ? $"{entity.Schedule.CreatedByUser.FirstName} {entity.Schedule.CreatedByUser.LastName}"
                    : null,
                Tickets = null,
                MaintenanceHistories = null
            };
        }

        // Lấy tên người thực hiện từ Schedule nếu có
        string? createdByUserName = null;
        if (entity.Schedule?.CreatedByUser != null)
        {
            createdByUserName = $"{entity.Schedule.CreatedByUser.FirstName} {entity.Schedule.CreatedByUser.LastName}";
        }

        // Lấy giá từ Voucher nếu đã tạo voucher, nếu không thì lấy từ CostAmount
        decimal? costAmount = entity.CostAmount;
        if (entity.Vouchers != null && entity.Vouchers.Any())
        {
            // Nếu đã có voucher, lấy TotalAmount từ voucher đầu tiên
            var voucher = entity.Vouchers.FirstOrDefault();
            if (voucher != null && voucher.TotalAmount > 0)
            {
                costAmount = voucher.TotalAmount;
            }
        }

        return new AssetMaintenanceHistoryDto
        {
            HistoryId = entity.HistoryId,
            AssetId = entity.AssetId,
            ScheduleId = entity.ScheduleId,
            ActionDate = entity.ActionDate,
            Action = entity.Action,
            CostAmount = costAmount,
            Notes = entity.Notes,
            NextDueDate = entity.NextDueDate,
            ActualStartDate = entity.ActualStartDate,
            ActualEndDate = entity.ActualEndDate,
            ScheduledStartDate = entity.ScheduledStartDate,
            ScheduledEndDate = entity.ScheduledEndDate,
            CompletionStatus = entity.CompletionStatus,
            DaysDifference = entity.DaysDifference,
            PerformedBy = entity.PerformedBy,
            CreatedByUserName = createdByUserName,
            PerformedByUserName = entity.PerformedByUser != null
                ? $"{entity.PerformedByUser.FirstName} {entity.PerformedByUser.LastName}"
                : null,
            Asset = entity.Asset?.ToDto(),
            Schedule = scheduleDto
        };
    }

    /// <summary>
    /// Map từ CreateAssetMaintenanceHistoryDto sang AssetMaintenanceHistory entity
    /// </summary>
    public static AssetMaintenanceHistory ToEntity(this CreateAssetMaintenanceHistoryDto dto)
    {
        return new AssetMaintenanceHistory
        {
            HistoryId = Guid.NewGuid(),
            AssetId = dto.AssetId,
            ScheduleId = dto.ScheduleId,
            ActionDate = dto.ActionDate ?? DateTime.UtcNow,
            Action = dto.Action,
            CostAmount = dto.CostAmount,
            Notes = dto.Notes,
            NextDueDate = dto.NextDueDate
        };
    }

    /// <summary>
    /// Map collection từ AssetMaintenanceHistory entities sang AssetMaintenanceHistoryDto list
    /// </summary>
    public static IEnumerable<AssetMaintenanceHistoryDto> ToDto(this IEnumerable<AssetMaintenanceHistory> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}

