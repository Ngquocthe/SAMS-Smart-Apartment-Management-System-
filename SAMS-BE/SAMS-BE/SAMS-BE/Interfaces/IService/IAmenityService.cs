using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService;

public interface IAmenityService
{
    // View operations only
    Task<IEnumerable<AmenityDto>> GetAllAmenitiesAsync();
    Task<AmenityDto?> GetAmenityByIdAsync(Guid amenityId);
    
    // Query operations
    Task<IEnumerable<AmenityDto>> SearchAmenitiesAsync(string searchTerm);
    Task<IEnumerable<AmenityDto>> GetAmenitiesByStatusAsync(string status);
    Task<IEnumerable<AmenityDto>> GetAmenitiesByLocationAsync(string location);
    Task<IEnumerable<AmenityDto>> GetAmenitiesByCategoryAsync(string categoryName);
    Task<IEnumerable<AmenityDto>> GetAmenitiesByPriceRangeAsync(int minPrice, int maxPrice);
    
    // Count operations
    Task<int> GetAmenityCountAsync();
    Task<int> GetAmenityCountByStatusAsync(string status);
    
    // Business logic operations
    Task<IEnumerable<AmenityDto>> GetAvailableAmenitiesAsync();
    Task<IEnumerable<AmenityDto>> GetAmenitiesRequiringBookingAsync();

    // Create operations
    Task<AmenityDto> CreateAmenityAsync(CreateAmenityDto createAmenityDto);
    
    // Update operations
    Task<AmenityDto?> UpdateAmenityAsync(UpdateAmenityDto updateAmenityDto, Guid amenityId);
    
    // Delete operations
    Task<bool> DeleteAmenityAsync(Guid amenityId);
}
