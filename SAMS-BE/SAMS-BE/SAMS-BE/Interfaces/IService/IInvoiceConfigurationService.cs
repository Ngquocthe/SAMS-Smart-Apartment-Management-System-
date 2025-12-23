using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService;

public interface IInvoiceConfigurationService
{
    /// <summary>
    /// L?y c?u hình hi?n t?i
    /// </summary>
    Task<InvoiceConfigurationResponseDto> GetCurrentConfigAsync();

    /// <summary>
    /// T?o ho?c c?p nh?t c?u hình
    /// </summary>
    Task<InvoiceConfigurationResponseDto> CreateOrUpdateAsync(CreateOrUpdateInvoiceConfigDto dto, string updatedBy);
}
