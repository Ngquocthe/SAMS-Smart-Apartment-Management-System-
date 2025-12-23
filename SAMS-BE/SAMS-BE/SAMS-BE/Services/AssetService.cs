using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using SAMS_BE.Mappers;

namespace SAMS_BE.Services;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly ILogger<AssetService> _logger;
    private readonly IAssetMaintenanceScheduleService _maintenanceScheduleService;

    public AssetService(
        IAssetRepository assetRepository, 
        ILogger<AssetService> logger,
        IAssetMaintenanceScheduleService maintenanceScheduleService)
    {
        _assetRepository = assetRepository;
        _logger = logger;
        _maintenanceScheduleService = maintenanceScheduleService;
    }

    public async Task<IEnumerable<AssetDto>> GetAllAssetsAsync()
    {
        try
        {
            var assets = await _assetRepository.GetAllAssetsAsync();
            return assets.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all assets");
            throw;
        }
    }

    public async Task<AssetDto?> GetAssetByIdAsync(Guid assetId)
    {
        try
        {
            var asset = await _assetRepository.GetAssetByIdAsync(assetId);
            return asset?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting asset by ID: {AssetId}", assetId);
            throw;
        }
    }

    public async Task<IEnumerable<AssetDto>> SearchAssetsAsync(string searchTerm)
    {
        try
        {
            var assets = await _assetRepository.SearchAssetsAsync(searchTerm);
            return assets.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching assets");
            throw;
        }
    }

    public async Task<IEnumerable<AssetDto>> GetAssetsByStatusAsync(string status)
    {
        try
        {
            var assets = await _assetRepository.GetAssetsByStatusAsync(status);
            return assets.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets by status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<AssetDto>> GetAssetsByLocationAsync(string location)
    {
        try
        {
            var assets = await _assetRepository.GetAssetsByLocationAsync(location);
            return assets.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets by location: {Location}", location);
            throw;
        }
    }

    public async Task<IEnumerable<AssetDto>> GetAssetsByCategoryAsync(Guid categoryId)
    {
        try
        {
            var assets = await _assetRepository.GetAssetsByCategoryAsync(categoryId);
            return assets.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets by category: {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<IEnumerable<AssetDto>> GetAssetsByApartmentAsync(Guid apartmentId)
    {
        try
        {
            var assets = await _assetRepository.GetAssetsByApartmentAsync(apartmentId);
            return assets.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets by apartment: {ApartmentId}", apartmentId);
            throw;
        }
    }

    public async Task<IEnumerable<AssetDto>> GetAssetsByBlockAsync(Guid blockId)
    {
        try
        {
            var assets = await _assetRepository.GetAssetsByBlockAsync(blockId);
            return assets.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets by block: {BlockId}", blockId);
            throw;
        }
    }

    public async Task<IEnumerable<AssetDto>> GetAssetsWithExpiredWarrantyAsync()
    {
        try
        {
            var assets = await _assetRepository.GetAssetsWithExpiredWarrantyAsync();
            return assets.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets with expired warranty");
            throw;
        }
    }

    public async Task<IEnumerable<AssetDto>> GetAssetsWithWarrantyExpiringInDaysAsync(int days)
    {
        try
        {
            var assets = await _assetRepository.GetAssetsWithWarrantyExpiringInDaysAsync(days);
            return assets.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets with warranty expiring in {Days} days", days);
            throw;
        }
    }

    public async Task<int> GetAssetCountAsync()
    {
        return await _assetRepository.GetAssetCountAsync();
    }

    public async Task<int> GetAssetCountByStatusAsync(string status)
    {
        return await _assetRepository.GetAssetCountByStatusAsync(status);
    }

    public async Task<int> GetAssetCountByCategoryAsync(Guid categoryId)
    {
        return await _assetRepository.GetAssetCountByCategoryAsync(categoryId);
    }

    public async Task<IEnumerable<AssetCategoryDto>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _assetRepository.GetAllCategoriesAsync();
            return categories.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all asset categories");
            throw;
        }
    }

    public async Task<AssetCategoryDto?> GetCategoryByIdAsync(Guid categoryId)
    {
        try
        {
            var category = await _assetRepository.GetCategoryByIdAsync(categoryId);
            return category?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting category by ID: {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<IEnumerable<AssetDto>> GetActiveAssetsAsync()
    {
        try
        {
            var assets = await _assetRepository.GetAssetsByStatusAsync("ACTIVE");
            return assets.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting active assets");
            throw;
        }
    }

    public async Task<AssetDto> CreateAssetAsync(CreateAssetDto createAssetDto)
    {
        try
        {
            // Validate: Kiểm tra mã tài sản đã tồn tại
            var existingAssetByCode = await _assetRepository.GetAssetByCodeAsync(createAssetDto.Code);
            if (existingAssetByCode != null)
            {
                throw new InvalidOperationException("Mã tài sản này đã tồn tại");
            }

            // Validate: Kiểm tra tên tài sản đã tồn tại
            var allAssets = await _assetRepository.GetAllAssetsAsync();
            if (allAssets.Any(a => a.Name.Equals(createAssetDto.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Tên tài sản này đã tồn tại");
            }

            // Validate: Kiểm tra ngày mua không được ở tương lai
            if (createAssetDto.PurchaseDate.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
                if (createAssetDto.PurchaseDate.Value > today)
                {
                    throw new InvalidOperationException("Ngày mua không được ở tương lai");
                }
            }

            // Validate: Kiểm tra ngày hết hạn bảo hành phải sau ngày mua
            if (createAssetDto.WarrantyExpire.HasValue && createAssetDto.PurchaseDate.HasValue)
            {
                if (createAssetDto.WarrantyExpire.Value <= createAssetDto.PurchaseDate.Value)
                {
                    throw new InvalidOperationException("Ngày hết hạn bảo hành phải sau ngày mua");
                }
            }

            // Lookup CategoryId by Code from Database
            var category = await _assetRepository.GetCategoryByCodeAsync(createAssetDto.CategoryId);
            
            // Auto-create category if missing (Self-healing logic for new databases)
            if (category == null)
            {
                category = await EnsureCategoryExistsAsync(createAssetDto.CategoryId);
            }

            var asset = createAssetDto.ToEntity(category.CategoryId); // Use the actual GUID from DB
            
            // Đảm bảo Status có giá trị mặc định
            if (string.IsNullOrWhiteSpace(asset.Status))
            {
                asset.Status = "ACTIVE";
            }

            var createdAsset = await _assetRepository.CreateAssetAsync(asset);
            
            // Tự động tạo lịch bảo trì nếu có maintenanceFrequency
            if (createAssetDto.MaintenanceFrequency.HasValue && 
                createAssetDto.MaintenanceFrequency.Value > 0)
            {
                DateOnly startDate = default;
                DateOnly endDate = default;
                
                try
                {
                    // Tính ngày bắt đầu: purchaseDate (hoặc ngày hiện tại) + maintenanceFrequency ngày
                    var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
                    var baseDate = createAssetDto.PurchaseDate ?? today;
                    // Đảm bảo baseDate không trong quá khứ
                    if (baseDate < today)
                    {
                        baseDate = today;
                    }
                    startDate = baseDate.AddDays(createAssetDto.MaintenanceFrequency.Value);
                    endDate = startDate.AddDays(3); // Bảo trì trong 3 ngày

                    var scheduleDto = new CreateAssetMaintenanceScheduleDto
                    {
                        AssetId = createdAsset.AssetId,
                        StartDate = startDate,
                        EndDate = endDate,
                        RecurrenceType = "DAILY",
                        RecurrenceInterval = createAssetDto.MaintenanceFrequency.Value,
                        ReminderDays = 3, // Nhắc nhở 3 ngày trước
                        Description = $"Lịch bảo trì tự động - Chu kỳ {createAssetDto.MaintenanceFrequency.Value} ngày",
                        Status = "SCHEDULED"
                    };

                    var createdSchedule = await _maintenanceScheduleService.CreateScheduleAsync(scheduleDto, null, skipDateValidation: true);
                    _logger.LogInformation(
                        "Auto-created maintenance schedule {ScheduleId} for asset {AssetId} with frequency {Frequency} days",
                        createdSchedule.ScheduleId,
                        createdAsset.AssetId,
                        createAssetDto.MaintenanceFrequency.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to auto-create maintenance schedule for asset {AssetId} (Code: {Code}, Name: {Name})",
                        createdAsset.AssetId,
                        createdAsset.Code,
                        createdAsset.Name);
                }
            }
            
            // Reload để lấy đầy đủ thông tin bao gồm navigation properties
            var reloadedAsset = await _assetRepository.GetAssetByIdAsync(createdAsset.AssetId);
            return reloadedAsset?.ToDto() ?? createdAsset.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating asset");
            throw;
        }
    }

    public async Task<AssetDto?> UpdateAssetAsync(UpdateAssetDto updateAssetDto, Guid assetId)
    {
        try
        {
            // Kiểm tra asset có tồn tại không
            var existingAsset = await _assetRepository.GetAssetByIdAsync(assetId);
            if (existingAsset == null)
            {
                return null;
            }

            // Validate: Kiểm tra mã tài sản đã tồn tại ở asset khác (ngoại trừ chính nó)
            if (updateAssetDto.Code != existingAsset.Code)
            {
                var assetWithSameCode = await _assetRepository.GetAssetByCodeAsync(updateAssetDto.Code);
                if (assetWithSameCode != null && assetWithSameCode.AssetId != assetId)
                {
                    throw new InvalidOperationException("Mã tài sản này đã tồn tại");
                }
            }

            // Validate: Kiểm tra tên tài sản đã tồn tại ở asset khác (ngoại trừ chính nó)
            if (!updateAssetDto.Name.Equals(existingAsset.Name, StringComparison.OrdinalIgnoreCase))
            {
                var allAssets = await _assetRepository.GetAllAssetsAsync();
                if (allAssets.Any(a => a.AssetId != assetId && 
                                       a.Name.Equals(updateAssetDto.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Tên tài sản này đã tồn tại");
                }
            }

            // Validate: Kiểm tra ngày mua không được ở tương lai
            if (updateAssetDto.PurchaseDate.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
                if (updateAssetDto.PurchaseDate.Value > today)
                {
                    throw new InvalidOperationException("Ngày mua không được ở tương lai");
                }
            }

            // Validate: Kiểm tra ngày hết hạn bảo hành phải sau ngày mua
            if (updateAssetDto.WarrantyExpire.HasValue && updateAssetDto.PurchaseDate.HasValue)
            {
                if (updateAssetDto.WarrantyExpire.Value <= updateAssetDto.PurchaseDate.Value)
                {
                    throw new InvalidOperationException("Ngày hết hạn bảo hành phải sau ngày mua");
                }
            }

            // Lookup CategoryId by Code from Database
            var category = await _assetRepository.GetCategoryByCodeAsync(updateAssetDto.CategoryId);
            if (category == null)
            {
                throw new InvalidOperationException($"Category with code '{updateAssetDto.CategoryId}' not found in database.");
            }

            var asset = updateAssetDto.ToEntity(assetId, category.CategoryId); // Use actual GUID from DB
            var updatedAsset = await _assetRepository.UpdateAssetAsync(asset);
            
            if (updatedAsset == null)
            {
                return null;
            }

            // Reload để lấy đầy đủ thông tin bao gồm navigation properties
            var reloadedAsset = await _assetRepository.GetAssetByIdAsync(updatedAsset.AssetId);
            return reloadedAsset?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating asset: {AssetId}", assetId);
            throw;
        }
    }

    public async Task<bool> DeleteAssetAsync(Guid assetId)
    {
        try
        {
            return await _assetRepository.DeleteAssetAsync(assetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting asset: {AssetId}", assetId);
            throw;
        }
    }

    private async Task<AssetCategory> EnsureCategoryExistsAsync(string code)
    {
        var normalizedCode = code?.Trim().ToUpperInvariant();
        string name = "Tài sản khác";
        string description = "Tự động tạo";
        int? frequency = 365;
        int? reminder = 7;

        switch (normalizedCode)
        {
            case "COMMON_SPACE":
                name = "Không gian & thiết bị chung";
                description = "Common area assets";
                frequency = 365;
                reminder = 7;
                break;
            case "SECURITY":
                name = "Hệ thống an ninh";
                description = "Security assets";
                frequency = 90;
                reminder = 3;
                break;
            case "ELECTRICAL":
                name = "Hệ thống điện & nước";
                description = "Electrical assets";
                frequency = 180;
                reminder = 5;
                break;
            case "AMENITY":
                name = "Tiện ích chung cư";
                description = "Amenities";
                frequency = null;
                reminder = 3;
                break;
        }

        if (string.IsNullOrEmpty(normalizedCode)) throw new ArgumentException("Code cannot be empty");

        var newCategory = new AssetCategory
        {
            CategoryId = Guid.NewGuid(),
            Code = normalizedCode,
            Name = name,
            Description = description,
            MaintenanceFrequency = frequency,
            DefaultReminderDays = reminder
        };

        return await _assetRepository.CreateCategoryAsync(newCategory);
    }
}

