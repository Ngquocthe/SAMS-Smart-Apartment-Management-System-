using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SAMS_BE.DTOs.Request;

public class CheckInWithFaceRequest
{
    [Required]
    public IFormFile FaceImage { get; set; } = null!;
}

