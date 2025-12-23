using System;
using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

/// <summary>
/// DTO cho việc tạo phiếu bảo trì từ cư dân
/// Category: Maintenance, Scope: APARTMENT
/// </summary>
public class CreateMaintenanceTicketDto
{
    [Required(ErrorMessage = "Subject is required")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Subject must be between 3 and 255 characters")]
    public string Subject { get; set; } = null!;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
    public string Description { get; set; } = null!;


    /// <summary>
    /// ApartmentId của căn hộ (optional)
    /// Nếu null: Backend tự động lấy primary apartment của user
    /// </summary>
    public Guid? ApartmentId { get; set; }

    /// <summary>
    /// Danh sách file IDs đã upload trước đó
    /// </summary>
    public List<Guid>? AttachmentFileIds { get; set; }
}

/// <summary>
/// DTO cho việc tạo phiếu khiếu nại từ cư dân
/// Category: Complaint, Scope: BUILDING
/// </summary>
public class CreateComplaintTicketDto
{
    [Required(ErrorMessage = "Subject is required")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Subject must be between 3 and 255 characters")]
    public string Subject { get; set; } = null!;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
    public string Description { get; set; } = null!;

    /// <summary>
    /// ApartmentId của căn hộ (optional)
    /// Nếu null: Backend tự động lấy primary apartment của user
    /// </summary>
    public Guid? ApartmentId { get; set; }

    /// <summary>
    /// Danh sách file IDs đã upload trước đó
    /// </summary>
    public List<Guid>? AttachmentFileIds { get; set; }
}

/// <summary>
/// DTO response cho resident ticket
/// </summary>
public class ResidentTicketDto
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
    public List<TicketCommentDto>? Comments { get; set; }
    
    // Thông tin xe đăng ký (chỉ có khi Category = "VehicleRegistration")
    public VehicleInfoDto? VehicleInfo { get; set; }
}

public class ResidentTicketInvoiceDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!;
    public DateOnly DueDate { get; set; }
}

public class ResidentTicketStatisticsDto
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Pending { get; set; }
    public int Closed { get; set; }
}

/// <summary>
/// DTO query cho resident tickets
/// </summary>
public class ResidentTicketQueryDto
{
    public string? Status { get; set; }
    public string? Category { get; set; } // Maintenance, Complaint
    public string? Priority { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// DTO cho việc thêm comment vào ticket của resident
/// </summary>
public class CreateResidentTicketCommentDto
{
    [Required]
    public Guid TicketId { get; set; }

    [Required]
    public string Content { get; set; } = null!;
}

/// <summary>
/// DTO cho việc upload file cho resident ticket
/// </summary>
public class ResidentTicketFileUploadDto
{
    [Required]
    public Guid TicketId { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

/// <summary>
/// DTO cho việc tạo ticket đăng ký xe
/// Category: VehicleRegistration, Scope: APARTMENT
/// </summary>
public class CreateVehicleRegistrationTicketDto
{
    [Required(ErrorMessage = "Subject is required")]
    [StringLength(255, ErrorMessage = "Subject cannot exceed 255 characters")]
    public string Subject { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// ApartmentId của căn hộ (optional)
    /// Nếu null: Backend tự động lấy primary apartment của user
    /// </summary>
    public Guid? ApartmentId { get; set; }

    /// <summary>
    /// Thông tin xe cần đăng ký
    /// </summary>
    [Required(ErrorMessage = "Vehicle information is required")]
    public VehicleRegistrationInfoDto VehicleInfo { get; set; } = null!;

    /// <summary>
    /// Danh sách file IDs đã upload trước đó (giấy tờ xe, ảnh xe, etc.)
    /// </summary>
    public List<Guid>? AttachmentFileIds { get; set; }
}

/// <summary>
/// DTO chứa thông tin xe cần đăng ký
/// </summary>
public class VehicleRegistrationInfoDto
{
    [Required(ErrorMessage = "Vehicle type ID is required")]
    public Guid VehicleTypeId { get; set; }

    [Required(ErrorMessage = "License plate is required")]
    [StringLength(64, ErrorMessage = "License plate cannot exceed 64 characters")]
    public string LicensePlate { get; set; } = null!;

    [StringLength(64, ErrorMessage = "Color cannot exceed 64 characters")]
    public string? Color { get; set; }

    [StringLength(128, ErrorMessage = "Brand/Model cannot exceed 128 characters")]
    public string? BrandModel { get; set; }

    /// <summary>
    /// Thông tin bổ sung (JSON format)
    /// </summary>
    public string? Meta { get; set; }
}

/// <summary>
/// DTO response cho thông tin xe trong ticket detail
/// </summary>
public class VehicleInfoDto
{
    public Guid VehicleId { get; set; }
    public Guid VehicleTypeId { get; set; }
    public string? VehicleTypeName { get; set; }
    public string LicensePlate { get; set; } = null!;
    public string? Color { get; set; }
    public string? BrandModel { get; set; }
    public string Status { get; set; } = null!;
    public DateTime RegisteredAt { get; set; }
    public string? Meta { get; set; }
}
