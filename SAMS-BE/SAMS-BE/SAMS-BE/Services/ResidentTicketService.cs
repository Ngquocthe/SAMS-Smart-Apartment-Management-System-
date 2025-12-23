using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Mappers;
using SAMS_BE.Models;
using SAMS_BE.Helpers;
using Microsoft.AspNetCore.Http;
using SAMS_BE.Enums;

namespace SAMS_BE.Services;

public class ResidentTicketService : IResidentTicketService
{
    private readonly IResidentTicketRepository _repository;
    private readonly IUserService _userService;
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageHelper _fileStorageHelper;
    private readonly ILogger<ResidentTicketService> _logger;

    public ResidentTicketService(
        IResidentTicketRepository repository,
        IUserService userService,
        IUserRepository userRepository,
        IFileStorageHelper fileStorageHelper,
        ILogger<ResidentTicketService> logger)
    {
        _repository = repository;
        _userService = userService;
        _userRepository = userRepository;
        _fileStorageHelper = fileStorageHelper;
        _logger = logger;
    }

    public async Task<ResidentTicketDto> CreateMaintenanceTicketAsync(CreateMaintenanceTicketDto dto, Guid userId)
    {
        // Validate Subject
        var subject = (dto.Subject ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Tiêu đề là bắt buộc.");
        if (subject.Length < 3)
            throw new ArgumentException("Tiêu đề phải có ít nhất 3 ký tự.");
        if (subject.Length > 255)
            throw new ArgumentException("Tiêu đề không được vượt quá 255 ký tự.");
        dto.Subject = subject;

        // Validate Description
        var description = (dto.Description ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Mô tả là bắt buộc.");
        if (description.Length > 4000)
            throw new ArgumentException("Mô tả không được vượt quá 4000 ký tự.");
        dto.Description = description;

        // 1. Lấy apartment ID
        Guid apartmentId;
        if (dto.ApartmentId.HasValue)
        {
            apartmentId = dto.ApartmentId.Value;
        }
        else
        {
            // Tự động lấy primary apartment của user
            var apartment = await _userService.GetUserPrimaryApartmentAsync(userId);
            if (apartment == null)
            {
                throw new InvalidOperationException("User does not have a primary apartment");
            }
            apartmentId = apartment.ApartmentId;
        }

        // 2. Tạo ticket entity
        var ticket = dto.ToEntity(userId, apartmentId);
        // Chỉ set ngày hoàn thành dự kiến khi có mức độ ưu tiên
        ticket.ExpectedCompletionAt = TicketPriorityHelper.CalculateExpectedCompletionDate(ticket.Priority, ticket.CreatedAt);

        // 3. Lưu ticket
        var createdTicket = await _repository.AddAsync(ticket);

        // 4. Thêm attachments nếu có
        if (dto.AttachmentFileIds != null && dto.AttachmentFileIds.Any())
        {
            // Kiểm tra số lượng file không vượt quá 5
            if (dto.AttachmentFileIds.Count > 5)
            {
                throw new ArgumentException("Chỉ được phép đính kèm tối đa 5 file.");
            }

            foreach (var fileId in dto.AttachmentFileIds)
            {
                // Lấy file để lấy tên file
                var file = await _repository.GetFileByIdAsync(fileId);
                if (file != null)
                {
                    var attachment = new TicketAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        TicketId = createdTicket.TicketId,
                        FileId = fileId,
                        UploadedBy = userId,
                        Note = file.OriginalName, // Lưu tên file vào note
                        UploadedAt = DateTimeHelper.VietnamNow
                    };
                    await _repository.AddAttachmentAsync(attachment);
                }
            }
        }

        // 5. Reload ticket với attachments
        var result = await _repository.GetByIdAsync(createdTicket.TicketId);
        return result!.ToResidentDto();
    }

    public async Task<ResidentTicketDto> CreateComplaintTicketAsync(CreateComplaintTicketDto dto, Guid userId)
    {
        // Validate Subject
        var subject = (dto.Subject ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Tiêu đề là bắt buộc.");
        if (subject.Length < 3)
            throw new ArgumentException("Tiêu đề phải có ít nhất 3 ký tự.");
        if (subject.Length > 255)
            throw new ArgumentException("Tiêu đề không được vượt quá 255 ký tự.");
        dto.Subject = subject;

        // Validate Description
        var description = (dto.Description ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Mô tả là bắt buộc.");
        if (description.Length > 4000)
            throw new ArgumentException("Mô tả không được vượt quá 4000 ký tự.");
        dto.Description = description;

        // 1. Lấy apartment ID
        // Nếu có ApartmentId trong dto → khiếu nại về căn hộ cụ thể (scope: "Theo căn hộ")
        // Nếu không có ApartmentId trong dto → khiếu nại về tòa nhà (scope: "Tòa nhà", ApartmentId = null)
        Guid apartmentId = Guid.Empty; // Default value, sẽ được xử lý trong mapper
        if (dto.ApartmentId.HasValue)
        {
            apartmentId = dto.ApartmentId.Value;
        }
        else
        {
            // Tự động lấy primary apartment của user
            var apartment = await _userService.GetUserPrimaryApartmentAsync(userId);
            if (apartment == null)
            {
                throw new InvalidOperationException("User does not have a primary apartment");
            }
            apartmentId = apartment.ApartmentId;
        }

        // 2. Tạo ticket entity
        var ticket = dto.ToEntity(userId, apartmentId);
        // Chỉ set ngày hoàn thành dự kiến khi có mức độ ưu tiên
        ticket.ExpectedCompletionAt = TicketPriorityHelper.CalculateExpectedCompletionDate(ticket.Priority, ticket.CreatedAt);

        // 3. Lưu ticket
        var createdTicket = await _repository.AddAsync(ticket);

        // 4. Thêm attachments nếu có
        if (dto.AttachmentFileIds != null && dto.AttachmentFileIds.Any())
        {
            // Kiểm tra số lượng file không vượt quá 5
            if (dto.AttachmentFileIds.Count > 5)
            {
                throw new ArgumentException("Chỉ được phép đính kèm tối đa 5 file.");
            }

            foreach (var fileId in dto.AttachmentFileIds)
            {
                // Lấy file để lấy tên file
                var file = await _repository.GetFileByIdAsync(fileId);
                if (file != null)
                {
                    var attachment = new TicketAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        TicketId = createdTicket.TicketId,
                        FileId = fileId,
                        UploadedBy = userId,
                        Note = file.OriginalName, // Lưu tên file vào note
                        UploadedAt = DateTimeHelper.VietnamNow
                    };
                    await _repository.AddAttachmentAsync(attachment);
                }
            }
        }

        // 5. Reload ticket với attachments
        var result = await _repository.GetByIdAsync(createdTicket.TicketId);
        return result!.ToResidentDto();
    }

    public async Task<ResidentTicketDto> CreateVehicleRegistrationTicketAsync(CreateVehicleRegistrationTicketDto dto, Guid userId)
    {
        _logger.LogInformation("=== START CreateVehicleRegistrationTicketAsync ===");
        _logger.LogInformation("UserId: {UserId}", userId);
        
        // 1. Lấy apartment ID và ResidentId
        Guid apartmentId;
        Guid? residentId = null;
        
        if (dto.ApartmentId.HasValue)
        {
            apartmentId = dto.ApartmentId.Value;
            _logger.LogInformation("Using provided ApartmentId: {ApartmentId}", apartmentId);
        }
        else
        {
            _logger.LogInformation("ApartmentId not provided, auto-detecting...");
            // Tự động lấy primary apartment của user
            var apartment = await _userService.GetUserPrimaryApartmentAsync(userId);
            if (apartment == null)
            {
                _logger.LogWarning("User {UserId} does not have a primary apartment", userId);
                throw new InvalidOperationException("User does not have a primary apartment");
            }
            apartmentId = apartment.ApartmentId;
            _logger.LogInformation("Auto-detected ApartmentId: {ApartmentId}", apartmentId);
        }

        // 2. Lấy ResidentId từ userId (cần để link với bảng vehicles)
        _logger.LogInformation("Looking up ResidentProfile for UserId: {UserId}", userId);
        var residentProfile = await _repository.GetResidentProfileByUserIdAsync(userId);
        if (residentProfile != null)
        {
            residentId = residentProfile.ResidentId;
            _logger.LogInformation("Found ResidentId: {ResidentId}", residentId);
        }
        else
        {
            _logger.LogWarning("No ResidentProfile found for UserId: {UserId}", userId);
        }

        // 3. Kiểm tra biển số xe đã tồn tại chưa
        _logger.LogInformation("Checking if license plate exists: {LicensePlate}", dto.VehicleInfo.LicensePlate);
        var existingVehicle = await _repository.GetVehicleByLicensePlateAsync(dto.VehicleInfo.LicensePlate);
        if (existingVehicle != null)
        {
            _logger.LogWarning("License plate {LicensePlate} already exists", dto.VehicleInfo.LicensePlate);
            throw new InvalidOperationException($"Vehicle with license plate {dto.VehicleInfo.LicensePlate} already exists");
        }
        _logger.LogInformation("License plate is available");

        // 4. Tạo ticket entity
        _logger.LogInformation("Creating ticket entity...");
        var ticket = new Ticket
        {
            TicketId = Guid.NewGuid(),
            CreatedByUserId = userId,
            Category = "Đăng ký xe",
            Priority = null, // Không cần priority cho đăng ký xe
            Subject = dto.Subject,
            Description = dto.Description,
            Status = "Mới tạo", // Pending
            Scope = "Theo căn hộ",
            ApartmentId = apartmentId,
            HasInvoice = false,
            CreatedAt = DateTimeHelper.VietnamNow,
            UpdatedAt = DateTimeHelper.VietnamNow
        };

        // Không cần ExpectedCompletionAt cho ticket đăng ký xe

        // 5. Lưu ticket
        _logger.LogInformation("Saving ticket to database...");
        var createdTicket = await _repository.AddAsync(ticket);
        _logger.LogInformation("Ticket created with ID: {TicketId}", createdTicket.TicketId);

        // 6. Tạo vehicle entity (không cần set TicketId - FK chỉ ở Ticket)
        _logger.LogInformation("Creating vehicle entity...");
        var vehicle = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            ResidentId = residentId, // Có thể null nếu user chưa có resident profile
            ApartmentId = apartmentId,
            VehicleTypeId = dto.VehicleInfo.VehicleTypeId,
            LicensePlate = dto.VehicleInfo.LicensePlate,
            Color = dto.VehicleInfo.Color,
            BrandModel = dto.VehicleInfo.BrandModel,
            RegisteredAt = DateTime.UtcNow.AddHours(7),
            Status = VehicleStatus.PENDING.ToString(), // Chờ duyệt
            Meta = dto.VehicleInfo.Meta
        };

        _logger.LogInformation("Saving vehicle to database. VehicleId: {VehicleId}, ResidentId: {ResidentId}", 
            vehicle.VehicleId, residentId?.ToString() ?? "null");
        await _repository.AddVehicleAsync(vehicle);
        _logger.LogInformation("Vehicle saved successfully");

        // 7. Update ticket với VehicleId (FK chỉ ở Ticket)
        _logger.LogInformation("Updating ticket with VehicleId: {VehicleId}", vehicle.VehicleId);
        createdTicket.VehicleId = vehicle.VehicleId;
        await _repository.UpdateAsync(createdTicket);
        _logger.LogInformation("Ticket updated with VehicleId");

        // 7. Thêm attachments nếu có
        if (dto.AttachmentFileIds != null && dto.AttachmentFileIds.Any())
        {
            _logger.LogInformation("Processing {Count} attachments...", dto.AttachmentFileIds.Count);
            int attachmentCount = 0;
            
            foreach (var fileId in dto.AttachmentFileIds)
            {
                _logger.LogInformation("Processing attachment FileId: {FileId}", fileId);
                
                // Lấy file để lấy tên file
                var file = await _repository.GetFileByIdAsync(fileId);
                if (file != null)
                {
                    var attachment = new TicketAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        TicketId = createdTicket.TicketId,
                        FileId = fileId,
                        UploadedBy = userId,
                        Note = file.OriginalName, // Lưu tên file vào note
                        UploadedAt = DateTimeHelper.VietnamNow
                    };
                    await _repository.AddAttachmentAsync(attachment);
                    attachmentCount++;
                    _logger.LogInformation("Attachment saved: {AttachmentId}, FileName: {FileName}", 
                        attachment.AttachmentId, file.OriginalName);
                }
                else
                {
                    _logger.LogWarning("File not found for FileId: {FileId}", fileId);
                }
            }
            
            _logger.LogInformation("Total attachments saved: {Count}", attachmentCount);
        }
        else
        {
            _logger.LogInformation("No attachments to process");
        }

        // 8. Reload ticket với attachments
        _logger.LogInformation("Reloading ticket with attachments...");
        var result = await _repository.GetByIdAsync(createdTicket.TicketId);
        _logger.LogInformation("=== END CreateVehicleRegistrationTicketAsync ===");
        
        return result!.ToResidentDto();
    }

