using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService;

public interface IAssetMaintenanceScheduleService
{
    // View operations
    Task<IEnumerable<AssetMaintenanceScheduleDto>> GetAllSchedulesAsync();
    Task<AssetMaintenanceScheduleDto?> GetScheduleByIdAsync(Guid scheduleId);
    Task<IEnumerable<AssetMaintenanceScheduleDto>> GetSchedulesByAssetIdAsync(Guid assetId);
    Task<IEnumerable<AssetMaintenanceScheduleDto>> GetSchedulesByStatusAsync(string status);
    
    // Create operations
    Task<AssetMaintenanceScheduleDto> CreateScheduleAsync(CreateAssetMaintenanceScheduleDto createDto, Guid? createdBy);
    Task<AssetMaintenanceScheduleDto> CreateScheduleAsync(CreateAssetMaintenanceScheduleDto createDto, Guid? createdBy, bool skipDateValidation);
    
    // Update operations
    Task<AssetMaintenanceScheduleDto?> UpdateScheduleAsync(UpdateAssetMaintenanceScheduleDto updateDto, Guid scheduleId);
    
    // Delete operations
    Task<bool> DeleteScheduleAsync(Guid scheduleId);
    
    // Business logic operations
    Task<IEnumerable<AssetMaintenanceScheduleDto>> GetSchedulesDueForReminderAsync();
    Task<IEnumerable<AssetMaintenanceScheduleDto>> GetSchedulesDueForMaintenanceAsync();
    
    // Filter/search operations
    Task<IEnumerable<AssetMaintenanceScheduleDto>> SearchSchedulesAsync(string? searchTerm, Guid? assetId, string? status, DateOnly? startDateFrom, DateOnly? startDateTo);
    
    // Hangfire job operations
    Task SendMaintenanceRemindersAsync();
    Task StartMaintenanceJobAsync();
    Task CompleteMaintenanceJobAsync();
}

