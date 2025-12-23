using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
    public interface IVoucherService
    {
        Task<Voucher> CreateAsync(CreateVoucherDto dto);
        Task<Voucher> GetByIdAsync(Guid id);
        Task<(IReadOnlyList<Voucher> Items, int Total)> ListAsync(VoucherListQueryDto query);
        Task<Voucher> UpdateAsync(Guid id, UpdateVoucherDto dto);
        Task<Voucher> UpdateStatusAsync(Guid id, UpdateVoucherStatusDto dto);
        Task DeleteAsync(Guid id);
    }
}

