// Interfaces/IServicePriceService.cs
using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces
{
    public interface IServicePriceService
    {
        Task<PagedResult<ServicePriceResponseDto>> ListAsync(Guid serviceTypeId, ServicePriceListQueryDto query);
        Task<ServicePriceResponseDto> CreateAsync(Guid serviceTypeId, CreateServicePriceDto dto, bool autoClosePrevious = true);
        Task<ServicePriceResponseDto?> UpdateAsync(Guid priceId, UpdateServicePriceDto dto);
        Task<bool> CancelAsync(Guid priceId);
        Task<decimal?> GetCurrentPriceAsync(Guid serviceTypeId, DateOnly? asOfDate = null);
    }
}
