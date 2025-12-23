using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using SAMS_BE.Mappers;
using SAMS_BE.Tenant;
using SAMS_BE.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SAMS_BE.Services;

public class AssetMaintenanceScheduleService : IAssetMaintenanceScheduleService
{
    private readonly IAssetMaintenanceScheduleRepository _scheduleRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly IAssetMaintenanceHistoryService _maintenanceHistoryService;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IBuildingRepository _buildingRepository;
    private readonly BuildingManagementContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AssetMaintenanceScheduleService> _logger;
    private readonly IAmenityRepository _amenityRepository;
    private readonly IServiceProvider _serviceProvider;
    private const string AmenityCategoryCode = "AMENITY";
    private const string AmenityMaintenanceReminderType = "AMENITY_MAINTENANCE_REMINDER";
    private const string AssetMaintenanceResidentNoticeType = "ASSET_MAINTENANCE_NOTICE";

    public AssetMaintenanceScheduleService(
        IAssetMaintenanceScheduleRepository scheduleRepository,
        IAssetRepository assetRepository,
        ITicketRepository ticketRepository,
        IAnnouncementRepository announcementRepository,
        ITenantContextAccessor tenantContextAccessor,
        IBuildingRepository buildingRepository,
        BuildingManagementContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AssetMaintenanceScheduleService> logger,
        IAmenityRepository amenityRepository,
        IAssetMaintenanceHistoryService maintenanceHistoryService,
        IServiceProvider serviceProvider)
    {
        _scheduleRepository = scheduleRepository;
        _assetRepository = assetRepository;
        _ticketRepository = ticketRepository;
        _announcementRepository = announcementRepository;
        _tenantContextAccessor = tenantContextAccessor;
        _buildingRepository = buildingRepository;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _amenityRepository = amenityRepository;
        _maintenanceHistoryService = maintenanceHistoryService;
        _serviceProvider = serviceProvider;
    }

    public async Task<IEnumerable<AssetMaintenanceScheduleDto>> GetAllSchedulesAsync()
    {
        try
        {
            var schedules = await _scheduleRepository.GetAllSchedulesAsync();
            return schedules.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all schedules");
            throw;
        }
    }

    public async Task<AssetMaintenanceScheduleDto?> GetScheduleByIdAsync(Guid scheduleId)
    {
        try
        {
            var schedule = await _scheduleRepository.GetScheduleByIdAsync(scheduleId);
            return schedule?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting schedule by ID: {ScheduleId}", scheduleId);
            throw;
        }
    }

    public async Task<IEnumerable<AssetMaintenanceScheduleDto>> GetSchedulesByAssetIdAsync(Guid assetId)
    {
        try
        {
            var schedules = await _scheduleRepository.GetSchedulesByAssetIdAsync(assetId);
            return schedules.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting schedules by asset ID: {AssetId}", assetId);
            throw;
        }
    }

    public async Task<IEnumerable<AssetMaintenanceScheduleDto>> GetSchedulesByStatusAsync(string status)
    {
        try
        {
            var schedules = await _scheduleRepository.GetSchedulesByStatusAsync(status);
            return schedules.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting schedules by status: {Status}", status);
            throw;
        }
    }


    public async Task<AssetMaintenanceScheduleDto> CreateScheduleAsync(CreateAssetMaintenanceScheduleDto createDto, Guid? createdBy)
    {
        return await CreateScheduleAsync(createDto, createdBy, skipDateValidation: false);
    }

