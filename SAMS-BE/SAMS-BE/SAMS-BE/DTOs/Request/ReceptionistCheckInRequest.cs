using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SAMS_BE.DTOs.Request;

public class ReceptionistCheckInRequest
{
    [Required]
    public IFormFile FaceImage { get; set; } = null!;

    /// <summary>
    /// Cho phép bỏ qua kết quả xác thực khuôn mặt nhưng vẫn ghi nhận check-in.
    /// </summary>
    public bool ManualOverride { get; set; }

    /// <summary>
    /// Cho phép skip xác thực khuôn mặt khi đã có xác minh trước đó hoặc lý do đặc biệt.
    /// </summary>
    public bool SkipFaceVerification { get; set; }

    /// <summary>
    /// Ghi chú thêm cho lễ tân.
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}














