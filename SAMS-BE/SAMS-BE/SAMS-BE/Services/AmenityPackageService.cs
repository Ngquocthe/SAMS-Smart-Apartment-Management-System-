using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Mappers;

namespace SAMS_BE.Services;

public class AmenityPackageService : IAmenityPackageService
{
    private readonly IAmenityPackageRepository _packageRepository;
    private readonly IAmenityRepository _amenityRepository;
    private readonly ILogger<AmenityPackageService> _logger;

    public AmenityPackageService(
        IAmenityPackageRepository packageRepository,
        IAmenityRepository amenityRepository,
        ILogger<AmenityPackageService> logger)
    {
        _packageRepository = packageRepository;
        _amenityRepository = amenityRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<AmenityPackageDto>> GetAllPackagesAsync()
    {
        try
        {
            var packages = await _packageRepository.GetAllPackagesAsync();
            return packages.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all packages");
            throw;
        }
    }

    public async Task<AmenityPackageDto?> GetPackageByIdAsync(Guid packageId)
    {
        try
        {
            var package = await _packageRepository.GetPackageByIdAsync(packageId);
            return package?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting package by ID: {PackageId}", packageId);
            throw;
        }
    }

    public async Task<IEnumerable<AmenityPackageDto>> GetPackagesByAmenityIdAsync(Guid amenityId)
    {
        try
        {
            var packages = await _packageRepository.GetPackagesByAmenityIdAsync(amenityId);
            return packages.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting packages by amenity ID: {AmenityId}", amenityId);
            throw;
        }
    }

    public async Task<IEnumerable<AmenityPackageDto>> GetPackagesByStatusAsync(string status)
    {
        try
        {
            var packages = await _packageRepository.GetPackagesByStatusAsync(status);
            return packages.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting packages by status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<AmenityPackageDto>> GetActivePackagesByAmenityIdAsync(Guid amenityId)
    {
        try
        {
            var packages = await _packageRepository.GetPackagesByAmenityIdAsync(amenityId);
            return packages.Where(p => p.Status == "ACTIVE").ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting active packages by amenity ID: {AmenityId}", amenityId);
            throw;
        }
    }

    public async Task<AmenityPackageDto> CreatePackageAsync(CreateAmenityPackageDto createPackageDto)
    {
        try
        {
            // Validate amenity exists
            var amenity = await _amenityRepository.GetAmenityByIdAsync(createPackageDto.AmenityId);
            if (amenity == null)
            {
                throw new ArgumentException($"Amenity with ID {createPackageDto.AmenityId} not found");
            }

            // Validate package logic
            ValidatePackageData(createPackageDto.PeriodUnit, createPackageDto.MonthCount, createPackageDto.DurationDays);
            
            // Check for duplicate package
            await ValidateDuplicatePackageAsync(createPackageDto.AmenityId, createPackageDto.PeriodUnit, createPackageDto.MonthCount, createPackageDto.DurationDays);
            
            // Validate price comparison between monthly and daily packages
            await ValidatePackagePricesAsync(createPackageDto.AmenityId, createPackageDto.PeriodUnit, createPackageDto.Price);

            var package = createPackageDto.ToEntity();
            var createdPackage = await _packageRepository.CreatePackageAsync(package);
            return createdPackage.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating package");
            throw;
        }
    }

    public async Task<AmenityPackageDto?> UpdatePackageAsync(UpdateAmenityPackageDto updatePackageDto, Guid packageId)
    {
        try
        {
            var existingPackage = await _packageRepository.GetPackageByIdAsync(packageId);
            if (existingPackage == null)
            {
                return null;
            }

            // Validate package logic
            ValidatePackageData(updatePackageDto.PeriodUnit, updatePackageDto.MonthCount, updatePackageDto.DurationDays);
            
            // Validate price comparison between monthly and daily packages
            await ValidatePackagePricesAsync(existingPackage.AmenityId, updatePackageDto.PeriodUnit, updatePackageDto.Price);

            var package = updatePackageDto.ToEntity(packageId, existingPackage.AmenityId);
            var updatedPackage = await _packageRepository.UpdatePackageAsync(package);
            return updatedPackage?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating package: {PackageId}", packageId);
            throw;
        }
    }

    /// <summary>
    /// Validate package data logic:
    /// - If PeriodUnit = "Day", DurationDays must have value > 0
    /// - If PeriodUnit = "Month" or null, MonthCount must be > 0
    /// </summary>
    private void ValidatePackageData(string? periodUnit, int monthCount, int? durationDays)
    {
        if (periodUnit == "Day")
        {
            if (!durationDays.HasValue || durationDays.Value <= 0)
            {
                throw new ArgumentException("DurationDays must be greater than 0 when PeriodUnit is 'Day'");
            }
            if (monthCount > 0)
            {
                throw new ArgumentException("MonthCount should be 0 when PeriodUnit is 'Day'");
            }
        }
        else if (periodUnit == "Month" || string.IsNullOrEmpty(periodUnit))
        {
            if (monthCount <= 0)
            {
                throw new ArgumentException("MonthCount must be greater than 0 when PeriodUnit is 'Month' or null");
            }
            if (durationDays.HasValue && durationDays.Value > 0)
            {
                throw new ArgumentException("DurationDays should be null or 0 when PeriodUnit is 'Month'");
            }
        }
        else if (!string.IsNullOrEmpty(periodUnit))
        {
            throw new ArgumentException("PeriodUnit must be either 'Day' or 'Month'");
        }
    }

    /// <summary>
    /// Validate that monthly package prices are higher than daily package prices for the same amenity
    /// </summary>
    private async Task ValidatePackagePricesAsync(Guid amenityId, string? periodUnit, int price)
    {
        if (periodUnit == "Month")
        {
            // Get all daily packages for this amenity
            var allPackages = await _packageRepository.GetPackagesByAmenityIdAsync(amenityId);
            var dailyPackages = allPackages.Where(p => p.PeriodUnit == "Day" && p.Status == "ACTIVE");
            
            if (dailyPackages.Any())
            {
                var maxDailyPrice = dailyPackages.Max(p => p.Price);
                if (price <= maxDailyPrice)
                {
                    throw new ArgumentException($"Monthly package price ({price} VNĐ) must be higher than the highest daily package price ({maxDailyPrice} VNĐ)");
                }
            }
        }
    }

    /// <summary>
    /// Validate that no duplicate package exists for the same amenity with same periodUnit and monthCount/durationDays
    /// </summary>
    private async Task ValidateDuplicatePackageAsync(Guid amenityId, string? periodUnit, int monthCount, int? durationDays)
    {
        var existingPackages = await _packageRepository.GetPackagesByAmenityIdAsync(amenityId);
        
        if (periodUnit == "Day")
        {
            // Check for duplicate day package
            var duplicateDayPackage = existingPackages.FirstOrDefault(p => 
                p.PeriodUnit == "Day" && 
                p.DurationDays.HasValue && 
                p.DurationDays.Value == durationDays.Value &&
                p.Status == "ACTIVE");
            
            if (duplicateDayPackage != null)
            {
                throw new ArgumentException($"Đã tồn tại gói {durationDays} ngày cho tiện ích này. Vui lòng cập nhật gói hiện có thay vì tạo mới.");
            }
        }
        else if (periodUnit == "Month" || string.IsNullOrEmpty(periodUnit))
        {
            // Check for duplicate month package
            var duplicateMonthPackage = existingPackages.FirstOrDefault(p => 
                (p.PeriodUnit == "Month" || string.IsNullOrEmpty(p.PeriodUnit)) && 
                p.MonthCount == monthCount &&
                p.Status == "ACTIVE");
            
            if (duplicateMonthPackage != null)
            {
                throw new ArgumentException($"Đã tồn tại gói {monthCount} tháng cho tiện ích này. Vui lòng cập nhật gói hiện có thay vì tạo mới.");
            }
        }
    }

    public async Task<bool> DeletePackageAsync(Guid packageId)
    {
        try
        {
            return await _packageRepository.DeletePackageAsync(packageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting package: {PackageId}", packageId);
            throw;
        }
    }
}

