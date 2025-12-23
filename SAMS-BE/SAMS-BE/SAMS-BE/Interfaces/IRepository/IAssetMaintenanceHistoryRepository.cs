using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface IAssetMaintenanceHistoryRepository
{
    // View operations
    Task<IEnumerable<AssetMaintenanceHistory>> GetAllHistoriesAsync();
    Task<AssetMaintenanceHistory?> GetHistoryByIdAsync(Guid historyId);
    Task<IEnumerable<AssetMaintenanceHistory>> GetHistoriesByAssetIdAsync(Guid assetId);
    Task<IEnumerable<AssetMaintenanceHistory>> GetHistoriesByScheduleIdAsync(Guid scheduleId);
    
    // Create operations
    Task<AssetMaintenanceHistory> CreateHistoryAsync(AssetMaintenanceHistory history);
    
    // Update operations
    Task<AssetMaintenanceHistory?> UpdateHistoryAsync(AssetMaintenanceHistory history);
}

