using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System;

namespace SAMS_BE.DTOs.Request;

public class ReceptionistScanRequest
{
    [Required]
    public IFormFile FaceImage { get; set; } = null!;

    public Guid? AmenityId { get; set; }
}