    public async Task<AssetMaintenanceScheduleDto> CreateScheduleAsync(CreateAssetMaintenanceScheduleDto createDto, Guid? createdBy, bool skipDateValidation)
    {
        try
        {
            var asset = await _assetRepository.GetAssetByIdAsync(createDto.AssetId);
            if (asset == null)
            {
                throw new ArgumentException($"Asset với ID {createDto.AssetId} không tồn tại");
            }

            if (createDto.EndDate < createDto.StartDate)
            {
                throw new ArgumentException("EndDate phải lớn hơn hoặc bằng StartDate");
            }

            if (!skipDateValidation)
            {
                var nowVN = DateTime.UtcNow.AddHours(7);
                var today = DateOnly.FromDateTime(nowVN);
                var currentTime = TimeOnly.FromDateTime(nowVN);
                
                if (createDto.StartDate < today)
                {
                    throw new ArgumentException("Ngày bắt đầu không được trong quá khứ");
                }
                
                // Nếu ngày bắt đầu là hôm nay và có StartTime, kiểm tra giờ không được trong quá khứ
                if (createDto.StartDate == today && createDto.StartTime.HasValue)
                {
                    if (createDto.StartTime.Value < currentTime)
                    {
                        throw new ArgumentException($"Giờ bắt đầu không được trong quá khứ. Giờ hiện tại: {currentTime:HH:mm}");
                    }
                }
            }

            if (createDto.StartTime.HasValue || createDto.EndTime.HasValue)
            {
                if (!createDto.StartTime.HasValue)
                {
                    throw new ArgumentException("Nếu có EndTime thì phải có StartTime");
                }
                if (!createDto.EndTime.HasValue)
                {
                    throw new ArgumentException("Nếu có StartTime thì phải có EndTime");
                }
                if (createDto.StartDate == createDto.EndDate && createDto.EndTime.Value <= createDto.StartTime.Value)
                {
                    throw new ArgumentException("Khi cùng ngày, giờ kết thúc phải sau giờ bắt đầu");
                }
            }

            // Bỏ qua validation này cho lịch tự động
            if (!skipDateValidation)
            {
                var isUnderMaintenance = await _scheduleRepository.IsAssetUnderMaintenanceAsync(createDto.AssetId);
                if (isUnderMaintenance)
                {
                    throw new ArgumentException($"Tài sản {asset.Name} đang trong quá trình bảo trì. Vui lòng chờ hoàn thành hoặc hủy lịch bảo trì hiện tại.");
                }

                var overlappingSchedules = await _scheduleRepository.GetOverlappingSchedulesAsync(
                    createDto.AssetId, 
                    createDto.StartDate, 
                    createDto.EndDate, 
                    createDto.StartTime, 
                    createDto.EndTime);
                
                if (overlappingSchedules.Any())
                {
                    var overlappingInfo = string.Join(", ", overlappingSchedules.Select(s => 
                        $"{s.StartDate:dd/MM/yyyy} - {s.EndDate:dd/MM/yyyy}"));
                    throw new ArgumentException($"Lịch bảo trì trùng với lịch đã có: {overlappingInfo}");
                }
            }

            if (createDto.ReminderDays <= 0 && asset.Category?.DefaultReminderDays.HasValue == true)
            {
                createDto.ReminderDays = asset.Category.DefaultReminderDays.Value;
            }

            var schedule = createDto.ToEntity();
            schedule.CreatedBy = createdBy;
            
            var createdSchedule = await _scheduleRepository.CreateScheduleAsync(schedule);
            var reloadedSchedule = await _scheduleRepository.GetScheduleByIdAsync(createdSchedule.ScheduleId);
            
            if (reloadedSchedule != null)
            {
                try
                {
                    var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
                    var reminderDays = reloadedSchedule.ReminderDays;
                    
                    if (reloadedSchedule.StartDate >= today && 
                        reloadedSchedule.StartDate <= today.AddDays(reminderDays))
                    {
                        var announcement = new Announcement
                        {
                            AnnouncementId = Guid.NewGuid(),
                            Title = $"Nhắc nhở bảo trì tài sản: {reloadedSchedule.Asset.Name}",
                            Content = $"Tài sản {reloadedSchedule.Asset.Code} - {reloadedSchedule.Asset.Name} sẽ bắt đầu bảo trì vào ngày {reloadedSchedule.StartDate:dd/MM/yyyy}. " +
                                     $"Lịch bảo trì kết thúc vào ngày {reloadedSchedule.EndDate:dd/MM/yyyy}. " +
                                     (!string.IsNullOrEmpty(reloadedSchedule.Description) ? $"Mô tả: {reloadedSchedule.Description}" : ""),
                            VisibleFrom = DateTime.UtcNow.AddHours(7),
                            VisibleTo = reloadedSchedule.EndDate.ToDateTime(new TimeOnly(23, 59, 59)),
                            VisibilityScope = "STAFF",
                            Status = "ACTIVE",
                            Type = "MAINTENANCE_REMINDER",
                            IsPinned = false,
                            ScheduleId = reloadedSchedule.ScheduleId,
                            CreatedAt = DateTime.UtcNow.AddHours(7)
                        };

                        await _announcementRepository.CreateAnnouncementAsync(announcement);
                        await NotifyResidentsAboutAmenityMaintenanceAsync(reloadedSchedule);
                        await NotifyResidentsAboutAssetMaintenanceAsync(reloadedSchedule);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating immediate reminder for schedule {ScheduleId}", reloadedSchedule.ScheduleId);
                }
            }
            
            return reloadedSchedule!.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating schedule");
            throw;
        }
    }

    public async Task<AssetMaintenanceScheduleDto?> UpdateScheduleAsync(UpdateAssetMaintenanceScheduleDto updateDto, Guid scheduleId)
    {
        try
        {
            var existingSchedule = await _scheduleRepository.GetScheduleByIdAsync(scheduleId);
            if (existingSchedule == null)
            {
                return null;
            }

            var currentUser = _httpContextAccessor.HttpContext?.User;
            if (currentUser == null || currentUser.Identity == null || !currentUser.Identity.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("User không được xác thực");
            }

            var currentUserId = UserClaimsHelper.GetUserIdOrThrow(currentUser);

            if (!string.IsNullOrEmpty(updateDto.Status) && updateDto.Status != existingSchedule.Status)
            {
                var oldStatus = existingSchedule.Status.ToUpperInvariant();
                var newStatus = updateDto.Status.ToUpperInvariant();

                var validTransitions = new Dictionary<string, List<string>>
                {
                    { "SCHEDULED", new List<string> { "IN_PROGRESS", "CANCELLED", "SCHEDULED" } },
                    { "IN_PROGRESS", new List<string> { "DONE", "SCHEDULED", "CANCELLED" } },
                    { "DONE", new List<string> { } },
                    { "CANCELLED", new List<string> { "SCHEDULED" } }
                };

                if (validTransitions.ContainsKey(oldStatus))
                {
                    if (!validTransitions[oldStatus].Contains(newStatus))
                    {
                        throw new ArgumentException($"Không thể đổi status từ {oldStatus} sang {newStatus}. " +
                            $"Các status hợp lệ từ {oldStatus}: {string.Join(", ", validTransitions[oldStatus])}");
                    }
                }
            }

            if (updateDto.AssetId.HasValue)
            {
                var asset = await _assetRepository.GetAssetByIdAsync(updateDto.AssetId.Value);
                if (asset == null)
                {
                    throw new ArgumentException($"Asset với ID {updateDto.AssetId.Value} không tồn tại");
                }
            }

            var finalStartDate = updateDto.StartDate ?? existingSchedule.StartDate;
            var finalEndDate = updateDto.EndDate ?? existingSchedule.EndDate;
            var finalStartTime = updateDto.StartTime ?? existingSchedule.StartTime;
            var finalEndTime = updateDto.EndTime ?? existingSchedule.EndTime;
            
            if (updateDto.EndDate.HasValue && updateDto.StartDate.HasValue)
            {
                if (updateDto.EndDate.Value < updateDto.StartDate.Value)
                {
                    throw new ArgumentException("EndDate phải lớn hơn hoặc bằng StartDate");
                }
            }

            // Chỉ validate ngày bắt đầu khi THAY ĐỔI startDate (không phải khi chỉ update status)
            // Và không validate khi đang complete lịch (chuyển  DONE)
            var isCompletingSchedule = !string.IsNullOrEmpty(updateDto.Status) && 
                                      updateDto.Status.ToUpperInvariant() == "DONE";
            
            if (updateDto.StartDate.HasValue && 
                updateDto.StartDate.Value != existingSchedule.StartDate && 
                !isCompletingSchedule)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
                if (updateDto.StartDate.Value < today)
                {
                    throw new ArgumentException("Ngày bắt đầu không được trong quá khứ");
                }
            }

            // Kiểm tra overlap cho tài sản mới (nếu có thay đổi AssetId) hoặc tài sản hiện tại
            var assetIdToCheck = updateDto.AssetId ?? existingSchedule.AssetId;
            
            if (updateDto.AssetId.HasValue || updateDto.StartDate.HasValue || updateDto.EndDate.HasValue || updateDto.StartTime.HasValue || updateDto.EndTime.HasValue)
            {
                var overlappingSchedules = await _scheduleRepository.GetOverlappingSchedulesAsync(
                    assetIdToCheck,
                    finalStartDate,
                    finalEndDate,
                    finalStartTime,
                    finalEndTime,
                    scheduleId);
                
                if (overlappingSchedules.Any())
                {
                    var overlappingInfo = string.Join(", ", overlappingSchedules.Select(s => 
                        $"{s.StartDate:dd/MM/yyyy} - {s.EndDate:dd/MM/yyyy}"));
                    throw new ArgumentException($"Lịch bảo trì trùng với lịch đã có: {overlappingInfo}");
                }
            }

            if (finalStartTime.HasValue || finalEndTime.HasValue)
            {
                if (!finalStartTime.HasValue)
                {
                    throw new ArgumentException("Nếu có EndTime thì phải có StartTime");
                }
                if (!finalEndTime.HasValue)
                {
                    throw new ArgumentException("Nếu có StartTime thì phải có EndTime");
                }
                if (finalStartDate == finalEndDate && finalEndTime.Value <= finalStartTime.Value)
                {
                    throw new ArgumentException("Khi cùng ngày, giờ kết thúc phải sau giờ bắt đầu");
                }
            }

            // ===== QUAN TRỌNG: LƯU THÔNG TIN CŨ TRƯỚC KHI CALL ToEntity() =====
            // ToEntity() sẽ modify existingSchedule, nên phải lưu trước!
            var previousStatus = existingSchedule.Status;
            var oldAssetId = existingSchedule.AssetId; // Lưu AssetId cũ trước khi ToEntity() thay đổi
            var isUpdatingStatus = !string.IsNullOrEmpty(updateDto.Status);
            var isChangingAsset = updateDto.AssetId.HasValue && updateDto.AssetId.Value != oldAssetId;
            
            // Nếu đổi tài sản và lịch đang IN_PROGRESS, cần restore trạng thái tài sản cũ
            if (isChangingAsset && previousStatus.ToUpperInvariant() == "IN_PROGRESS")
            {
                var oldAsset = await _assetRepository.GetAssetByIdAsync(oldAssetId);
                if (oldAsset != null && oldAsset.Status == "MAINTENANCE")
                {
                    // Kiểm tra xem có lịch bảo trì nào khác đang IN_PROGRESS cho tài sản cũ không
                    var otherActiveSchedulesForOldAsset = await _scheduleRepository.GetSchedulesByStatusAsync("IN_PROGRESS");
                    var hasOtherActiveMaintenanceForOldAsset = otherActiveSchedulesForOldAsset.Any(s => 
                        s.AssetId == oldAssetId && s.ScheduleId != scheduleId);
                    
                    if (!hasOtherActiveMaintenanceForOldAsset)
                    {
                        oldAsset.Status = "ACTIVE";
                        await _assetRepository.UpdateAssetAsync(oldAsset);
                        
                        // Nếu là amenity, cũng update amenity status
                        if (oldAsset.Category?.Code != null && 
                            oldAsset.Category.Code.Equals(AmenityCategoryCode, StringComparison.OrdinalIgnoreCase))
                        {
                            var amenity = await _context.Amenities
                                .FirstOrDefaultAsync(a => a.AssetId == oldAssetId && !a.IsDelete);
                            
                            if (amenity != null && amenity.Status == "MAINTENANCE")
                            {
                                amenity.Status = "ACTIVE";
                                await _amenityRepository.UpdateAmenityAsync(amenity);
                            }
                        }
                    }
                }
                
                // Set trạng thái MAINTENANCE cho tài sản mới nếu lịch đang IN_PROGRESS
                var newAsset = await _assetRepository.GetAssetByIdAsync(updateDto.AssetId.Value);
                if (newAsset != null)
                {
                    newAsset.Status = "MAINTENANCE";
                    await _assetRepository.UpdateAssetAsync(newAsset);
                    
                    // Nếu là amenity, cũng update amenity status
                    if (newAsset.Category?.Code != null && 
                        newAsset.Category.Code.Equals(AmenityCategoryCode, StringComparison.OrdinalIgnoreCase))
                    {
                        var amenity = await _context.Amenities
                            .FirstOrDefaultAsync(a => a.AssetId == updateDto.AssetId.Value && !a.IsDelete);
                        
                        if (amenity != null)
                        {
                            amenity.Status = "MAINTENANCE";
                            await _amenityRepository.UpdateAmenityAsync(amenity);
                        }
                    }
                }
            }
            
            var updatedSchedule = updateDto.ToEntity(scheduleId, existingSchedule);
            var result = await _scheduleRepository.UpdateScheduleAsync(updatedSchedule);
            
            if (result == null)
            {
                return null;
            }

            // Tự động set ActualEndDate và CompletedBy khi chuyển sang DONE
            var statusChangedToDone = isUpdatingStatus && 
                                     (previousStatus.ToUpperInvariant() == "IN_PROGRESS" || 
                                      previousStatus.ToUpperInvariant() == "SCHEDULED") && 
                                     updateDto.Status.ToUpperInvariant() == "DONE";
            
            if (statusChangedToDone)
            {
                var now = DateTime.UtcNow.AddHours(7);
                var actualEndDate = updateDto.ActualEndDate ?? now;
                
                result.ActualEndDate = actualEndDate;
                result.CompletedBy = currentUserId;
                result.CompletedAt = now;
                if (!string.IsNullOrEmpty(updateDto.CompletionNotes))
                {
                    result.CompletionNotes = updateDto.CompletionNotes;
                }
                await _scheduleRepository.UpdateScheduleAsync(result);
                
                // Update Asset status về ACTIVE (dùng AssetId MỚI - result.AssetId)
                var asset = await _assetRepository.GetAssetByIdAsync(result.AssetId);
                
                if (asset != null && asset.Status == "MAINTENANCE")
                {
                    // Kiểm tra xem có lịch bảo trì nào khác đang IN_PROGRESS không
                    var otherActiveSchedules = await _scheduleRepository.GetSchedulesByStatusAsync("IN_PROGRESS");
                    var hasOtherActiveMaintenance = otherActiveSchedules.Any(s => 
                        s.AssetId == result.AssetId && s.ScheduleId != scheduleId);
                    
                    if (!hasOtherActiveMaintenance)
                    {
                        asset.Status = "ACTIVE";
                        await _assetRepository.UpdateAssetAsync(asset);
                        
                        // Nếu là amenity, cũng update amenity status
                        if (asset.Category?.Code != null && 
                            asset.Category.Code.Equals(AmenityCategoryCode, StringComparison.OrdinalIgnoreCase))
                        {
                            var amenity = await _context.Amenities
                                .FirstOrDefaultAsync(a => a.AssetId == result.AssetId && !a.IsDelete);
                            
                            if (amenity != null && amenity.Status == "MAINTENANCE")
                            {
                                amenity.Status = "ACTIVE";
                                await _amenityRepository.UpdateAmenityAsync(amenity);
                            }
                        }
                    }
                }
            }
            
            if (statusChangedToDone)
            {
                // Lấy actualEndDate từ result (đã được set ở trên)
                var actualEndDate = result.ActualEndDate ?? DateTime.UtcNow.AddHours(7);
                
                var existingHistories = await _context.AssetMaintenanceHistories
                    .Where(h => h.ScheduleId == scheduleId)
                    .ToListAsync();

                if (!existingHistories.Any())
                {
                    // Dùng result.AssetId (AssetId MỚI) cho history
                    var asset = await _assetRepository.GetAssetByIdAsync(result.AssetId);
                    
                    // Đảm bảo ScheduledStartDate và ScheduledEndDate có giá trị
                    if (!result.ScheduledStartDate.HasValue)
                    {
                        result.ScheduledStartDate = result.StartDate;
                    }
                    if (!result.ScheduledEndDate.HasValue)
                    {
                        result.ScheduledEndDate = result.EndDate;
                    }
                    await _scheduleRepository.UpdateScheduleAsync(result);
                    
                    // Tính scheduled end datetime (có thể có time hoặc không)
                    var scheduledEndDateTime = result.EndTime.HasValue
                        ? result.ScheduledEndDate.Value.ToDateTime(result.EndTime.Value)
                        : result.ScheduledEndDate.Value.ToDateTime(new TimeOnly(23, 59, 59));
                    
                    // Tính chênh lệch theo giờ
                    var timeDifference = actualEndDate - scheduledEndDateTime;
                    var hoursDifference = timeDifference.TotalHours;
                    var daysDifference = (int)Math.Round(timeDifference.TotalDays);
                    
                    var completionStatus = hoursDifference < 0 ? "EARLY" : 
                                         hoursDifference == 0 ? "ON_TIME" : "LATE";
                    
                    var createHistoryDto = new CreateAssetMaintenanceHistoryDto
                    {
                        AssetId = result.AssetId, // Dùng AssetId MỚI
                        ScheduleId = scheduleId,
                        ActionDate = actualEndDate,
                        Action = $"Hoàn thành bảo trì: {asset?.Name ?? "Tài sản"}",
                        Notes = !string.IsNullOrEmpty(updateDto.CompletionNotes) 
                            ? updateDto.CompletionNotes 
                            : result.Description, // Dùng result.Description thay vì existingSchedule
                        CostAmount = null,
                        NextDueDate = null
                    };

                    try
                    {
                        // FINAL CHECK: Re-check right before creating to prevent race condition
                        var finalCheck = await _context.AssetMaintenanceHistories
                            .AnyAsync(h => h.ScheduleId == scheduleId);
                        
                        if (finalCheck)
                        {
                            return result.ToDto(); // Another process already created - skip
                        }
                        
                        var history = await _maintenanceHistoryService.CreateHistoryAsync(createHistoryDto);
                        
                        // Update history với thông tin scheduled/actual dates
                        var historyEntity = await _context.AssetMaintenanceHistories
                            .FirstOrDefaultAsync(h => h.HistoryId == Guid.Parse(history.HistoryId.ToString()));
                        
                        if (historyEntity != null)
                        {
                            historyEntity.ScheduledStartDate = result.ScheduledStartDate;
                            historyEntity.ScheduledEndDate = result.ScheduledEndDate;
                            historyEntity.ActualStartDate = result.ActualStartDate;
                            historyEntity.ActualEndDate = actualEndDate;
                            historyEntity.CompletionStatus = completionStatus;
                            historyEntity.DaysDifference = daysDifference;
                            historyEntity.PerformedBy = currentUserId;
                            await _context.SaveChangesAsync();
                        }
                        
                        // Tạo thông báo hoàn thành bảo trì cho cư dân
                        await CreateMaintenanceCompletionNotificationAsync(scheduleId, asset, actualEndDate, completionStatus);
                    }
                    catch (DbUpdateException ex)
                    {
                        // Handle duplicate-related exceptions 
                        var isDuplicate = ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
                                         ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true ||
                                         ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                                         ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase);
                        
                        if (!isDuplicate)
                        {
                            throw;
                        }
                    }
                }
            }
            
            // Luôn ẩn thông báo bảo trì cũ khi status chuyển sang DONE (bất kể đã có history hay chưa)
            if (statusChangedToDone)
            {
                var actualEndDate = result.ActualEndDate ?? DateTime.UtcNow.AddHours(7);
                
                // Cập nhật VisibleTo của thông báo bảo trì để ẩn ngay khi complete sớm
                var maintenanceAnnouncements = await _context.Announcements
                    .Where(a => a.ScheduleId == scheduleId && 
                               (a.Type == AssetMaintenanceResidentNoticeType || 
                                a.Type == AmenityMaintenanceReminderType) &&
                               a.Status == "ACTIVE")
                    .ToListAsync();
                
                foreach (var announcement in maintenanceAnnouncements)
                {
                    // Set Status = "INACTIVE" để ẩn ngay lập tức
                    announcement.Status = "INACTIVE";
                    announcement.VisibleTo = actualEndDate;
                }
                
                if (maintenanceAnnouncements.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Hidden {Count} maintenance announcements for schedule {ScheduleId}", 
                        maintenanceAnnouncements.Count, scheduleId);
                }
            }

