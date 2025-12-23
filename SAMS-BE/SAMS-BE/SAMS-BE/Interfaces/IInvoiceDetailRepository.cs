using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
    public interface IInvoiceDetailRepository
    {
      Task<(IReadOnlyList<InvoiceDetail> Items, int Total)> ListAsync(InvoiceDetailListQueryDto query);
  Task<InvoiceDetail> CreateAsync(InvoiceDetail detail);
     Task<InvoiceDetail?> GetByIdAsync(Guid id);
    Task<InvoiceDetail?> GetByIdForUpdateAsync(Guid id);
  Task<InvoiceDetail> UpdateAsync(InvoiceDetail entity);
     Task DeleteAsync(InvoiceDetail entity);
        Task<bool> InvoiceExistsAsync(Guid invoiceId);
        Task<bool> ServiceExistsAsync(Guid serviceId);
        Task<List<InvoiceDetail>> GetByInvoiceIdAsync(Guid invoiceId);
    }
}
