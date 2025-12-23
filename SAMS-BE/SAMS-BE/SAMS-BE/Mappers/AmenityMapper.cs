using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class AmenityMapper
{
    /// <summary>
    /// Map từ Amenity entity sang AmenityDto
    /// </summary>
    public static AmenityDto ToDto(this Amenity entity)
    {
        return new AmenityDto
        {
            AmenityId = entity.AmenityId,
            AssetId = entity.AssetId,
            Code = entity.Code,
            Name = entity.Name,
            CategoryName = entity.CategoryName,
            Location = entity.Location,
            HasMonthlyPackage = entity.HasMonthlyPackage,
            FeeType = entity.FeeType,
            Status = entity.Status,
            RequiresFaceVerification = entity.RequiresFaceVerification,
            Packages = entity.AmenityPackages?.Select(p => p.ToDto()).ToList()
        };
    }

    /// <summary>
    /// Map từ CreateAmenityDto sang Amenity entity
    /// </summary>
    public static Amenity ToEntity(this CreateAmenityDto dto)
    {
        return new Amenity
        {
            AmenityId = Guid.NewGuid(),
            AssetId = dto.AssetId,
            Code = dto.Code,
            Name = dto.Name,
            CategoryName = dto.CategoryName,
            Location = dto.Location,
            HasMonthlyPackage = dto.HasMonthlyPackage,
            RequiresFaceVerification = dto.RequiresFaceVerification,
            FeeType = dto.FeeType,
            Status = dto.Status
        };
    }

    /// <summary>
    /// Map từ UpdateAmenityDto sang Amenity entity
    /// </summary>
    public static Amenity ToEntity(this UpdateAmenityDto dto, Guid amenityId)
    {
        return new Amenity
        {
            AmenityId = amenityId,
            AssetId = dto.AssetId,
            Code = dto.Code,
            Name = dto.Name,
            CategoryName = dto.CategoryName,
            Location = dto.Location,
            HasMonthlyPackage = dto.HasMonthlyPackage,
            RequiresFaceVerification = dto.RequiresFaceVerification,
            FeeType = dto.FeeType,
            Status = dto.Status
        };
    }

    /// <summary>
    /// Map collection từ Amenity entities sang AmenityDto list
    /// </summary>
    public static IEnumerable<AmenityDto> ToDto(this IEnumerable<Amenity> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}

