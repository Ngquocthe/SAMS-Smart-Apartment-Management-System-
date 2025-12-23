using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers
{
    public static class ServicePriceMapper
    {
        public static ServicePriceResponseDto ToDto(this ServicePrice e) => new()
        {
            ServicePrices = e.ServicePrices,
            ServiceTypeId = e.ServiceTypeId,
            ServiceTypeCode = e.ServiceType?.Code,
            ServiceTypeName = e.ServiceType?.Name,
            UnitPrice = e.UnitPrice,
            EffectiveDate = e.EffectiveDate,
            EndDate = e.EndDate,
            Status = e.Status,
            CreatedBy = e.CreatedBy,
            CreatedByName = e.CreatedByNavigation is null ? null :
                          $"{e.CreatedByNavigation.User?.FirstName} {e.CreatedByNavigation.User?.LastName}".Trim(),
            ApprovedBy = e.ApprovedBy,
            ApprovedByName = e.ApprovedByNavigation is null ? null :
                          $"{e.ApprovedByNavigation.User?.FirstName} {e.ApprovedByNavigation.User?.LastName}".Trim(),
            ApprovedDate = e.ApprovedDate,
            Notes = e.Notes,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
