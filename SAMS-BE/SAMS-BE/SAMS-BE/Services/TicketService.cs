using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Mappers;
using SAMS_BE.Models;
using System.Collections.Generic;
using System.Linq;
using static SAMS_BE.Helpers.DateTimeHelper;

namespace SAMS_BE.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _repo;
    private readonly IUserRepository _userRepo;
    private readonly IFileStorageHelper _fileStorage;
    private readonly BuildingManagementContext _context;
    private readonly ILogger<TicketService>? _logger;
    private static readonly HashSet<string> AllowedScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tòa nhà", "Theo căn hộ"
    };
    private static readonly HashSet<string> AllowedPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Thấp", "Bình thường", "Khẩn cấp"
    };
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Mới tạo", "Chờ xử lý", "Đã tiếp nhận", "Đang xử lý", "Hoàn thành", "Đã đóng"
    };
    private static readonly Dictionary<string, string[]> AllowedStatusTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Mới tạo",       new[] { "Đã tiếp nhận" } },
        { "Đã tiếp nhận",  new[] { "Đang xử lý" } },
        { "Đang xử lý",    new[] { "Hoàn thành" } },
        { "Hoàn thành",    new[] { "Đã đóng" } },
        { "Đã đóng",       Array.Empty<string>() }
    };

    private async Task AddSystemComment(Guid ticketId, string content, Guid? userId = null)
    {
        var comment = new TicketComment
        {
            CommentId = Guid.NewGuid(),
            TicketId = ticketId,
            Content = content,
            CommentTime = VietnamNow,
            CommentedBy = userId,
        };
        await _repo.AddCommentAsync(comment);
    }

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Bảo trì", "An ninh", "Hóa đơn", "Khiếu nại",
        "Vệ sinh", "Bãi đỗ xe", "Tiện ích", "Khác"
    };

    public TicketService(
        ITicketRepository repo,
        IUserRepository userRepo,
        IFileStorageHelper fileStorage,
        BuildingManagementContext context,
        ILogger<TicketService>? logger = null)
    {
        _repo = repo;
        _userRepo = userRepo;
        _fileStorage = fileStorage;
        _context = context;
        _logger = logger;
    }

    public async Task<(IEnumerable<TicketDto> items, int total)> SearchAsync(TicketQueryDto dto)
    {
        var q = _repo.Query();

        // Chặn các category không được phép hiển thị
        var blockedCategories = new[] { "Đăng ký xe", "Hủy gửi xe" };
        q = q.Where(x => !blockedCategories.Contains(x.Category));

        if (!string.IsNullOrWhiteSpace(dto.Status)) q = q.Where(x => x.Status == dto.Status);
        if (!string.IsNullOrWhiteSpace(dto.Priority)) q = q.Where(x => x.Priority == dto.Priority);
        if (!string.IsNullOrWhiteSpace(dto.Category)) q = q.Where(x => x.Category == dto.Category);
        if (!string.IsNullOrWhiteSpace(dto.Search)) q = q.Where(x => x.Subject.Contains(dto.Search) || (x.Description ?? "").Contains(dto.Search));
        if (dto.FromDate.HasValue) q = q.Where(x => x.CreatedAt >= dto.FromDate.Value);
        if (dto.ToDate.HasValue) q = q.Where(x => x.CreatedAt <= dto.ToDate.Value);
        // Filter theo người tạo
        if (dto.CreatedByUserId.HasValue) q = q.Where(x => x.CreatedByUserId == dto.CreatedByUserId.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((dto.Page - 1) * dto.PageSize)
            .Take(dto.PageSize)
            .ToListAsync();

        var dtos = items.ToDto().ToList();
        
        foreach (var ticketDto in dtos)
        {
            if (!string.IsNullOrWhiteSpace(ticketDto.Scope))
            {
                var normalizedScope = AllowedScopes.FirstOrDefault(s => string.Equals(s, ticketDto.Scope, StringComparison.OrdinalIgnoreCase));
                if (normalizedScope != null && string.Equals(normalizedScope, "Tòa nhà", StringComparison.OrdinalIgnoreCase))
                {
                    ticketDto.ApartmentId = null;
                    ticketDto.ApartmentNumber = null;
                }
            }
        }

        return (dtos, total);
    }

    public async Task<TicketDto?> GetAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        var dto = entity?.ToDto();
        if (dto == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Scope))
        {
            var normalizedScope = AllowedScopes.FirstOrDefault(s => string.Equals(s, dto.Scope, StringComparison.OrdinalIgnoreCase));
            if (normalizedScope != null && string.Equals(normalizedScope, "Tòa nhà", StringComparison.OrdinalIgnoreCase))
            {
                dto.ApartmentId = null;
                dto.ApartmentNumber = null;
            }
        }

        if (dto.TicketComments != null && dto.TicketComments.Count > 0)
        {
            await PopulateCommentUserNamesAsync(dto.TicketComments);
        }
        return dto;
    }

    public async Task<TicketDto> CreateAsync(CreateTicketDto dto)
    {
        var subject = (dto.Subject ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Tiêu đề là bắt buộc.");
        if (subject.Length < 3) throw new ArgumentException("Tiêu đề phải có ít nhất 3 ký tự.");
        if (subject.Length > 255) throw new ArgumentException("Tiêu đề không được vượt quá 255 ký tự.");
        dto.Subject = subject;
        if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > 4000)
            throw new ArgumentException("Mô tả không được vượt quá 4000 ký tự.");
        if (string.IsNullOrWhiteSpace(dto.Category)) throw new ArgumentException("Category là bắt buộc.");

        var rawScope = string.IsNullOrWhiteSpace(dto.Scope)
            ? "Tòa nhà"
            : dto.Scope.Trim();
        if (!AllowedScopes.Contains(rawScope))
            throw new ArgumentException("Scope không hợp lệ. Chỉ chấp nhận: Tòa nhà, Theo căn hộ.");
        var normalizedScope = AllowedScopes.First(s => string.Equals(s, rawScope, StringComparison.OrdinalIgnoreCase));
        if (string.Equals(normalizedScope, "Theo căn hộ", StringComparison.OrdinalIgnoreCase) && !dto.ApartmentId.HasValue)
            throw new ArgumentException("Căn hộ là bắt buộc khi Scope là Theo căn hộ.");
        if (string.Equals(normalizedScope, "Tòa nhà", StringComparison.OrdinalIgnoreCase))
            dto.ApartmentId = null; // Không gắn căn hộ khi scope là tòa nhà
        dto.Scope = normalizedScope;

        var normalizedPriority = (dto.Priority ?? string.Empty).Trim();
        if (!AllowedPriorities.Contains(normalizedPriority))
            throw new ArgumentException("Priority không hợp lệ. Chỉ chấp nhận: Thấp, Bình thường, Khẩn cấp.");
        // Normalize priority to standard format from AllowedPriorities
        dto.Priority = AllowedPriorities.First(p => string.Equals(p, normalizedPriority, StringComparison.OrdinalIgnoreCase));

        var normalizedCategory = (dto.Category ?? string.Empty).Trim();
        if (!AllowedCategories.Contains(normalizedCategory))
            throw new ArgumentException("Category không hợp lệ. Chỉ chấp nhận: Bảo trì, An ninh, Hóa đơn, Khiếu nại, Vệ sinh, Bãi đỗ xe, Tiện ích, Khác.");
        // Normalize category to standard format from AllowedCategories
        dto.Category = AllowedCategories.First(c => string.Equals(c, normalizedCategory, StringComparison.OrdinalIgnoreCase));

        var entity = dto.ToEntity();
        // Chỉ set ngày hoàn thành dự kiến khi có mức độ ưu tiên
        entity.ExpectedCompletionAt = TicketPriorityHelper.CalculateExpectedCompletionDate(entity.Priority, entity.CreatedAt);
        var created = await _repo.AddAsync(entity);
        return created.ToDto();
    }

    public async Task<TicketDto?> UpdateAsync(UpdateTicketDto dto)
    {
        var entity = await _repo.GetByIdAsync(dto.TicketId);
        if (entity == null) return null;
        if (string.Equals(entity.Status, "Đã đóng", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ticket đã đóng, không thể chỉnh sửa.");
        // Không cho chỉnh mức độ ưu tiên nếu ticket đã hoàn thành
        if (string.Equals(entity.Status, "Hoàn thành", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(entity.Priority, dto.Priority, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Ticket đã hoàn thành, không thể chỉnh sửa mức độ ưu tiên.");
        }
        var subject = (dto.Subject ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Tiêu đề là bắt buộc.");
        if (subject.Length < 3) throw new ArgumentException("Tiêu đề phải có ít nhất 3 ký tự.");
        if (subject.Length > 255) throw new ArgumentException("Tiêu đề không được vượt quá 255 ký tự.");
        dto.Subject = subject;
        if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > 4000)
            throw new ArgumentException("Mô tả không được vượt quá 4000 ký tự.");
        if (string.IsNullOrWhiteSpace(dto.Category)) throw new ArgumentException("Category là bắt buộc.");

        // Không update scope - giữ nguyên scope của entity
        // Nhưng đảm bảo logic: nếu scope là "Tòa nhà" thì apartmentId phải null
        dto.Scope = entity.Scope;
        var normalizedScope = AllowedScopes.FirstOrDefault(s => string.Equals(s, entity.Scope, StringComparison.OrdinalIgnoreCase));
        if (normalizedScope != null && string.Equals(normalizedScope, "Tòa nhà", StringComparison.OrdinalIgnoreCase))
        {
            // Scope là "Tòa nhà" -> không được có apartmentId
            dto.ApartmentId = null;
        }
        else
        {
            // Giữ nguyên apartmentId từ entity
            dto.ApartmentId = entity.ApartmentId;
        }

        var normalizedPriority = (dto.Priority ?? string.Empty).Trim();
        if (!AllowedPriorities.Contains(normalizedPriority))
            throw new ArgumentException("Priority không hợp lệ. Chỉ chấp nhận: Thấp, Bình thường, Khẩn cấp.");
        // Normalize priority to standard format from AllowedPriorities
        dto.Priority = AllowedPriorities.First(p => string.Equals(p, normalizedPriority, StringComparison.OrdinalIgnoreCase));

        var normalizedCategory = (dto.Category ?? string.Empty).Trim();
        if (!AllowedCategories.Contains(normalizedCategory))
            throw new ArgumentException("Category không hợp lệ. Chỉ chấp nhận: Bảo trì, An ninh, Hóa đơn, Khiếu nại, Vệ sinh, Bãi đỗ xe, Tiện ích, Khác.");
        // Normalize category to standard format from AllowedCategories
        dto.Category = AllowedCategories.First(c => string.Equals(c, normalizedCategory, StringComparison.OrdinalIgnoreCase));

        // Ghi nhận thay đổi để add comment sau khi lưu
        var changes = new List<string>();
        string FormatExpected(DateTime? dt) => dt.HasValue ? dt.Value.ToString("dd/MM/yyyy HH:mm") : "—";
        var originalExpected = entity.ExpectedCompletionAt;

        if (!string.Equals(entity.Category, dto.Category, StringComparison.OrdinalIgnoreCase))
            changes.Add($"Category: \"{entity.Category}\" → \"{dto.Category}\"");
        if (!string.Equals(entity.Priority, dto.Priority, StringComparison.OrdinalIgnoreCase))
            changes.Add($"Priority: \"{entity.Priority}\" → \"{dto.Priority}\"");
        if (!string.Equals(entity.Subject, dto.Subject, StringComparison.Ordinal))
            changes.Add("Subject thay đổi");
        if (!string.Equals(entity.Description ?? string.Empty, dto.Description ?? string.Empty, StringComparison.Ordinal))
            changes.Add("Description thay đổi");

        entity.ApplyUpdate(dto);

        // Đảm bảo logic: nếu scope là "Tòa nhà" thì apartmentId phải null
        if (!string.IsNullOrWhiteSpace(entity.Scope))
        {
            var entityScopeNormalized = AllowedScopes.FirstOrDefault(s => string.Equals(s, entity.Scope, StringComparison.OrdinalIgnoreCase));
            if (entityScopeNormalized != null && string.Equals(entityScopeNormalized, "Tòa nhà", StringComparison.OrdinalIgnoreCase))
            {
                entity.ApartmentId = null;
            }
        }

        // Chỉ set ngày hoàn thành dự kiến khi có mức độ ưu tiên
        if (!string.IsNullOrWhiteSpace(entity.Priority))
        {
            // Chuẩn hóa giờ VN nếu FE gửi vào
            DateTime? normalizedExpected = null;
            if (dto.ExpectedCompletionAt.HasValue)
            {
                var incoming = dto.ExpectedCompletionAt.Value;
                if (incoming.Kind == DateTimeKind.Utc)
                {
                    incoming = incoming.AddHours(7);
                }
                incoming = DateTime.SpecifyKind(incoming, DateTimeKind.Unspecified);

                // Validate: chỉ cho phép đặt từ thời điểm hiện tại + 30 phút trở đi (VietnamNow)
                var now = VietnamNow;
                var min = now.AddMinutes(30);
                if (incoming < min)
                {
                    throw new ArgumentException("Ngày hoàn thành dự kiến phải lớn hơn hoặc bằng 30 phút kể từ thời điểm hiện tại.");
                }

                normalizedExpected = incoming;
            }

            var newExpected = normalizedExpected
                ?? TicketPriorityHelper.CalculateExpectedCompletionDate(entity.Priority, entity.CreatedAt);

            // Ghi nhận thay đổi SLA
            if (!Nullable.Equals(originalExpected, newExpected))
            {
                changes.Add($"Ngày hoàn thành dự kiến: \"{FormatExpected(originalExpected)}\" → \"{FormatExpected(newExpected)}\"");
            }

            entity.ExpectedCompletionAt = newExpected;
        }
        else
        {
            // Nếu không có priority, xóa ngày hoàn thành dự kiến
            entity.ExpectedCompletionAt = null;
        }
        await _repo.UpdateAsync(entity);

        // Thêm comment mô tả thay đổi
        if (changes.Count > 0)
        {
            var comment = new TicketComment
            {
                CommentId = Guid.NewGuid(),
                TicketId = entity.TicketId,
                Content = "Đã cập nhật: " + string.Join("; ", changes),
                CommentTime = VietnamNow,
                CommentedBy = dto.UpdatedByUserId
            };
            await _repo.AddCommentAsync(comment);
        }

        return entity.ToDto();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var exists = await _repo.GetByIdAsync(id);
        if (exists is null) return false;
        await _repo.DeleteAsync(id);
        return true;
    }

    public async Task<TicketDto?> ChangeStatusAsync(ChangeTicketStatusDto dto)
    {
        var entity = await _repo.GetByIdAsync(dto.TicketId);
        if (entity == null) return null;
        if (string.Equals(entity.Status, "Đã đóng", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ticket đã đóng, không thể đổi trạng thái.");
        var status = (dto.Status ?? string.Empty).Trim();
        if (!AllowedStatuses.Contains(status))
            throw new ArgumentException("Status không hợp lệ. Chỉ chấp nhận: Mới tạo, Đã tiếp nhận, Đang xử lý, Hoàn thành, Đã đóng.");

        // Không cho phép nhảy/lùi sai thứ tự
        if (AllowedStatusTransitions.TryGetValue(entity.Status, out var nextStatuses))
        {
            if (!nextStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Không thể chuyển từ \"{entity.Status}\" sang \"{status}\". Vui lòng đi theo thứ tự quy trình.");
        }
        else
        {
            throw new InvalidOperationException($"Trạng thái hiện tại \"{entity.Status}\" không hợp lệ để chuyển tiếp.");
        }

        // Không cho phép đóng khi còn hóa đơn chưa thanh toán hoặc phiếu chi chưa được duyệt
        if (string.Equals(status, "Đã đóng", StringComparison.OrdinalIgnoreCase))
        {
            // Kiểm tra hóa đơn chưa thanh toán
            var invoices = await _context.Invoices
                .Where(i => i.TicketId == dto.TicketId)
                .ToListAsync();

            var hasUnpaidInvoice = invoices.Any(i =>
                !string.Equals(i.Status, "PAID", StringComparison.OrdinalIgnoreCase));

            // Kiểm tra phiếu chi chưa được duyệt
            var vouchers = await _context.Vouchers
                .Where(v => v.TicketId == dto.TicketId)
                .ToListAsync();

            var hasUnapprovedVoucher = vouchers.Any(v =>
                !string.Equals(v.Status, "APPROVED", StringComparison.OrdinalIgnoreCase));

            // Tạo danh sách lỗi
            var errorMessages = new List<string>();

            if (hasUnpaidInvoice)
            {
                var unpaidInvoices = invoices
                    .Where(i => !string.Equals(i.Status, "PAID", StringComparison.OrdinalIgnoreCase))
                    .Select(i => i.InvoiceNo ?? i.InvoiceId.ToString())
                    .ToList();
                errorMessages.Add($"Các hóa đơn chưa thanh toán: {string.Join(", ", unpaidInvoices)}");
            }

            if (hasUnapprovedVoucher)
            {
                var unapprovedVouchers = vouchers
                    .Where(v => !string.Equals(v.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                    .Select(v => v.VoucherNumber ?? v.VoucherId.ToString())
                    .ToList();
                errorMessages.Add($"Các phiếu chi chưa được duyệt: {string.Join(", ", unapprovedVouchers)}");
            }

            if (errorMessages.Any())
            {
                throw new InvalidOperationException(
                    $"Không thể đóng ticket khi còn tài chính chưa được xử lý. " +
                    string.Join(" | ", errorMessages));
            }
        }

        var oldStatus = entity.Status;
        entity.Status = status;
        // Lưu giờ VN để tránh hiển thị lệch múi giờ
        entity.UpdatedAt = VietnamNow;

        // Set ClosedAt khi đóng ticket
        if (string.Equals(status, "Đã đóng", StringComparison.OrdinalIgnoreCase))
        {
            entity.ClosedAt = VietnamNow;
        }

        await _repo.UpdateAsync(entity);
        await AddSystemComment(dto.TicketId, $"Trạng thái được thay đổi từ: \"{oldStatus}\" → \"{status}\"", dto.ChangedByUserId);
        return entity.ToDto();
    }


    public async Task<IEnumerable<TicketCommentDto>> GetCommentsAsync(Guid ticketId)
    {
        var q = _repo.QueryComments().Where(c => c.TicketId == ticketId).OrderBy(c => c.CommentTime);
        var items = await q.ToListAsync();

        var dtos = items.ToDto().ToList();
        await PopulateCommentUserNamesAsync(dtos);
        return dtos;
    }

    public async Task<TicketCommentDto> AddCommentAsync(CreateTicketCommentDto dto)
    {
        var t = await _repo.GetByIdAsync(dto.TicketId);
        if (t == null) throw new ArgumentException("Ticket không tồn tại");
        if (string.Equals(t.Status, "Đã đóng", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ticket đã đóng, không thể thêm bình luận.");
        if (string.IsNullOrWhiteSpace(dto.Content))
            throw new ArgumentException("Content là bắt buộc.");
        if (dto.Content.Length > 4000)
            throw new ArgumentException("Nội dung bình luận không được vượt quá 4000 ký tự.");
        var entity = dto.ToEntity();
        var created = await _repo.AddCommentAsync(entity);
        var createdDto = created.ToDto();
        await PopulateCommentUserNamesAsync(new[] { createdDto });
        return createdDto;
    }

    // Ticket Attachments methods
    public async Task<IEnumerable<TicketAttachmentDto>> GetAttachmentsAsync(Guid ticketId)
    {
        var items = await _repo.GetAttachmentsByTicketIdAsync(ticketId);
        return items.ToDto();
    }

    public async Task<TicketAttachmentDto> AddAttachmentAsync(CreateTicketAttachmentDto dto)
    {
        var t = await _repo.GetByIdAsync(dto.TicketId);
        if (t == null) throw new ArgumentException("Ticket không tồn tại");
        if (string.Equals(t.Status, "Đã đóng", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ticket đã đóng, không thể thêm đính kèm.");
        var entity = dto.ToEntity();
        var created = await _repo.AddAttachmentAsync(entity);
        return created.ToDto();
    }

    public async Task<bool> DeleteAttachmentAsync(Guid attachmentId)
    {
        var exists = await _repo.GetAttachmentByIdAsync(attachmentId);
        if (exists is null) return false;
        await _repo.DeleteAttachmentAsync(attachmentId);
        return true;
    }

    // File methods
    public async Task<FileDto> UploadFileAsync(IFormFile file, string subFolder, string? uploadedBy)
    {
        // Use CloudinaryStorageHelper to upload file
        var uploadedFile = await _fileStorage.SaveAsync(file, "tickets", uploadedBy);

        // Save file info to database
        var created = await _repo.AddFileAsync(uploadedFile);
        return created.ToDto();
    }

    public async Task<FileDto?> GetFileAsync(Guid fileId)
    {
        var entity = await _repo.GetFileByIdAsync(fileId);
        return entity?.ToDto();
    }

    public async Task<bool> DeleteFileAsync(Guid fileId)
    {
        var exists = await _repo.GetFileByIdAsync(fileId);
        if (exists is null) return false;
        await _repo.DeleteFileAsync(fileId);
        return true;
    }

    // Ticket related data methods
    public async Task<IEnumerable<InvoiceDetailDto>> GetInvoiceDetailsAsync(Guid ticketId)
    {
        // Lấy invoice theo ticketId, sau đó lấy tất cả details của invoice đó
        var invoice = await _context.Invoices
            .Where(i => i.TicketId == ticketId)
            .FirstOrDefaultAsync();

        if (invoice == null)
        {
            return Enumerable.Empty<InvoiceDetailDto>();
        }

        var q = _repo.QueryInvoiceDetails().Where(i => i.InvoiceId == invoice.InvoiceId);
        var items = await q.ToListAsync();
        return items.ToDto();
    }

    public async Task<IEnumerable<VoucherItemDto>> GetVoucherItemsAsync(Guid ticketId)
    {
        // Lấy voucher theo ticketId, sau đó lấy tất cả items của voucher đó
        var voucher = await _context.Vouchers
            .Where(v => v.TicketId == ticketId)
            .FirstOrDefaultAsync();

        if (voucher == null)
        {
            return Enumerable.Empty<VoucherItemDto>();
        }

        var q = _repo.QueryVoucherItems().Where(v => v.VoucherId == voucher.VoucherId);
        var items = await q.ToListAsync();
        return items.ToDto();
    }

    private async Task PopulateCommentUserNamesAsync(IEnumerable<TicketCommentDto> comments)
    {
        var list = comments?.ToList();
        if (list == null || list.Count == 0) return;

        var userIds = list.Where(c => c.CommentedBy.HasValue)
            .Select(c => c.CommentedBy!.Value)
            .Distinct()
            .ToList();

        var userNameLookup = new Dictionary<Guid, string>();
        if (userIds.Count > 0)
        {
            var users = await _userRepo.GetByIdsAsync(userIds);
            if (users != null)
            {
                foreach (var user in users)
                {
                    if (user != null)
                    {
                        userNameLookup[user.UserId] = !string.IsNullOrWhiteSpace(user.Username)
                            ? user.Username
                            : (!string.IsNullOrWhiteSpace(user.Email) ? user.Email : user.UserId.ToString());
                    }
                }
            }
        }

        foreach (var comment in list)
        {
            if (comment.CommentedBy.HasValue && userNameLookup.TryGetValue(comment.CommentedBy.Value, out var username))
            {
                comment.CreatedByUserName = username;
            }
            else
            {
                comment.CreatedByUserName = "Unknown";
            }
        }
    }
}


