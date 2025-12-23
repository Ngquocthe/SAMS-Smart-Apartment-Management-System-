using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
    public interface IVoucherRepository
    {
        Task<(IReadOnlyList<Voucher> Items, int Total)> ListAsync(VoucherListQueryDto query);
        Task<Voucher> CreateAsync(Voucher voucher);
        Task<Voucher?> GetByIdForUpdateAsync(Guid id);
        Task<Voucher?> GetByIdAsync(Guid id);
        Task<Voucher?> GetByIdWithItemsAsync(Guid id);
        Task<Voucher> UpdateAsync(Voucher entity);
        Task DeleteAsync(Guid id);
    }
}

