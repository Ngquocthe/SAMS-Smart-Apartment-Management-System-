using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SAMS_BE.DTOs.Request;

public class CreateAmenityBookingWithFaceRequest
{
    [Required]
    public Guid AmenityId { get; set; }

    [Required]
    public Guid PackageId { get; set; }

    public Guid? ApartmentId { get; set; }

    public string? Notes { get; set; }

    public IFormFile? FaceImage { get; set; }
}