            // Cập nhật thông báo khi thay đổi thời gian lịch bảo trì
            var isUpdatingTime = updateDto.StartDate.HasValue || updateDto.EndDate.HasValue || 
                                updateDto.StartTime.HasValue || updateDto.EndTime.HasValue;
            
            if (isUpdatingTime && result != null)
            {
                var relatedAnnouncements = await _context.Announcements
                    .Where(a => a.ScheduleId == scheduleId && 
                               (a.Type == AssetMaintenanceResidentNoticeType || 
                                a.Type == AmenityMaintenanceReminderType ||
                                a.Type == "MAINTENANCE_REMINDER") &&
                               a.Status == "ACTIVE")
                    .ToListAsync();
                
                foreach (var announcement in relatedAnnouncements)
                {
                    // Cập nhật nội dung thông báo với thời gian mới
                    var asset = await _assetRepository.GetAssetByIdAsync(result.AssetId);
                    if (asset != null)
                    {
                        var startTimeStr = result.StartTime.HasValue ? $" {result.StartTime.Value:HH:mm}" : "";
                        var endTimeStr = result.EndTime.HasValue ? $" {result.EndTime.Value:HH:mm}" : "";
                        
                        announcement.Title = $"Cập nhật lịch bảo trì tài sản: {asset.Name}";
                        announcement.Content = $"Bắt đầu bảo trì từ {startTimeStr} ngày {result.StartDate:dd/MM/yyyy} " +
                                             $"đến {endTimeStr} ngày {result.EndDate:dd/MM/yyyy}.";
                        
                        // Cập nhật VisibleTo theo thời gian kết thúc mới
                        announcement.VisibleTo = result.EndTime.HasValue 
                            ? result.EndDate.ToDateTime(result.EndTime.Value)
                            : result.EndDate.ToDateTime(new TimeOnly(23, 59, 59));
                    }
                }
                
                if (relatedAnnouncements.Any())
                {
                    await _context.SaveChangesAsync();
                }
            }

