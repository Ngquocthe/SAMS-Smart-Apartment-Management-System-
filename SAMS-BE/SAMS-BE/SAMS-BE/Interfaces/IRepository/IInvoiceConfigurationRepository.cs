using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface IInvoiceConfigurationRepository
{
    /// <summary>
    /// L?y c?u hình hi?n t?i (ch? có 1 record duy nh?t)
  /// </summary>
    Task<InvoiceConfiguration?> GetCurrentConfigAsync();

    /// <summary>
    /// T?o c?u hình m?i
    /// </summary>
    Task<InvoiceConfiguration> CreateAsync(InvoiceConfiguration config);

    /// <summary>
    /// C?p nh?t c?u hình
    /// </summary>
    Task<InvoiceConfiguration> UpdateAsync(InvoiceConfiguration config);

    /// <summary>
    /// Ki?m tra xem ?ã có c?u hình ch?a
    /// </summary>
    Task<bool> ExistsAsync();
}
