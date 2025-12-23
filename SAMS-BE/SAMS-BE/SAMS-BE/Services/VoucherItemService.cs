using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Services
{
    public class VoucherItemService : IVoucherItemService
    {
        private readonly IVoucherItemRepository _repository;
        private readonly Interfaces.IRepository.IVoucherRepository _voucherRepository;
        private readonly BuildingManagementContext _context;

        public VoucherItemService(
            IVoucherItemRepository repository,
            Interfaces.IRepository.IVoucherRepository voucherRepository,
            BuildingManagementContext context)
        {
            _repository = repository;
            _voucherRepository = voucherRepository;
            _context = context;
        }

        public async Task<VoucherItemResponseDto> CreateAsync(CreateVoucherItemDto dto)
        {
            var voucher = await _voucherRepository.GetByIdForUpdateAsync(dto.VoucherId);
            if (voucher == null)
                throw new KeyNotFoundException($"Voucher with ID {dto.VoucherId} not found.");

            EnsureVoucherDraft(voucher);

            if (dto.ServiceTypeId.HasValue && !await _repository.ServiceTypeExistsAsync(dto.ServiceTypeId.Value))
                throw new KeyNotFoundException($"ServiceType with ID {dto.ServiceTypeId} not found.");

            var amount = CalculateAmount(dto.Quantity, dto.UnitPrice, dto.Amount);

            var item = new VoucherItem
            {
                VoucherItemsId = Guid.NewGuid(),
                VoucherId = dto.VoucherId,
                Description = dto.Description?.Trim(),
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                Amount = amount,
                ServiceTypeId = dto.ServiceTypeId,
                ApartmentId = dto.ApartmentId,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(item);
            await UpdateVoucherTotalAmountAsync(created.VoucherId);
            var result = await _repository.GetByIdAsync(created.VoucherItemsId);
            return MapToDto(result!);
        }

        public async Task<VoucherItemResponseDto> GetByIdAsync(Guid id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                throw new KeyNotFoundException($"VoucherItem with ID {id} not found.");
            return MapToDto(item);
        }

        public async Task<PagedResult<VoucherItemResponseDto>> ListAsync(VoucherItemListQueryDto query)
        {
            if (query.Page <= 0) query.Page = 1;
            if (query.PageSize <= 0 || query.PageSize > 200) query.PageSize = 20;

            var (items, total) = await _repository.ListAsync(query);
            var dtos = items.Select(MapToDto).ToList();

            return new PagedResult<VoucherItemResponseDto>
            {
                Items = dtos,
                TotalItems = total,
                PageNumber = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<VoucherItemResponseDto> UpdateAsync(Guid id, UpdateVoucherItemDto dto)
        {
            var item = await _repository.GetByIdForUpdateAsync(id);
            if (item == null)
                throw new KeyNotFoundException($"VoucherItem with ID {id} not found.");

            EnsureVoucherDraft(item.Voucher);

            if (dto.ServiceTypeId.HasValue && !await _repository.ServiceTypeExistsAsync(dto.ServiceTypeId.Value))
                throw new KeyNotFoundException($"ServiceType with ID {dto.ServiceTypeId} not found.");

            if (dto.Quantity.HasValue)
            {
                if (dto.Quantity.Value < 0)
                    throw new ArgumentException("Quantity cannot be negative.");
                item.Quantity = dto.Quantity;
            }
            if (dto.UnitPrice.HasValue)
            {
                if (dto.UnitPrice.Value < 0)
                    throw new ArgumentException("Unit price cannot be negative.");
                item.UnitPrice = dto.UnitPrice;
            }
            if (dto.Amount.HasValue)
            {
                if (dto.Amount.Value < 0)
                    throw new ArgumentException("Amount cannot be negative.");
                item.Amount = dto.Amount;
            }
            else if (item.Quantity.HasValue && item.UnitPrice.HasValue)
            {
                // Recalculate amount if possible
                item.Amount = item.Quantity.Value * item.UnitPrice.Value;
            }

            if (dto.Description != null)
                item.Description = dto.Description.Trim();
            if (dto.ServiceTypeId.HasValue)
                item.ServiceTypeId = dto.ServiceTypeId;
            if (dto.ApartmentId.HasValue)
                item.ApartmentId = dto.ApartmentId;

            if (!item.Amount.HasValue || item.Amount.Value <= 0)
                item.Amount = CalculateAmount(item.Quantity, item.UnitPrice, item.Amount);

            var updated = await _repository.UpdateAsync(item);
            await UpdateVoucherTotalAmountAsync(item.VoucherId);
            return MapToDto(await _repository.GetByIdAsync(updated.VoucherItemsId) ?? updated);
        }

        public async Task DeleteAsync(Guid id)
        {
            var item = await _repository.GetByIdForUpdateAsync(id);
            if (item == null)
                throw new KeyNotFoundException($"VoucherItem with ID {id} not found.");

            EnsureVoucherDraft(item.Voucher);

            var voucherId = item.VoucherId;
            await _repository.DeleteAsync(item);
            await UpdateVoucherTotalAmountAsync(voucherId);
        }

        private async Task UpdateVoucherTotalAmountAsync(Guid voucherId)
        {
            var voucher = await _voucherRepository.GetByIdForUpdateAsync(voucherId);
            if (voucher != null)
            {
                var items = await _repository.GetByVoucherIdAsync(voucherId);
                // Tính lại TotalAmount dựa trên tổng của tất cả items
                // Với schema mới, TotalAmount = tổng Amount của tất cả items
                voucher.TotalAmount = items.Sum(i => i.Amount ?? 0);
                await _voucherRepository.UpdateAsync(voucher);
            }
        }

        public async Task<List<VoucherItemResponseDto>> GetByVoucherIdAsync(Guid voucherId)
        {
            if (!await _repository.VoucherExistsAsync(voucherId))
                throw new KeyNotFoundException($"Voucher with ID {voucherId} not found.");

            var items = await _repository.GetByVoucherIdAsync(voucherId);
            return items.Select(MapToDto).ToList();
        }

        private static void EnsureVoucherDraft(Voucher voucher)
        {
            if (!VoucherHelper.CanEditOrDelete(voucher.Status))
            {
                throw new InvalidOperationException(
                    $"Cannot change voucher items when voucher status is {VoucherHelper.GetStatusDisplayName(voucher.Status)}.");
            }
        }

        private static decimal CalculateAmount(decimal? quantity, decimal? unitPrice, decimal? explicitAmount)
        {
            if (explicitAmount.HasValue)
            {
                if (explicitAmount.Value < 0)
                    throw new ArgumentException("Amount must be greater than or equal to 0.");
                return explicitAmount.Value;
            }

            if (quantity.HasValue && unitPrice.HasValue)
            {
                if (quantity.Value < 0)
                    throw new ArgumentException("Quantity must be greater than or equal to 0.");
                if (unitPrice.Value < 0)
                    throw new ArgumentException("Unit price must be greater than or equal to 0.");

                return quantity.Value * unitPrice.Value;
            }

            throw new ArgumentException("Amount must be greater than or equal to 0 or provide valid Quantity & UnitPrice.");
        }

        private static VoucherItemResponseDto MapToDto(VoucherItem item)
        {
            return new VoucherItemResponseDto
            {
                VoucherItemsId = item.VoucherItemsId,
                ServiceTypeId = item.ServiceTypeId,
                ServiceTypeName = item.ServiceType?.Name ?? string.Empty,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Amount
            };
        }
    }
}