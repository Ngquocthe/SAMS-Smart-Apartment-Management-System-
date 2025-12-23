using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories;

public class AmenityRepository : IAmenityRepository
{
    private readonly BuildingManagementContext _context;

    public AmenityRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Amenity>> GetAllAmenitiesAsync()
    {
        return await _context.Amenities
            .Include(a => a.AmenityPackages)
            .Where(a => !a.IsDelete)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Amenity?> GetAmenityByIdAsync(Guid amenityId)
    {
        return await _context.Amenities
            .Include(a => a.AmenityPackages)
            .Where(a => !a.IsDelete)
            .FirstOrDefaultAsync(a => a.AmenityId == amenityId);
    }

    public async Task<Amenity?> GetAmenityByCodeAsync(string code)
    {
        return await _context.Amenities
            .Where(a => !a.IsDelete)
            .FirstOrDefaultAsync(a => a.Code == code);
    }

    public async Task<IEnumerable<Amenity>> SearchAmenitiesAsync(string searchTerm)
    {
        return await _context.Amenities
            .Where(a => !a.IsDelete &&
                       (a.Name.Contains(searchTerm) ||
                       a.Code.Contains(searchTerm) ||
                       (a.Location != null && a.Location.Contains(searchTerm))))
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Amenity>> GetAmenitiesByStatusAsync(string status)
    {
        return await _context.Amenities
            .Where(a => !a.IsDelete && a.Status == status)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Amenity>> GetAmenitiesByLocationAsync(string location)
    {
        return await _context.Amenities
            .Where(a => !a.IsDelete && a.Location != null && a.Location.Contains(location))
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Amenity>> GetAmenitiesByCategoryAsync(string categoryName)
    {
        return await _context.Amenities
            .Where(a => !a.IsDelete && a.CategoryName != null && a.CategoryName.Contains(categoryName))
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Amenity>> GetAmenitiesByPriceRangeAsync(int minPrice, int maxPrice)
    {
        // Vì bây giờ không còn HourPrice, ta tìm theo giá của packages
        var amenityIds = await _context.AmenityPackages
            .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
            .Select(p => p.AmenityId)
            .Distinct()
            .ToListAsync();

        return await _context.Amenities
            .Where(a => !a.IsDelete && amenityIds.Contains(a.AmenityId))
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<int> GetAmenityCountAsync()
    {
        return await _context.Amenities.CountAsync(a => !a.IsDelete);
    }

    public async Task<int> GetAmenityCountByStatusAsync(string status)
    {
        return await _context.Amenities
            .CountAsync(a => !a.IsDelete && a.Status == status);
    }

    public async Task<Amenity> CreateAmenityAsync(Amenity amenity)
    {
        _context.Amenities.Add(amenity);
        await _context.SaveChangesAsync();
        return amenity;
    }

    public async Task<Amenity?> UpdateAmenityAsync(Amenity amenity)
    {
        var existingAmenity = await _context.Amenities.FindAsync(amenity.AmenityId);
        if (existingAmenity == null || existingAmenity.IsDelete)
        {
            return null;
        }

        // Update properties (KHÔNG update IsDelete)
        existingAmenity.Code = amenity.Code;
        existingAmenity.Name = amenity.Name;
        existingAmenity.CategoryName = amenity.CategoryName;
        existingAmenity.Location = amenity.Location;
        existingAmenity.HasMonthlyPackage = amenity.HasMonthlyPackage;
        existingAmenity.RequiresFaceVerification = amenity.RequiresFaceVerification;
        existingAmenity.FeeType = amenity.FeeType;
        existingAmenity.Status = amenity.Status;
        existingAmenity.AssetId = amenity.AssetId;
        // KHÔNG update IsDelete - giữ nguyên giá trị cũ

        await _context.SaveChangesAsync();
        return existingAmenity;
    }

    public async Task<bool> DeleteAmenityAsync(Guid amenityId)
    {
        var amenity = await _context.Amenities
            .Include(a => a.Asset)
            .FirstOrDefaultAsync(a => a.AmenityId == amenityId);
            
        if (amenity == null || amenity.IsDelete)
        {
            return false;
        }

        // Soft delete amenity
        amenity.IsDelete = true;
        
        // Cũng soft delete asset tương ứng nếu có
        if (amenity.AssetId.HasValue)
        {
            var asset = amenity.Asset ?? await _context.Assets.FindAsync(amenity.AssetId.Value);
            if (asset != null && !asset.IsDelete)
            {
                asset.IsDelete = true;
            }
        }
        
        await _context.SaveChangesAsync();
        return true;
    }
}