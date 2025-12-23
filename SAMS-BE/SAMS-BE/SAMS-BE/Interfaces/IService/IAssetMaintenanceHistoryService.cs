using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService;

public interface IAssetMaintenanceHistoryService
{
    // View operations
    Task<IEnumerable<AssetMaintenanceHistoryDto>> GetAllHistoriesAsync();
    Task<AssetMaintenanceHistoryDto?> GetHistoryByIdAsync(Guid historyId);
    Task<IEnumerable<AssetMaintenanceHistoryDto>> GetHistoriesByAssetIdAsync(Guid assetId);
    Task<IEnumerable<AssetMaintenanceHistoryDto>> GetHistoriesByScheduleIdAsync(Guid scheduleId);
    
    // Create operations
    Task<AssetMaintenanceHistoryDto> CreateHistoryAsync(CreateAssetMaintenanceHistoryDto createDto);
    
    // Update operations
    Task<AssetMaintenanceHistoryDto?> UpdateHistoryAsync(UpdateAssetMaintenanceHistoryDto updateDto, Guid historyId);
}

