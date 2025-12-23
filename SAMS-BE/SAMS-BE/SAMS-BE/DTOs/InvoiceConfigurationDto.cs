using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

/// <summary>
/// DTO ?? t?o ho?c c?p nh?t c?u hình t? ??ng t?o invoice
/// </summary>
public class CreateOrUpdateInvoiceConfigDto
{
    /// <summary>
    /// Ngày trong tháng ?? t?o invoice (1-28)
    /// </summary>
    [Required]
    [Range(1, 28, ErrorMessage = "Ngày t?o invoice ph?i t? 1 ??n 28")]
    public int GenerationDayOfMonth { get; set; }

 /// <summary>
    /// S? ngày sau ngày issue ?? ??n due date (1-90)
    /// </summary>
    [Required]
    [Range(1, 90, ErrorMessage = "S? ngày h?t h?n ph?i t? 1 ??n 90")]
    public int DueDaysAfterIssue { get; set; }

    /// <summary>
    /// Có kích ho?t t? ??ng t?o invoice không
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Ghi chú
 /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO response cho c?u hình invoice
/// </summary>
public class InvoiceConfigurationResponseDto
{
    public Guid ConfigId { get; set; }
    public int GenerationDayOfMonth { get; set; }
    public int DueDaysAfterIssue { get; set; }
    public bool IsEnabled { get; set; }
  public string? Notes { get; set; }
public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
