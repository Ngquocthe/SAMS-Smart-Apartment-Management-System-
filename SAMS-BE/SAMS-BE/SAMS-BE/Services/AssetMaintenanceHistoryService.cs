using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Mappers;
using SAMS_BE.Models;

namespace SAMS_BE.Services;

public class AssetMaintenanceHistoryService : IAssetMaintenanceHistoryService
{
    private readonly IAssetMaintenanceHistoryRepository _historyRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IAssetMaintenanceScheduleRepository _scheduleRepository;
    private readonly ILogger<AssetMaintenanceHistoryService> _logger;
    private readonly IUserRepository _userRepository;

    public AssetMaintenanceHistoryService(
        IAssetMaintenanceHistoryRepository historyRepository,
        IAssetRepository assetRepository,
        IAssetMaintenanceScheduleRepository scheduleRepository,
        IUserRepository userRepository,
        ILogger<AssetMaintenanceHistoryService> logger)
    {
        _historyRepository = historyRepository;
        _assetRepository = assetRepository;
        _scheduleRepository = scheduleRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<AssetMaintenanceHistoryDto>> GetAllHistoriesAsync()
    {
        try
        {
            var histories = await _historyRepository.GetAllHistoriesAsync();
            return await MapHistoriesToDtoAsync(histories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all histories");
            throw;
        }
    }

    public async Task<AssetMaintenanceHistoryDto?> GetHistoryByIdAsync(Guid historyId)
    {
        try
        {
            var history = await _historyRepository.GetHistoryByIdAsync(historyId);
            return await MapHistoryToDtoAsync(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting history by ID: {HistoryId}", historyId);
            throw;
        }
    }

    public async Task<IEnumerable<AssetMaintenanceHistoryDto>> GetHistoriesByAssetIdAsync(Guid assetId)
    {
        try
        {
            var histories = await _historyRepository.GetHistoriesByAssetIdAsync(assetId);
            return await MapHistoriesToDtoAsync(histories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting histories by asset ID: {AssetId}", assetId);
            throw;
        }
    }

    public async Task<IEnumerable<AssetMaintenanceHistoryDto>> GetHistoriesByScheduleIdAsync(Guid scheduleId)
    {
        try
        {
            var histories = await _historyRepository.GetHistoriesByScheduleIdAsync(scheduleId);
            return await MapHistoriesToDtoAsync(histories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting histories by schedule ID: {ScheduleId}", scheduleId);
            throw;
        }
    }

    public async Task<AssetMaintenanceHistoryDto> CreateHistoryAsync(CreateAssetMaintenanceHistoryDto createDto)
    {
        try
        {
            // Validate asset exists
            var asset = await _assetRepository.GetAssetByIdAsync(createDto.AssetId);
            if (asset == null)
            {
                throw new ArgumentException($"Asset với ID {createDto.AssetId} không tồn tại");
            }

            // Validate schedule exists if provided
            if (createDto.ScheduleId.HasValue)
            {
                var schedule = await _scheduleRepository.GetScheduleByIdAsync(createDto.ScheduleId.Value);
                if (schedule == null)
                {
                    throw new ArgumentException($"Schedule với ID {createDto.ScheduleId} không tồn tại");
                }
            }

            // Tính toán next_due_date nếu có schedule và recurrence
            DateOnly? nextDueDate = createDto.NextDueDate;
            if (!nextDueDate.HasValue && createDto.ScheduleId.HasValue)
            {
                var schedule = await _scheduleRepository.GetScheduleByIdAsync(createDto.ScheduleId.Value);
                if (schedule != null && !string.IsNullOrEmpty(schedule.RecurrenceType) && schedule.RecurrenceInterval.HasValue)
                {
                    var actionDate = createDto.ActionDate?.Date ?? DateTime.UtcNow.Date;
                    nextDueDate = AssetMaintenanceScheduleService.CalculateNextDueDate(
                        DateOnly.FromDateTime(actionDate),
                        schedule.RecurrenceType,
                        schedule.RecurrenceInterval
                    );
                }
            }

            var history = createDto.ToEntity();
            history.NextDueDate = nextDueDate;

            var createdHistory = await _historyRepository.CreateHistoryAsync(history);

            // Nếu có schedule, cập nhật status của schedule thành DONE
            if (createDto.ScheduleId.HasValue)
            {
                var schedule = await _scheduleRepository.GetScheduleByIdAsync(createDto.ScheduleId.Value);
                if (schedule != null)
                {
                    schedule.Status = "DONE";
                    await _scheduleRepository.UpdateScheduleAsync(schedule);

                    // Nếu có next_due_date, tạo schedule mới cho lần bảo trì tiếp theo
                    if (nextDueDate.HasValue)
                    {
                        var originalDuration = (schedule.EndDate.DayNumber - schedule.StartDate.DayNumber) + 1;

                        DateOnly? recurrenceStartDate = null;
                        if (!string.IsNullOrEmpty(schedule.RecurrenceType) && schedule.RecurrenceInterval.HasValue)
                        {
                            var interval = schedule.RecurrenceInterval.Value;
                            // DAILY & WEEKLY: Tính từ EndDate (tránh chồng lấn)
                            // MONTHLY & YEARLY: Tính từ StartDate (giữ pattern thời gian)
                            recurrenceStartDate = schedule.RecurrenceType.ToUpperInvariant() switch
                            {
                                "DAILY" => schedule.EndDate.AddDays(interval),
                                "WEEKLY" => schedule.EndDate.AddDays(interval * 7),
                                "MONTHLY" => schedule.StartDate.AddMonths(interval),
                                "YEARLY" => schedule.StartDate.AddYears(interval),
                                _ => null
                            };
                        }

                        var newStartDate = recurrenceStartDate ?? nextDueDate.Value;
                        var newEndDate = newStartDate.AddDays(originalDuration - 1);
                        
                        var newSchedule = new AssetMaintenanceSchedule
                        {
                            ScheduleId = Guid.NewGuid(),
                            AssetId = schedule.AssetId,
                            StartDate = newStartDate,
                            EndDate = newEndDate,
                            StartTime = schedule.StartTime,
                            EndTime = schedule.EndTime,
                            ReminderDays = schedule.ReminderDays,
                            Description = schedule.Description,
                            Status = "SCHEDULED",
                            RecurrenceType = schedule.RecurrenceType,
                            RecurrenceInterval = schedule.RecurrenceInterval,
                            CreatedBy = schedule.CreatedBy,
                            CreatedAt = DateTime.UtcNow.AddHours(7)
                        };
                        await _scheduleRepository.CreateScheduleAsync(newSchedule);
                    }
                }
            }

            // Reload để lấy navigation properties
            var reloadedHistory = await _historyRepository.GetHistoryByIdAsync(createdHistory.HistoryId);
            return await MapHistoryToDtoAsync(reloadedHistory)
                   ?? throw new InvalidOperationException("Không thể lấy lịch sử bảo trì vừa tạo.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating history");
            throw;
        }
    }

    public async Task<AssetMaintenanceHistoryDto?> UpdateHistoryAsync(UpdateAssetMaintenanceHistoryDto updateDto, Guid historyId)
    {
        try
        {
            var existingHistory = await _historyRepository.GetHistoryByIdAsync(historyId);
            if (existingHistory == null)
            {
                return null;
            }

            if (updateDto.ActionDate.HasValue)
            {
                existingHistory.ActionDate = updateDto.ActionDate.Value;
            }
            if (!string.IsNullOrEmpty(updateDto.Action))
            {
                existingHistory.Action = updateDto.Action;
            }
            if (updateDto.CostAmount.HasValue)
            {
                existingHistory.CostAmount = updateDto.CostAmount;
            }
            if (updateDto.Notes != null)
            {
                existingHistory.Notes = updateDto.Notes;
            }
            if (updateDto.NextDueDate.HasValue)
            {
                existingHistory.NextDueDate = updateDto.NextDueDate;
            }

            var result = await _historyRepository.UpdateHistoryAsync(existingHistory);
            if (result == null)
            {
                return null;
            }

            // Reload để lấy navigation properties
            var reloadedHistory = await _historyRepository.GetHistoryByIdAsync(historyId);
            return await MapHistoryToDtoAsync(reloadedHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating history: {HistoryId}", historyId);
            throw;
        }
    }

    private async Task<List<AssetMaintenanceHistoryDto>> MapHistoriesToDtoAsync(IEnumerable<AssetMaintenanceHistory> histories)
    {
        var historyList = histories?.ToList() ?? new List<AssetMaintenanceHistory>();
        if (!historyList.Any())
        {
            return new List<AssetMaintenanceHistoryDto>();
        }

        var missingUserIds = historyList
            .Where(h => h.Schedule?.CreatedBy.HasValue == true && h.Schedule.CreatedByUser == null)
            .Select(h => h.Schedule!.CreatedBy!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, string>? userNameMap = null;
        if (missingUserIds.Any())
        {
            var users = await _userRepository.GetByIdsAsync(missingUserIds);
            userNameMap = users
                .Where(u => u != null)
                .ToDictionary(u => u.UserId, u => FormatUserName(u));
        }

        var dtoList = new List<AssetMaintenanceHistoryDto>();
        foreach (var history in historyList)
        {
            var dto = history.ToDto();
            if (string.IsNullOrWhiteSpace(dto.CreatedByUserName) &&
                history.Schedule?.CreatedBy.HasValue == true &&
                history.Schedule.CreatedByUser == null &&
                userNameMap != null &&
                userNameMap.TryGetValue(history.Schedule.CreatedBy.Value, out var userName))
            {
                dto.CreatedByUserName = userName;
            }

            dtoList.Add(dto);
        }

        return dtoList;
    }

    private async Task<AssetMaintenanceHistoryDto?> MapHistoryToDtoAsync(AssetMaintenanceHistory? history)
    {
        if (history == null)
        {
            return null;
        }

        var dtos = await MapHistoriesToDtoAsync(new[] { history });
        return dtos.FirstOrDefault();
    }

    private static string FormatUserName(User user)
    {
        var nameParts = new[] { user.FirstName, user.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        var fullName = string.Join(" ", nameParts);
        return string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName.Trim();
    }
}

