using System;
using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class AccessCardDto
{
    public Guid CardId { get; set; }
    public string CardNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid? IssuedToUserId { get; set; }
    public string? IssuedToUserName { get; set; }
    public Guid? IssuedToApartmentId { get; set; }
    public string? IssuedToApartmentNumber { get; set; }
    public DateTime IssuedDate { get; set; }
    public DateTime? ExpiredDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDelete { get; set; }
    public List<AccessCardCapabilityDto> Capabilities { get; set; } = new List<AccessCardCapabilityDto>();
}

public class CreateAccessCardDto
{
    [Required(ErrorMessage = "Số thẻ là bắt buộc")]
    public string CardNumber { get; set; } = null!;
    
    [Required]
    public string Status { get; set; } = "ACTIVE";
    
    public Guid? IssuedToUserId { get; set; }
    
    // Hỗ trợ nhập GUID căn hộ (cách cũ)
    public Guid? IssuedToApartmentId { get; set; }
    
    // Hỗ trợ nhập số căn hộ (ví dụ: A0108) 
    public string? IssuedToApartmentNumber { get; set; }
    
    public DateTime? IssuedDate { get; set; }
    public DateTime? ExpiredDate { get; set; }
    public string CreatedBy { get; set; } = "buildingmanager";
    public string? UpdatedBy { get; set; }
    public List<Guid> CardTypeIds { get; set; } = new List<Guid>(); // Danh sách quyền
}

public class UpdateAccessCardDto
{
    public string? CardNumber { get; set; }
    public string? Status { get; set; }
    public Guid? IssuedToUserId { get; set; }
    
    // Hỗ trợ nhập GUID căn hộ (cách cũ)
    public Guid? IssuedToApartmentId { get; set; }
    
    // Hỗ trợ nhập số căn hộ (ví dụ: A0108) - cách mới
    public string? IssuedToApartmentNumber { get; set; }
    
    public DateTime? IssuedDate { get; set; } // Ngày cấp thẻ
    public DateTime? ExpiredDate { get; set; }
    public string? UpdatedBy { get; set; }
    public List<Guid>? CardTypeIds { get; set; } // Danh sách quyền mới
}
