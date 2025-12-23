using System.Security.Claims;
using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces
{
    public interface IInvoiceService
    {
        Task<PagedResult<InvoiceResponseDto>> ListAsync(InvoiceListQueryDto query);
        Task<InvoiceResponseDto> CreateAsync(CreateInvoiceDto dto);
        Task<InvoiceResponseDto> UpdateAsync(Guid id, UpdateInvoiceDto dto);
        Task<InvoiceResponseDto> GetByIdAsync(Guid id);
        Task<InvoiceResponseDto> UpdateStatusAsync(Guid id, UpdateInvoiceStatusDto dto);

        // Automatic invoice generation (the core generation method)
        Task<List<InvoiceResponseDto>> GenerateMonthlyFixedFeeInvoicesAsync(int year, int month, string createdBy = "SYSTEM");

        // Automatic invoice generation driven by configuration (used by Hangfire job)
        Task<List<InvoiceResponseDto>> RunConfiguredMonthlyGenerationAsync();

        // Auto update overdue invoices
        Task<int> UpdateOverdueInvoicesAsync();

        // Delete draft invoice
        Task DeleteAsync(Guid id);

        // Get current user's invoices (ISSUED, PAID, OVERDUE) with details
        Task<List<InvoiceResponseDto>> GetMyInvoicesAsync(ClaimsPrincipal user);
        
        // Ticket context methods
        Task<(Guid InvoiceId, string InvoiceNo)> CreateAsyncInvoice(CreateInvoiceRequest request);
        Task<List<FinanceItemSummaryDto>> GetByTicketAsync(Guid ticketId);
        Task<InvoiceResponseDto?> GetByIdAsyncInvoice(Guid invoiceId);
        Task<InvoiceDetailResponseDto?> GetDetailByIdAsync(Guid detailId);
        Task<bool> UpdateDetailAsync(Guid detailId, UpdateInvoiceDetailDto dto);
    }
}
