using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface IAssetMaintenanceScheduleRepository
{
    // View operations
    Task<IEnumerable<AssetMaintenanceSchedule>> GetAllSchedulesAsync();
    Task<AssetMaintenanceSchedule?> GetScheduleByIdAsync(Guid scheduleId);
    Task<IEnumerable<AssetMaintenanceSchedule>> GetSchedulesByAssetIdAsync(Guid assetId);
    Task<IEnumerable<AssetMaintenanceSchedule>> GetSchedulesByStatusAsync(string status);
    
    // Query operations for reminders
    Task<IEnumerable<AssetMaintenanceSchedule>> GetSchedulesDueForReminderAsync(DateOnly today);
    Task<IEnumerable<AssetMaintenanceSchedule>> GetSchedulesDueForMaintenanceAsync(DateOnly today);
    
    // Check overlapping schedules
    Task<IEnumerable<AssetMaintenanceSchedule>> GetOverlappingSchedulesAsync(Guid assetId, DateOnly startDate, DateOnly endDate, TimeOnly? startTime, TimeOnly? endTime, Guid? excludeScheduleId = null);
    
    // Check if asset is currently under maintenance
    Task<bool> IsAssetUnderMaintenanceAsync(Guid assetId);
    Task<Dictionary<Guid, AssetMaintenanceSchedule>> GetActiveMaintenanceSchedulesByAssetIdsAsync(IEnumerable<Guid> assetIds);
    
    // Filter/search operations
    Task<IEnumerable<AssetMaintenanceSchedule>> SearchSchedulesAsync(string? searchTerm, Guid? assetId, string? status, DateOnly? startDateFrom, DateOnly? startDateTo);
    
    // Create operations
    Task<AssetMaintenanceSchedule> CreateScheduleAsync(AssetMaintenanceSchedule schedule);
    
    // Update operations
    Task<AssetMaintenanceSchedule?> UpdateScheduleAsync(AssetMaintenanceSchedule schedule);
    
    // Delete operations
    Task<bool> DeleteScheduleAsync(Guid scheduleId);
}

