using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
    public interface IVoucherItemService
    {
        Task<VoucherItemResponseDto> CreateAsync(CreateVoucherItemDto dto);
        Task<VoucherItemResponseDto> GetByIdAsync(Guid id);
        Task<PagedResult<VoucherItemResponseDto>> ListAsync(VoucherItemListQueryDto query);
        Task<VoucherItemResponseDto> UpdateAsync(Guid id, UpdateVoucherItemDto dto);
        Task DeleteAsync(Guid id);
        Task<List<VoucherItemResponseDto>> GetByVoucherIdAsync(Guid voucherId);
    }
}