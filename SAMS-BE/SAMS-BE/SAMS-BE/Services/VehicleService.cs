using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Enums;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Mappers;
using SAMS_BE.Models;

namespace SAMS_BE.Services;

public class VehicleService : IVehicleService
{
    private readonly BuildingManagementContext _context;
    private readonly IResidentTicketRepository _ticketRepository;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(
        BuildingManagementContext context,
        IResidentTicketRepository ticketRepository,
        ILogger<VehicleService> logger)
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<MyVehicleDto>> GetMyVehiclesAsync(Guid userId)
    {
        _logger.LogInformation("Getting vehicles for user {UserId}", userId);

        // Lấy ResidentProfile của user
        var residentProfile = await _context.ResidentProfiles
            .FirstOrDefaultAsync(rp => rp.UserId == userId);

        if (residentProfile == null)
        {
            _logger.LogWarning("No resident profile found for user {UserId}", userId);
            return Enumerable.Empty<MyVehicleDto>();
        }

        // Lấy tất cả xe của resident
        var vehicles = await _context.Vehicles
            .Include(v => v.VehicleType)
            .Include(v => v.Apartment)
            .Include(v => v.ParkingCard)
            .Include(v => v.Resident)
            .Where(v => v.ResidentId == residentProfile.ResidentId && !v.Status.Contains("CANCELLED"))
            .OrderByDescending(v => v.RegisteredAt)
            .ToListAsync();

        // Lấy tickets liên quan
        var vehicleIds = vehicles.Select(v => v.VehicleId).ToList();
        var tickets = await _context.Tickets
            .Where(t => t.VehicleId.HasValue && vehicleIds.Contains(t.VehicleId.Value))
            .ToListAsync();

        // Lấy attachments riêng
        var ticketIds = tickets.Select(t => t.TicketId).ToList();
        var attachments = await _context.TicketAttachments
            .Where(ta => ticketIds.Contains(ta.TicketId))
            .Select(ta => new
            {
                ta.AttachmentId,
                ta.TicketId,
                ta.FileId,
                ta.Note,
                ta.UploadedAt,
                File = ta.File != null ? new
                {
                    ta.File.FileId,
                    ta.File.OriginalName,
                    ta.File.MimeType,
                    ta.File.StoragePath,
                    ta.File.UploadedAt
                } : null
            })
            .ToListAsync();

        var attachmentsByTicket = attachments.GroupBy(a => a.TicketId).ToDictionary(g => g.Key, g => g.ToList());
        var ticketsByVehicle = tickets.ToDictionary(t => t.VehicleId!.Value, t => t);

        var result = vehicles.Select(v =>
        {
            var ticket = ticketsByVehicle.ContainsKey(v.VehicleId) ? ticketsByVehicle[v.VehicleId] : null;
            var ticketAttachments = ticket != null && attachmentsByTicket.ContainsKey(ticket.TicketId) 
                ? attachmentsByTicket[ticket.TicketId] 
                : null;
            
            return new MyVehicleDto
            {
                VehicleId = v.VehicleId,
                ResidentId = v.ResidentId,
                ResidentName = v.Resident?.FullName,
                ResidentPhone = v.Resident?.Phone,
                ApartmentId = v.ApartmentId,
                ApartmentNumber = v.Apartment?.Number,
                VehicleTypeId = v.VehicleTypeId,
                VehicleTypeName = v.VehicleType?.Name,
                LicensePlate = v.LicensePlate,
                Color = v.Color,
                BrandModel = v.BrandModel,
                ParkingCardId = v.ParkingCardId,
                ParkingCardNumber = v.ParkingCard?.CardNumber,
                RegisteredAt = v.RegisteredAt,
                Status = v.Status,
                Meta = v.Meta,
                RegistrationTicketId = ticket?.TicketId,
                RegistrationTicketStatus = ticket?.Status,
                Attachments = ticketAttachments?.Select(ta => new TicketAttachmentDto
                {
                    AttachmentId = ta.AttachmentId,
                    TicketId = ta.TicketId,
                    FileId = ta.FileId,
                    Note = ta.Note,
                    UploadedAt = ta.UploadedAt,
                    File = ta.File != null ? new FileDto
                    {
                        FileId = ta.File.FileId,
                        OriginalName = ta.File.OriginalName,
                        MimeType = ta.File.MimeType,
                        StoragePath = ta.File.StoragePath,
                        UploadedAt = ta.File.UploadedAt
                    } : null
                }).ToList()
            };
        });

        _logger.LogInformation("Found {Count} vehicles for user {UserId}", result.Count(), userId);
        return result;
    }

