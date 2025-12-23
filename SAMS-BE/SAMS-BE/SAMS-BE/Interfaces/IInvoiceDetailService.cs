using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces
{
    public interface IInvoiceDetailService
 {
      Task<InvoiceDetailResponseDto> CreateAsync(CreateInvoiceDetailDto dto);
 Task<InvoiceDetailResponseDto> GetByIdAsync(Guid id);
   Task<PagedResult<InvoiceDetailResponseDto>> ListAsync(InvoiceDetailListQueryDto query);
     Task<InvoiceDetailResponseDto> UpdateAsync(Guid id, UpdateInvoiceDetailDto dto);
      Task DeleteAsync(Guid id);
        Task<List<InvoiceDetailResponseDto>> GetByInvoiceIdAsync(Guid invoiceId);
    }
}