    public async Task<(IEnumerable<ResidentTicketDto> items, int total)> GetMyTicketsAsync(
        ResidentTicketQueryDto query, Guid userId)
    {
        var q = _repository.Query()
            .Where(t => t.CreatedByUserId == userId);

        // Filter by status
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            q = q.Where(t => t.Status == query.Status);
        }

        // Filter by category
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            q = q.Where(t => t.Category == query.Category);
        }

        // Filter by priority
        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            q = q.Where(t => t.Priority == query.Priority);
        }

        // Filter by date range
        if (query.FromDate.HasValue)
        {
            q = q.Where(t => t.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            q = q.Where(t => t.CreatedAt <= query.ToDate.Value);
        }

        // Get total count
        var total = await q.CountAsync();

        // Order by created date descending
        q = q.OrderByDescending(t => t.CreatedAt);

        // Pagination
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items.ToResidentDto(), total);
    }

    public async Task<IEnumerable<ResidentTicketInvoiceDto>> GetInvoicesForTicketAsync(Guid ticketId, Guid userId)
    {
        var ticket = await _repository.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found");
        }

        if (ticket.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view invoices of this ticket");
        }

        return await _repository.GetInvoicesByTicketIdAsync(ticketId);
    }

    public async Task<ResidentTicketStatisticsDto> GetMyTicketStatisticsAsync(Guid userId)
    {
        var q = _repository.Query()
            .Where(t => t.CreatedByUserId == userId);

        var groups = await q
            .GroupBy(t => t.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        int total = groups.Sum(g => g.Count);
        int completed = groups.FirstOrDefault(g => g.Status == "Hoàn thành")?.Count ?? 0;
        int closed = groups.FirstOrDefault(g => g.Status == "Đã đóng")?.Count ?? 0;
        int inProgress = groups.FirstOrDefault(g => g.Status == "Đang xử lý")?.Count ?? 0;
        int pending = groups.FirstOrDefault(g => g.Status == "Mới tạo")?.Count ?? 0;

        return new ResidentTicketStatisticsDto
        {
            Total = total,
            Completed = completed,
            Closed = closed,
            InProgress = inProgress,
            Pending = pending
        };
    }

    public async Task<ResidentTicketDto?> GetTicketByIdAsync(Guid ticketId, Guid userId)
    {
        var ticket = await _repository.GetByIdAsync(ticketId);

        if (ticket == null)
        {
            return null;
        }

        // Kiểm tra quyền: chỉ người tạo ticket mới được xem
        if (ticket.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this ticket");
        }

        var ticketDto = ticket.ToResidentDto();

        // Load usernames cho comments
        if (ticketDto.Comments != null && ticketDto.Comments.Any())
        {
            await PopulateCommentUserNamesAsync(ticketDto.Comments);
        }

        return ticketDto;
    }

    public async Task<TicketCommentDto> AddCommentAsync(CreateResidentTicketCommentDto dto, Guid userId)
    {
        // 1. Kiểm tra ticket có tồn tại không
        var ticket = await _repository.GetByIdAsync(dto.TicketId);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found");
        }

        // 2. Kiểm tra quyền: chỉ người tạo ticket mới được comment
        if (ticket.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to comment on this ticket");
        }

        // 3. Tạo comment
        var comment = dto.ToEntity(userId);
        var createdComment = await _repository.AddCommentAsync(comment);

        // 4. Load username cho comment
        var commentDto = createdComment.ToDto();
        var user = await _userService.GetLoginUserAsync(userId);
        if (user != null)
        {
            commentDto.CreatedByUserName = user.Username;
        }

        return commentDto;
    }

    public async Task<IEnumerable<TicketCommentDto>> GetCommentsAsync(Guid ticketId, Guid userId)
    {
        // 1. Kiểm tra ticket có tồn tại không
        var ticket = await _repository.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found");
        }

        // 2. Kiểm tra quyền: chỉ người tạo ticket mới được xem comments
        if (ticket.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view comments of this ticket");
        }

        // 3. Lấy comments (không bao gồm internal comments)
        var comments = await _repository.QueryComments()
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CommentTime)
            .ToListAsync();

        // 4. Load usernames cho comments
        var commentDtos = comments.ToDto().ToList();
        await PopulateCommentUserNamesAsync(commentDtos);

        return commentDtos;
    }

    public async Task<FileDto> UploadFileAsync(IFormFile file, Guid userId)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty");
        }

        // 1. Upload file lên Cloudinary sử dụng CloudinaryStorageHelper
        var fileEntity = await _fileStorageHelper.SaveAsync(file, "resident-tickets", userId.ToString());

        // 2. Lưu vào database
        var createdFile = await _repository.AddFileAsync(fileEntity);

        return createdFile.ToDto();
    }

    public async Task<TicketAttachmentDto> AddAttachmentAsync(Guid ticketId, Guid fileId, string? note, Guid userId)
    {
        // 1. Kiểm tra ticket có tồn tại không
        var ticket = await _repository.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found");
        }

        // 2. Kiểm tra quyền: chỉ người tạo ticket mới được thêm attachment
        if (ticket.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to add attachment to this ticket");
        }

        // 3. Kiểm tra file có tồn tại không
        var file = await _repository.GetFileByIdAsync(fileId);
        if (file == null)
        {
            throw new ArgumentException("File not found");
        }

        // 4. Tạo attachment - Lưu tên file vào note
        // Nếu user cung cấp note thì dùng note đó, không thì dùng tên file
        var finalNote = string.IsNullOrWhiteSpace(note)
            ? file.OriginalName
            : note;

        var attachment = new TicketAttachment
        {
            AttachmentId = Guid.NewGuid(),
            TicketId = ticketId,
            FileId = fileId,
            UploadedBy = userId,
            Note = finalNote, // Lưu tên file hoặc note vào note
            UploadedAt = DateTime.UtcNow.AddHours(7)
        };

        var createdAttachment = await _repository.AddAttachmentAsync(attachment);

        // 5. Reload attachment với file info
        var result = await _repository.GetAttachmentByIdAsync(createdAttachment.AttachmentId);
        return result!.ToDto();
    }

    public async Task<IEnumerable<TicketAttachmentDto>> GetAttachmentsAsync(Guid ticketId, Guid userId)
    {
        // 1. Kiểm tra ticket có tồn tại không
        var ticket = await _repository.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found");
        }

        // 2. Kiểm tra quyền: chỉ người tạo ticket mới được xem attachments
        if (ticket.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view attachments of this ticket");
        }

        // 3. Lấy attachments (không select UploadedBy)
        var attachmentData = await _repository.QueryAttachments()
            .Where(a => a.TicketId == ticketId)
            .Select(a => new
            {
                a.AttachmentId,
                a.TicketId,
                a.FileId,
                a.Note,
                a.UploadedAt
            })
            .OrderBy(a => a.UploadedAt)
            .ToListAsync();

        // 4. Load File cho mỗi attachment
        var attachments = new List<TicketAttachment>();
        foreach (var data in attachmentData)
        {
            var attachment = new TicketAttachment
            {
                AttachmentId = data.AttachmentId,
                TicketId = data.TicketId,
                FileId = data.FileId,
                UploadedBy = null, // Không load từ DB
                Note = data.Note,
                UploadedAt = data.UploadedAt
            };

            var fileEntity = await _repository.GetFileByIdAsync(data.FileId);
            attachment.File = fileEntity ?? throw new InvalidOperationException("Attachment file missing");
            attachments.Add(attachment);
        }

        return attachments.ToDto();
    }

    public async Task<bool> DeleteAttachmentAsync(Guid attachmentId, Guid userId)
    {
        // 1. Kiểm tra attachment có tồn tại không
        var attachment = await _repository.GetAttachmentByIdAsync(attachmentId);
        if (attachment == null)
        {
            return false;
        }

        // 2. Kiểm tra quyền: chỉ người upload mới được xóa
        if (attachment.UploadedBy != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this attachment");
        }

        // 3. Xóa attachment
        await _repository.DeleteAttachmentAsync(attachmentId);

        return true;
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
            var users = await _userRepository.GetByIdsAsync(userIds);
            userNameLookup = users.ToDictionary(
                u => u.UserId,
                u => !string.IsNullOrWhiteSpace(u.Username)
                    ? u.Username
                    : (!string.IsNullOrWhiteSpace(u.Email) ? u.Email : u.UserId.ToString()));
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