    public async Task<ResidentTicketDto> CreateCancelVehicleTicketAsync(CreateCancelVehicleTicketDto dto, Guid userId)
    {
        _logger.LogInformation("Creating cancel vehicle ticket for VehicleId: {VehicleId}, UserId: {UserId}", 
            dto.VehicleId, userId);

        // 1. Kiểm tra xe có tồn tại không
        var vehicle = await _context.Vehicles
            .Include(v => v.VehicleType)
            .Include(v => v.Apartment)
            .FirstOrDefaultAsync(v => v.VehicleId == dto.VehicleId);

        if (vehicle == null)
        {
            throw new ArgumentException("Xe không tồn tại");
        }

        // 2. Kiểm tra xe có thuộc về user không
        var residentProfile = await _context.ResidentProfiles
            .FirstOrDefaultAsync(rp => rp.UserId == userId);

        if (residentProfile == null || vehicle.ResidentId != residentProfile.ResidentId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền hủy xe này");
        }

        // 3. Kiểm tra xe đã bị hủy chưa
        if (vehicle.Status.Contains("CANCELLED"))
        {
            throw new InvalidOperationException("Xe đã được hủy trước đó");
        }

        // 4. Tạo ticket hủy xe
        var ticket = new Ticket
        {
            TicketId = Guid.NewGuid(),
            CreatedByUserId = userId,
            Category = "Hủy gửi xe",
            Priority = null,
            Subject = dto.Subject,
            Description = dto.Description,
            Status = "Mới tạo",
            Scope = "Theo căn hộ",
            ApartmentId = vehicle.ApartmentId,
            HasInvoice = false,
            VehicleId = vehicle.VehicleId, // Link với vehicle
            CreatedAt = DateTimeHelper.VietnamNow,
            UpdatedAt = DateTimeHelper.VietnamNow
        };

        var createdTicket = await _ticketRepository.AddAsync(ticket);
        _logger.LogInformation("Cancel ticket created with ID: {TicketId}", createdTicket.TicketId);

        // 5. Thêm attachments nếu có
        if (dto.AttachmentFileIds != null && dto.AttachmentFileIds.Any())
        {
            _logger.LogInformation("Processing {Count} attachments...", dto.AttachmentFileIds.Count);

            foreach (var fileId in dto.AttachmentFileIds)
            {
                var file = await _ticketRepository.GetFileByIdAsync(fileId);
                if (file != null)
                {
                    var attachment = new TicketAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        TicketId = createdTicket.TicketId,
                        FileId = fileId,
                        Note = file.OriginalName,
                        UploadedAt = DateTimeHelper.VietnamNow
                    };

                    await _ticketRepository.AddAttachmentAsync(attachment);
                }
            }

            _logger.LogInformation("Attachments added successfully");
        }

        // 6. Load lại ticket với đầy đủ thông tin và map sang ResidentTicketDto
        var ticketWithDetails = await _ticketRepository.GetByIdAsync(createdTicket.TicketId);

        _logger.LogInformation("Cancel vehicle ticket created successfully");

