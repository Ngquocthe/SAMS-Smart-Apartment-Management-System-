using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Mappers;
using SAMS_BE.Models;
using SAMS_BE.Helpers;

namespace SAMS_BE.Services
{
    public class InvoiceDetailService : IInvoiceDetailService
    {
        private readonly IInvoiceDetailRepository _repository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly BuildingManagementContext _context;
        private readonly ILogger<InvoiceDetailService> _logger;

        public InvoiceDetailService(
            IInvoiceDetailRepository repository,
            IInvoiceRepository invoiceRepository,
            BuildingManagementContext context,
            ILogger<InvoiceDetailService> logger)
        {
            _repository = repository;
            _invoiceRepository = invoiceRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<InvoiceDetailResponseDto> CreateAsync(CreateInvoiceDetailDto dto)
        {
            try
            {
                // Validate invoice exists
                if (!await _repository.InvoiceExistsAsync(dto.InvoiceId))
                {
                    throw new KeyNotFoundException($"Invoice with ID {dto.InvoiceId} not found.");
                }

                // Validate service exists and is active
                if (!await _repository.ServiceExistsAsync(dto.ServiceId))
                {
                    throw new KeyNotFoundException($"Service with ID {dto.ServiceId} not found or is inactive.");
                }

                // Validate quantity
                if (dto.Quantity <= 0)
                {
                    throw new ArgumentException("Quantity must be greater than 0.", nameof(dto.Quantity));
                }

                // ? AUTO-POPULATE: L?y gi� t? ServicePrice
                var unitPrice = await ServicePriceHelper.GetCurrentPriceAsync(_context, dto.ServiceId);

                if (!unitPrice.HasValue)
                {
                    throw new InvalidOperationException(
                        $"No active price found for service {dto.ServiceId}. Please configure service price in ServicePrice table first.");
                }

                // T?o entity v?i gi� t? ServicePrice (SNAPSHOT)
                var detail = new InvoiceDetail
                {
                    InvoiceDetailId = Guid.NewGuid(),
                    InvoiceId = dto.InvoiceId,
                    ServiceId = dto.ServiceId,
                    Description = dto.Description?.Trim(),
                    Quantity = dto.Quantity,
                    UnitPrice = unitPrice.Value,  // ? SNAPSHOT gi� t? ServicePrice
                    VatRate = dto.VatRate
                    // TicketId đã được chuyển lên Invoice, không set vào InvoiceDetail nữa
                };

                // Calculate amounts
                detail.Amount = detail.Quantity * detail.UnitPrice;
                detail.VatAmount = detail.VatRate.HasValue
                    ? detail.Amount * (detail.VatRate.Value / 100)
                    : 0;

                // Create detail
                var created = await _repository.CreateAsync(detail);

                // Update invoice totals
                await UpdateInvoiceTotalsAsync(created.InvoiceId);

                _logger.LogInformation(
                    "Invoice detail created with ID {DetailId} for Invoice {InvoiceId}. " +
                    "Unit price {UnitPrice} was auto-populated from ServicePrice table.",
                    created.InvoiceDetailId, created.InvoiceId, unitPrice.Value);

                // Reload with navigation properties
                var result = await _repository.GetByIdAsync(created.InvoiceDetailId);
                return result!.ToDto();
            }
            catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException and not InvalidOperationException)
            {
                _logger.LogError(ex, "Error creating invoice detail for Invoice {InvoiceId}", dto.InvoiceId);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var detail = await _repository.GetByIdForUpdateAsync(id);

                if (detail == null)
                {
                    _logger.LogWarning("Invoice detail with ID {DetailId} not found for deletion", id);
                    throw new KeyNotFoundException($"Invoice detail with ID {id} not found.");
                }

                var invoice = await _invoiceRepository.GetByIdAsync(detail.InvoiceId)
                              ?? throw new KeyNotFoundException($"Invoice with ID {detail.InvoiceId} not found.");

                if (!string.Equals(invoice.Status, "DRAFT", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Chỉ có thể xóa InvoiceDetail khi Invoice status = DRAFT. Hiện tại: {invoice.Status}");
                }

                // Kiểm tra ticket từ Invoice thay vì InvoiceDetail
                if (invoice.TicketId.HasValue)
                {
                    var ticket = await _context.Tickets
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.TicketId == invoice.TicketId.Value);

                    if (ticket != null && string.Equals(ticket.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Ticket đã CLOSED, không thể xóa chi tiết hóa đơn.");
                    }
                }

                var invoiceId = detail.InvoiceId;
                await _repository.DeleteAsync(detail);

                // Update invoice totals
                await UpdateInvoiceTotalsAsync(invoiceId);

                _logger.LogInformation("Invoice detail {DetailId} deleted successfully from Invoice {InvoiceId}",
                    id, invoiceId);
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                _logger.LogError(ex, "Error deleting invoice detail with ID: {DetailId}", id);
                throw;
            }
        }

        public async Task<InvoiceDetailResponseDto> GetByIdAsync(Guid id)
        {
            try
            {
                var detail = await _repository.GetByIdAsync(id);

                if (detail == null)
                {
                    _logger.LogWarning("Invoice detail with ID {DetailId} not found", id);
                    throw new KeyNotFoundException($"Invoice detail with ID {id} not found.");
                }

                return detail.ToDto();
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                _logger.LogError(ex, "Error retrieving invoice detail with ID: {DetailId}", id);
                throw;
            }
        }

        public async Task<List<InvoiceDetailResponseDto>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            try
            {
                var details = await _repository.GetByInvoiceIdAsync(invoiceId);
                return details.ToDtoList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice details for Invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<PagedResult<InvoiceDetailResponseDto>> ListAsync(InvoiceDetailListQueryDto query)
        {
            try
            {
                // Validate and normalize query parameters
                if (query.Page < 1) query.Page = 1;
                if (query.PageSize < 1 || query.PageSize > 200) query.PageSize = 20;

                // Get data from repository
                var (items, total) = await _repository.ListAsync(query);

                // Map to DTOs
                var dtos = items.ToDtoList();

                return new PagedResult<InvoiceDetailResponseDto>
                {
                    Items = dtos,
                    TotalItems = total,
                    PageNumber = query.Page,
                    PageSize = query.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing invoice details with query: {@Query}", query);
                throw;
            }
        }

        public async Task<InvoiceDetailResponseDto> UpdateAsync(Guid id, UpdateInvoiceDetailDto dto)
        {
            try
            {
                // Get existing detail
                var detail = await _repository.GetByIdForUpdateAsync(id);

                if (detail == null)
                {
                    _logger.LogWarning("Invoice detail with ID {DetailId} not found for update", id);
                    throw new KeyNotFoundException($"Invoice detail with ID {id} not found.");
                }

                // Kiểm tra invoice status - chỉ cho phép edit khi status là DRAFT
                var invoice = await _invoiceRepository.GetByIdAsync(detail.InvoiceId);
                if (invoice == null)
                {
                    throw new KeyNotFoundException($"Invoice with ID {detail.InvoiceId} not found.");
                }

                if (!string.Equals(invoice.Status, "DRAFT", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Chỉ có thể chỉnh sửa InvoiceDetail khi Invoice status = DRAFT. Hiện tại: {invoice.Status}");
                }

                // ? IMMUTABLE PRICE: N?u ??i service, l?y gi� m?i t? ServicePrice
                if (dto.ServiceId.HasValue && dto.ServiceId.Value != detail.ServiceId)
                {
                    if (!await _repository.ServiceExistsAsync(dto.ServiceId.Value))
                    {
                        throw new KeyNotFoundException($"Service with ID {dto.ServiceId.Value} not found or is inactive.");
                    }

                    // L?y gi� m?i t? ServicePrice khi ??i service
                    var newPrice = await ServicePriceHelper.GetCurrentPriceAsync(_context, dto.ServiceId.Value);

                    if (!newPrice.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"No active price found for service {dto.ServiceId.Value}.");
                    }

                    detail.ServiceId = dto.ServiceId.Value;
                    detail.UnitPrice = newPrice.Value;  // ? SNAPSHOT gi� m?i

                    _logger.LogInformation(
                        "Service changed for detail {DetailId}. New unit price {NewPrice} auto-populated from ServicePrice.",
                        id, newPrice.Value);
                }

                // Update other fields
                if (dto.Quantity.HasValue)
                {
                    if (dto.Quantity.Value <= 0)
                    {
                        throw new ArgumentException("Quantity must be greater than 0.", nameof(dto.Quantity));
                    }
                    detail.Quantity = dto.Quantity.Value;
                }

                if (dto.VatRate.HasValue)
                {
                    detail.VatRate = dto.VatRate.Value;
                }

                if (dto.Description != null)
                {
                    detail.Description = dto.Description.Trim();
                }

                // TicketId đã được chuyển lên Invoice, không cần set vào InvoiceDetail nữa
                // if (dto.TicketId.HasValue)
                // {
                //     detail.TicketId = dto.TicketId.Value;
                // }

                // Recalculate amounts - sử dụng UnitPrice hiện tại nếu không được cập nhật
                var unitPrice = detail.UnitPrice; // Giữ nguyên giá cũ nếu không được update
                detail.Amount = detail.Quantity * unitPrice;
                detail.VatAmount = detail.VatRate.HasValue
                    ? detail.Amount * (detail.VatRate.Value / 100)
                    : 0;

                // Save changes
                var updated = await _repository.UpdateAsync(detail);

                // Update invoice totals
                await UpdateInvoiceTotalsAsync(updated.InvoiceId);

                _logger.LogInformation("Invoice detail {DetailId} updated successfully for Invoice {InvoiceId}",
                    updated.InvoiceDetailId, updated.InvoiceId);

                // Reload with navigation properties
                var result = await _repository.GetByIdAsync(updated.InvoiceDetailId);
                return result!.ToDto();
            }
            catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException and not InvalidOperationException)
            {
                _logger.LogError(ex, "Error updating invoice detail with ID: {DetailId}", id);
                throw;
            }
        }

        private async Task UpdateInvoiceTotalsAsync(Guid invoiceId)
        {
            var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(invoiceId);
            if (invoice != null)
            {
                InvoiceCalculationHelper.UpdateInvoiceTotals(invoice, invoice.InvoiceDetails);
                await _invoiceRepository.UpdateAsync(invoice);
            }
        }
    }
}