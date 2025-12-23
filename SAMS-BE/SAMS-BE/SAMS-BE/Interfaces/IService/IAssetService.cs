using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService;

public interface IAssetService
{
    // View operations
    Task<IEnumerable<AssetDto>> GetAllAssetsAsync();
    Task<AssetDto?> GetAssetByIdAsync(Guid assetId);
    
    // Query operations
    Task<IEnumerable<AssetDto>> SearchAssetsAsync(string searchTerm);
    Task<IEnumerable<AssetDto>> GetAssetsByStatusAsync(string status);
    Task<IEnumerable<AssetDto>> GetAssetsByLocationAsync(string location);
    Task<IEnumerable<AssetDto>> GetAssetsByCategoryAsync(Guid categoryId);
    Task<IEnumerable<AssetDto>> GetAssetsByApartmentAsync(Guid apartmentId);
    Task<IEnumerable<AssetDto>> GetAssetsByBlockAsync(Guid blockId);
    Task<IEnumerable<AssetDto>> GetAssetsWithExpiredWarrantyAsync();
    Task<IEnumerable<AssetDto>> GetAssetsWithWarrantyExpiringInDaysAsync(int days);
    
    // Count operations
    Task<int> GetAssetCountAsync();
    Task<int> GetAssetCountByStatusAsync(string status);
    Task<int> GetAssetCountByCategoryAsync(Guid categoryId);

    // Category operations
    Task<IEnumerable<AssetCategoryDto>> GetAllCategoriesAsync();
    Task<AssetCategoryDto?> GetCategoryByIdAsync(Guid categoryId);

    // Business logic operations
    Task<IEnumerable<AssetDto>> GetActiveAssetsAsync();

    // Create operations
    Task<AssetDto> CreateAssetAsync(CreateAssetDto createAssetDto);
    
    // Update operations
    Task<AssetDto?> UpdateAssetAsync(UpdateAssetDto updateAssetDto, Guid assetId);
    
    // Delete operations
    Task<bool> DeleteAssetAsync(Guid assetId);
}

