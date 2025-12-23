using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
    public interface IReceiptRepository
{
        Task<(IReadOnlyList<Receipt> Items, int Total)> ListAsync(ReceiptListQueryDto query);
        Task<Receipt> CreateAsync(Receipt receipt);
        Task<Receipt?> GetByIdForUpdateAsync(Guid id);
        Task<Receipt?> GetByIdAsync(Guid id);
 Task<Receipt> UpdateAsync(Receipt entity);
        Task DeleteAsync(Guid id);
  Task<bool> InvoiceExistsAsync(Guid invoiceId);
        Task<bool> PaymentMethodExistsAsync(Guid methodId);
        Task<Receipt?> GetByInvoiceIdAsync(Guid invoiceId);
    }
}
