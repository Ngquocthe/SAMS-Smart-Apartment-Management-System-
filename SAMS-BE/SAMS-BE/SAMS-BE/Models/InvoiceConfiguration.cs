using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAMS_BE.Models;

/// <summary>
/// C?u hình t? ??ng t?o hóa ??n hàng tháng
/// </summary>
[Table("invoice_configurations")]
public class InvoiceConfiguration
{
    [Key]
    [Column("config_id")]
    public Guid ConfigId { get; set; }

    /// <summary>
    /// Ngày trong tháng ?? t?o invoice (1-28)
    /// VD: 1 = ngày 1, 5 = ngày 5
    /// </summary>
    [Column("generation_day_of_month")]
    [Range(1, 28)]
    public int GenerationDayOfMonth { get; set; } = 1;

    /// <summary>
    /// S? ngày sau ngày issue ?? ??n due date
    /// VD: 30 = h?t h?n sau 30 ngày, 40 = sau 40 ngày
    /// </summary>
    [Column("due_days_after_issue")]
    [Range(1, 90)]
    public int DueDaysAfterIssue { get; set; } = 40;

    /// <summary>
    /// Có kích ho?t t? ??ng t?o invoice không
    /// </summary>
    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Ghi chú
    /// </summary>
    [Column("notes")]
    [MaxLength(500)]
public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(190)]
    public string? CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    [MaxLength(190)]
    public string? UpdatedBy { get; set; }
}
