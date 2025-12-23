using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class AmenityPackageMapper
{
    /// <summary>
    /// Map từ AmenityPackage entity sang AmenityPackageDto
    /// </summary>
    public static AmenityPackageDto ToDto(this AmenityPackage entity)
    {
        return new AmenityPackageDto
        {
            PackageId = entity.PackageId,
            AmenityId = entity.AmenityId,
            Name = entity.Name,
            MonthCount = entity.MonthCount,
            DurationDays = entity.DurationDays,
            PeriodUnit = entity.PeriodUnit,
            Price = entity.Price,
            Description = entity.Description,
            Status = entity.Status
        };
    }

    /// <summary>
    /// Map từ CreateAmenityPackageDto sang AmenityPackage entity
    /// </summary>
    public static AmenityPackage ToEntity(this CreateAmenityPackageDto dto)
    {
        return new AmenityPackage
        {
            PackageId = Guid.NewGuid(),
            AmenityId = dto.AmenityId,
            Name = dto.Name,
            MonthCount = dto.MonthCount,
            DurationDays = dto.DurationDays,
            PeriodUnit = dto.PeriodUnit,
            Price = dto.Price,
            Description = dto.Description,
            Status = dto.Status
        };
    }

    /// <summary>
    /// Map từ UpdateAmenityPackageDto sang AmenityPackage entity
    /// </summary>
    public static AmenityPackage ToEntity(this UpdateAmenityPackageDto dto, Guid packageId, Guid amenityId)
    {
        return new AmenityPackage
        {
            PackageId = packageId,
            AmenityId = amenityId,
            Name = dto.Name,
            MonthCount = dto.MonthCount,
            DurationDays = dto.DurationDays,
            PeriodUnit = dto.PeriodUnit,
            Price = dto.Price,
            Description = dto.Description,
            Status = dto.Status
        };
    }

    /// <summary>
    /// Map collection từ AmenityPackage entities sang AmenityPackageDto list
    /// </summary>
    public static IEnumerable<AmenityPackageDto> ToDto(this IEnumerable<AmenityPackage> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}

