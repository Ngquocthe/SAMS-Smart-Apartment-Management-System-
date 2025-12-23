using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IService;

public interface IVoucherService
{
    // Methods from ticket management
    Task<(Guid VoucherId, string VoucherNumber)> CreateFromTicketAsync(CreateVoucherRequest request);
    Task<List<FinanceItemSummaryDto>> GetByTicketAsync(Guid ticketId);
    
    // Methods from maintenance history management
    Task<(Guid VoucherId, string VoucherNumber)> CreateFromMaintenanceAsync(CreateVoucherFromMaintenanceRequest request);
    Task<Guid?> GetVoucherIdByHistoryAsync(Guid historyId);
    Task<(Guid Id, string Name)?> GetDefaultMaintenanceServiceTypeAsync();
    Task<VoucherDto?> GetByIdAsync(Guid voucherId);
    Task<VoucherItemResponseDto?> GetItemByIdAsync(Guid itemId);
    Task<bool> UpdateItemAsync(Guid itemId, UpdateVoucherItemDto dto);

    // Methods from voucher management
    Task<VoucherDto> CreateAsync(CreateVoucherDto dto);
    Task<PagedResult<VoucherDto>> ListAsync(VoucherListQueryDto query);
    Task<VoucherDto> UpdateAsync(Guid id, UpdateVoucherDto dto);
    Task<VoucherDto> UpdateStatusAsync(Guid id, UpdateVoucherStatusDto dto);
    Task DeleteAsync(Guid id);
}


