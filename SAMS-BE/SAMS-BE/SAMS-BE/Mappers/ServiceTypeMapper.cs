using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class ServiceTypeMapper
{
    public static ServiceType ToEntity(this CreateServiceTypeDto dto)
    {
        return new ServiceType
        {
            ServiceTypeId = Guid.NewGuid(),
            Code = dto.Code.Trim().ToUpper(),
            Name = dto.Name.Trim(),
            CategoryId = dto.CategoryId,
            Unit = dto.Unit?.Trim(),
            IsMandatory = dto.IsMandatory,
            IsRecurring = dto.IsRecurring,
            IsActive = true,
            IsDelete = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    public static ServiceTypeResponseDto ToDto(this ServiceType entity)
    {
        return new ServiceTypeResponseDto
        {
            ServiceTypeId = entity.ServiceTypeId,
            Code = entity.Code,
            Name = entity.Name,
            CategoryId = entity.CategoryId,
            CategoryName = entity.Category?.Name ?? string.Empty,
            Unit = entity.Unit,
            IsMandatory = entity.IsMandatory,
            IsRecurring = entity.IsRecurring,
            IsActive = entity.IsActive,
            IsDelete = entity.IsDelete ?? false,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static List<ServiceTypeResponseDto> ToDto(this IEnumerable<ServiceType> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }
}