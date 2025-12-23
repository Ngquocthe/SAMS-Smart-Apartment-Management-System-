using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class AssetMapper
{
    /// <summary>
    /// Map từ Asset entity sang AssetDto
    /// </summary>
    public static AssetDto ToDto(this Asset entity)
    {
        return new AssetDto
        {
            AssetId = entity.AssetId,
            CategoryId = entity.CategoryId,
            Code = entity.Code,
            Name = entity.Name,
            ApartmentId = entity.ApartmentId,
            BlockId = entity.BlockId,
            Location = entity.Location,
            PurchaseDate = entity.PurchaseDate,
            WarrantyExpire = entity.WarrantyExpire,
            MaintenanceFrequency = entity.MaintenanceFrequency,
            Status = entity.Status,
            IsDelete = entity.IsDelete,
            Category = entity.Category?.ToDto(),
            ApartmentNumber = entity.Apartment?.Number
        };
    }

    /// <summary>
    /// Map từ CreateAssetDto sang Asset entity
    /// Note: CategoryId in DTO is now a string (code), needs to be resolved to Guid
    /// </summary>
    public static Asset ToEntity(this CreateAssetDto dto, Guid categoryGuid)
    {
        return new Asset
        {
            AssetId = Guid.NewGuid(),
            CategoryId = categoryGuid, // Use resolved GUID
            Code = dto.Code,
            Name = dto.Name,
            ApartmentId = dto.ApartmentId,
            BlockId = dto.BlockId,
            Location = dto.Location,
            PurchaseDate = dto.PurchaseDate,
            WarrantyExpire = dto.WarrantyExpire,
            MaintenanceFrequency = dto.MaintenanceFrequency,
            Status = dto.Status
        };
    }

    /// <summary>
    /// Map từ UpdateAssetDto sang Asset entity
    /// Note: CategoryId in DTO is now a string (code), needs to be resolved to Guid
    /// </summary>
    public static Asset ToEntity(this UpdateAssetDto dto, Guid assetId, Guid categoryGuid)
    {
        return new Asset
        {
            AssetId = assetId,
            CategoryId = categoryGuid, // Use resolved GUID
            Code = dto.Code,
            Name = dto.Name,
            ApartmentId = dto.ApartmentId,
            BlockId = dto.BlockId,
            Location = dto.Location,
            PurchaseDate = dto.PurchaseDate,
            WarrantyExpire = dto.WarrantyExpire,
            MaintenanceFrequency = dto.MaintenanceFrequency,
            Status = dto.Status
        };
    }

    /// <summary>
    /// Map collection từ Asset entities sang AssetDto list
    /// </summary>
    public static IEnumerable<AssetDto> ToDto(this IEnumerable<Asset> entities)
    {
        return entities.Select(e => e.ToDto());
    }

    /// <summary>
    /// Map từ AssetCategory entity sang AssetCategoryDto
    /// </summary>
    public static AssetCategoryDto ToDto(this AssetCategory entity)
    {
        return new AssetCategoryDto
        {
            CategoryId = entity.CategoryId,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            MaintenanceFrequency = entity.MaintenanceFrequency,
            DefaultReminderDays = entity.DefaultReminderDays
        };
    }

    /// <summary>
    /// Map từ AssetCategoryDto sang AssetCategory entity
    /// </summary>
    public static AssetCategory ToEntity(this AssetCategoryDto dto)
    {
        return new AssetCategory
        {
            CategoryId = dto.CategoryId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            MaintenanceFrequency = dto.MaintenanceFrequency,
            DefaultReminderDays = dto.DefaultReminderDays
        };
    }

    /// <summary>
    /// Map collection từ AssetCategory entities sang AssetCategoryDto list
    /// </summary>
    public static IEnumerable<AssetCategoryDto> ToDto(this IEnumerable<AssetCategory> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}

