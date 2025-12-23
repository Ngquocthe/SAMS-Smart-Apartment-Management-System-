using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs.Request;

/// <summary>
/// Request DTO cho việc cập nhật trạng thái thanh toán
/// </summary>
public class UpdatePaymentStatusRequest
{
    [Required(ErrorMessage = "Payment status is required")]
    [StringLength(20)]
    public string PaymentStatus { get; set; } = null!;
}

