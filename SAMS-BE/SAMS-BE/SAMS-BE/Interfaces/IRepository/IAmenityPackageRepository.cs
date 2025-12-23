using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface IAmenityPackageRepository
{
    // View operations
    Task<IEnumerable<AmenityPackage>> GetAllPackagesAsync();
    Task<AmenityPackage?> GetPackageByIdAsync(Guid packageId);
    Task<IEnumerable<AmenityPackage>> GetPackagesByAmenityIdAsync(Guid amenityId);
    
    // Query operations
    Task<IEnumerable<AmenityPackage>> GetPackagesByStatusAsync(string status);
    Task<IEnumerable<AmenityPackage>> GetPackagesByMonthCountAsync(int monthCount);
    Task<IEnumerable<AmenityPackage>> GetPackagesByPriceRangeAsync(int minPrice, int maxPrice);
    
    // Count operations
    Task<int> GetPackageCountAsync();
    Task<int> GetPackageCountByAmenityIdAsync(Guid amenityId);

    // Create operations
    Task<AmenityPackage> CreatePackageAsync(AmenityPackage package);
    
    // Update operations
    Task<AmenityPackage?> UpdatePackageAsync(AmenityPackage package);
    
    // Delete operations
    Task<bool> DeletePackageAsync(Guid packageId);
}

