using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
    public interface IVoucherItemRepository
    {
        Task<(IReadOnlyList<VoucherItem> Items, int Total)> ListAsync(VoucherItemListQueryDto query);
 Task<VoucherItem> CreateAsync(VoucherItem item);
        Task<VoucherItem?> GetByIdAsync(Guid id);
    Task<VoucherItem?> GetByIdForUpdateAsync(Guid id);
        Task<VoucherItem> UpdateAsync(VoucherItem entity);
        Task DeleteAsync(VoucherItem entity);
     Task<bool> VoucherExistsAsync(Guid voucherId);
  Task<bool> ServiceTypeExistsAsync(Guid serviceTypeId);
        Task<List<VoucherItem>> GetByVoucherIdAsync(Guid voucherId);
    }
}
