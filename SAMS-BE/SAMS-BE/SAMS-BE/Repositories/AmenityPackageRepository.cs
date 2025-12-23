using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories;

public class AmenityPackageRepository : IAmenityPackageRepository
{
    private readonly BuildingManagementContext _context;

    public AmenityPackageRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AmenityPackage>> GetAllPackagesAsync()
    {
        return await _context.AmenityPackages
            .Include(p => p.Amenity)
            .OrderBy(p => p.MonthCount)
            .ToListAsync();
    }

    public async Task<AmenityPackage?> GetPackageByIdAsync(Guid packageId)
    {
        return await _context.AmenityPackages
            .Include(p => p.Amenity)
            .FirstOrDefaultAsync(p => p.PackageId == packageId);
    }

    public async Task<IEnumerable<AmenityPackage>> GetPackagesByAmenityIdAsync(Guid amenityId)
    {
        return await _context.AmenityPackages
            .Include(p => p.Amenity)
            .Where(p => p.AmenityId == amenityId)
            .OrderBy(p => p.MonthCount)
            .ToListAsync();
    }

    public async Task<IEnumerable<AmenityPackage>> GetPackagesByStatusAsync(string status)
    {
        return await _context.AmenityPackages
            .Include(p => p.Amenity)
            .Where(p => p.Status == status)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<AmenityPackage>> GetPackagesByMonthCountAsync(int monthCount)
    {
        return await _context.AmenityPackages
            .Include(p => p.Amenity)
            .Where(p => p.MonthCount == monthCount)
            .OrderBy(p => p.Price)
            .ToListAsync();
    }

    public async Task<IEnumerable<AmenityPackage>> GetPackagesByPriceRangeAsync(int minPrice, int maxPrice)
    {
        return await _context.AmenityPackages
            .Include(p => p.Amenity)
            .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
            .OrderBy(p => p.Price)
            .ToListAsync();
    }

    public async Task<int> GetPackageCountAsync()
    {
        return await _context.AmenityPackages.CountAsync();
    }

    public async Task<int> GetPackageCountByAmenityIdAsync(Guid amenityId)
    {
        return await _context.AmenityPackages
            .CountAsync(p => p.AmenityId == amenityId);
    }

    public async Task<AmenityPackage> CreatePackageAsync(AmenityPackage package)
    {
        _context.AmenityPackages.Add(package);
        await _context.SaveChangesAsync();
        return package;
    }

    public async Task<AmenityPackage?> UpdatePackageAsync(AmenityPackage package)
    {
        var existingPackage = await _context.AmenityPackages.FindAsync(package.PackageId);
        if (existingPackage == null)
        {
            return null;
        }

        // Update properties
        existingPackage.Name = package.Name;
        existingPackage.MonthCount = package.MonthCount;
        existingPackage.Price = package.Price;
        existingPackage.Description = package.Description;
        existingPackage.Status = package.Status;

        await _context.SaveChangesAsync();
        return existingPackage;
    }

    public async Task<bool> DeletePackageAsync(Guid packageId)
    {
        var package = await _context.AmenityPackages.FindAsync(packageId);
        if (package == null)
        {
            return false;
        }

        // Hard delete - xóa luôn
        _context.AmenityPackages.Remove(package);
        await _context.SaveChangesAsync();
        return true;
    }
}

