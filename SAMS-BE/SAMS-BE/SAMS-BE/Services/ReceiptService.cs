using Microsoft.EntityFrameworkCore.Storage;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Services
{
    public class ReceiptService : IReceiptService
    {
        private readonly IReceiptRepository _repository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly BuildingManagementContext _context;
        private readonly IJournalEntryService _journalEntryService;
        private readonly ILogger<ReceiptService> _logger;

        public ReceiptService(
            IReceiptRepository repository,
            IInvoiceRepository invoiceRepository,
            BuildingManagementContext context,
            IJournalEntryService journalEntryService,
            ILogger<ReceiptService> logger)
        {
            _repository = repository;
            _invoiceRepository = invoiceRepository;
            _context = context;
            _journalEntryService = journalEntryService;
            _logger = logger;
        }

        public async Task<Receipt> CreateAsync(CreateReceiptDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate Invoice exists
                if (!await _repository.InvoiceExistsAsync(dto.InvoiceId))
                    throw new KeyNotFoundException($"Invoice with ID {dto.InvoiceId} not found.");

                // Validate PaymentMethod exists
                if (!await _repository.PaymentMethodExistsAsync(dto.MethodId))
                    throw new KeyNotFoundException($"Payment Method with ID {dto.MethodId} not found.");

                // Check if Invoice already has a Receipt (1-1 relationship)
                var existingReceipt = await _repository.GetByInvoiceIdAsync(dto.InvoiceId);
                if (existingReceipt != null)
                    throw new InvalidOperationException($"Invoice {dto.InvoiceId} already has a receipt (Receipt No: {existingReceipt.ReceiptNo}).");

                // Get Invoice to validate amount
                var invoice = await _invoiceRepository.GetByIdAsync(dto.InvoiceId);
                if (invoice == null)
                    throw new KeyNotFoundException($"Invoice with ID {dto.InvoiceId} not found.");

                // Validate amount matches invoice total
                if (dto.AmountTotal != invoice.TotalAmount)
                {
                    throw new ArgumentException(
                    $"Receipt amount ({dto.AmountTotal:N2}) does not match Invoice total amount ({invoice.TotalAmount:N2}).");
                }

                // 👇 AUTO-GENERATE Receipt Number if not provided
                var receiptNo = dto.ReceiptNo;
                if (string.IsNullOrWhiteSpace(receiptNo))
                {
                    // Use ReceivedDate from DTO, or current time if not provided
                    var receiptDate = dto.ReceivedDate ?? DateTime.UtcNow;

                    // Format: REC-{InvoiceNo}-YYYY/MM/DD
                    // Example: REC-INV202500001-2025/01/20
                    receiptNo = ReceiptHelper.GenerateReceiptNumber(invoice.InvoiceNo, receiptDate);
                }

                var receipt = new Receipt
                {
                    ReceiptId = Guid.NewGuid(),
                    InvoiceId = dto.InvoiceId,
                    ReceiptNo = receiptNo,  // 👈 Use auto-generated or provided
                    ReceivedDate = dto.ReceivedDate ?? DateTime.UtcNow,  // 👈 Use provided or current time
                    MethodId = dto.MethodId,
                    AmountTotal = dto.AmountTotal,
                    Note = dto.Note,
                    CreatedBy = dto.CreatedBy!.Value  // 👈 CreatedBy đã được set từ Controller
                };

                // 1. Create Receipt
                var createdReceipt = await _repository.CreateAsync(receipt);

                // 2. Update Invoice status to PAID
                invoice.Status = "PAID";
                await _invoiceRepository.UpdateAsync(invoice);

                // 3. Create Journal Entry for accounting (if enabled)
                await _journalEntryService.CreateJournalEntryFromReceiptAsync(createdReceipt, invoice);

                await transaction.CommitAsync();
                return createdReceipt;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Tạo Receipt tự động từ payment online (VietQR, MoMo, etc.)
        /// Được gọi từ PaymentService khi payment thành công
        /// NOTE: Method này KHÔNG tự tạo transaction, caller phải quản lý transaction
        /// </summary>
        public async Task<Receipt?> CreateReceiptFromPaymentAsync(
            Guid invoiceId,
            decimal amount,
            string paymentMethodCode,
            DateTime paymentDate,
            string? note = null)
        {
            try
            {
                _logger.LogInformation("Creating receipt from payment - InvoiceId: {InvoiceId}, Amount: {Amount}, Method: {PaymentMethodCode}",
                      invoiceId, amount, paymentMethodCode);

                // 1. Validate Invoice exists (AsNoTracking vì invoice đã được track ở parent method)
                var invoice = await _context.Invoices
                    .AsNoTracking() // ✅ Không track để tránh conflict
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

                if (invoice == null)
                {
                    _logger.LogWarning("Invoice {InvoiceId} not found", invoiceId);
                    return null;
                }

                _logger.LogInformation("Found invoice {InvoiceNo} with status {Status}", invoice.InvoiceNo, invoice.Status);

                // 2. Check if Invoice already has a Receipt
                var existingReceipt = await _repository.GetByInvoiceIdAsync(invoiceId);
                if (existingReceipt != null)
                {
                    _logger.LogWarning("Invoice {InvoiceId} already has receipt {ReceiptNo}", invoiceId, existingReceipt.ReceiptNo);
                    return existingReceipt;
                }

                // 3. Get PaymentMethod by code
                var paymentMethod = await _context.PaymentMethods
              .AsNoTracking() // ✅ Read-only
                    .FirstOrDefaultAsync(pm => pm.Code.ToUpper() == paymentMethodCode.ToUpper() && pm.Active);

                if (paymentMethod == null)
                {
                    _logger.LogWarning("Payment method with code '{PaymentMethodCode}' not found or inactive", paymentMethodCode);

                    // Fallback: try to find any active payment method
                    paymentMethod = await _context.PaymentMethods
               .AsNoTracking()
               .FirstOrDefaultAsync(pm => pm.Active);

                    if (paymentMethod == null)
                    {
                        throw new InvalidOperationException("No active payment method found in database");
                    }

                    _logger.LogInformation("Using fallback payment method: {PaymentMethodName}", paymentMethod.Name);
                }

                _logger.LogInformation("Using payment method: {PaymentMethodName} (ID: {PaymentMethodId})",
                    paymentMethod.Name, paymentMethod.PaymentMethodId);

                // 4. Validate amount
                var amountDiff = Math.Abs(amount - invoice.TotalAmount);
                if (amountDiff > 1)
                {
                    _logger.LogWarning("Payment amount ({Amount}) does not match Invoice total ({InvoiceTotal}), diff: {Diff}",
                       amount, invoice.TotalAmount, amountDiff);
                }

                // 5. Generate Receipt Number
                var receiptNo = ReceiptHelper.GenerateReceiptNumber(invoice.InvoiceNo, paymentDate);
                _logger.LogInformation("Generated receipt number: {ReceiptNo}", receiptNo);

                // 6. Create Receipt entity
                var receipt = new Receipt
                {
                    ReceiptId = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    ReceiptNo = receiptNo,
                    ReceivedDate = paymentDate,
                    MethodId = paymentMethod.PaymentMethodId,
                    AmountTotal = amount,
                    Note = note ?? $"Thanh toán online qua {paymentMethod.Name}",
                    CreatedBy = null, // ✅ NULL for system-generated
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Creating receipt entity: {ReceiptNo} for invoice {InvoiceNo}",
                      receiptNo, invoice.InvoiceNo);

                // 7. Save Receipt to database
                Receipt createdReceipt;
                try
                {
                    createdReceipt = await _repository.CreateAsync(receipt);
                    _logger.LogInformation("Receipt {ReceiptNo} saved to database with ID {ReceiptId}",
                                 receiptNo, createdReceipt.ReceiptId);
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error creating receipt {ReceiptNo}. Inner: {InnerException}",
                            receiptNo, dbEx.InnerException?.Message);
                    throw;
                }

                // 8. Update Invoice status to PAID
                // ✅ Load invoice từ DB với tracking để update
                var invoiceToUpdate = await _context.Invoices.FindAsync(invoiceId);
                if (invoiceToUpdate == null)
                {
                    throw new InvalidOperationException($"Invoice {invoiceId} not found for update");
                }

                _logger.LogInformation("Updating invoice {InvoiceNo} status from {OldStatus} to PAID",
                 invoiceToUpdate.InvoiceNo, invoiceToUpdate.Status);

                invoiceToUpdate.Status = "PAID";
                invoiceToUpdate.UpdatedAt = DateTime.UtcNow;

                try
                {
                    _context.Invoices.Update(invoiceToUpdate);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Invoice {InvoiceNo} status updated to PAID", invoiceToUpdate.InvoiceNo);
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error updating invoice {InvoiceNo}. Inner: {InnerException}",
                 invoiceToUpdate.InvoiceNo, dbEx.InnerException?.Message);
                    throw;
                }

                // 9. Create Journal Entry for accounting
                try
                {
                    _logger.LogInformation("Creating journal entry for receipt {ReceiptNo}", receiptNo);

                    // ✅ Load invoice fresh cho journal entry (không tracking conflict vì đã SaveChanges)
                    var invoiceForJournal = await _context.Invoices
          .AsNoTracking()
          .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

                    if (invoiceForJournal != null)
                    {
                        await _journalEntryService.CreateJournalEntryFromReceiptAsync(createdReceipt, invoiceForJournal);
                        _logger.LogInformation("Journal entry created successfully for receipt {ReceiptNo}", receiptNo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create journal entry for receipt {ReceiptNo}. Continuing without journal entry.", receiptNo);
                    // Don't fail the whole transaction if journal entry fails
                }

                _logger.LogInformation("Receipt creation completed successfully for Invoice {InvoiceNo}", invoice.InvoiceNo);
                return createdReceipt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating receipt from payment for Invoice {InvoiceId}. Exception type: {ExceptionType}, Inner: {InnerException}",
                    invoiceId, ex.GetType().Name, ex.InnerException?.Message);
                throw;
            }
        }
        public async Task<Receipt> GetByIdAsync(Guid id)
        {
            var receipt = await _repository.GetByIdAsync(id);
            if (receipt == null)
                throw new KeyNotFoundException($"Receipt with ID {id} not found.");
            return receipt;
        }

        public async Task<(IReadOnlyList<Receipt> Items, int Total)> ListAsync(ReceiptListQueryDto query)
        {
            return await _repository.ListAsync(query);
        }

        public async Task<Receipt> UpdateAsync(Guid id, UpdateReceiptDto dto)
        {
            var receipt = await _repository.GetByIdForUpdateAsync(id);
            if (receipt == null)
                throw new KeyNotFoundException($"Receipt with ID {id} not found.");

            // Validate PaymentMethod if changed
            if (dto.MethodId.HasValue && !await _repository.PaymentMethodExistsAsync(dto.MethodId.Value))
                throw new KeyNotFoundException($"Payment Method with ID {dto.MethodId} not found.");

            if (dto.ReceivedDate.HasValue)
                receipt.ReceivedDate = dto.ReceivedDate.Value;
            if (dto.MethodId.HasValue)
                receipt.MethodId = dto.MethodId.Value;
            if (dto.Note != null)
                receipt.Note = dto.Note;

            return await _repository.UpdateAsync(receipt);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var receipt = await _repository.GetByIdAsync(id);
                if (receipt == null)
                    throw new KeyNotFoundException($"Receipt with ID {id} not found.");

                // 1. Revert Invoice status back to ISSUED
                var invoice = await _invoiceRepository.GetByIdAsync(receipt.InvoiceId);
                if (invoice != null && invoice.Status == "PAID")
                {
                    invoice.Status = "ISSUED";
                    await _invoiceRepository.UpdateAsync(invoice);
                }

                // 2. Delete Receipt
                await _repository.DeleteAsync(id);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
