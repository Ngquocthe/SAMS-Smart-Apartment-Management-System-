using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface IVoucherRepository
{
    // Methods from ticket management
    Task<Voucher> AddVoucherAsync(Voucher voucher);
    Task<VoucherItem> AddVoucherItemAsync(VoucherItem item);
    Task<List<(Guid VoucherId, string VoucherNumber, decimal Amount)>> GetVouchersByTicketAsync(Guid ticketId);
    Task<Guid?> GetVoucherIdByTicketAsync(Guid ticketId);
    Task<Guid?> GetVoucherIdByHistoryAsync(Guid historyId);
    Task<Voucher?> GetByIdAsync(Guid voucherId);
    Task<VoucherItem?> GetItemByIdAsync(Guid itemId);
    Task<Voucher> UpdateVoucherAsync(Voucher voucher);
    Task<VoucherItem> UpdateVoucherItemAsync(VoucherItem item);

    // Methods from voucher management
    Task<Voucher> CreateAsync(Voucher voucher);
    Task<(IReadOnlyList<Voucher> Items, int Total)> ListAsync(VoucherListQueryDto query);
    Task<Voucher?> GetByIdForUpdateAsync(Guid id);
    Task<Voucher> UpdateAsync(Voucher entity);
    Task DeleteAsync(Guid id);
}


