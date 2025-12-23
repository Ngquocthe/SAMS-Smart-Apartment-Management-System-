using SAMS_BE.DTOs;
using SAMS_BE.Models;
using static SAMS_BE.Helpers.DateTimeHelper;

namespace SAMS_BE.Mappers;

public static class TicketMapper
{
    public static TicketDto ToDto(this Ticket entity)
    {
        return new TicketDto
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
            TicketComments = entity.TicketComments?
                .OrderByDescending(c => c.CommentTime)
                .ToDto()
                ?.ToList()
        };
    }

    public static IEnumerable<TicketDto> ToDto(this IEnumerable<Ticket> entities)
    {
        return entities.Select(e => e.ToDto());
    }

    public static Ticket ToEntity(this CreateTicketDto dto)
    {
        return new Ticket
        {
            TicketId = Guid.NewGuid(),
            CreatedByUserId = dto.CreatedByUserId,
            Category = dto.Category,
            Priority = dto.Priority,
            Subject = dto.Subject,
            Description = dto.Description,
            Status = "Mới tạo",
            Scope = string.IsNullOrWhiteSpace(dto.Scope) ? "Tòa nhà" : dto.Scope,
            ApartmentId = dto.ApartmentId,
            HasInvoice = dto.HasInvoice,
            // Lưu giờ VN và để Kind.Unspecified để tránh cộng +7 lần nữa khi tính ExpectedCompletion
            CreatedAt = VietnamNow
        };
    }

    public static void ApplyUpdate(this Ticket entity, UpdateTicketDto dto)
    {
        entity.Category = dto.Category;
        entity.Priority = dto.Priority;
        entity.Subject = dto.Subject;
        entity.Description = dto.Description;
        // DB yêu cầu cột scope NOT NULL -> không ghi đè null
        if (!string.IsNullOrWhiteSpace(dto.Scope))
            entity.Scope = dto.Scope;
        entity.ApartmentId = dto.ApartmentId;
        entity.HasInvoice = dto.HasInvoice;
        entity.UpdatedAt = VietnamNow;
    }
}

public static class TicketCommentMapper
{
    public static TicketCommentDto ToDto(this TicketComment entity)
    {
        return new TicketCommentDto
        {
            CommentId = entity.CommentId,
            TicketId = entity.TicketId,
            CommentedBy = entity.CommentedBy,
            CreatedByUserName = null,
            CommentTime = entity.CommentTime,
            Content = entity.Content,
        };
    }

    public static IEnumerable<TicketCommentDto> ToDto(this IEnumerable<TicketComment> entities)
    {
        return entities.Select(e => e.ToDto());
    }

    public static TicketComment ToEntity(this CreateTicketCommentDto dto)
    {
        return new TicketComment
        {
            CommentId = Guid.NewGuid(),
            TicketId = dto.TicketId,
            CommentedBy = dto.CommentedBy,
            CommentTime = VietnamNow,
            Content = dto.Content,

        };
    }
}

// TicketAttachment Mappers
public static class TicketAttachmentMapper
{
    public static TicketAttachmentDto ToDto(this TicketAttachment entity)
    {
        return new TicketAttachmentDto
        {
            AttachmentId = entity.AttachmentId,
            TicketId = entity.TicketId,
            FileId = entity.FileId,
            UploadedBy = entity.UploadedBy,
            Note = entity.Note,
            UploadedAt = entity.UploadedAt,
            File = entity.File?.ToDto()
        };
    }

    public static IEnumerable<TicketAttachmentDto> ToDto(this IEnumerable<TicketAttachment> entities)
    {
        return entities.Select(e => e.ToDto());
    }

    public static TicketAttachment ToEntity(this CreateTicketAttachmentDto dto)
    {
        return new TicketAttachment
        {
            AttachmentId = Guid.NewGuid(),
            TicketId = dto.TicketId,
            FileId = dto.FileId,
            UploadedBy = dto.UploadedBy,
            Note = dto.Note,
            UploadedAt = VietnamNow
        };
    }
}

// File Mappers
public static class FileMapper
{
    public static FileDto ToDto(this Models.File entity)
    {
        return new FileDto
        {
            FileId = entity.FileId,
            OriginalName = entity.OriginalName,
            MimeType = entity.MimeType,
            StoragePath = entity.StoragePath,
            UploadedBy = entity.UploadedBy,
            UploadedAt = entity.UploadedAt
        };
    }

    public static IEnumerable<FileDto> ToDto(this IEnumerable<Models.File> entities)
    {
        return entities.Select(e => e.ToDto());
    }

    public static Models.File ToEntity(this CreateFileDto dto)
    {
        return new Models.File
        {
            FileId = Guid.NewGuid(),
            OriginalName = dto.OriginalName,
            MimeType = dto.MimeType ?? "application/octet-stream",
            StoragePath = dto.StoragePath,
            UploadedBy = dto.UploadedBy,
            UploadedAt = VietnamNow
        };
    }
}

// InvoiceDetail Mappers
public static class InvoiceDetailMapperTicket
{
    public static InvoiceDetailDto ToDtoTicket(this InvoiceDetail entity)
    {
     return new InvoiceDetailDto
        {
      InvoiceDetailId = entity.InvoiceDetailId,
  InvoiceId = entity.InvoiceId,
   ServiceId = entity.ServiceId,
        Description = entity.Description,
  Quantity = entity.Quantity,
    UnitPrice = entity.UnitPrice,
            Amount = entity.Amount,
            VatRate = entity.VatRate,
            VatAmount = entity.VatAmount,
        };
 }

    public static IEnumerable<InvoiceDetailDto> ToDto(this IEnumerable<InvoiceDetail> entities)
    {
        return entities.Select(e => e.ToDtoTicket());
    }
}

// VoucherItem Mappers
public static class VoucherItemMapper
{
    public static VoucherItemDto ToDto(this VoucherItem entity)
    {
        return new VoucherItemDto
     {
            VoucherItemsId = entity.VoucherItemsId,
            VoucherId = entity.VoucherId,
            Description = entity.Description,
            Quantity = entity.Quantity,
            UnitPrice = entity.UnitPrice,
            Amount = entity.Amount,
            ServiceTypeId = entity.ServiceTypeId,
            ApartmentId = entity.ApartmentId,
            CreatedAt = entity.CreatedAt,
        };
    }

    public static IEnumerable<VoucherItemDto> ToDto(this IEnumerable<VoucherItem> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}


