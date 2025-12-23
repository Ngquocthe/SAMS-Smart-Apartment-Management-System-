using System;
using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class TicketDto
{
    public Guid TicketId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public string Category { get; set; } = null!;
    public string? Priority { get; set; }
    public string Subject { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? ExpectedCompletionAt { get; set; }
    public string? Scope { get; set; }
    public Guid? ApartmentId { get; set; }
    public string? ApartmentNumber { get; set; }
    public bool HasInvoice { get; set; }
    public List<TicketAttachmentDto>? Attachments { get; set; }
    public List<InvoiceDetailDto>? InvoiceDetails { get; set; }
    public List<VoucherItemDto>? VoucherItems { get; set; }
    public List<TicketCommentDto>? TicketComments { get; set; }
}

public class CreateTicketDto
{
    [Required]
    [StringLength(64, ErrorMessage = "Category không được vượt quá 64 ký tự.")]
    public string Category { get; set; } = null!;

    [Required(ErrorMessage = "Priority là bắt buộc.")]
    [StringLength(32, ErrorMessage = "Priority không được vượt quá 32 ký tự.")]
    public string Priority { get; set; } = null!;

    [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Tiêu đề phải có từ 3 đến 255 ký tự.")]
    public string Subject { get; set; } = null!;

    [StringLength(4000, ErrorMessage = "Mô tả không được vượt quá 4000 ký tự.")]
    public string? Description { get; set; }

    public Guid? CreatedByUserId { get; set; }

    [StringLength(64)]
    public string? Scope { get; set; }

    public Guid? ApartmentId { get; set; }

    public bool HasInvoice { get; set; } = false;
}

public class UpdateTicketDto
{
    [Required]
    public Guid TicketId { get; set; }

    [Required(ErrorMessage = "Category là bắt buộc.")]
    [StringLength(64, ErrorMessage = "Category không được vượt quá 64 ký tự.")]
    public string Category { get; set; } = null!;

    [Required(ErrorMessage = "Priority là bắt buộc.")]
    [StringLength(32, ErrorMessage = "Priority không được vượt quá 32 ký tự.")]
    public string Priority { get; set; } = null!;

    [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Tiêu đề phải có từ 3 đến 255 ký tự.")]
    public string Subject { get; set; } = null!;

    [StringLength(4000, ErrorMessage = "Mô tả không được vượt quá 4000 ký tự.")]
    public string? Description { get; set; }

    [StringLength(64)]
    public string? Scope { get; set; }

    public Guid? ApartmentId { get; set; }

    public bool HasInvoice { get; set; }

    public DateTime? ExpectedCompletionAt { get; set; }

    // User thực hiện update (dùng cho audit/comment)
    public Guid? UpdatedByUserId { get; set; }
}

public class ChangeTicketStatusDto
{
    [Required]
    public Guid TicketId { get; set; }

    [Required]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    public Guid? ChangedByUserId { get; set; }
}

public class TicketQueryDto
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Category { get; set; }
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class TicketCommentDto
{
    public Guid CommentId { get; set; }
    public Guid TicketId { get; set; }
    public Guid? CommentedBy { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CommentTime { get; set; }
    public string Content { get; set; } = null!;
    public bool IsInternal { get; set; }
}

public class CreateTicketCommentDto
{
    [Required]
    public Guid TicketId { get; set; }
    public Guid? CommentedBy { get; set; }
    [Required(ErrorMessage = "Content là bắt buộc.")]
    [StringLength(4000, ErrorMessage = "Nội dung bình luận không được vượt quá 4000 ký tự.")]
    public string Content { get; set; } = null!;
}

// TicketAttachment DTOs
public class TicketAttachmentDto
{
    public Guid AttachmentId { get; set; }
    public Guid TicketId { get; set; }
    public Guid FileId { get; set; }
    public Guid? UploadedBy { get; set; }
    public string? Note { get; set; }
    public DateTime UploadedAt { get; set; }
    public FileDto? File { get; set; }
}

public class CreateTicketAttachmentDto
{
    [Required]
    public Guid TicketId { get; set; }

    [Required]
    public Guid FileId { get; set; }

    public Guid? UploadedBy { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

// File DTOs
public class FileDto
{
    public Guid FileId { get; set; }
    public string OriginalName { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public string StoragePath { get; set; } = null!;
    public string? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class CreateFileDto
{
    [Required]
    [StringLength(255)]
    public string OriginalName { get; set; } = null!;

    [StringLength(128)]
    public string? MimeType { get; set; }

    [Required]
    [StringLength(1000)]
    public string StoragePath { get; set; } = null!;

    [StringLength(190)]
    public string? UploadedBy { get; set; }
}

// InvoiceDetail DTOs
public class InvoiceDetailDto
{
    public Guid InvoiceDetailId { get; set; }
  public Guid InvoiceId { get; set; }
    public Guid ServiceId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? Amount { get; set; }
    public decimal? VatRate { get; set; }
    public decimal? VatAmount { get; set; }
}


public class ApartmentLookupDto
{
    public Guid ApartmentId { get; set; }
    public string Number { get; set; } = null!;
    public string? OwnerName { get; set; }  // full_name từ resident_profiles (chủ hộ thực sự)
    public string? MatchedResidentName { get; set; }  // Tên cư dân khớp khi tìm kiếm (có thể không phải chủ hộ)
}


public class ApartmentSummaryDto
{
    public Guid ApartmentId { get; set; }
    public string Number { get; set; } = null!;
    public string? OwnerName { get; set; }
    public Guid? OwnerUserId { get; set; }  // nếu cần
}



// Finance DTOs moved to separate files:
// - CreateVoucherRequest.cs
// - CreateVoucherFromMaintenanceRequest.cs
// - FinanceItemSummaryDto.cs

public class CreateInvoiceRequest
{
    [Required]
  public Guid TicketId { get; set; }

    [Range(1, double.MaxValue)]
    public decimal Quantity { get; set; } = 1;

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public Guid? ServiceTypeId { get; set; }

    // Người thực hiện tạo invoice (để ghi log vào ticket)
    public Guid? CreatedByUserId { get; set; }
}

public class UpdateInvoiceDetailDtoInvoice
{
    [Range(1, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

  [StringLength(1000)]
    public string? Note { get; set; }
}




