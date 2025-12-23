using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<(IReadOnlyList<Invoice> Items, int Total)> ListAsync(InvoiceListQueryDto query);
        Task<Invoice> CreateAsync(Invoice invoice);
        Task<Invoice?> GetByIdForUpdateAsync(Guid id);
        Task<Invoice?> GetByIdAsync(Guid id); // ✅ Add this for status check
        Task<Invoice> UpdateAsync(Invoice entity);
        Task<Invoice?> GetByIdWithDetailsAsync(Guid id);
        
        // ✅ NEW: For automatic invoice generation
        Task<Invoice?> CheckExistingMonthlyInvoiceAsync(Guid apartmentId, int year, int month);
        Task<Invoice> CreateWithDetailsAsync(Invoice invoice, List<InvoiceDetail> details);
        
        // ✅ NEW: For overdue check
      Task<List<Invoice>> GetOverdueInvoicesAsync(DateOnly today);
    
        // ✅ NEW: For delete
        Task DeleteAsync(Guid id);

        // ✅ NEW: Get invoices by apartment and statuses (include details)
        Task<List<Invoice>> GetByApartmentAndStatusesWithDetailsAsync(Guid apartmentId, IEnumerable<string> statuses);

        // ✅ NEW: Get invoices by user and statuses (via resident -> apartment), include details
        Task<List<Invoice>> GetByUserAndStatusesWithDetailsAsync(Guid userId, IEnumerable<string> statuses);
        Task<InvoiceDetail> AddInvoiceDetailAsync(InvoiceDetail detail);
        Task<List<(Guid InvoiceId, string InvoiceNo, decimal Amount)>> GetInvoicesByTicketAsync(Guid ticketId);
        Task<Guid?> GetInvoiceIdByTicketAsync(Guid ticketId);
        Task<InvoiceDetail?> GetDetailByIdAsync(Guid detailId);
        Task<InvoiceDetail> UpdateInvoiceDetailAsync(InvoiceDetail detail);
    }
}
