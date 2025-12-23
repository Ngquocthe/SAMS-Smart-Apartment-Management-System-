using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

/// <summary>
/// DTO cho thông tin xe của cư dân
/// </summary>
public class MyVehicleDto
{
    public Guid VehicleId { get; set; }
    public Guid? ResidentId { get; set; }
    public string? ResidentName { get; set; }
    public string? ResidentPhone { get; set; }
    public Guid? ApartmentId { get; set; }
    public string? ApartmentNumber { get; set; }
    public Guid VehicleTypeId { get; set; }
    public string? VehicleTypeName { get; set; }
    public string LicensePlate { get; set; } = null!;
    public string? Color { get; set; }
    public string? BrandModel { get; set; }
    public Guid? ParkingCardId { get; set; }
    public string? ParkingCardNumber { get; set; }
    public DateTime RegisteredAt { get; set; }
    public string Status { get; set; } = null!;
    public string? Meta { get; set; }
    
    // Thông tin ticket đăng ký (nếu có)
    public Guid? RegistrationTicketId { get; set; }
    public string? RegistrationTicketStatus { get; set; }
    
    // Danh sách file đính kèm từ ticket
    public List<TicketAttachmentDto>? Attachments { get; set; }
}

/// <summary>
/// DTO request để tạo ticket hủy đăng ký xe
/// </summary>
public class CreateCancelVehicleTicketDto
{
    [Required(ErrorMessage = "VehicleId là bắt buộc")]
    public Guid VehicleId { get; set; }
    
    [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
    [StringLength(255, ErrorMessage = "Tiêu đề không được quá 255 ký tự")]
    public string Subject { get; set; } = null!;
    
    [Required(ErrorMessage = "Lý do hủy là bắt buộc")]
    [StringLength(1000, ErrorMessage = "Lý do hủy không được quá 1000 ký tự")]
    public string Description { get; set; } = null!;
    
    /// <summary>
    /// Danh sách file đính kèm (nếu có)
    /// </summary>
    public List<Guid>? AttachmentFileIds { get; set; }
}

/// <summary>
/// DTO request để update status xe
/// </summary>
public class UpdateVehicleStatusDto
{
    [Required(ErrorMessage = "Status là bắt buộc")]
    public string Status { get; set; } = null!;
}
