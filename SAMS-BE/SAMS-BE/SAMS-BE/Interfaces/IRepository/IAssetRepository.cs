using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface IAssetRepository
{
    // View operations
    Task<IEnumerable<Asset>> GetAllAssetsAsync();
    Task<Asset?> GetAssetByIdAsync(Guid assetId);
    Task<Asset?> GetAssetByCodeAsync(string code);
    
    // Query operations
    Task<IEnumerable<Asset>> SearchAssetsAsync(string searchTerm);
    Task<IEnumerable<Asset>> GetAssetsByStatusAsync(string status);
    Task<IEnumerable<Asset>> GetAssetsByLocationAsync(string location);
    Task<IEnumerable<Asset>> GetAssetsByCategoryAsync(Guid categoryId);
    Task<IEnumerable<Asset>> GetAssetsByApartmentAsync(Guid apartmentId);
    Task<IEnumerable<Asset>> GetAssetsByBlockAsync(Guid blockId);
    Task<IEnumerable<Asset>> GetAssetsWithExpiredWarrantyAsync();
    Task<IEnumerable<Asset>> GetAssetsWithWarrantyExpiringInDaysAsync(int days);
    
    // Count operations
    Task<int> GetAssetCountAsync();
    Task<int> GetAssetCountByStatusAsync(string status);
    Task<int> GetAssetCountByCategoryAsync(Guid categoryId);

    // Category operations
    Task<IEnumerable<AssetCategory>> GetAllCategoriesAsync();
    Task<AssetCategory?> GetCategoryByIdAsync(Guid categoryId);
    Task<AssetCategory?> GetCategoryByCodeAsync(string code);
    Task<AssetCategory> CreateCategoryAsync(AssetCategory category);

    // Create operations
    Task<Asset> CreateAssetAsync(Asset asset);
    
    // Update operations
    Task<Asset?> UpdateAssetAsync(Asset asset);
    
    // Delete operations
    Task<bool> DeleteAssetAsync(Guid assetId);
}

