using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface IAmenityRepository
{
    // View operations only
    Task<IEnumerable<Amenity>> GetAllAmenitiesAsync();
    Task<Amenity?> GetAmenityByIdAsync(Guid amenityId);
    Task<Amenity?> GetAmenityByCodeAsync(string code);
    
    // Query operations
    Task<IEnumerable<Amenity>> SearchAmenitiesAsync(string searchTerm);
    Task<IEnumerable<Amenity>> GetAmenitiesByStatusAsync(string status);
    Task<IEnumerable<Amenity>> GetAmenitiesByLocationAsync(string location);
    Task<IEnumerable<Amenity>> GetAmenitiesByCategoryAsync(string categoryName);
    Task<IEnumerable<Amenity>> GetAmenitiesByPriceRangeAsync(int minPrice, int maxPrice);
    
    // Count operations
    Task<int> GetAmenityCountAsync();
    Task<int> GetAmenityCountByStatusAsync(string status);

    // Create operations
    Task<Amenity> CreateAmenityAsync(Amenity amenity);
    
    // Update operations
    Task<Amenity?> UpdateAmenityAsync(Amenity amenity);
    
    // Delete operations
    Task<bool> DeleteAmenityAsync(Guid amenityId);
}
