using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using System.Security.Claims;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaceRecognitionController : ControllerBase
{
    private readonly IFaceRecognitionService _service;
    private readonly ILogger<FaceRecognitionController> _logger;

    public FaceRecognitionController(
        IFaceRecognitionService service,
        ILogger<FaceRecognitionController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Đăng ký khuôn mặt cho user
    /// POST /api/FaceRecognition/register
    /// </summary>
    [Authorize]
    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> RegisterFace([FromForm] FaceRegisterRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Lấy userId từ token nếu không có trong request
            var userId = GetCurrentUserId();
            if (userId != Guid.Empty && request.UserId == Guid.Empty)
            {
                request.UserId = userId;
            }

            if (request.UserId == Guid.Empty)
            {
                return BadRequest(new { message = "UserId is required" });
            }

            // Kiểm tra quyền: User chỉ có thể đăng ký cho chính mình, trừ khi là receptionist/admin
            var isReceptionist = User.IsInRole("receptionist")
                                 || User.IsInRole("global_admin")
                                 || User.IsInRole("building_admin")
                                 || User.IsInRole("building-manager")
                                 || User.IsInRole("building_management");

            if (userId != Guid.Empty && request.UserId != userId && !isReceptionist)
            {
                return Forbid("Bạn chỉ có thể đăng ký khuôn mặt cho chính mình");
            }

            var result = await _service.RegisterFaceAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("Face registered successfully for user {UserId}", request.UserId);
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering face for user {UserId}", request.UserId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Xác thực khuôn mặt
    /// POST /api/FaceRecognition/verify
    /// </summary>
    [Authorize]
    [HttpPost("verify")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> VerifyFace([FromForm] FaceVerifyRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Lấy userId từ token nếu không có trong request
            var userId = GetCurrentUserId();
            if (userId != Guid.Empty && request.UserId == Guid.Empty)
            {
                request.UserId = userId;
            }

            if (request.UserId == Guid.Empty)
            {
                return BadRequest(new { message = "UserId is required" });
            }

            var result = await _service.VerifyFaceAsync(request);

            if (result.IsVerified)
            {
                _logger.LogInformation("Face verification successful for user {UserId}. Similarity: {Similarity}",
                    request.UserId, result.Similarity);
            }
            else
            {
                _logger.LogWarning("Face verification failed for user {UserId}. Similarity: {Similarity}",
                    request.UserId, result.Similarity);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying face for user {UserId}", request.UserId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Helper method để lấy user ID từ JWT token
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("user_id")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

