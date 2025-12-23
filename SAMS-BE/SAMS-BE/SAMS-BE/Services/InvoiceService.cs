using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Mappers;
using SAMS_BE.Models;
using SAMS_BE.Interfaces.IMail;

namespace SAMS_BE.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _repository;
        private readonly ILogger<InvoiceService> _logger;
        private readonly IApartmentRepository _apartmentRepository;
        private readonly IServiceTypeRepository _serviceTypeRepository;
        private readonly IServicePriceRepository _servicePriceRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly BuildingManagementContext _context;
        private readonly Interfaces.IService.IVoucherService _voucherService;
        private readonly IEmailSender _emailSender;
        private readonly IInvoiceConfigurationService _invoiceConfigurationService;

        private static readonly string[] EditableStatuses = { "DRAFT", "ISSUED", "OVERDUE" };

        private async Task AddInvoiceCommentAsync(Guid ticketId, string content, Guid? commentedBy)
        {
            // Lưu comment theo giờ Việt Nam (UTC+7) để hiển thị khớp với người dùng
            var comment = new TicketComment
            {
                CommentId = Guid.NewGuid(),
                TicketId = ticketId,
                Content = content,
                CommentTime = DateTimeHelper.VietnamNow,
                CommentedBy = commentedBy
            };
            await _ticketRepository.AddCommentAsync(comment);
        }

        public InvoiceService(
            IInvoiceRepository repository,
            ILogger<InvoiceService> logger,
            IApartmentRepository apartmentRepository,
            IServiceTypeRepository serviceTypeRepository,
            IServicePriceRepository servicePriceRepository,
            ITicketRepository ticketRepository,
            IInvoiceRepository invoiceRepo,
            BuildingManagementContext context,
            Interfaces.IService.IVoucherService voucherService,
            IInvoiceConfigurationService invoiceConfigurationService,
            IEmailSender emailSender)
        {
            _repository = repository;
            _logger = logger;
            _apartmentRepository = apartmentRepository;
            _serviceTypeRepository = serviceTypeRepository;
            _servicePriceRepository = servicePriceRepository;
            _ticketRepository = ticketRepository;
            _invoiceRepo = invoiceRepo;
            _context = context;
            _voucherService = voucherService;
            _invoiceConfigurationService = invoiceConfigurationService;
            _emailSender = emailSender;
        }

        // -----------------------------
        // BASIC CRUD METHODS
        // -----------------------------

        public async Task<InvoiceResponseDto> CreateAsync(CreateInvoiceDto dto)
        {
            try
            {
                var status = InvoiceStatusHelper.EnsureValid(dto.Status, nameof(dto.Status));
                if (dto.DueDate < dto.IssueDate)
                    throw new ArgumentException("Due date cannot be earlier than issue date.", nameof(dto.DueDate));

                // Kiểm tra và đảm bảo InvoiceNo là unique
                var invoiceNo = dto.InvoiceNo?.Trim();
                if (string.IsNullOrWhiteSpace(invoiceNo))
                {
                    invoiceNo = await GenerateUniqueInvoiceNoAsync();
                }
                else
                {
                    // Kiểm tra xem InvoiceNo đã tồn tại chưa
                    var exists = await _context.Invoices.AnyAsync(i => i.InvoiceNo == invoiceNo);
                    if (exists)
                    {
                        _logger.LogWarning("Invoice number {InvoiceNo} already exists, generating unique number", invoiceNo);
                        invoiceNo = await GenerateUniqueInvoiceNoAsync();
                    }
                }

                var invoice = dto.ToEntity();
                invoice.InvoiceNo = invoiceNo;
                invoice.Status = status;

                var created = await _repository.CreateAsync(invoice);
                _logger.LogInformation("Invoice created successfully with ID {InvoiceId}, InvoiceNo: {InvoiceNo}", created.InvoiceId, created.InvoiceNo);

                var result = await _repository.GetByIdAsync(created.InvoiceId);
                return result!.ToDto();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
            {
                // Nếu vẫn bị duplicate (race condition), thử lại với số mới
                _logger.LogWarning(ex, "Duplicate invoice number detected, retrying with new number");
                var invoiceNo = await GenerateUniqueInvoiceNoAsync();
                var invoice = dto.ToEntity();
                invoice.InvoiceNo = invoiceNo;
                invoice.Status = InvoiceStatusHelper.EnsureValid(dto.Status, nameof(dto.Status));

                var created = await _repository.CreateAsync(invoice);
                var result = await _repository.GetByIdAsync(created.InvoiceId);
                return result!.ToDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                throw;
            }
        }

        public async Task<InvoiceResponseDto> GetByIdAsync(Guid id)
        {
            var invoice = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found.");
            return invoice.ToDto();
        }

        public async Task<PagedResult<InvoiceResponseDto>> ListAsync(InvoiceListQueryDto query)
        {
            var (items, total) = await _repository.ListAsync(query);
            return new PagedResult<InvoiceResponseDto>
            {
                Items = items.ToDtoList(),
                TotalItems = total,
                PageNumber = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<InvoiceResponseDto> UpdateAsync(Guid id, UpdateInvoiceDto dto)
        {
            var invoice = await _repository.GetByIdForUpdateAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with ID {id} not found.");

            if (!EditableStatuses.Contains(invoice.Status))
                throw new InvalidOperationException($"Cannot update invoice with status {invoice.Status}");

            if (dto.ApartmentId.HasValue)
            {
                var apartment = await _apartmentRepository.GetApartmentByIdAsync(dto.ApartmentId.Value)
                    ?? throw new ArgumentException($"Apartment {dto.ApartmentId} not found.");
                invoice.ApartmentId = apartment.ApartmentId;
            }

            if (dto.DueDate.HasValue)
            {
                if (dto.DueDate.Value < invoice.IssueDate)
                    throw new ArgumentException("Due date cannot be earlier than issue date.", nameof(dto.DueDate));
                invoice.DueDate = dto.DueDate.Value;
            }

            if (!string.IsNullOrEmpty(dto.Note))
                invoice.Note = dto.Note.Trim();

            var updated = await _repository.UpdateAsync(invoice);
            var result = await _repository.GetByIdAsync(updated.InvoiceId);
            return result!.ToDto();
        }

        public async Task DeleteAsync(Guid id)
        {
            var invoice = await _repository.GetByIdForUpdateAsync(id)
                ?? throw new KeyNotFoundException($"Invoice {id} not found.");
            if (invoice.Status != "DRAFT")
                throw new InvalidOperationException("Only DRAFT invoices can be deleted.");
            await _repository.DeleteAsync(id);
        }

        // -----------------------------
        // STATUS UPDATE
        // -----------------------------
        public async Task<InvoiceResponseDto> UpdateStatusAsync(Guid id, UpdateInvoiceStatusDto dto)
        {
            var invoice = await _repository.GetByIdForUpdateAsync(id)
                ?? throw new KeyNotFoundException($"Invoice {id} not found.");

            var oldStatus = invoice.Status;
            var newStatus = InvoiceStatusHelper.EnsureValid(dto.Status, nameof(dto.Status));
            ValidateStatusTransition(invoice.Status, newStatus);

            invoice.Status = newStatus;
            if (!string.IsNullOrEmpty(dto.Note))
                invoice.Note = (invoice.Note ?? "") + $"\n[Status {oldStatus} → {newStatus}] {dto.Note}";

            var updated = await _repository.UpdateAsync(invoice);
            var result = await _repository.GetByIdAsync(updated.InvoiceId);
            // Khi hóa đơn được chuyển sang trạng thái ISSUED thì gửi email cho cư dân
            if (!string.Equals(oldStatus, "ISSUED", StringComparison.OrdinalIgnoreCase)
                && string.Equals(newStatus, "ISSUED", StringComparison.OrdinalIgnoreCase))
            {
                await SendInvoiceIssuedEmailAsync(updated);
            }
            return result!.ToDto();
        }

        /// <summary>
        /// Gửi email thông báo khi hóa đơn được phát hành (ISSUED) đến cư dân liên quan căn hộ.
        /// </summary>
        private async Task SendInvoiceIssuedEmailAsync(Invoice invoice)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTimeHelper.VietnamNow);

                // Lấy căn hộ để hiển thị số căn
                var apartment = await _context.Apartments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.ApartmentId == invoice.ApartmentId);

                var apartmentNumber = apartment?.Number ?? "N/A";

                // Lấy danh sách cư dân chính đang ở trong căn hộ, còn hiệu lực, có email
                var residentApartments = await _context.ResidentApartments
                    .Include(ra => ra.Resident)
                    .Where(ra => ra.ApartmentId == invoice.ApartmentId
                                 && ra.IsPrimary
                                 && (ra.EndDate == null || ra.EndDate >= today)
                                 && ra.Resident.Status == "ACTIVE"
                                 && ra.Resident.Email != null
                                 && ra.Resident.Email != "")
                    .ToListAsync();

                if (residentApartments.Count == 0)
                {
                    _logger.LogInformation(
                        "Không tìm thấy cư dân chính có email để gửi hóa đơn {InvoiceNo} cho căn hộ {ApartmentId}",
                        invoice.InvoiceNo, invoice.ApartmentId);
                    return;
                }

                foreach (var ra in residentApartments)
                {
                    var resident = ra.Resident;
                    if (string.IsNullOrWhiteSpace(resident.Email))
                        continue;

                    var subject = $"[SAMS] Hóa đơn mới {invoice.InvoiceNo} đã được phát hành cho căn hộ {apartmentNumber}";
                    var html = GenerateInvoiceIssuedEmailHtml(resident, invoice, apartmentNumber);

                    await _emailSender.SendEmailAsync(resident.Email!, subject, html);

                    _logger.LogInformation(
                        "Đã gửi email hóa đơn ISSUED {InvoiceNo} đến cư dân {ResidentId} - {Email}",
                        invoice.InvoiceNo, resident.ResidentId, resident.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi khi gửi email thông báo hóa đơn ISSUED. InvoiceId: {InvoiceId}, InvoiceNo: {InvoiceNo}",
                    invoice.InvoiceId, invoice.InvoiceNo);
            }
        }

        private static string GenerateInvoiceIssuedEmailHtml(ResidentProfile resident, Invoice invoice, string apartmentNumber)
        {
            var issueDate = invoice.IssueDate.ToString("dd/MM/yyyy");
            var dueDate = invoice.DueDate.ToString("dd/MM/yyyy");
            var totalAmount = invoice.TotalAmount.ToString("N0");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Hóa đơn mới được phát hành</title>
</head>
<body style=""margin:0;padding:0;font-family:Arial,sans-serif;background-color:#f4f4f4;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f4;padding:20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 4px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background-color:#007bff;padding:24px 30px;text-align:center;"">
                            <h1 style=""color:#ffffff;margin:0;font-size:22px;"">Thông báo hóa đơn mới</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:24px 30px;"">
                            <p style=""color:#333;font-size:15px;line-height:1.6;margin:0 0 16px 0;"">
                                Kính gửi <strong>{resident.FullName}</strong>,
                            </p>
                            <p style=""color:#333;font-size:15px;line-height:1.6;margin:0 0 16px 0;"">
                                Hệ thống xin thông báo một hóa đơn mới đã được phát hành cho căn hộ
                                <strong>{apartmentNumber}</strong>.
                            </p>

                            <div style=""background-color:#f8f9fa;border-left:4px solid #007bff;padding:16px 18px;margin:16px 0;"">
                                <h2 style=""color:#007bff;margin:0 0 10px 0;font-size:17px;"">Thông tin hóa đơn</h2>
                                <table width=""100%"" cellpadding=""4"" cellspacing=""0"" style=""font-size:14px;color:#333;"">
                                    <tr>
                                        <td style=""width:160px;font-weight:bold;"">Mã hóa đơn:</td>
                                        <td>{invoice.InvoiceNo}</td>
                                    </tr>
                                    <tr>
                                        <td style=""font-weight:bold;"">Căn hộ:</td>
                                        <td>{apartmentNumber}</td>
                                    </tr>
                                    <tr>
                                        <td style=""font-weight:bold;"">Ngày phát hành:</td>
                                        <td>{issueDate}</td>
                                    </tr>
                                    <tr>
                                        <td style=""font-weight:bold;"">Hạn thanh toán:</td>
                                        <td><strong>{dueDate}</strong></td>
                                    </tr>
                                    <tr>
                                        <td style=""font-weight:bold;"">Số tiền phải thanh toán:</td>
                                        <td><strong>{totalAmount} VNĐ</strong></td>
                                    </tr>
                                </table>
                            </div>

                            <p style=""color:#333;font-size:14px;line-height:1.6;margin:0 0 12px 0;"">
                                Quý cư dân vui lòng thanh toán đúng hạn để tránh phát sinh phí phạt chậm thanh toán.
                            </p>

                            <p style=""color:#6c757d;font-size:12px;line-height:1.6;margin:20px 0 0 0;"">
                                Đây là email tự động từ hệ thống SAMS. Vui lòng không trả lời email này.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private void ValidateStatusTransition(string oldStatus, string newStatus)
        {
            if (oldStatus == newStatus)
                throw new InvalidOperationException($"Invoice is already {newStatus}");

            var allowed = new Dictionary<string, string[]>
            {
                { "DRAFT", new[] { "ISSUED", "CANCELLED" } },
                { "ISSUED", new[] { "PAID", "OVERDUE", "CANCELLED" } },
                { "OVERDUE", new[] { "PAID", "CANCELLED" } },
                { "PAID", Array.Empty<string>() },
                { "CANCELLED", Array.Empty<string>() }
            };

            if (!allowed.ContainsKey(oldStatus) || !allowed[oldStatus].Contains(newStatus))
                throw new InvalidOperationException($"Cannot change from {oldStatus} to {newStatus}");
        }

        // -----------------------------
        // CREATE FROM TICKET
        // -----------------------------
        public async Task<(Guid InvoiceId, string InvoiceNo)> CreateFromTicketAsync(CreateInvoiceRequest request)
        {
            try
            {
                // Validate số lượng và đơn giá để tránh tạo hóa đơn số tiền âm
                if (request.UnitPrice < 0)
                {
                    throw new ArgumentException("Đơn giá không được âm.", nameof(request.UnitPrice));
                }

                if (request.Quantity <= 0)
                {
                    throw new ArgumentException("Số lượng phải lớn hơn 0.", nameof(request.Quantity));
                }

                var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
                    ?? throw new ArgumentException("Ticket không tồn tại");

                if (ticket.ApartmentId == null)
                    throw new ArgumentException("Ticket không có ApartmentId");

                if (ticket.Status.Equals("CLOSED", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Ticket đã CLOSED, không thể tạo hóa đơn.");

                var now = DateTimeHelper.VietnamNow; // Lưu giờ VN
                var quantity = request.Quantity;
                decimal amount = request.UnitPrice * quantity;

                Guid? existed = null;
                try
                {
                    existed = await _repository.GetInvoiceIdByTicketAsync(ticket.TicketId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi query invoice by ticket, có thể do database chưa có cột ticket_id. Tiếp tục tạo invoice mới.");
                    // Nếu query lỗi (có thể do cột ticket_id chưa có), tiếp tục tạo invoice mới
                }

                Invoice invoice;
                bool isNewInvoice = false;
                decimal? oldTotalAmount = null; // Lưu tổng tiền cũ để so sánh khi thêm detail

                if (existed != null)
                {
                    // Load invoice đã tồn tại để update
                    invoice = await _context.Invoices
                        .FirstAsync(x => x.InvoiceId == existed.Value);

                    // Lưu tổng tiền cũ để so sánh sau khi thêm detail
                    oldTotalAmount = invoice.TotalAmount;

                    // Không cộng thêm, sẽ tính lại từ tất cả details sau
                    invoice.UpdatedAt = now;
                    // Đảm bảo TicketId được set nếu chưa có (cho các invoice cũ)
                    if (invoice.TicketId == null)
                    {
                        invoice.TicketId = ticket.TicketId;
                    }

                    // Save changes - invoice đã được track nên chỉ cần SaveChanges
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Đã update invoice đã tồn tại. InvoiceId: {InvoiceId}, TicketId: {TicketId}, OldTotalAmount: {OldTotalAmount}",
                        invoice.InvoiceId, invoice.TicketId, oldTotalAmount);
                }
                else
                {
                    isNewInvoice = true;
                    var number = await GenerateUniqueInvoiceNoAsync();
                    invoice = new Invoice
                    {
                        InvoiceId = Guid.NewGuid(),
                        InvoiceNo = number,
                        ApartmentId = ticket.ApartmentId.Value,
                        TicketId = ticket.TicketId,  // Set TicketId vào Invoice
                        IssueDate = DateOnly.FromDateTime(now),
                        DueDate = DateOnly.FromDateTime(now.AddDays(7)),
                        Status = "ISSUED",
                        SubtotalAmount = amount,
                        TaxAmount = 0,
                        TotalAmount = amount,
                        CreatedAt = now
                    };

                    try
                    {
                        await _repository.CreateAsync(invoice);
                        _logger.LogInformation("Đã tạo invoice mới. InvoiceId: {InvoiceId}, InvoiceNo: {InvoiceNo}, TicketId: {TicketId}",
                            invoice.InvoiceId, invoice.InvoiceNo, invoice.TicketId);
                    }
                    catch (DbUpdateException dbEx) when (dbEx.InnerException is SqlException sqlEx)
                    {
                        // Nếu lỗi do cột ticket_id chưa tồn tại, thử tạo lại không có TicketId
                        if (sqlEx.Number == 207 || sqlEx.Number == 8152 || sqlEx.Message.Contains("ticket_id") || sqlEx.Message.Contains("Invalid column name"))
                        {
                            _logger.LogWarning("Cột ticket_id chưa tồn tại trong database, tạo invoice không có TicketId. SQL Error: {Error}", sqlEx.Message);
                            invoice.TicketId = null;
                            await _repository.CreateAsync(invoice);
                            _logger.LogInformation("Đã tạo invoice không có TicketId. InvoiceId: {InvoiceId}, InvoiceNo: {InvoiceNo}",
                                invoice.InvoiceId, invoice.InvoiceNo);
                        }
                        else
                        {
                            _logger.LogError(dbEx, "Lỗi database khi tạo invoice. SQL Error Number: {Number}, Message: {Message}",
                                sqlEx.Number, sqlEx.Message);
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi không xác định khi tạo invoice. Error: {Error}", ex.Message);
                        throw;
                    }

                    // Comment: tạo mới invoice
                    await AddInvoiceCommentAsync(ticket.TicketId,
                        $"Đã tạo hóa đơn {invoice.InvoiceNo} (trạng thái {invoice.Status}, tổng {invoice.TotalAmount:N0}).",
                        request.CreatedByUserId);
                }

                var serviceTypeId = request.ServiceTypeId
                    ?? await _context.ServiceTypes.Select(s => (Guid?)s.ServiceTypeId).FirstOrDefaultAsync()
                    ?? throw new ArgumentException("Chưa có ServiceType để tạo InvoiceDetail");

                var detail = new InvoiceDetail
                {
                    InvoiceDetailId = Guid.NewGuid(),
                    InvoiceId = invoice.InvoiceId,
                    ServiceId = serviceTypeId,
                    Description = request.Note,
                    Quantity = quantity,
                    UnitPrice = request.UnitPrice,
                    Amount = amount,
                    VatRate = 0,
                    VatAmount = 0
                };

                await _repository.AddInvoiceDetailAsync(detail);

                // Tính lại TotalAmount và SubtotalAmount từ tất cả details để đảm bảo chính xác
                // Reload invoice từ context để đảm bảo có thể update
                var invoiceToUpdate = await _context.Invoices.FindAsync(invoice.InvoiceId);
                if (invoiceToUpdate == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy invoice với ID {invoice.InvoiceId} để update totals");
                }

                var allDetails = await _context.InvoiceDetails
                    .Where(d => d.InvoiceId == invoice.InvoiceId)
                    .ToListAsync();

                invoiceToUpdate.SubtotalAmount = allDetails.Sum(d => d.Amount ?? 0);
                invoiceToUpdate.TaxAmount = allDetails.Sum(d => d.VatAmount ?? 0);
                invoiceToUpdate.TotalAmount = invoiceToUpdate.SubtotalAmount + invoiceToUpdate.TaxAmount;

                await _context.SaveChangesAsync();
                invoice = invoiceToUpdate;

                _logger.LogInformation("Đã update totals cho invoice. InvoiceId: {InvoiceId}, TotalAmount: {TotalAmount}",
                    invoice.InvoiceId, invoice.TotalAmount);

                ticket.HasInvoice = true;
                ticket.UpdatedAt = now;
                await _ticketRepository.UpdateAsync(ticket);

                // Ghi comment cho dòng InvoiceDetail và tổng mới
                await AddInvoiceCommentAsync(ticket.TicketId,
                    $"Đã thêm dòng hóa đơn số tiền {detail.Amount:N0}đ cho hóa đơn {invoice.InvoiceNo}. Tổng mới: {invoice.TotalAmount:N0}đ.",
                    request.CreatedByUserId);

                // Gửi email khi:
                // 1. Tạo hóa đơn mới từ ticket với status ISSUED
                // 2. Thêm detail vào invoice đã tồn tại với status ISSUED và tổng tiền thay đổi
                if (string.Equals(invoice.Status, "ISSUED", StringComparison.OrdinalIgnoreCase))
                {
                    bool shouldSendEmail = false;

                    if (isNewInvoice)
                    {
                        // Trường hợp 1: Tạo invoice mới
                        shouldSendEmail = true;
                    }
                    else if (oldTotalAmount.HasValue && Math.Abs(invoice.TotalAmount - oldTotalAmount.Value) > 0.01m)
                    {
                        // Trường hợp 2: Thêm detail vào invoice đã tồn tại và tổng tiền thay đổi (chênh lệch > 0.01 để tránh lỗi làm tròn)
                        shouldSendEmail = true;
                        _logger.LogInformation("Tổng tiền hóa đơn đã thay đổi từ {OldAmount} sang {NewAmount}. Sẽ gửi email thông báo.",
                            oldTotalAmount.Value, invoice.TotalAmount);
                    }

                    if (shouldSendEmail)
                    {
                        // Reload invoice từ database để đảm bảo có đầy đủ thông tin trước khi gửi email
                        var invoiceForEmail = await _repository.GetByIdAsync(invoice.InvoiceId);
                        if (invoiceForEmail != null)
                        {
                            await SendInvoiceIssuedEmailAsync(invoiceForEmail);
                        }
                    }
                }

                return (invoice.InvoiceId, invoice.InvoiceNo);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Lỗi validation khi tạo invoice từ ticket. TicketId: {TicketId}", request.TicketId);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Lỗi business logic khi tạo invoice từ ticket. TicketId: {TicketId}", request.TicketId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi tạo invoice từ ticket. TicketId: {TicketId}", request.TicketId);
                throw;
            }
        }

        // -----------------------------
        // DETAIL UPDATE
        // -----------------------------
        public async Task<bool> UpdateDetailAsync(Guid detailId, UpdateInvoiceDetailDto dto)
        {
            var detail = await _context.InvoiceDetails
                .Include(d => d.Invoice)
                .FirstOrDefaultAsync(d => d.InvoiceDetailId == detailId);
            if (detail == null) return false;

            if (!detail.Invoice.Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ có thể chỉnh sửa InvoiceDetail khi Invoice ở trạng thái DRAFT");

            // Giữ nguyên ServiceId để không bị mất
            var serviceId = detail.ServiceId;

            // Giữ nguyên nếu không truyền
            var newQuantity = dto.Quantity ?? detail.Quantity;

            // Validate nếu có cung cấp quantity
            if (dto.Quantity.HasValue && dto.Quantity.Value <= 0)
                throw new ArgumentException("Quantity must be greater than 0.", nameof(dto.Quantity));

            // Kiểm tra nếu ServiceType có giá cố định (service price)
            decimal newUnitPrice = detail.UnitPrice;
            if (detail.ServiceId != Guid.Empty)
            {
                var currentPrice = await _context.ServicePrices
                    .Where(sp => sp.ServiceTypeId == detail.ServiceId
                                 && sp.Status == "APPROVED"
                                 && sp.EffectiveDate <= DateOnly.FromDateTime(DateTime.UtcNow)
                                 && (sp.EndDate == null || sp.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow)))
                    .OrderByDescending(sp => sp.EffectiveDate)
                    .Select(sp => (decimal?)sp.UnitPrice)
                    .FirstOrDefaultAsync();

                if (currentPrice.HasValue)
                {
                    // Nếu có giá cố định, luôn dùng giá đó (kể cả khi chỉnh sửa số lượng)
                    // Nếu người dùng cố gắng chỉnh sửa đơn giá khác với giá cố định, từ chối
                    if (dto.UnitPrice.HasValue && Math.Abs(dto.UnitPrice.Value - currentPrice.Value) > 0.01m)
                    {
                        throw new InvalidOperationException(
                            $"Dịch vụ này có giá cố định ({currentPrice.Value:N0} đ), không thể chỉnh sửa đơn giá. " +
                            $"Số lượng có thể chỉnh sửa, đơn giá sẽ tự động lấy từ giá cố định.");
                    }
                    newUnitPrice = currentPrice.Value;
                }
                else if (dto.UnitPrice.HasValue)
                {
                    // Nếu không có giá cố định và có truyền unitPrice, dùng giá truyền vào
                    newUnitPrice = dto.UnitPrice.Value;
                }
            }
            else if (dto.UnitPrice.HasValue)
            {
                // Nếu không có ServiceId và có truyền unitPrice, dùng giá truyền vào
                newUnitPrice = dto.UnitPrice.Value;
            }

            detail.Quantity = newQuantity;
            detail.UnitPrice = newUnitPrice;
            detail.Amount = newQuantity * newUnitPrice;

            if (dto.Description != null)
            {
                detail.Description = dto.Description;
            }

            // Đảm bảo ServiceId không bị mất - luôn giữ nguyên giá trị cũ nếu không truyền vào
            if (dto.ServiceId.HasValue && dto.ServiceId.Value != Guid.Empty)
            {
                detail.ServiceId = dto.ServiceId.Value;
            }
            else
            {
                // Giữ nguyên ServiceId nếu không truyền vào
                detail.ServiceId = serviceId;
            }

            // Lưu thay đổi detail trước
            await _context.SaveChangesAsync();

            // Tính lại TotalAmount và SubtotalAmount từ tất cả details để đảm bảo chính xác
            var invoice = detail.Invoice;
            var allDetails = await _context.InvoiceDetails
                .Where(d => d.InvoiceId == invoice.InvoiceId)
                .ToListAsync();
            invoice.SubtotalAmount = allDetails.Sum(d => d.Amount ?? 0);
            invoice.TaxAmount = allDetails.Sum(d => d.VatAmount ?? 0);
            invoice.TotalAmount = invoice.SubtotalAmount + invoice.TaxAmount;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // -----------------------------
        // MONTHLY GENERATION
        // -----------------------------
        private async Task<string> GenerateUniqueInvoiceNoAsync()
        {
            for (var i = 0; i < 5; i++)
            {
                var candidate = $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}";
                var exists = await _context.Invoices.AnyAsync(x => x.InvoiceNo == candidate);
                if (!exists) return candidate;
                await Task.Delay(5);
            }
            return $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}".Substring(0, 6);
        }

        public async Task<List<InvoiceResponseDto>> GenerateMonthlyFixedFeeInvoicesAsync(int year, int month, string createdBy = "SYSTEM")
        {
            var results = new List<InvoiceResponseDto>();

            // Đọc cấu hình hóa đơn (ngày tạo, số ngày đến hạn, bật/tắt)
            var config = await _invoiceConfigurationService.GetCurrentConfigAsync();
            if (config == null || !config.IsEnabled)
            {
                _logger.LogInformation("Invoice generation is disabled by configuration");
                return results;
            }

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var generationDay = config.GenerationDayOfMonth;
            if (generationDay < 1) generationDay = 1;
            if (generationDay > daysInMonth) generationDay = daysInMonth;

            var issueDate = new DateOnly(year, month, generationDay);
            var dueDate = issueDate.AddDays(config.DueDaysAfterIssue);

            var activeApartments = await _apartmentRepository.GetActiveApartmentsAsync();

            // Lấy tất cả service tính phí định kỳ theo tháng
            var recurringServices = await _serviceTypeRepository.GetMonthlyRecurringServicesAsync();

            // Logic tạo invoice:
            // - MGMT_FEE: luôn có, tính theo diện tích căn hộ
            // - PARKING_BIKE: chỉ tạo nếu có xe máy
            // - PARKING_CAR: chỉ tạo nếu có ô tô
            foreach (var apartment in activeApartments)
            {
                try
                {
                    var existing = await _repository.CheckExistingMonthlyInvoiceAsync(apartment.ApartmentId, year, month);
                    if (existing != null) continue;

                    // Generate unique invoice number để tránh trùng
                    var baseInvoiceNo = $"INV-{year:D4}{month:D2}-{apartment.Number}";
                    var invoiceNo = baseInvoiceNo;
                    var counter = 1;
                    while (await _context.Invoices.AnyAsync(i => i.InvoiceNo == invoiceNo))
                    {
                        invoiceNo = $"{baseInvoiceNo}-{counter:D2}";
                        counter++;
                        if (counter > 99) // Safety limit
                        {
                            invoiceNo = await GenerateUniqueInvoiceNoAsync();
                            break;
                        }
                    }

                    var invoice = new Invoice
                    {
                        InvoiceId = Guid.NewGuid(),
                        InvoiceNo = invoiceNo,
                        ApartmentId = apartment.ApartmentId,
                        IssueDate = issueDate,
                        DueDate = dueDate,
                        Status = "ISSUED",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = createdBy,
                        Note = $"Tự động tạo hóa đơn tháng {month}/{year}"
                    };

                    var details = new List<InvoiceDetail>();

                    foreach (var service in recurringServices)
                    {
                        var price = await _servicePriceRepository.GetCurrentPriceAsync(service.ServiceTypeId, issueDate);
                        if (price == null) continue;

                        decimal quantity = 1;
                        decimal amount = 0;
                        string description = $"{service.Name} - Tháng {month}/{year}";

                        // Phí quản lý - tính theo diện tích
                        if (service.Code.Equals("MGMT_FEE", StringComparison.OrdinalIgnoreCase) ||
                            service.Code.Equals("MANAGEMENT_FEE", StringComparison.OrdinalIgnoreCase))
                        {
                            if (apartment.AreaM2.HasValue && apartment.AreaM2.Value > 0)
                            {
                                quantity = apartment.AreaM2.Value;
                                amount = quantity * price.UnitPrice;
                                description = $"Phí quản lý - {apartment.AreaM2.Value}m² × {price.UnitPrice:N0}đ/m²";
                            }
                            else
                            {
                                _logger.LogWarning("Apartment {AptNumber} has no area, skipping management fee", apartment.Number);
                                continue;
                            }
                        }
                        // Phí gửi xe máy - tính theo số xe máy
                        else if (service.Code.Equals("PARKING_BIKE", StringComparison.OrdinalIgnoreCase) ||
                                 service.Code.Equals("MOTORBIKE_PARKING_FEE", StringComparison.OrdinalIgnoreCase))
                        {
                            var motorbikeCount = await _context.Vehicles
                                .Where(v => v.ApartmentId == apartment.ApartmentId
                                            && v.Status == "ACTIVE"
                                            && v.VehicleType.Code == "MOTORBIKE")
                                .CountAsync();

                            if (motorbikeCount > 0)
                            {
                                quantity = motorbikeCount;
                                amount = quantity * price.UnitPrice;
                                description = $"Phí gửi xe máy - {motorbikeCount} xe × {price.UnitPrice:N0}đ/xe";
                            }
                            else
                            {
                                _logger.LogInformation("Apartment {AptNumber} has no motorbikes, skipping motorbike parking fee", apartment.Number);
                                continue;
                            }
                        }
                        // Phí gửi ô tô - tính theo số ô tô
                        else if (service.Code.Equals("PARKING_CAR", StringComparison.OrdinalIgnoreCase) ||
                                 service.Code.Equals("CAR_PARKING_FEE", StringComparison.OrdinalIgnoreCase))
                        {
                            var carCount = await _context.Vehicles
                                .Where(v => v.ApartmentId == apartment.ApartmentId
                                            && v.Status == "ACTIVE"
                                            && v.VehicleType.Code == "CAR")
                                .CountAsync();

                            if (carCount > 0)
                            {
                                quantity = carCount;
                                amount = quantity * price.UnitPrice;
                                description = $"Phí gửi xe ô tô - {carCount} xe × {price.UnitPrice:N0}đ/xe";
                            }
                            else
                            {
                                _logger.LogInformation("Apartment {AptNumber} has no cars, skipping car parking fee", apartment.Number);
                                continue;
                            }
                        }
                        // Các dịch vụ định kỳ khác - giữ quantity = 1, amount = unitPrice
                        else
                        {
                            quantity = 1;
                            amount = price.UnitPrice;
                        }

                        decimal vat = amount * 0.1m;

                        details.Add(new InvoiceDetail
                        {
                            InvoiceDetailId = Guid.NewGuid(),
                            ServiceId = service.ServiceTypeId,
                            Description = description,
                            Quantity = quantity,
                            UnitPrice = price.UnitPrice,
                            Amount = amount,
                            VatRate = 10,
                            VatAmount = vat
                        });
                    }

                    if (details.Any())
                    {
                        invoice.SubtotalAmount = details.Sum(d => d.Amount ?? 0);
                        invoice.TaxAmount = details.Sum(d => d.VatAmount ?? 0);
                        invoice.TotalAmount = invoice.SubtotalAmount + invoice.TaxAmount;

                        var created = await _repository.CreateWithDetailsAsync(invoice, details);
                        var result = await _repository.GetByIdAsync(created.InvoiceId);
                        if (result != null) results.Add(result.ToDto());

                        _logger.LogInformation(
                            "Generated invoice {InvoiceNo} for apartment {AptNumber} with {DetailCount} details, total: {Total:N0}đ",
                            invoiceNo, apartment.Number, details.Count, invoice.TotalAmount);
                    }
                    else
                    {
                        _logger.LogWarning("No invoice details generated for apartment {AptNumber}, skipping invoice creation", apartment.Number);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating invoice for apartment {Apt}", apartment.Number);
                }
            }

            _logger.LogInformation("Generated {Count} invoices for {Year}/{Month}", results.Count, year, month);
            return results;
        }

        public async Task<List<InvoiceResponseDto>> RunConfiguredMonthlyGenerationAsync()
        {
            var config = await _invoiceConfigurationService.GetCurrentConfigAsync();
            if (config == null || !config.IsEnabled)
            {
                _logger.LogInformation("Invoice generation job is disabled by configuration");
                return new List<InvoiceResponseDto>();
            }

            var today = DateTime.Today;
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var generationDay = config.GenerationDayOfMonth;
            if (generationDay < 1) generationDay = 1;
            if (generationDay > daysInMonth) generationDay = daysInMonth;

            if (today.Day != generationDay)
            {
                _logger.LogInformation(
                    "Today {Today} is not configured generation day {GenerationDay}, skipping monthly invoice generation",
                    today.ToString("yyyy-MM-dd"), generationDay);
                return new List<InvoiceResponseDto>();
            }

            return await GenerateMonthlyFixedFeeInvoicesAsync(today.Year, today.Month, "SYSTEM");
        }

        public async Task<int> UpdateOverdueInvoicesAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var invoices = await _repository.GetOverdueInvoicesAsync(today);
            int count = 0;

            foreach (var i in invoices)
            {
                if (InvoiceStatusHelper.ShouldBeOverdue(today, i.DueDate, i.Status))
                {
                    i.Status = "OVERDUE";
                    i.UpdatedAt = DateTime.UtcNow;
                    await _repository.UpdateAsync(i);
                    count++;
                }
            }
            return count;
        }

        // -----------------------------
        // METHODS FROM TICKET MANAGEMENT
        // -----------------------------
        public async Task<(Guid InvoiceId, string InvoiceNo)> CreateAsyncInvoice(CreateInvoiceRequest request)
        {
            return await CreateFromTicketAsync(request);
        }

        public async Task<List<FinanceItemSummaryDto>> GetByTicketAsync(Guid ticketId)
        {
            var list = await _repository.GetInvoicesByTicketAsync(ticketId);
            return list.Select(x => new FinanceItemSummaryDto { Id = x.InvoiceId, Number = x.InvoiceNo, Amount = x.Amount }).ToList();
        }

        public async Task<InvoiceResponseDto?> GetByIdAsyncInvoice(Guid invoiceId)
        {
            try
            {
                var invoice = await _repository.GetByIdWithDetailsAsync(invoiceId);
                if (invoice == null)
                {
                    _logger.LogWarning("Không tìm thấy invoice với ID: {InvoiceId}", invoiceId);
                    return null;
                }

                var dto = new InvoiceResponseDto
                {
                    InvoiceId = invoice.InvoiceId,
                    InvoiceNo = invoice.InvoiceNo,
                    ApartmentId = invoice.ApartmentId,
                    IssueDate = invoice.IssueDate,
                    DueDate = invoice.DueDate,
                    Status = invoice.Status,
                    SubtotalAmount = invoice.SubtotalAmount,
                    TaxAmount = invoice.TaxAmount,
                    TotalAmount = invoice.TotalAmount,
                    Note = invoice.Note,
                    CreatedAt = invoice.CreatedAt,
                    UpdatedAt = invoice.UpdatedAt,
                    TicketId = invoice.TicketId,
                    Details = invoice.InvoiceDetails?.Select(d => d.ToDto()).ToList() ?? new List<InvoiceDetailResponseDto>()
                };

                _logger.LogInformation("Đã load invoice thành công. InvoiceId: {InvoiceId}, DetailsCount: {Count}",
                    invoice.InvoiceId, dto.Details.Count);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi load invoice. InvoiceId: {InvoiceId}, Error: {Error}", invoiceId, ex.Message);
                throw;
            }
        }

        public async Task<InvoiceDetailResponseDto?> GetDetailByIdAsync(Guid detailId)
        {
            var detail = await _repository.GetDetailByIdAsync(detailId);
            return detail?.ToDto();
        }

        public async Task<List<InvoiceResponseDto>> GetMyInvoicesAsync(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("User ID not found in claims.");

            var statuses = new[] { "ISSUED", "PAID", "OVERDUE" };
            var invoices = await _repository.GetByUserAndStatusesWithDetailsAsync(userId, statuses);
            return invoices.ToDtoList();
        }
    }
}