        // Map sang ResidentTicketDto
        return new ResidentTicketDto
        {
            TicketId = ticketWithDetails!.TicketId,
            CreatedByUserId = ticketWithDetails.CreatedByUserId,
            CreatedByUserName = ticketWithDetails.CreatedByUser?.Username,
            Category = ticketWithDetails.Category,
            Priority = ticketWithDetails.Priority,
            Subject = ticketWithDetails.Subject,
            Description = ticketWithDetails.Description,
            Status = ticketWithDetails.Status,
            CreatedAt = ticketWithDetails.CreatedAt,
            UpdatedAt = ticketWithDetails.UpdatedAt,
            ClosedAt = ticketWithDetails.ClosedAt,
            ExpectedCompletionAt = ticketWithDetails.ExpectedCompletionAt,
            Scope = ticketWithDetails.Scope,
            ApartmentId = ticketWithDetails.ApartmentId,
            ApartmentNumber = ticketWithDetails.Apartment?.Number,
            HasInvoice = ticketWithDetails.HasInvoice,
            Attachments = ticketWithDetails.TicketAttachments?.ToDto()?.ToList(),
            Comments = ticketWithDetails.TicketComments?.ToDto()?.ToList(),
            VehicleInfo = ticketWithDetails.Vehicle != null ? new VehicleInfoDto
            {
                VehicleId = ticketWithDetails.Vehicle.VehicleId,
                VehicleTypeId = ticketWithDetails.Vehicle.VehicleTypeId,
                VehicleTypeName = ticketWithDetails.Vehicle.VehicleType?.Name,
                LicensePlate = ticketWithDetails.Vehicle.LicensePlate,
                Color = ticketWithDetails.Vehicle.Color,
                BrandModel = ticketWithDetails.Vehicle.BrandModel,
                Status = ticketWithDetails.Vehicle.Status,
                RegisteredAt = ticketWithDetails.Vehicle.RegisteredAt,
                Meta = ticketWithDetails.Vehicle.Meta
            } : null
        };
    }

    public async Task<IEnumerable<MyVehicleDto>> GetAllVehiclesAsync()
    {
        _logger.LogInformation("Getting all vehicles");

        // Lấy tất cả xe
        var vehicles = await _context.Vehicles
            .Include(v => v.VehicleType)
            .Include(v => v.Apartment)
            .Include(v => v.ParkingCard)
            .Include(v => v.Resident)
            .OrderByDescending(v => v.RegisteredAt)
            .ToListAsync();

        // Lấy tickets liên quan
        var vehicleIds = vehicles.Select(v => v.VehicleId).ToList();
        var tickets = await _context.Tickets
            .Where(t => t.VehicleId.HasValue && vehicleIds.Contains(t.VehicleId.Value))
            .ToListAsync();

        // Lấy attachments riêng
        var ticketIds = tickets.Select(t => t.TicketId).ToList();
        var attachments = await _context.TicketAttachments
            .Where(ta => ticketIds.Contains(ta.TicketId))
            .Select(ta => new
            {
                ta.AttachmentId,
                ta.TicketId,
                ta.FileId,
                ta.Note,
                ta.UploadedAt,
                File = ta.File != null ? new
                {
                    ta.File.FileId,
                    ta.File.OriginalName,
                    ta.File.MimeType,
                    ta.File.StoragePath,
                    ta.File.UploadedAt
                } : null
            })
            .ToListAsync();

        var attachmentsByTicket = attachments.GroupBy(a => a.TicketId).ToDictionary(g => g.Key, g => g.ToList());
        var ticketsByVehicle = tickets.ToDictionary(t => t.VehicleId!.Value, t => t);

        var result = vehicles.Select(v =>
        {
            var ticket = ticketsByVehicle.ContainsKey(v.VehicleId) ? ticketsByVehicle[v.VehicleId] : null;
            var ticketAttachments = ticket != null && attachmentsByTicket.ContainsKey(ticket.TicketId) 
                ? attachmentsByTicket[ticket.TicketId] 
                : null;
            
            return new MyVehicleDto
            {
                VehicleId = v.VehicleId,
                ResidentId = v.ResidentId,
                ResidentName = v.Resident?.FullName,
                ResidentPhone = v.Resident?.Phone,
                ApartmentId = v.ApartmentId,
                ApartmentNumber = v.Apartment?.Number,
                VehicleTypeId = v.VehicleTypeId,
                VehicleTypeName = v.VehicleType?.Name,
                LicensePlate = v.LicensePlate,
                Color = v.Color,
                BrandModel = v.BrandModel,
                ParkingCardId = v.ParkingCardId,
                ParkingCardNumber = v.ParkingCard?.CardNumber,
                RegisteredAt = v.RegisteredAt,
                Status = v.Status,
                Meta = v.Meta,
                RegistrationTicketId = ticket?.TicketId,
                RegistrationTicketStatus = ticket?.Status,
                Attachments = ticketAttachments?.Select(ta => new TicketAttachmentDto
                {
                    AttachmentId = ta.AttachmentId,
                    TicketId = ta.TicketId,
                    FileId = ta.FileId,
                    Note = ta.Note,
                    UploadedAt = ta.UploadedAt,
                    File = ta.File != null ? new FileDto
                    {
                        FileId = ta.File.FileId,
                        OriginalName = ta.File.OriginalName,
                        MimeType = ta.File.MimeType,
                        StoragePath = ta.File.StoragePath,
                        UploadedAt = ta.File.UploadedAt
                    } : null
                }).ToList()
            };
        });

        _logger.LogInformation("Found {Count} vehicles", result.Count());
        return result;
    }

    public async Task<MyVehicleDto> UpdateVehicleStatusAsync(Guid vehicleId, UpdateVehicleStatusDto dto)
    {
        _logger.LogInformation("Updating vehicle status. VehicleId: {VehicleId}, NewStatus: {Status}", 
            vehicleId, dto.Status);

        // 1. Lấy vehicle
        var vehicle = await _context.Vehicles
            .Include(v => v.VehicleType)
            .Include(v => v.Apartment)
            .Include(v => v.ParkingCard)
            .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);

        if (vehicle == null)
        {
            throw new ArgumentException("Xe không tồn tại");
        }

        // 2. Update vehicle status
        vehicle.Status = dto.Status;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle status updated to {Status}", dto.Status);

        // 3. Tìm và đóng ticket liên quan (nếu có)
        var relatedTicket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.VehicleId == vehicleId && t.Status != "Đã đóng");

        if (relatedTicket != null)
        {
            relatedTicket.Status = "Đã đóng";
            relatedTicket.ClosedAt = DateTimeHelper.VietnamNow;
            relatedTicket.UpdatedAt = DateTimeHelper.VietnamNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Related ticket {TicketId} closed", relatedTicket.TicketId);
        }

        // 4. Return updated vehicle
        return new MyVehicleDto
        {
            VehicleId = vehicle.VehicleId,
            ResidentId = vehicle.ResidentId,
            ApartmentId = vehicle.ApartmentId,
            ApartmentNumber = vehicle.Apartment?.Number,
            VehicleTypeId = vehicle.VehicleTypeId,
            VehicleTypeName = vehicle.VehicleType?.Name,
            LicensePlate = vehicle.LicensePlate,
            Color = vehicle.Color,
            BrandModel = vehicle.BrandModel,
            ParkingCardId = vehicle.ParkingCardId,
            ParkingCardNumber = vehicle.ParkingCard?.CardNumber,
            RegisteredAt = vehicle.RegisteredAt,
            Status = vehicle.Status,
            Meta = vehicle.Meta,
            RegistrationTicketId = relatedTicket?.TicketId,
            RegistrationTicketStatus = relatedTicket?.Status
        };
    }

    public async Task<ResidentTicketDto> CreateVehicleRegistrationTicketForManagerAsync(
        CreateVehicleRegistrationTicketDto dto, 
        Guid residentId, 
        Guid managerId)
    {
        _logger.LogInformation("=== START CreateVehicleRegistrationTicketForManagerAsync ===");
        _logger.LogInformation("ResidentId: {ResidentId}, ManagerId: {ManagerId}", residentId, managerId);
        
        // 1. Manager truyền trực tiếp apartmentId, không cần tìm kiếm
        if (!dto.ApartmentId.HasValue)
        {
            _logger.LogWarning("ApartmentId is required for manager registration");
            throw new ArgumentException("ApartmentId is required");
        }
        
        Guid apartmentId = dto.ApartmentId.Value;
        _logger.LogInformation("Using provided ApartmentId: {ApartmentId}", apartmentId);

        // 2. Kiểm tra biển số xe đã tồn tại chưa
        _logger.LogInformation("Checking if license plate exists: {LicensePlate}", dto.VehicleInfo.LicensePlate);
        var existingVehicle = await _ticketRepository.GetVehicleByLicensePlateAsync(dto.VehicleInfo.LicensePlate);
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
            CreatedByUserId = managerId, // Manager tạo ticket
            Category = "Đăng ký xe",
            Priority = null, // Không cần priority cho đăng ký xe
            Subject = dto.Subject,
            Description = dto.Description,
            Status = "Đã đóng", // closed
            Scope = "Theo căn hộ",
            ApartmentId = apartmentId,
            HasInvoice = false,
            CreatedAt = DateTimeHelper.VietnamNow,
            UpdatedAt = DateTimeHelper.VietnamNow
        };

        // Không cần ExpectedCompletionAt cho ticket đăng ký xe

        // 5. Lưu ticket
        _logger.LogInformation("Saving ticket to database...");
        var createdTicket = await _ticketRepository.AddAsync(ticket);
        _logger.LogInformation("Ticket created with ID: {TicketId}", createdTicket.TicketId);

        // 6. Tạo vehicle entity
        _logger.LogInformation("Creating vehicle entity...");
        var vehicle = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            ResidentId = residentId, // Sử dụng residentId được truyền vào
            ApartmentId = apartmentId,
            VehicleTypeId = dto.VehicleInfo.VehicleTypeId,
            LicensePlate = dto.VehicleInfo.LicensePlate,
            Color = dto.VehicleInfo.Color,
            BrandModel = dto.VehicleInfo.BrandModel,
            RegisteredAt = DateTime.UtcNow.AddHours(7),
            Status = Enums.VehicleStatus.ACTIVE.ToString(), // Chờ duyệt
            Meta = dto.VehicleInfo.Meta
        };

        _logger.LogInformation("Saving vehicle to database. VehicleId: {VehicleId}, ResidentId: {ResidentId}", 
            vehicle.VehicleId, residentId);
        await _ticketRepository.AddVehicleAsync(vehicle);
        _logger.LogInformation("Vehicle saved successfully");

        // 7. Update ticket với VehicleId (FK chỉ ở Ticket)
        _logger.LogInformation("Updating ticket with VehicleId: {VehicleId}", vehicle.VehicleId);
        createdTicket.VehicleId = vehicle.VehicleId;
        await _ticketRepository.UpdateAsync(createdTicket);
        _logger.LogInformation("Ticket updated with VehicleId");

        // 8. Thêm attachments nếu có
        if (dto.AttachmentFileIds != null && dto.AttachmentFileIds.Any())
        {
            _logger.LogInformation("Processing {Count} attachments...", dto.AttachmentFileIds.Count);
            int attachmentCount = 0;
            
            foreach (var fileId in dto.AttachmentFileIds)
            {
                _logger.LogInformation("Processing attachment FileId: {FileId}", fileId);
                
                // Lấy file để lấy tên file
                var file = await _ticketRepository.GetFileByIdAsync(fileId);
                if (file != null)
                {
                    var attachment = new TicketAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        TicketId = createdTicket.TicketId,
                        FileId = fileId,
                        UploadedBy = managerId, // Manager upload
                        Note = file.OriginalName, // Lưu tên file vào note
                        UploadedAt = DateTimeHelper.VietnamNow
                    };
                    await _ticketRepository.AddAttachmentAsync(attachment);
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

        // 9. Reload ticket với attachments
        _logger.LogInformation("Reloading ticket with attachments...");
        var result = await _ticketRepository.GetByIdAsync(createdTicket.TicketId);
        _logger.LogInformation("=== END CreateVehicleRegistrationTicketForManagerAsync ===");
        
        return result!.ToResidentDto();
    }

    /// <summary>
    /// Lấy danh sách ticket hủy gửi xe (Category = "Hủy gửi xe")
    /// </summary>
    public async Task<(IEnumerable<ResidentTicketDto> items, int total)> GetCancelVehicleTicketsAsync(
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        _logger.LogInformation("Getting cancel vehicle tickets. Status: {Status}, Page: {Page}, PageSize: {PageSize}", 
            status ?? "All", page, pageSize);

        // Query tickets với category "Hủy gửi xe"
        var query = _context.Tickets
            .Include(t => t.CreatedByUser)
            .Include(t => t.Apartment)
            .Include(t => t.Vehicle)
                .ThenInclude(v => v.VehicleType)
            .Include(t => t.TicketAttachments)
                .ThenInclude(ta => ta.File)
            .Include(t => t.TicketComments)
            .Where(t => t.Category == "Hủy gửi xe");

        // Filter by status
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        // Filter by date range
        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= toDate.Value);
        }

        // Get total count
        var total = await query.CountAsync();

        // Get paginated items
        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Map to DTO
        var ticketDtos = tickets.Select(t => new ResidentTicketDto
        {
            TicketId = t.TicketId,
            CreatedByUserId = t.CreatedByUserId,
            CreatedByUserName = t.CreatedByUser?.Username,
            Category = t.Category,
            Priority = t.Priority,
            Subject = t.Subject,
            Description = t.Description,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            ClosedAt = t.ClosedAt,
            ExpectedCompletionAt = t.ExpectedCompletionAt,
            Scope = t.Scope,
            ApartmentId = t.ApartmentId,
            ApartmentNumber = t.Apartment?.Number,
            HasInvoice = t.HasInvoice,
            VehicleInfo = t.Vehicle != null ? new VehicleInfoDto
            {
                VehicleId = t.Vehicle.VehicleId,
                VehicleTypeId = t.Vehicle.VehicleTypeId,
                VehicleTypeName = t.Vehicle.VehicleType?.Name,
                LicensePlate = t.Vehicle.LicensePlate,
                Color = t.Vehicle.Color,
                BrandModel = t.Vehicle.BrandModel,
                Status = t.Vehicle.Status,
                RegisteredAt = t.Vehicle.RegisteredAt,
                Meta = t.Vehicle.Meta
            } : null,
            Attachments = t.TicketAttachments?.Select(ta => new TicketAttachmentDto
            {
                AttachmentId = ta.AttachmentId,
                TicketId = ta.TicketId,
                FileId = ta.FileId,
                Note = ta.Note,
                UploadedAt = ta.UploadedAt,
                File = ta.File != null ? new FileDto
                {
                    FileId = ta.File.FileId,
                    OriginalName = ta.File.OriginalName,
                    MimeType = ta.File.MimeType,
                    StoragePath = ta.File.StoragePath,
                    UploadedAt = ta.File.UploadedAt
                } : null
            }).ToList(),
            Comments = t.TicketComments?.Select(tc => new TicketCommentDto
            {
                CommentId = tc.CommentId,
                TicketId = tc.TicketId,
                CommentedBy = tc.CommentedBy,
                Content = tc.Content,
                CommentTime = tc.CommentTime,
                IsInternal = false // TicketComment model doesn't have IsInternal property
            }).ToList()
        }).ToList();

        _logger.LogInformation("Found {Total} cancel vehicle tickets, returning {Count} items", total, ticketDtos.Count);

        return (ticketDtos, total);
    }
}
