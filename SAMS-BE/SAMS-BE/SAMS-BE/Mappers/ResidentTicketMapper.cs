using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Models;
using static SAMS_BE.Helpers.DateTimeHelper;

namespace SAMS_BE.Mappers;

public static class ResidentTicketMapper
{

    public static ResidentTicketDto ToResidentDto(this Ticket entity)
    {
        var dto = new ResidentTicketDto
        {
            TicketId = entity.TicketId,
            CreatedByUserId = entity.CreatedByUserId,
            CreatedByUserName = entity.CreatedByUser != null ? entity.CreatedByUser.Username : null,
            Category = entity.Category,
            Priority = entity.Priority,
            Subject = entity.Subject,
            Description = entity.Description,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            ClosedAt = entity.ClosedAt,
            ExpectedCompletionAt = entity.ExpectedCompletionAt,
            Scope = entity.Scope,
            ApartmentId = entity.ApartmentId,
            ApartmentNumber = entity.Apartment?.Number,
            HasInvoice = entity.HasInvoice,
            Attachments = entity.TicketAttachments?.ToDto()?.ToList(),
            Comments = entity.TicketComments?.ToDto()?.ToList()
        };

        // Thêm thông tin xe nếu là ticket đăng ký xe
        if ((entity.Category == "Đăng ký xe" || entity.Category == "VehicleRegistration") && entity.Vehicle != null)
        {
            var vehicle = entity.Vehicle;
            dto.VehicleInfo = new VehicleInfoDto
            {
                VehicleId = vehicle.VehicleId,
                VehicleTypeId = vehicle.VehicleTypeId,
                VehicleTypeName = vehicle.VehicleType?.Name,
                LicensePlate = vehicle.LicensePlate,
                Color = vehicle.Color,
                BrandModel = vehicle.BrandModel,
                Status = vehicle.Status,
                RegisteredAt = vehicle.RegisteredAt,
                Meta = vehicle.Meta
            };
        }

        return dto;
    }

    /// <summary>
    /// Map collection từ Ticket entities sang ResidentTicketDto list
    /// </summary>
    public static IEnumerable<ResidentTicketDto> ToResidentDto(this IEnumerable<Ticket> entities)
    {
        return entities.Select(e => e.ToResidentDto());
    }

    /// <summary>
    /// Map từ CreateMaintenanceTicketDto sang Ticket entity
    /// Category: Maintenance, Scope: APARTMENT
    /// </summary>
    public static Ticket ToEntity(this CreateMaintenanceTicketDto dto, Guid userId, Guid apartmentId)
    {
        return new Ticket
        {
            TicketId = Guid.NewGuid(),
            CreatedByUserId = userId,
            Category = "Bảo trì",
            Subject = dto.Subject,
            Description = dto.Description,
            Status = "Mới tạo",
            Scope = "Theo căn hộ",
            ApartmentId = apartmentId,
            HasInvoice = false,
            // Lưu giờ VN và để Kind.Unspecified để tránh cộng +7 lần nữa khi tính ExpectedCompletion
            CreatedAt = VietnamNow
        };
    }

    /// <summary>
    /// Map từ CreateComplaintTicketDto sang Ticket entity
    /// Category: Complaint, Scope: BUILDING
    /// </summary>
    public static Ticket ToEntity(this CreateComplaintTicketDto dto, Guid userId, Guid apartmentId)
    {
        return new Ticket
        {
            TicketId = Guid.NewGuid(),
            CreatedByUserId = userId,
            Category = "Khiếu nại",
            Subject = dto.Subject,
            Description = dto.Description,
            Status = "Mới tạo",
            Scope = "Toà nhà",
            ApartmentId = apartmentId,
            HasInvoice = false,
            // Lưu giờ VN và để Kind.Unspecified để tránh cộng +7 lần nữa khi tính ExpectedCompletion
            CreatedAt = VietnamNow
        };
    }

    /// <summary>
    /// Map từ CreateResidentTicketCommentDto sang TicketComment entity
    /// </summary>
    public static TicketComment ToEntity(this CreateResidentTicketCommentDto dto, Guid userId)
    {
        return new TicketComment
        {
            CommentId = Guid.NewGuid(),
            TicketId = dto.TicketId,
            CommentedBy = userId,
            CommentTime = VietnamNow, // Lưu giờ VN
            Content = dto.Content,

        };
    }
}
