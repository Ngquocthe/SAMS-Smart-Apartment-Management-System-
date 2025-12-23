using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using SAMS_BE.Mappers;
using System.Linq;

namespace SAMS_BE.Services;

public class AmenityService : IAmenityService
{
    private readonly IAmenityRepository _amenityRepository;
    private readonly IAmenityPackageRepository _packageRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IAssetMaintenanceScheduleRepository _scheduleRepository;
    private readonly ILogger<AmenityService> _logger;
    private const string AmenityCategoryCode = "AMENITY";

    public AmenityService(
        IAmenityRepository amenityRepository, 
        IAmenityPackageRepository packageRepository,
        IAssetRepository assetRepository,
        IAssetMaintenanceScheduleRepository scheduleRepository,
        ILogger<AmenityService> logger)
    {
        _amenityRepository = amenityRepository;
        _packageRepository = packageRepository;
        _assetRepository = assetRepository;
        _scheduleRepository = scheduleRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<AmenityDto>> GetAllAmenitiesAsync()
    {
        try
        {
            var amenities = await _amenityRepository.GetAllAmenitiesAsync();
            return await MapAmenitiesWithMaintenanceAsync(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all amenities");
            throw;
        }
    }

    public async Task<AmenityDto?> GetAmenityByIdAsync(Guid amenityId)
    {
        try
        {
            var amenity = await _amenityRepository.GetAmenityByIdAsync(amenityId);
            return await MapAmenityWithMaintenanceAsync(amenity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenity by ID: {AmenityId}", amenityId);
            throw;
        }
    }

    public async Task<IEnumerable<AmenityDto>> SearchAmenitiesAsync(string searchTerm)
    {
        try
        {
            var amenities = await _amenityRepository.SearchAmenitiesAsync(searchTerm);
            return await MapAmenitiesWithMaintenanceAsync(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching amenities");
            throw;
        }
    }

    public async Task<IEnumerable<AmenityDto>> GetAmenitiesByStatusAsync(string status)
    {
        try
        {
            var amenities = await _amenityRepository.GetAmenitiesByStatusAsync(status);
            return await MapAmenitiesWithMaintenanceAsync(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenities by status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<AmenityDto>> GetAmenitiesByLocationAsync(string location)
    {
        try
        {
            var amenities = await _amenityRepository.GetAmenitiesByLocationAsync(location);
            return await MapAmenitiesWithMaintenanceAsync(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenities by location: {Location}", location);
            throw;
        }
    }

    public async Task<IEnumerable<AmenityDto>> GetAmenitiesByCategoryAsync(string categoryName)
    {
        try
        {
            var amenities = await _amenityRepository.GetAmenitiesByCategoryAsync(categoryName);
            return await MapAmenitiesWithMaintenanceAsync(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenities by category: {CategoryName}", categoryName);
            throw;
        }
    }

    public async Task<IEnumerable<AmenityDto>> GetAmenitiesByPriceRangeAsync(int minPrice, int maxPrice)
    {
        try
        {
            var amenities = await _amenityRepository.GetAmenitiesByPriceRangeAsync(minPrice, maxPrice);
            return await MapAmenitiesWithMaintenanceAsync(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenities by price range: {MinPrice}-{MaxPrice}", minPrice, maxPrice);
            throw;
        }
    }

    public async Task<int> GetAmenityCountAsync()
    {
        return await _amenityRepository.GetAmenityCountAsync();
    }

    public async Task<int> GetAmenityCountByStatusAsync(string status)
    {
        return await _amenityRepository.GetAmenityCountByStatusAsync(status);
    }

    public async Task<IEnumerable<AmenityDto>> GetAvailableAmenitiesAsync()
    {
        try
        {
            var amenities = await _amenityRepository.GetAmenitiesByStatusAsync("Active");
            return await MapAmenitiesWithMaintenanceAsync(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting available amenities");
            throw;
        }
    }

    public async Task<IEnumerable<AmenityDto>> GetAmenitiesRequiringBookingAsync()
    {
        try
        {
            var amenities = await _amenityRepository.GetAllAmenitiesAsync();
            var requiring = amenities
                .Where(a => a.HasMonthlyPackage && a.Status == "ACTIVE")
                .ToList();
            return await MapAmenitiesWithMaintenanceAsync(requiring);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenities requiring booking");
            throw;
        }
    }

    public async Task<AmenityDto> CreateAmenityAsync(CreateAmenityDto createAmenityDto)
    {
        try
        {
            // Validate: Kiểm tra mã tiện ích đã tồn tại
            var existingCode = await _amenityRepository.GetAmenityByCodeAsync(createAmenityDto.Code);
            if (existingCode != null)
            {
                throw new InvalidOperationException("Mã tiện ích này đã tồn tại");
            }

            // Validate: Kiểm tra tên tiện ích đã tồn tại
            var amenities = await _amenityRepository.GetAllAmenitiesAsync();
            if (amenities.Any(a => a.Name.Equals(createAmenityDto.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Tên tiện ích này đã tồn tại");
            }

            // 1. Tạo amenity
            var amenity = createAmenityDto.ToEntity();
            amenity.AssetId = await EnsureAssetLinkForAmenityAsync(
                createAmenityDto.Code,
                createAmenityDto.Name,
                createAmenityDto.Location,
                amenity.AssetId);

            var createdAmenity = await _amenityRepository.CreateAmenityAsync(amenity);

            // 2. Nếu có packages, tạo các packages cho amenity
            if (createAmenityDto.Packages != null && createAmenityDto.Packages.Any())
            {
                foreach (var packageDto in createAmenityDto.Packages)
                {
                    var package = new AmenityPackage
                    {
                        PackageId = Guid.NewGuid(),
                        AmenityId = createdAmenity.AmenityId,
                        Name = packageDto.Name,
                        MonthCount = packageDto.MonthCount,
                        DurationDays = packageDto.DurationDays,
                        PeriodUnit = packageDto.PeriodUnit,
                        Price = packageDto.Price,
                        Description = packageDto.Description,
                        Status = packageDto.Status
                    };
                    
                    await _packageRepository.CreatePackageAsync(package);
                }

                // 3. Reload amenity với packages để trả về đầy đủ thông tin
                var amenityWithPackages = await _amenityRepository.GetAmenityByIdAsync(createdAmenity.AmenityId);
                return await MapAmenityWithMaintenanceAsync(amenityWithPackages ?? createdAmenity);
            }

            return await MapAmenityWithMaintenanceAsync(createdAmenity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating amenity");
            throw;
        }
    }

    public async Task<AmenityDto?> UpdateAmenityAsync(UpdateAmenityDto updateAmenityDto, Guid amenityId)
    {
        try
        {
            var existingAmenity = await _amenityRepository.GetAmenityByIdAsync(amenityId);
            if (existingAmenity == null)
            {
                return null;
            }

            // Validate: Kiểm tra mã tiện ích đã tồn tại (ngoại trừ chính nó)
            var existingCode = await _amenityRepository.GetAmenityByCodeAsync(updateAmenityDto.Code);
            if (existingCode != null && existingCode.AmenityId != amenityId)
            {
                throw new InvalidOperationException("Mã tiện ích này đã tồn tại");
            }

            // Validate: Kiểm tra tên tiện ích đã tồn tại (ngoại trừ chính nó)
            var amenities = await _amenityRepository.GetAllAmenitiesAsync();
            if (amenities.Any(a => a.AmenityId != amenityId && 
                                   a.Name.Equals(updateAmenityDto.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Tên tiện ích này đã tồn tại");
            }

            // 1. Update amenity
            var amenity = updateAmenityDto.ToEntity(amenityId);
            amenity.AssetId = await EnsureAssetLinkForAmenityAsync(
                updateAmenityDto.Code,
                updateAmenityDto.Name,
                updateAmenityDto.Location,
                updateAmenityDto.AssetId ?? existingAmenity.AssetId);

            var updatedAmenity = await _amenityRepository.UpdateAmenityAsync(amenity);
            if (updatedAmenity == null)
            {
                return null;
            }

            // 2. Nếu có Packages trong request, update packages
            if (updateAmenityDto.Packages != null)
            {
                // Lấy danh sách packages hiện tại
                var existingPackages = await _packageRepository.GetPackagesByAmenityIdAsync(amenityId);
                var existingPackageIds = existingPackages.Select(p => p.PackageId).ToHashSet();

                // Lấy danh sách package IDs từ request
                var requestPackageIds = updateAmenityDto.Packages
                    .Where(p => p.PackageId.HasValue)
                    .Select(p => p.PackageId!.Value)
                    .ToHashSet();

                // Xóa các packages không còn trong request
                foreach (var existingPackage in existingPackages)
                {
                    if (!requestPackageIds.Contains(existingPackage.PackageId))
                    {
                        await _packageRepository.DeletePackageAsync(existingPackage.PackageId);
                    }
                }

                // Update hoặc tạo mới packages
                foreach (var packageDto in updateAmenityDto.Packages)
                {
                    if (packageDto.PackageId.HasValue && existingPackageIds.Contains(packageDto.PackageId.Value))
                    {
                        // Update existing package
                        var package = new AmenityPackage
                        {
                            PackageId = packageDto.PackageId.Value,
                            AmenityId = amenityId,
                            Name = packageDto.Name,
                            MonthCount = packageDto.MonthCount,
                            DurationDays = packageDto.DurationDays,
                            PeriodUnit = packageDto.PeriodUnit,
                            Price = packageDto.Price,
                            Description = packageDto.Description,
                            Status = packageDto.Status
                        };
                        await _packageRepository.UpdatePackageAsync(package);
                    }
                    else
                    {
                        // Create new package
                        var package = new AmenityPackage
                        {
                            PackageId = Guid.NewGuid(),
                            AmenityId = amenityId,
                            Name = packageDto.Name,
                            MonthCount = packageDto.MonthCount,
                            DurationDays = packageDto.DurationDays,
                            PeriodUnit = packageDto.PeriodUnit,
                            Price = packageDto.Price,
                            Description = packageDto.Description,
                            Status = packageDto.Status
                        };
                        await _packageRepository.CreatePackageAsync(package);
                    }
                }

                // Reload với packages mới
                var amenityWithPackages = await _amenityRepository.GetAmenityByIdAsync(amenityId);
                return await MapAmenityWithMaintenanceAsync(amenityWithPackages);
            }

            return await MapAmenityWithMaintenanceAsync(updatedAmenity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating amenity: {AmenityId}", amenityId);
            throw;
        }
    }

    public async Task<bool> DeleteAmenityAsync(Guid amenityId)
    {
        try
        {
            var result = await _amenityRepository.DeleteAmenityAsync(amenityId);
            
            if (result)
            {
                _logger.LogInformation("Successfully deleted amenity: {AmenityId}", amenityId);
            }
            else
            {
                _logger.LogWarning("Amenity not found for deletion: {AmenityId}", amenityId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting amenity: {AmenityId}", amenityId);
            throw;
        }
    }

    private async Task<Guid?> EnsureAssetLinkForAmenityAsync(string code, string name, string? location, Guid? requestedAssetId)
    {
        if (requestedAssetId.HasValue)
        {
            var existingAsset = await _assetRepository.GetAssetByIdAsync(requestedAssetId.Value);
            if (existingAsset == null)
            {
                throw new ArgumentException($"Asset với ID {requestedAssetId} không tồn tại");
            }

            bool hasChanges = false;
            if (!string.Equals(existingAsset.Name, name, StringComparison.Ordinal))
            {
                existingAsset.Name = name;
                hasChanges = true;
            }
            if (!string.Equals(existingAsset.Code, code, StringComparison.Ordinal))
            {
                existingAsset.Code = code;
                hasChanges = true;
            }
            if (!string.Equals(existingAsset.Location, location, StringComparison.Ordinal))
            {
                existingAsset.Location = location;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _assetRepository.UpdateAssetAsync(existingAsset);
            }

            return requestedAssetId;
        }

        var assetByCode = await _assetRepository.GetAssetByCodeAsync(code);
        if (assetByCode != null)
        {
            return assetByCode.AssetId;
        }

        var amenityCategory = await _assetRepository.GetCategoryByCodeAsync(AmenityCategoryCode);
        if (amenityCategory == null)
        {
            amenityCategory = new AssetCategory
            {
                CategoryId = Guid.NewGuid(),
                Code = AmenityCategoryCode,
                Name = "Tiện ích chung cư",
                Description = "Các tiện ích chung của tòa nhà",
                MaintenanceFrequency = 1,
                DefaultReminderDays = 3
            };
            await _assetRepository.CreateCategoryAsync(amenityCategory);
        }

        var newAsset = new Asset
        {
            AssetId = Guid.NewGuid(),
            CategoryId = amenityCategory.CategoryId,
            Code = code,
            Name = name,
            Location = location,
            Status = "ACTIVE",
            MaintenanceFrequency = amenityCategory.MaintenanceFrequency,
            IsDelete = false
        };

        var createdAsset = await _assetRepository.CreateAssetAsync(newAsset);
        _logger.LogInformation("Auto-created asset {AssetId} for amenity {AmenityCode}", createdAsset.AssetId, code);
        return createdAsset.AssetId;
    }

    private async Task<AmenityDto?> MapAmenityWithMaintenanceAsync(Amenity? amenity)
    {
        if (amenity == null)
        {
            return null;
        }

        var result = await MapAmenitiesWithMaintenanceAsync(new[] { amenity });
        return result.FirstOrDefault();
    }

    private async Task<List<AmenityDto>> MapAmenitiesWithMaintenanceAsync(IEnumerable<Amenity> amenities)
    {
        var amenityList = amenities?.ToList() ?? new List<Amenity>();
        if (!amenityList.Any())
        {
            return new List<AmenityDto>();
        }

        var dtos = amenityList.Select(a => a.ToDto()).ToList();
        var assetIds = amenityList
            .Where(a => a.AssetId.HasValue)
            .Select(a => a.AssetId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, AssetMaintenanceSchedule> activeSchedules = new();
        if (assetIds.Any())
        {
            activeSchedules = await _scheduleRepository.GetActiveMaintenanceSchedulesByAssetIdsAsync(assetIds);
        }

        foreach (var dto in dtos)
        {
            if (dto.AssetId.HasValue && activeSchedules.TryGetValue(dto.AssetId.Value, out var schedule))
            {
                dto.IsUnderMaintenance = true;
                dto.MaintenanceStart = schedule.StartTime.HasValue
                    ? schedule.StartDate.ToDateTime(schedule.StartTime.Value)
                    : schedule.StartDate.ToDateTime(TimeOnly.MinValue);
                dto.MaintenanceEnd = schedule.EndTime.HasValue
                    ? schedule.EndDate.ToDateTime(schedule.EndTime.Value)
                    : schedule.EndDate.ToDateTime(new TimeOnly(23, 59, 59));
            }
        }

        return dtos;
    }
}