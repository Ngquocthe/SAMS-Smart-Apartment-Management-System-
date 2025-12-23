using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly BuildingManagementContext _context;

    public AssetRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
    {
        return await _context.Assets
            .Where(a => !a.IsDelete)
            .Include(a => a.Category)
            .Include(a => a.Apartment)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Asset?> GetAssetByIdAsync(Guid assetId)
    {
        return await _context.Assets
            .Where(a => !a.IsDelete)
            .Include(a => a.Category)
            .Include(a => a.Apartment)
            .FirstOrDefaultAsync(a => a.AssetId == assetId);
    }

    public async Task<Asset?> GetAssetByCodeAsync(string code)
    {
        return await _context.Assets
            .Where(a => !a.IsDelete)
            .Include(a => a.Category)
            .Include(a => a.Apartment)
            .FirstOrDefaultAsync(a => a.Code == code);
    }

    public async Task<IEnumerable<Asset>> SearchAssetsAsync(string searchTerm)
    {
        return await _context.Assets
            .Where(a => !a.IsDelete)
            .Include(a => a.Category)
            .Include(a => a.Apartment)
            .Where(a => a.Name.Contains(searchTerm) ||
                       a.Code.Contains(searchTerm) ||
                       (a.Location != null && a.Location.Contains(searchTerm)))
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetAssetsByStatusAsync(string status)
    {
        return await _context.Assets
            .Where(a => !a.IsDelete && a.Status == status)
            .Include(a => a.Category)
            .Include(a => a.Apartment)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetAssetsByLocationAsync(string location)
    {
        return await _context.Assets
            .Where(a => !a.IsDelete)
            .Include(a => a.Category)
            .Include(a => a.Apartment)
            .Where(a => a.Location != null && a.Location.Contains(location))
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetAssetsByCategoryAsync(Guid categoryId)
    {
        return await _context.Assets
            .Where(a => !a.IsDelete && a.CategoryId == categoryId)
            .Include(a => a.Category)
            .Include(a => a.Apartment)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetAssetsByApartmentAsync(Guid apartmentId)
    {
        return await _context.Assets
            .Where(a => !a.IsDelete && a.ApartmentId == apartmentId)
            .Include(a => a.Category)
            .Include(a => a.Apartment)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetAssetsByBlockAsync(Guid blockId)
    {
        return await _context.Assets
            .Where(a => !a.IsDelete && a.BlockId == blockId)
            .Include(a => a.Category)
            .Include(a => a.Apartment)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetAssetsWithExpiredWarrantyAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _context.Assets
            .Where(a => !a.IsDelete && a.WarrantyExpire != null && a.WarrantyExpire < today)
            .Include(a => a.Category)
            .Include(a => a.Apartment)
            .OrderBy(a => a.WarrantyExpire)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetAssetsWithWarrantyExpiringInDaysAsync(int days)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var futureDate = today.AddDays(days);
        return await _context.Assets
            .Where(a => !a.IsDelete && 
                       a.WarrantyExpire != null && 
                       a.WarrantyExpire >= today && 
                       a.WarrantyExpire <= futureDate)
            .Include(a => a.Category)
            .Include(a => a.Apartment)
            .OrderBy(a => a.WarrantyExpire)
            .ToListAsync();
    }

    public async Task<int> GetAssetCountAsync()
    {
        return await _context.Assets.CountAsync(a => !a.IsDelete);
    }

    public async Task<int> GetAssetCountByStatusAsync(string status)
    {
        return await _context.Assets
            .CountAsync(a => !a.IsDelete && a.Status == status);
    }

    public async Task<int> GetAssetCountByCategoryAsync(Guid categoryId)
    {
        return await _context.Assets
            .CountAsync(a => !a.IsDelete && a.CategoryId == categoryId);
    }

    public async Task<IEnumerable<AssetCategory>> GetAllCategoriesAsync()
    {
        return await _context.AssetCategories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<AssetCategory?> GetCategoryByIdAsync(Guid categoryId)
    {
        return await _context.AssetCategories
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
    }

    public async Task<AssetCategory?> GetCategoryByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        // 1. Try exact match (fastest)
        var category = await _context.AssetCategories
            .FirstOrDefaultAsync(c => c.Code == code);
        if (category != null) return category;

        // 2. Fallback: Fetch all (assuming small list) and match in memory
        // This handles case sensitivity/collation/whitespace issues perfectly
        var allCategories = await _context.AssetCategories.ToListAsync();
        return allCategories.FirstOrDefault(c => 
            c.Code?.Trim().Equals(code.Trim(), StringComparison.OrdinalIgnoreCase) == true);
    }

    public async Task<AssetCategory> CreateCategoryAsync(AssetCategory category)
    {
        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<Asset> CreateAssetAsync(Asset asset)
    {
        asset.IsDelete = false; // Ensure soft delete flag is false for new assets
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
        
        // Load relationships to return full object to FE
        await _context.Entry(asset).Reference(a => a.Category).LoadAsync();
        
        return asset;
    }

    public async Task<Asset?> UpdateAssetAsync(Asset asset)
    {
        var existingAsset = await _context.Assets.FindAsync(asset.AssetId);
        if (existingAsset == null)
        {
            return null;
        }

        existingAsset.CategoryId = asset.CategoryId;
        existingAsset.Code = asset.Code;
        existingAsset.Name = asset.Name;
        existingAsset.ApartmentId = asset.ApartmentId;
        existingAsset.BlockId = asset.BlockId;
        existingAsset.Location = asset.Location;
        existingAsset.PurchaseDate = asset.PurchaseDate;
        existingAsset.WarrantyExpire = asset.WarrantyExpire;
        existingAsset.MaintenanceFrequency = asset.MaintenanceFrequency;
        existingAsset.Status = asset.Status;

        await _context.SaveChangesAsync();
        
        // Load relationships to return full object to FE
        await _context.Entry(existingAsset).Reference(a => a.Category).LoadAsync();
        
        return existingAsset;
    }

    public async Task<bool> DeleteAssetAsync(Guid assetId)
    {
        var asset = await _context.Assets.FindAsync(assetId);
        if (asset == null || asset.IsDelete)
        {
            return false;
        }

        // Soft delete
        asset.IsDelete = true;
        await _context.SaveChangesAsync();
        return true;
    }
}

