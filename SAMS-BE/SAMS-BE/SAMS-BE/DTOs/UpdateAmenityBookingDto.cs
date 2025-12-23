using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

/// <summary>
/// DTO cho việc cập nhật đăng ký tiện ích
/// Chỉ cho phép update khi booking đang ở trạng thái Pending
/// </summary>
public class UpdateAmenityBookingDto
{
    /// <summary>
    /// Thay đổi gói package (ví dụ từ 1 tháng lên 3 tháng)
    /// EndDate sẽ tự động được tính lại dựa trên StartDate hiện tại + MonthCount mới
    /// </summary>
    [Required(ErrorMessage = "Package ID is required")]
    public Guid PackageId { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}

