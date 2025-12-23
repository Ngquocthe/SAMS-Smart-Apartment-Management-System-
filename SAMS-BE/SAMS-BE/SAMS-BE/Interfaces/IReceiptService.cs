using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
  public interface IReceiptService
    {
 Task<Receipt> CreateAsync(CreateReceiptDto dto);
        Task<Receipt> GetByIdAsync(Guid id);
 Task<(IReadOnlyList<Receipt> Items, int Total)> ListAsync(ReceiptListQueryDto query);
        Task<Receipt> UpdateAsync(Guid id, UpdateReceiptDto dto);
        Task DeleteAsync(Guid id);
        
        /// <summary>
        /// T?o Receipt t? ??ng t? payment online (VietQR)
     /// </summary>
        Task<Receipt?> CreateReceiptFromPaymentAsync(Guid invoiceId, decimal amount, string paymentMethodCode, DateTime paymentDate, string? note = null);
    }
}
