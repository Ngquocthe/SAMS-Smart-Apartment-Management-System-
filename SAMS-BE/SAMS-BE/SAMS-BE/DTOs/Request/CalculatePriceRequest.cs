using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs.Request;

/// <summary>
/// Request DTO cho tính giá theo package
/// </summary>
public class CalculatePriceRequest
{
    [Required(ErrorMessage = "Package ID is required")]
    public Guid PackageId { get; set; }
}

