using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class FaceVerifyRequestDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public IFormFile Image { get; set; } = null!;
}

public class FaceRegisterRequestDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public IFormFile Image { get; set; } = null!;
}

public class FaceVerifyResponseDto
{
    public bool IsVerified { get; set; }
    public float Similarity { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class FaceRegisterResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class FaceCheckInRequestDto
{
    [Required]
    public Guid BookingId { get; set; }

    [Required]
    public IFormFile FaceImage { get; set; } = null!;
}

public class FaceCheckInResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public float? Similarity { get; set; }
    public DateTime? CheckedInAt { get; set; }
}

public class FaceIdentifyResponseDto
{
    public bool IsIdentified { get; set; }
    public Guid? UserId { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public float Similarity { get; set; }
    public string Message { get; set; } = string.Empty;
}