            var reloadedSchedule = await _scheduleRepository.GetScheduleByIdAsync(scheduleId);
            return reloadedSchedule?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating schedule: {ScheduleId}", scheduleId);
            throw;
        }
    }

    public async Task<bool> DeleteScheduleAsync(Guid scheduleId)
    {
        try
        {
            return await _scheduleRepository.DeleteScheduleAsync(scheduleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting schedule: {ScheduleId}", scheduleId);
            throw;
        }
    }

    public async Task<IEnumerable<AssetMaintenanceScheduleDto>> GetSchedulesDueForReminderAsync()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var schedules = await _scheduleRepository.GetSchedulesDueForReminderAsync(today);
            return schedules.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting schedules due for reminder");
            throw;
        }
    }

    public async Task<IEnumerable<AssetMaintenanceScheduleDto>> GetSchedulesDueForMaintenanceAsync()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var schedules = await _scheduleRepository.GetSchedulesDueForMaintenanceAsync(today);
            return schedules.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting schedules due for maintenance");
            throw;
        }
    }

    public async Task SendMaintenanceRemindersAsync()
    {
        try
        {
            var buildings = await _buildingRepository.GetAllAsync(CancellationToken.None);
            
            foreach (var building in buildings)
            {
                try
                {
                    _tenantContextAccessor.SetSchema(building.SchemaName);
                    
                    var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
                    var schedules = await _scheduleRepository.GetSchedulesDueForReminderAsync(today);
                    
                    foreach (var schedule in schedules)
                    {
                        try
                        {
                            var announcement = new Announcement
                            {
                                AnnouncementId = Guid.NewGuid(),
                                Title = $"Cập nhật lịch bảo trì tài sản: {schedule.Asset.Name}",
                                Content = $"Bắt đầu bảo trì từ {schedule.StartDate:dd/MM/yyyy}. " +
                                         $"đến {schedule.EndDate:dd/MM/yyyy}.",
                                VisibleFrom = DateTime.UtcNow.AddHours(7),
                                VisibleTo = schedule.EndDate.ToDateTime(new TimeOnly(23, 59, 59)),
                                VisibilityScope = "STAFF",
                                Status = "ACTIVE",
                                Type = "MAINTENANCE_REMINDER",
                                IsPinned = false,
                                ScheduleId = schedule.ScheduleId,
                                CreatedAt = DateTime.UtcNow.AddHours(7)
                            };

                            await _announcementRepository.CreateAnnouncementAsync(announcement);
                            await NotifyResidentsAboutAmenityMaintenanceAsync(schedule);
                            await NotifyResidentsAboutAssetMaintenanceAsync(schedule);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error creating reminder for schedule {ScheduleId}", schedule.ScheduleId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing reminders for building {BuildingName}", building.BuildingName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendMaintenanceRemindersAsync job");
            throw;
        }
    }

    public async Task StartMaintenanceJobAsync()
    {
        try
        {
            _logger.LogInformation("[StartMaintenanceJob] Job started");
            
            var buildings = await _buildingRepository.GetAllAsync(CancellationToken.None);
            _logger.LogInformation($"[StartMaintenanceJob] Found {buildings.Count()} buildings");
            
            foreach (var building in buildings)
            {
                try
                {
                    _logger.LogInformation($"[StartMaintenanceJob] Processing building: {building.BuildingName}, Schema: {building.SchemaName}");
                    
                    // Create a new scope for each building to ensure DbContext is recreated with correct schema
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
                        var scheduleRepository = scope.ServiceProvider.GetRequiredService<IAssetMaintenanceScheduleRepository>();
                        var assetRepository = scope.ServiceProvider.GetRequiredService<IAssetRepository>();
                        var historyService = scope.ServiceProvider.GetRequiredService<IAssetMaintenanceHistoryService>();
                        var context = scope.ServiceProvider.GetRequiredService<BuildingManagementContext>();
                        var amenityRepository = scope.ServiceProvider.GetRequiredService<IAmenityRepository>();
                        
                        tenantAccessor.SetSchema(building.SchemaName);
                        
                        var now = DateTime.UtcNow.AddHours(7);
                        
                        var schedules = await scheduleRepository.GetSchedulesByStatusAsync("SCHEDULED");
                        

                        
                        foreach (var schedule in schedules)
                        {
                            try
                            {
                                // Double check status from DB to avoid race condition with User actions
                                var currentStatusResult = await context.AssetMaintenanceSchedules
                                    .Where(s => s.ScheduleId == schedule.ScheduleId)
                                    .Select(s => s.Status)
                                    .FirstOrDefaultAsync();
                                
                                if (currentStatusResult != "SCHEDULED") continue;

                                bool shouldStart = false;
                                DateTime startDateTime;
                                
                                if (schedule.StartTime.HasValue && schedule.EndTime.HasValue)
                                {
                                    // Create DateTime from DateOnly and TimeOnly
                                    var tempDateTime = schedule.StartDate.ToDateTime(schedule.StartTime.Value);
                                    // Ensure it's treated as local time (UTC+7)
                                    startDateTime = DateTime.SpecifyKind(tempDateTime, DateTimeKind.Unspecified);
                                    

                                    
                                    if (now >= startDateTime)
                                    {
                                        shouldStart = true;
                                    }
                                }
                                else
                                {
                                    var tempDateTime = schedule.StartDate.ToDateTime(TimeOnly.MinValue);
                                    startDateTime = DateTime.SpecifyKind(tempDateTime, DateTimeKind.Unspecified);
                                    

                                    
                                    if (now >= startDateTime)
                                    {
                                        shouldStart = true;
                                    }
                                }
                                
                                if (shouldStart)
                                {

                                    
                                    schedule.Status = "IN_PROGRESS";
                                    schedule.ActualStartDate = now;
                                    
                                    var asset = await assetRepository.GetAssetByIdAsync(schedule.AssetId);
                                    if (asset != null)
                                    {
                                        asset.Status = "MAINTENANCE";
                                        await assetRepository.UpdateAssetAsync(asset);
                                        
                                        // Update amenity status if this is an amenity asset
                                        if (asset.Category?.Code != null && 
                                            asset.Category.Code.Equals(AmenityCategoryCode, StringComparison.OrdinalIgnoreCase))
                                        {
                                            var amenity = await context.Amenities
                                                .FirstOrDefaultAsync(a => a.AssetId == schedule.AssetId && !a.IsDelete);
                                            
                                            if (amenity != null && amenity.Status != "MAINTENANCE")
                                            {
                                                amenity.Status = "MAINTENANCE";
                                                await amenityRepository.UpdateAmenityAsync(amenity);
                                            }
                                        }

                                    }
                                    
                                    await scheduleRepository.UpdateScheduleAsync(schedule);
                                    
                                    _logger.LogInformation($"Started maintenance for schedule {schedule.ScheduleId} - {schedule.Asset?.Name}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"[StartMaintenanceJob] Error processing schedule {schedule.ScheduleId}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[StartMaintenanceJob] Error processing building {building.BuildingName}: {ex.Message}");
                }
            }
            
            _logger.LogInformation("[StartMaintenanceJob] Job completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StartMaintenanceJobAsync job");
            throw;
        }
    }

    public async Task CompleteMaintenanceJobAsync()
    {
        try
        {
            _logger.LogInformation("[CompleteMaintenanceJob] Job started");
            
            var buildings = await _buildingRepository.GetAllAsync(CancellationToken.None);
            _logger.LogInformation($"[CompleteMaintenanceJob] Found {buildings.Count()} buildings");
            
            foreach (var building in buildings)
            {
                try
                {

                    
                    // Create a new scope for each building to ensure DbContext is recreated with correct schema
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
                        var scheduleRepository = scope.ServiceProvider.GetRequiredService<IAssetMaintenanceScheduleRepository>();
                        var assetRepository = scope.ServiceProvider.GetRequiredService<IAssetRepository>();
                        var context = scope.ServiceProvider.GetRequiredService<BuildingManagementContext>();
                        var historyService = scope.ServiceProvider.GetRequiredService<IAssetMaintenanceHistoryService>();
                        var amenityRepository = scope.ServiceProvider.GetRequiredService<IAmenityRepository>();
                        
                        tenantAccessor.SetSchema(building.SchemaName);
                        
                        var now = DateTime.UtcNow.AddHours(7);
                        
                        var schedules = await scheduleRepository.GetSchedulesByStatusAsync("IN_PROGRESS");
                        

                        
                        foreach (var schedule in schedules)
                        {
                            try
                            {
                                // Double check status from DB to avoid race condition with User actions
                                var currentStatusResult = await context.AssetMaintenanceSchedules
                                    .Where(s => s.ScheduleId == schedule.ScheduleId)
                                    .Select(s => s.Status)
                                    .FirstOrDefaultAsync();
                                
                                if (currentStatusResult != "IN_PROGRESS") continue;

                                bool shouldComplete = false;
                                DateTime endDateTime;
                                
                                if (schedule.StartTime.HasValue && schedule.EndTime.HasValue)
                                {
                                    var tempDateTime = schedule.EndDate.ToDateTime(schedule.EndTime.Value);
                                    endDateTime = DateTime.SpecifyKind(tempDateTime, DateTimeKind.Unspecified);
                                    

                                    
                                    if (now >= endDateTime)
                                    {
                                        shouldComplete = true;
                                    }
                                }
                                else
                                {
                                    var tempDateTime = schedule.EndDate.ToDateTime(new TimeOnly(23, 59, 59));
                                    endDateTime = DateTime.SpecifyKind(tempDateTime, DateTimeKind.Unspecified);
                                    

                                    
                                    if (now >= endDateTime)
                                    {
                                        shouldComplete = true;
                                    }
                                }
                                
                                if (shouldComplete)
                                {

                                    
                                    var existingHistories = await context.AssetMaintenanceHistories
                                        .Where(h => h.ScheduleId == schedule.ScheduleId)
                                        .ToListAsync();

                                    // Lấy asset để sử dụng cho cả việc tạo history và đổi trạng thái
                                    var asset = await assetRepository.GetAssetByIdAsync(schedule.AssetId);

                                    if (!existingHistories.Any() && historyService != null)
                                    {
                                        // Sử dụng thời gian thực tế (now) thay vì thời gian theo lịch đã đặt
                                        DateTime actionDate = now;
                                        
                                        var actualEndDate = now;
                                        
                                        // Đảm bảo ScheduledStartDate và ScheduledEndDate có giá trị
                                        if (!schedule.ScheduledStartDate.HasValue)
                                        {
                                            schedule.ScheduledStartDate = schedule.StartDate;
                                        }
                                        if (!schedule.ScheduledEndDate.HasValue)
                                        {
                                            schedule.ScheduledEndDate = schedule.EndDate;
                                        }
                                        
                                        // Tính scheduled end datetime
                                        var scheduledEndDateTime = schedule.EndTime.HasValue
                                            ? schedule.ScheduledEndDate.Value.ToDateTime(schedule.EndTime.Value)
                                            : schedule.ScheduledEndDate.Value.ToDateTime(new TimeOnly(23, 59, 59));
                                        
                                        // Tính chênh lệch theo giờ
                                        var timeDifference = actualEndDate - scheduledEndDateTime;
                                        var hoursDifference = timeDifference.TotalHours;
                                        var daysDifference = (int)Math.Round(timeDifference.TotalDays);
                                        
                                        var completionStatus = hoursDifference < 0 ? "EARLY" : 
                                                             hoursDifference == 0 ? "ON_TIME" : "LATE";
                                        
                                        var createHistoryDto = new CreateAssetMaintenanceHistoryDto
                                        {
                                            AssetId = schedule.AssetId,
                                            ScheduleId = schedule.ScheduleId,
                                            ActionDate = actionDate,
                                            Action = $"Hoàn thành bảo trì: {asset?.Name ?? "Tài sản"}",
                                            Notes = schedule.Description ?? "", // Chỉ lấy mô tả từ lịch, không tự động sinh
                                            CostAmount = null,
                                            NextDueDate = null
                                        };

                                        try
                                        {
                                            // FINAL CHECK: Re-check right before creating to prevent race condition
                                            var finalCheck = await context.AssetMaintenanceHistories
                                                .AnyAsync(h => h.ScheduleId == schedule.ScheduleId);
                                            
                                            if (finalCheck)
                                            {
                                                continue; // Another process already created - skip this schedule
                                            }
                                            
                                            var history = await historyService.CreateHistoryAsync(createHistoryDto);
                                            
                                            // Update history với thông tin scheduled/actual dates
                                            var historyEntity = await context.AssetMaintenanceHistories
                                                .FirstOrDefaultAsync(h => h.HistoryId == Guid.Parse(history.HistoryId.ToString()));
                                            
                                            if (historyEntity != null)
                                            {
                                                historyEntity.ScheduledStartDate = schedule.ScheduledStartDate;
                                                historyEntity.ScheduledEndDate = schedule.ScheduledEndDate;
                                                historyEntity.ActualStartDate = schedule.ActualStartDate;
                                                historyEntity.ActualEndDate = actualEndDate;
                                                historyEntity.CompletionStatus = completionStatus;
                                                historyEntity.DaysDifference = daysDifference;
                                                await context.SaveChangesAsync();
                                            }
                                        }
                                        catch (DbUpdateException ex)
                                        {
                                            // Handle duplicate-related exceptions gracefully
                                            var isDuplicate = ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
                                                             ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true ||
                                                             ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                                                             ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase);
                                            
                                            if (!isDuplicate)
                                            {
                                                throw;
                                            }
                                        }
                                    }

                                    schedule.Status = "DONE";
                                    schedule.ActualEndDate = now;
                                    schedule.CompletedAt = now;
                                    await scheduleRepository.UpdateScheduleAsync(schedule);
                                    
                                    if (asset != null)
                                    {
                                        var otherActiveSchedules = await scheduleRepository.GetSchedulesByStatusAsync("IN_PROGRESS");
                                        var hasOtherActiveMaintenance = otherActiveSchedules.Any(s => s.AssetId == schedule.AssetId && s.ScheduleId != schedule.ScheduleId);
                                        
                                        if (!hasOtherActiveMaintenance && asset.Status == "MAINTENANCE")
                                        {
                                            asset.Status = "ACTIVE";
                                            await assetRepository.UpdateAssetAsync(asset);
                                            
                                            if (asset.Category?.Code != null && 
                                                asset.Category.Code.Equals(AmenityCategoryCode, StringComparison.OrdinalIgnoreCase))
                                            {
                                                var amenity = await context.Amenities
                                                    .FirstOrDefaultAsync(a => a.AssetId == schedule.AssetId && !a.IsDelete);
                                                
                                                if (amenity != null && amenity.Status == "MAINTENANCE")
                                                {
                                                    amenity.Status = "ACTIVE";
                                                    await amenityRepository.UpdateAmenityAsync(amenity);
                                                }
                                            }
                                        }
                                    }
                                    
                                    _logger.LogInformation($"[CompleteMaintenanceJob] Successfully completed maintenance for schedule {schedule.ScheduleId}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"[CompleteMaintenanceJob] Error processing schedule {schedule.ScheduleId}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[CompleteMaintenanceJob] Error processing building {building.BuildingName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CompleteMaintenanceJobAsync job");
            throw;
        }
    }

    public async Task<IEnumerable<AssetMaintenanceScheduleDto>> SearchSchedulesAsync(string? searchTerm, Guid? assetId, string? status, DateOnly? startDateFrom, DateOnly? startDateTo)
    {
        try
        {
            var schedules = await _scheduleRepository.SearchSchedulesAsync(searchTerm, assetId, status, startDateFrom, startDateTo);
            return schedules.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching schedules");
            throw;
        }
    }

    public static DateOnly CalculateNextDueDate(DateOnly actionDate, string? recurrenceType, int? recurrenceInterval)
    {
        if (string.IsNullOrEmpty(recurrenceType) || !recurrenceInterval.HasValue || recurrenceInterval.Value <= 0)
        {
            return actionDate;
        }

        return recurrenceType.ToUpperInvariant() switch
        {
            "DAILY" => actionDate.AddDays(recurrenceInterval.Value),
            "WEEKLY" => actionDate.AddDays(recurrenceInterval.Value * 7),
            "MONTHLY" => actionDate.AddMonths(recurrenceInterval.Value),
            "YEARLY" => actionDate.AddYears(recurrenceInterval.Value),
            _ => actionDate
        };
    }

    private async Task NotifyResidentsAboutAmenityMaintenanceAsync(AssetMaintenanceSchedule schedule)
    {
        if (schedule.Asset?.Category?.Code == null ||
            !schedule.Asset.Category.Code.Equals(AmenityCategoryCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var amenity = await _context.Amenities
            .FirstOrDefaultAsync(a => a.AssetId == schedule.AssetId && !a.IsDelete);

        if (amenity == null)
        {
            return;
        }

        DateTime maintenanceStart = schedule.StartTime.HasValue
            ? schedule.StartDate.ToDateTime(schedule.StartTime.Value)
            : schedule.StartDate.ToDateTime(TimeOnly.MinValue);

        DateTime maintenanceEnd = schedule.EndTime.HasValue
            ? schedule.EndDate.ToDateTime(schedule.EndTime.Value)
            : schedule.EndDate.ToDateTime(new TimeOnly(23, 59, 59));

        var affectedBookings = await _context.AmenityBookings
            .Where(b => b.AmenityId == amenity.AmenityId &&
                        !b.IsDelete &&
                        b.Status == "Confirmed" &&
                        b.PaymentStatus == "Paid" &&
                        b.StartDate <= schedule.EndDate &&
                        b.EndDate >= schedule.StartDate &&
                        b.UserId.HasValue)
            .ToListAsync();

        foreach (var booking in affectedBookings)
        {
            if (booking.UserId == null)
            {
                continue;
            }

            var exists = await _context.Announcements
                .AnyAsync(a => a.BookingId == booking.BookingId &&
                               a.Type == AmenityMaintenanceReminderType &&
                               a.ScheduleId == schedule.ScheduleId);

            if (exists)
            {
                continue;
            }

            var announcement = new Announcement
            {
                AnnouncementId = Guid.NewGuid(),
                Title = $"Thông báo bảo trì tiện ích: {amenity.Name}",
                Content = $"Tiện ích {amenity.Name} sẽ bảo trì từ {maintenanceStart:dd/MM/yyyy HH:mm} " +
                          $"đến {maintenanceEnd:dd/MM/yyyy HH:mm}. Vui lòng sắp xếp thời gian sử dụng phù hợp.",
                VisibleFrom = DateTime.UtcNow.AddHours(7),
                VisibleTo = maintenanceEnd,
                VisibilityScope = "RESIDENT",
                Status = "ACTIVE",
                Type = AmenityMaintenanceReminderType,
                IsPinned = false,
                ScheduleId = schedule.ScheduleId,
                BookingId = booking.BookingId,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                CreatedBy = booking.UserId?.ToString()
            };

            await _announcementRepository.CreateAnnouncementAsync(announcement);
        }
    }

    private async Task NotifyResidentsAboutAssetMaintenanceAsync(AssetMaintenanceSchedule schedule)
    {
        if (schedule.Asset?.Category?.Code != null &&
            schedule.Asset.Category.Code.Equals(AmenityCategoryCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var exists = await _context.Announcements
            .AnyAsync(a => a.ScheduleId == schedule.ScheduleId && a.Type == AssetMaintenanceResidentNoticeType);

        if (exists)
        {
            return;
        }

        var assetName = schedule.Asset?.Name ?? schedule.Asset?.Code ?? "Tài sản";

        DateTime maintenanceStart = schedule.StartTime.HasValue
            ? schedule.StartDate.ToDateTime(schedule.StartTime.Value)
            : schedule.StartDate.ToDateTime(TimeOnly.MinValue);

        DateTime maintenanceEnd = schedule.EndTime.HasValue
            ? schedule.EndDate.ToDateTime(schedule.EndTime.Value)
            : schedule.EndDate.ToDateTime(new TimeOnly(23, 59, 59));

        var announcement = new Announcement
        {
            AnnouncementId = Guid.NewGuid(),
            Title = $"Thông báo bảo trì tài sản: {assetName}",
            Content = $"Tài sản {assetName} sẽ được bảo trì từ lúc {maintenanceStart:HH:mm} ngày {maintenanceStart:dd/MM/yyyy} " +
                      $"đến {maintenanceEnd:HH:mm} ngày {maintenanceEnd:dd/MM/yyyy}. Chúng tôi xin cảm ơn!",
            VisibleFrom = DateTime.UtcNow.AddHours(7),
            VisibleTo = maintenanceEnd,
            VisibilityScope = "RESIDENT",
            Status = "ACTIVE",
            Type = AssetMaintenanceResidentNoticeType,
            IsPinned = false,
            ScheduleId = schedule.ScheduleId,
            CreatedAt = DateTime.UtcNow.AddHours(7),
            CreatedBy = schedule.CreatedBy?.ToString()
        };

        await _announcementRepository.CreateAnnouncementAsync(announcement);
    }

    /// <summary>
    /// Tạo thông báo hoàn thành bảo trì cho cư dân
    /// </summary>
    private async Task CreateMaintenanceCompletionNotificationAsync(
        Guid scheduleId, 
        Asset? asset, 
        DateTime actualEndDate,
        string completionStatus)
    {
        try
        {
            // Kiểm tra xem đã có thông báo hoàn thành cho schedule này chưa
            var existingNotification = await _context.Announcements
                .FirstOrDefaultAsync(a => a.ScheduleId == scheduleId && 
                                         a.Type == "MAINTENANCE_COMPLETED" &&
                                         a.Status == "ACTIVE");
            
            if (existingNotification != null)
            {
                _logger.LogInformation("Completion notification already exists for schedule {ScheduleId}", scheduleId);
                return;
            }

            var assetName = asset?.Name ?? asset?.Code ?? "Tài sản";
            var visibleFrom = DateTime.UtcNow.AddHours(7);
            var visibleTo = visibleFrom.AddDays(1); // Hiển thị đúng 24 giờ từ lúc tạo thông báo

            var completionMessage = completionStatus == "EARLY" 
                ? $"Tài sản {assetName} đã hoàn thành bảo trì sớm hơn dự kiến. Cảm ơn quý cư dân đã thông cảm!"
                : $"Tài sản {assetName} đã hoàn thành bảo trì. Cảm ơn quý cư dân đã thông cảm!";

            var announcement = new Announcement
            {
                AnnouncementId = Guid.NewGuid(),
                Title = "Đã hoàn thành bảo trì tài sản",
                Content = completionMessage,
                VisibleFrom = visibleFrom,
                VisibleTo = visibleTo,
                VisibilityScope = "RESIDENT",
                Status = "ACTIVE",
                Type = "MAINTENANCE_COMPLETED",
                IsPinned = false,
                ScheduleId = scheduleId,
                CreatedAt = visibleFrom
            };

            await _announcementRepository.CreateAnnouncementAsync(announcement);
            _logger.LogInformation("Created maintenance completion notification for schedule {ScheduleId}, asset {AssetName}", 
                scheduleId, assetName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance completion notification for schedule {ScheduleId}", scheduleId);
            // Không throw exception để không ảnh hưởng đến quá trình update schedule
        }
    }
}

