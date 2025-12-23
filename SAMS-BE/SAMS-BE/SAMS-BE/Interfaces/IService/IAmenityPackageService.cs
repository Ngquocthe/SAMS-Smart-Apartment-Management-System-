using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService;

public interface IAmenityPackageService
{
    // View operations
    Task<IEnumerable<AmenityPackageDto>> GetAllPackagesAsync();
    Task<AmenityPackageDto?> GetPackageByIdAsync(Guid packageId);
    Task<IEnumerable<AmenityPackageDto>> GetPackagesByAmenityIdAsync(Guid amenityId);
    
    // Query operations
    Task<IEnumerable<AmenityPackageDto>> GetPackagesByStatusAsync(string status);
    Task<IEnumerable<AmenityPackageDto>> GetActivePackagesByAmenityIdAsync(Guid amenityId);
    
    // Create operations
    Task<AmenityPackageDto> CreatePackageAsync(CreateAmenityPackageDto createPackageDto);
    
    // Update operations
    Task<AmenityPackageDto?> UpdatePackageAsync(UpdateAmenityPackageDto updatePackageDto, Guid packageId);
    
    // Delete operations
    Task<bool> DeletePackageAsync(Guid packageId);
}

