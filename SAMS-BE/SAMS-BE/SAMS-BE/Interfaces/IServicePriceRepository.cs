using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
    public interface IServicePriceRepository
    {
        Task<(IReadOnlyList<ServicePrice> Items, int Total)> ListAsync (Guid serviceTypeId, ServicePriceListQueryDto query);
        Task<ServicePrice?> GetByIdAsync (Guid priceId);
        Task<ServicePrice?> GetOpenEndedAsync(Guid serviceTypeId);
        Task<bool> AnyOverlapAsync(Guid serviceTypeId, DateOnly start, DateOnly? end, Guid? excludeId = null);
        Task AddAsync (ServicePrice entity);
        Task UpdateAsync(ServicePrice entity);
        Task<ServicePrice?> GetCurrentPriceAsync(Guid serviceTypeId, DateOnly effectiveDate);
        Task<ServicePrice?> GetLatestPriceAsync(Guid serviceTypeId);
    }
}
