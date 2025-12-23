using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Request;
using SAMS_BE.DTOs.Response;
using SAMS_BE.Interfaces.IService;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.Models;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/amenitybooking")]
public class AmenityBookingController : ControllerBase
{
    private readonly IAmenityBookingService _bookingService;
    private readonly IUserService _userService;
    private readonly IAmenityService _amenityService;
    private readonly IFaceRecognitionService _faceRecognitionService;
    private readonly IAmenityCheckInService _checkInService;
    private readonly ILogger<AmenityBookingController> _logger;
    private readonly BuildingManagementContext _context;

    public AmenityBookingController(
        IAmenityBookingService bookingService,
        IUserService userService,
        IAmenityService amenityService,
        IFaceRecognitionService faceRecognitionService,
        IAmenityCheckInService checkInService,
        ILogger<AmenityBookingController> logger,
        BuildingManagementContext context)
    {
        _bookingService = bookingService;
        _userService = userService;
        _amenityService = amenityService;
        _faceRecognitionService = faceRecognitionService;
        _checkInService = checkInService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 1. Lấy tất cả bookings (với phân trang và lọc)
    /// GET /api/amenitybooking
    /// </summary>
    // Quản lý xem tất cả với phân trang/lọc
    // [Authorize(Policy = "RequireManagerOrAdmin")]
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<AmenityBookingDto>>> GetAllBookings([FromQuery] AmenityBookingQueryDto query)
    {
        try
        {
            var result = await _bookingService.GetPagedAsync(query);

            var response = new PagedApiResponse<AmenityBookingDto>(
                result.Items.ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 1b. Lấy danh sách cư dân đã đăng ký tiện ích (với phân trang và lọc)
    /// GET /api/amenitybooking/registered-residents
    /// </summary>
    [HttpGet("registered-residents")]
    public async Task<ActionResult<PagedApiResponse<RegisteredResidentDto>>> GetRegisteredResidents([FromQuery] AmenityBookingQueryDto query)
    {
        try
        {
            var result = await _bookingService.GetRegisteredResidentsAsync(query);

            var response = new PagedApiResponse<RegisteredResidentDto>(
                result.Items.ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 2. Lấy booking theo ID
    /// GET /api/amenitybooking/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AmenityBookingDto>> GetBookingById(Guid id)
    {
        try
        {
            var booking = await _bookingService.GetByIdAsync(id);
            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }
            return Ok(booking);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 3. Lấy booking của user hiện tại
    /// GET /api/amenitybooking/my-bookings
    /// </summary>
    // Cư dân xem lịch sử của chính mình
    [Authorize]
    [HttpGet("my-bookings")]
    public async Task<ActionResult<ApiResponse<List<AmenityBookingDto>>>> GetMyBookings()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var bookings = await _bookingService.GetMyBookingsAsync(userId);
            var response = new ApiResponse<List<AmenityBookingDto>>
            {
                Data = bookings.ToList(),
                Success = true
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 3b. Quản lý xem lịch sử theo cư dân
    /// GET /api/amenitybooking/by-user/{userId}
    /// </summary>
    // [Authorize(Policy = "RequireManagerOrAdmin")]
    [HttpGet("by-user/{userId}")]
    public async Task<ActionResult<ApiResponse<List<AmenityBookingDto>>>> GetBookingsByUser(Guid userId)
    {
        try
        {
            var bookings = await _bookingService.GetByUserIdAsync(userId);
            var response = new ApiResponse<List<AmenityBookingDto>>
            {
                Data = bookings.ToList(),
                Success = true
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 4. Lấy booking theo amenity
    /// GET /api/amenitybooking/by-amenity/{amenityId}
    /// </summary>
    // Quản lý tra cứu theo tiện ích
    // [Authorize(Policy = "RequireManagerOrAdmin")]
    [HttpGet("by-amenity/{amenityId}")]
    public async Task<ActionResult<ApiResponse<List<AmenityBookingDto>>>> GetBookingsByAmenity(Guid amenityId)
    {
        try
        {
            var bookings = await _bookingService.GetByAmenityIdAsync(amenityId);
            var response = new ApiResponse<List<AmenityBookingDto>>
            {
                Data = bookings.ToList(),
                Success = true
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 5. Lấy booking theo apartment
    /// GET /api/amenitybooking/by-apartment/{apartmentId}
    /// </summary>
    // Quản lý tra cứu theo căn hộ
    // [Authorize(Policy = "RequireManagerOrAdmin")]
    [HttpGet("by-apartment/{apartmentId}")]
    public async Task<ActionResult<ApiResponse<List<AmenityBookingDto>>>> GetBookingsByApartment(Guid apartmentId)
    {
        try
        {
            var bookings = await _bookingService.GetByApartmentIdAsync(apartmentId);
            var response = new ApiResponse<List<AmenityBookingDto>>
            {
                Data = bookings.ToList(),
                Success = true
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 6. Kiểm tra tính khả dụng (Availability Check)
    /// GET /api/amenitybooking/check-availability?amenityId={guid}
    /// Với hệ thống packages theo tháng, chỉ cần check amenity có active không
    /// </summary>
    [Authorize]
    [HttpGet("check-availability")]
    public async Task<ActionResult<AvailabilityCheckResponse>> CheckAvailability(
        [FromQuery] Guid? amenityId)
    {
        try
        {
            // Validate required parameters
            if (!amenityId.HasValue || amenityId == Guid.Empty)
            {
                return BadRequest(new AvailabilityCheckResponse
                {
                    IsAvailable = false,
                    Message = "amenityId is required",
                    ConflictingBookings = new List<ConflictingBookingInfo>()
                });
            }

            var response = await _bookingService.CheckAvailabilityAsync(amenityId.Value);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AvailabilityCheckResponse
            {
                IsAvailable = false,
                Message = $"Internal server error: {ex.Message}",
                ConflictingBookings = new List<ConflictingBookingInfo>()
            });
        }
    }

    /// <summary>
    /// 7. Tính toán giá (Calculate Price)
    /// POST /api/amenitybooking/calculate-price
    /// </summary>
    [Authorize]
    [HttpPost("calculate-price")]
    public async Task<ActionResult<PriceCalculationResponse>> CalculatePrice([FromBody] CalculatePriceRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Bad Request",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            if (request == null || request.PackageId == Guid.Empty)
            {
                return BadRequest(new { message = "PackageId is required" });
            }

            var response = await _bookingService.CalculatePriceAsync(request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating price for PackageId: {request?.PackageId}");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 8. Tạo booking mới (JSON)
    /// POST /api/amenitybooking
    /// </summary>
    [Authorize]
    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult<ApiResponse<AmenityBookingDto>>> CreateBooking([FromBody] CreateAmenityBookingDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Request body is required" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Bad Request",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            // ✅ Kiểm tra amenity có yêu cầu face verification không
            var amenity = await _amenityService.GetAmenityByIdAsync(dto.AmenityId);
            if (amenity == null)
            {
                return BadRequest(new { message = "Amenity không tồn tại" });
            }

            // Nếu amenity yêu cầu face verification, phải dùng endpoint /with-face
            if (amenity.RequiresFaceVerification)
            {
                return BadRequest(new
                {
                    message = "This amenity requires face verification. Please use POST /api/amenitybooking/with-face endpoint.",
                    requiresFaceVerification = true
                });
            }

            // ✅ Lấy apartmentId: Ưu tiên từ DTO, nếu không có thì lấy primary
            Guid apartmentId;
            if (dto.ApartmentId.HasValue && dto.ApartmentId.Value != Guid.Empty)
            {
                apartmentId = dto.ApartmentId.Value;
            }
            else
            {
                var userApartment = await _userService.GetUserPrimaryApartmentAsync(userId);
                if (userApartment == null)
                {
                    return BadRequest(new { message = "User không có căn hộ liên kết. Vui lòng chọn căn hộ." });
                }
                apartmentId = userApartment.ApartmentId;
            }

            var booking = await _bookingService.CreateBookingAsync(dto, userId, apartmentId);

            var response = new ApiResponse<AmenityBookingDto>
            {
                Data = booking,
                Message = "Booking created successfully",
                Success = true
            };

            return CreatedAtAction(nameof(GetBookingById), new { id = booking.BookingId }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                errors = new { timeSlot = ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                errors = new { timeSlot = ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 8b. Tạo booking mới với face verification (multipart/form-data)
    /// POST /api/amenitybooking/with-face
    /// Note: Endpoint này không hiển thị trong Swagger UI do giới hạn của Swashbuckle với IFormFile
    /// </summary>
    [Authorize]
    [HttpPost("with-face")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<AmenityBookingDto>>> CreateBookingWithFace(
        [FromForm] CreateAmenityBookingWithFaceRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request body is required" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Bad Request",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            if (request.AmenityId == Guid.Empty || request.PackageId == Guid.Empty)
            {
                return BadRequest(new { message = "AmenityId and PackageId are required" });
            }

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            // ✅ Kiểm tra amenity có yêu cầu face verification không
            var amenity = await _amenityService.GetAmenityByIdAsync(request.AmenityId);
            if (amenity == null)
            {
                return BadRequest(new { message = "Amenity không tồn tại" });
            }

            // Nếu amenity yêu cầu face verification
            if (amenity.RequiresFaceVerification)
            {
                // Kiểm tra xem user đã đăng ký khuôn mặt chưa
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                var hasFaceRegistered = user != null && user.FaceEmbedding != null && user.FaceEmbedding.Length > 0;

                // Chỉ yêu cầu ảnh nếu user chưa đăng ký khuôn mặt
                if (!hasFaceRegistered && (request.FaceImage == null || request.FaceImage.Length == 0))
                {
                    return BadRequest(new
                    {
                        message = "Face verification required for this amenity",
                        requiresFaceVerification = true
                    });
                }

                // Nếu user đã đăng ký khuôn mặt và không gửi ảnh mới, bỏ qua verification
                if (hasFaceRegistered && (request.FaceImage == null || request.FaceImage.Length == 0))
                {
                    _logger.LogInformation("User {UserId} has already registered face, skipping face verification for amenity {AmenityId}",
                        userId, request.AmenityId);
                }
                else if (request.FaceImage != null && request.FaceImage.Length > 0)
                {

                    await using var faceMemoryStream = new MemoryStream();
                    await request.FaceImage.CopyToAsync(faceMemoryStream);
                    var faceBytes = faceMemoryStream.ToArray();

                    var fileName = string.IsNullOrWhiteSpace(request.FaceImage.FileName)
                        ? $"{userId}.jpg"
                        : request.FaceImage.FileName;
                    var fieldName = string.IsNullOrWhiteSpace(request.FaceImage.Name)
                        ? "FaceImage"
                        : request.FaceImage.Name;
                    var contentType = string.IsNullOrWhiteSpace(request.FaceImage.ContentType)
                        ? "image/jpeg"
                        : request.FaceImage.ContentType;

                    async Task<FaceVerifyResponseDto> VerifyFaceAsync()
                    {
                        await using var verifyStream = new MemoryStream(faceBytes);
                        var verifyFile = new FormFile(verifyStream, 0, faceBytes.Length, fieldName, fileName)
                        {
                            Headers = new HeaderDictionary(),
                            ContentType = contentType
                        };

                        var verifyRequest = new FaceVerifyRequestDto
                        {
                            UserId = userId,
                            Image = verifyFile
                        };

                        return await _faceRecognitionService.VerifyFaceAsync(verifyRequest);
                    }

                    async Task<FaceRegisterResponseDto> RegisterFaceAsync()
                    {
                        await using var registerStream = new MemoryStream(faceBytes);
                        var registerFile = new FormFile(registerStream, 0, faceBytes.Length, fieldName, fileName)
                        {
                            Headers = new HeaderDictionary(),
                            ContentType = contentType
                        };

                        var registerRequest = new FaceRegisterRequestDto
                        {
                            UserId = userId,
                            Image = registerFile
                        };

                        return await _faceRecognitionService.RegisterFaceAsync(registerRequest);
                    }

                    //Verify lần đầu
                    var verifyResult = await VerifyFaceAsync();
                    //Nếu không xác thực được, đăng ký khuôn mặt
                    if (!verifyResult.IsVerified)
                    {
                        var message = verifyResult.Message?.ToLowerInvariant();
                        if (!string.IsNullOrWhiteSpace(message) && message.Contains("chưa đăng ký khuôn mặt"))
                        {
                            var registerResult = await RegisterFaceAsync();
                            if (!registerResult.Success)
                            {
                                return BadRequest(new
                                {
                                    message = "Không thể đăng ký khuôn mặt từ ảnh chụp. Vui lòng thử lại hoặc liên hệ lễ tân.",
                                    error = registerResult.Message,
                                    requiresFaceVerification = true
                                });
                            }

                            verifyResult = await VerifyFaceAsync();
                        }
                    }

                    if (!verifyResult.IsVerified)
                    {
                        return BadRequest(new
                        {
                            message = "Xác minh khuôn mặt không thành công. Vui lòng đảm bảo bạn khuôn mặt chân dung và hình ảnh rõ nét.",
                            similarity = verifyResult.Similarity,
                            requiresFaceVerification = true,
                            error = verifyResult.Message
                        });
                    }

                    _logger.LogInformation("Face verification successful for user {UserId} booking amenity {AmenityId}. Similarity: {Similarity}",
                        userId, request.AmenityId, verifyResult.Similarity);
                }
            }

            // ✅ Lấy apartmentId: Ưu tiên từ request, nếu không có thì lấy primary
            Guid finalApartmentId;
            if (request.ApartmentId.HasValue && request.ApartmentId.Value != Guid.Empty)
            {
                finalApartmentId = request.ApartmentId.Value;
            }
            else
            {
                var userApartment = await _userService.GetUserPrimaryApartmentAsync(userId);
                if (userApartment == null)
                {
                    return BadRequest(new { message = "User không có căn hộ liên kết. Vui lòng chọn căn hộ." });
                }
                finalApartmentId = userApartment.ApartmentId;
            }

            // Tạo DTO từ request
            var dto = new CreateAmenityBookingDto
            {
                AmenityId = request.AmenityId,
                PackageId = request.PackageId,
                ApartmentId = finalApartmentId,
                Notes = request.Notes
            };

            var booking = await _bookingService.CreateBookingAsync(dto, userId, finalApartmentId);

            var response = new ApiResponse<AmenityBookingDto>
            {
                Data = booking,
                Message = "Booking created successfully",
                Success = true
            };

            return CreatedAtAction(nameof(GetBookingById), new { id = booking.BookingId }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                errors = new { timeSlot = ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                errors = new { timeSlot = ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking with face for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 9. Cập nhật booking
    /// PUT /api/amenitybooking/{id}
    /// </summary>
    // Cư dân được phép chỉnh booking Pending của chính họ (service đã kiểm tra)
    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<AmenityBookingDto>>> UpdateBooking(Guid id, [FromBody] UpdateAmenityBookingDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var booking = await _bookingService.UpdateBookingAsync(id, dto, userId);
            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            var response = new ApiResponse<AmenityBookingDto>
            {
                Data = booking,
                Message = "Booking updated successfully",
                Success = true
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 10. Hủy booking
    /// POST /api/amenitybooking/{id}/cancel
    /// </summary>
    // Cư dân hủy booking của mình
    [Authorize]
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> CancelBooking(Guid id, [FromBody] CancelBookingRequest? request = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            // Kiểm tra quyền admin/receptionist để cho phép hủy booking của cư dân
            var isAdminOrReceptionist = HasReceptionistPrivilege()
                                        || User.IsInRole("global_admin")
                                        || User.IsInRole("building_admin")
                                        || User.IsInRole("building-manager")
                                        || User.IsInRole("building_management");

            var result = await _bookingService.CancelBookingAsync(id, userId, request?.Reason, isAdminOrReceptionist);
            if (!result)
            {
                return NotFound(new { message = "Booking not found" });
            }

            var response = new ApiResponse<object>
            {
                Data = new
                {
                    bookingId = id,
                    status = "Cancelled",
                    cancellationReason = request?.Reason,
                    cancelledAt = DateTime.UtcNow
                },
                Message = "Booking cancelled successfully",
                Success = true
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 11. Xác nhận booking (Admin/Staff)
    /// POST /api/amenitybooking/{id}/confirm
    /// </summary>
    // Quản lý xác nhận
    // [Authorize(Policy = "RequireManagerOrAdmin")]
    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmBooking(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var result = await _bookingService.ConfirmBookingAsync(id, userId);
            if (!result)
            {
                return NotFound(new { message = "Booking not found" });
            }

            var response = new ApiResponse<object>
            {
                Data = new
                {
                    bookingId = id,
                    status = "Confirmed",
                    confirmedAt = DateTime.UtcNow
                },
                Message = "Booking confirmed successfully",
                Success = true
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 12. Hoàn thành booking (Admin/Staff)
    /// POST /api/amenitybooking/{id}/complete
    /// </summary>
    // Quản lý hoàn thành
    // [Authorize(Policy = "RequireManagerOrAdmin")]
    [HttpPost("{id}/complete")]
    public async Task<ActionResult<ApiResponse<object>>> CompleteBooking(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var result = await _bookingService.CompleteBookingAsync(id, userId);
            if (!result)
            {
                return NotFound(new { message = "Booking not found" });
            }

            var response = new ApiResponse<object>
            {
                Data = new
                {
                    bookingId = id,
                    status = "Completed",
                    completedAt = DateTime.UtcNow
                },
                Message = "Booking completed successfully",
                Success = true
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 12b. Check-in vào tiện ích bằng khuôn mặt
    /// POST /api/amenitybooking/{bookingId}/check-in
    /// </summary>
    [Authorize]
    [HttpPost("{bookingId}/check-in")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<FaceCheckInResponseDto>>> CheckInWithFace(
        Guid bookingId,
        [FromForm] CheckInWithFaceRequest request)
    {
        try
        {
            if (request == null || request.FaceImage == null || request.FaceImage.Length == 0)
            {
                return BadRequest(new { message = "Face image is required" });
            }

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            // 1. Lấy booking
            var booking = await _bookingService.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return NotFound(new { message = "Booking không tồn tại" });
            }

            // 2. Kiểm tra booking thuộc về user hiện tại hoặc user có quyền admin
            if (booking.UserId != userId)
            {
                // Có thể kiểm tra role admin ở đây nếu cần
                return Forbid("Bạn không có quyền check-in booking này");
            }

            // 3. Kiểm tra booking đã được confirm chưa
            if (booking.Status != "Confirmed")
            {
                return BadRequest(new
                {
                    message = $"Booking chưa được xác nhận. Trạng thái hiện tại: {booking.Status}",
                    currentStatus = booking.Status
                });
            }

            // 4. Xác thực khuôn mặt
            var verifyRequest = new FaceVerifyRequestDto
            {
                UserId = userId,
                Image = request.FaceImage
            };

            var verifyResult = await _faceRecognitionService.VerifyFaceAsync(verifyRequest);
            var targetUserId = booking.UserId ?? userId;

            if (!verifyResult.IsVerified)
            {
                var failedRecord = await _checkInService.RecordCheckInAsync(
                    bookingId,
                    targetUserId,
                    userId,
                    false,
                    "Failed",
                    verifyResult.Similarity,
                    verifyResult.Message,
                    false);

                return BadRequest(new ApiResponse<FaceCheckInResponseDto>
                {
                    Success = false,
                    Message = "Xác thực khuôn mặt thất bại. Vui lòng đảm bảo ảnh rõ ràng và đúng người.",
                    Data = new FaceCheckInResponseDto
                    {
                        Success = false,
                        Message = verifyResult.Message,
                        Similarity = verifyResult.Similarity,
                        CheckedInAt = failedRecord.CheckedInAt
                    }
                });
            }

            // 5. Ghi nhận check-in (có thể tạo bảng check-in history hoặc cập nhật booking)
            _logger.LogInformation("Check-in successful for booking {BookingId}, user {UserId}. Similarity: {Similarity}",
                bookingId, userId, verifyResult.Similarity);

            var record = await _checkInService.RecordCheckInAsync(
                bookingId,
                targetUserId,
                userId,
                true,
                "SelfCheckIn",
                verifyResult.Similarity,
                verifyResult.Message,
                false);

            var response = new ApiResponse<FaceCheckInResponseDto>
            {
                Success = true,
                Message = "Check-in thành công",
                Data = new FaceCheckInResponseDto
                {
                    Success = true,
                    Message = "Check-in thành công",
                    Similarity = verifyResult.Similarity,
                    CheckedInAt = record.CheckedInAt
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-in for booking {BookingId}", bookingId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 12c. Lễ tân check-in cư dân bằng khuôn mặt
    /// POST /api/amenitybooking/{bookingId}/receptionist-check-in
    /// </summary>
    [Authorize]
    [HttpPost("{bookingId}/receptionist-check-in")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<AmenityCheckInDto>>> ReceptionistCheckIn(
        Guid bookingId,
        [FromForm] ReceptionistCheckInRequest request)
    {
        try
        {
            if (!HasReceptionistPrivilege())
            {
                return Forbid("Bạn không có quyền thực hiện check-in thay cư dân");
            }

            if (request == null || request.FaceImage == null || request.FaceImage.Length == 0)
            {
                return BadRequest(new { message = "Face image is required" });
            }

            var operatorUserId = GetCurrentUserId();
            if (operatorUserId == Guid.Empty)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var booking = await _bookingService.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return NotFound(new { message = "Booking không tồn tại" });
            }

            if (booking.UserId == null || booking.UserId == Guid.Empty)
            {
                return BadRequest(new { message = "Booking chưa gắn với cư dân cụ thể" });
            }

            if (booking.Status != "Confirmed")
            {
                return BadRequest(new
                {
                    message = $"Booking chưa được xác nhận. Trạng thái hiện tại: {booking.Status}",
                    currentStatus = booking.Status
                });
            }

            var targetUserId = booking.UserId.Value;
            float? similarity = null;
            var resultStatus = "Success";
            string? verificationMessage = null;
            var isSuccess = true;

            if (!request.SkipFaceVerification)
            {
                var verifyRequest = new FaceVerifyRequestDto
                {
                    UserId = targetUserId,
                    Image = request.FaceImage
                };

                var verifyResult = await _faceRecognitionService.VerifyFaceAsync(verifyRequest);
                similarity = verifyResult.Similarity;
                verificationMessage = verifyResult.Message;

                if (!verifyResult.IsVerified)
                {
                    if (!request.ManualOverride)
                    {
                        var failedRecord = await _checkInService.RecordCheckInAsync(
                            bookingId,
                            targetUserId,
                            operatorUserId,
                            false,
                            "Failed",
                            verifyResult.Similarity,
                            verificationMessage,
                            false,
                            notes: request.Notes);

                        return BadRequest(new ApiResponse<AmenityCheckInDto>
                        {
                            Success = false,
                            Message = "Xác thực khuôn mặt thất bại. Vui lòng thử lại hoặc yêu cầu cư dân đăng ký khuôn mặt.",
                            Data = failedRecord
                        });
                    }

                    // Manual override
                    resultStatus = "ManualOverride";
                    verificationMessage = $"{verificationMessage ?? "Manual override"} - overridden bởi lễ tân.";
                }
            }
            else
            {
                resultStatus = request.ManualOverride ? "ManualOverride" : "SkippedVerification";
            }

            var record = await _checkInService.RecordCheckInAsync(
                bookingId,
                targetUserId,
                operatorUserId,
                isSuccess,
                resultStatus,
                similarity,
                verificationMessage,
                request.ManualOverride,
                notes: request.Notes);

            _logger.LogInformation("Receptionist check-in recorded for booking {BookingId} by {Operator}. Status: {Status}, Success: {Success}",
                bookingId, operatorUserId, resultStatus, isSuccess);

            return Ok(new ApiResponse<AmenityCheckInDto>
            {
                Success = true,
                Message = resultStatus == "ManualOverride"
                    ? "Đã check-in với chế độ override."
                    : "Check-in thành công.",
                Data = record
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during receptionist check-in for booking {BookingId}", bookingId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// 12d. Danh sách lịch sử check-in
    /// GET /api/amenitybooking/check-ins
    /// </summary>
    [Authorize]
    [HttpGet("check-ins")]
    public async Task<ActionResult<PagedApiResponse<AmenityCheckInDto>>> GetCheckInHistory([FromQuery] AmenityCheckInQueryDto query)
    {
        if (!HasReceptionistPrivilege())
        {
            return Forbid("Bạn không có quyền xem lịch sử check-in");
        }

        var result = await _checkInService.GetPagedAsync(query);
        var response = new PagedApiResponse<AmenityCheckInDto>(
            result.Items.ToList(),
            result.TotalCount,
            result.PageNumber,
            result.PageSize)
        {
            TotalPages = result.TotalPages
        };

        return Ok(response);
    }

    /// <summary>
    /// 12e. Lấy check-in gần nhất của một booking
    /// GET /api/amenitybooking/{bookingId}/check-ins/latest
    /// </summary>
    [Authorize]
    [HttpGet("{bookingId}/check-ins/latest")]
    public async Task<ActionResult<ApiResponse<AmenityCheckInDto>>> GetLatestCheckIn(Guid bookingId)
    {
        if (!HasReceptionistPrivilege())
        {
            return Forbid("Bạn không có quyền xem lịch sử check-in");
        }

        var record = await _checkInService.GetLatestByBookingIdAsync(bookingId);
        if (record == null)
        {
            return NotFound(new { message = "Chưa có dữ liệu check-in cho booking này" });
        }

        return Ok(new ApiResponse<AmenityCheckInDto>
        {
            Data = record,
            Success = true,
            Message = "Lấy dữ liệu check-in thành công"
        });
    }

    /// <summary>
    /// 13. Cập nhật trạng thái thanh toán (Admin/Staff)
    /// PATCH /api/amenitybooking/{id}/payment-status
    /// </summary>
    // Quản lý cập nhật thanh toán
    // [Authorize(Policy = "RequireManagerOrAdmin")]
    [HttpPatch("{id}/payment-status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePaymentStatus(Guid id, [FromBody] UpdatePaymentStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var result = await _bookingService.UpdatePaymentStatusAsync(id, request.PaymentStatus, userId);
            if (!result)
            {
                return NotFound(new { message = "Booking not found" });
            }

            var response = new ApiResponse<object>
            {
                Data = new
                {
                    bookingId = id,
                    paymentStatus = request.PaymentStatus,
                    paidAt = request.PaymentStatus == "Paid" ? DateTime.UtcNow : (DateTime?)null
                },
                Message = "Payment status updated successfully",
                Success = true
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Helper method để lấy user ID từ JWT token (Keycloak)
    /// </summary>
    private Guid GetCurrentUserId()
    {
        // Keycloak thường dùng claim "sub" cho user ID
        var userIdClaim = User.FindFirst("sub")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("user_id")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool HasReceptionistPrivilege()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
        {
            return false;
        }

        return User.IsInRole("receptionist")
               || User.IsInRole("global_admin")
               || User.IsInRole("building_admin")
               || User.IsInRole("building-manager")
               || User.IsInRole("building_management");
    }

    /// <summary>
    /// Lễ tân tạo booking cho cư dân
    /// POST /api/amenitybooking/receptionist/create-for-resident
    /// </summary>
    [Authorize]
    [HttpPost("receptionist/create-for-resident")]
    [Consumes("application/json")]
    public async Task<ActionResult<ApiResponse<AmenityBookingDto>>> ReceptionistCreateBookingForResident(
        [FromBody] ReceptionistCreateBookingRequest request)
    {
        if (!HasReceptionistPrivilege())
        {
            return Forbid("Bạn không có quyền thực hiện thao tác này");
        }

        try
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request body is required" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Bad Request",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var operatorId = GetCurrentUserId();

            // Kiểm tra amenity có tồn tại không
            var amenity = await _amenityService.GetAmenityByIdAsync(request.AmenityId);
            if (amenity == null)
            {
                return BadRequest(new { message = "Amenity không tồn tại" });
            }

            // Kiểm tra amenity có đang active không
            if (amenity.Status != "ACTIVE")
            {
                return BadRequest(new { message = "Amenity không khả dụng" });
            }

            // Lấy apartmentId: Ưu tiên từ request, nếu không có thì lấy primary apartment của user
            Guid apartmentId;
            if (request.ApartmentId.HasValue && request.ApartmentId.Value != Guid.Empty)
            {
                apartmentId = request.ApartmentId.Value;
            }
            else
            {
                var userApartment = await _userService.GetUserPrimaryApartmentAsync(request.UserId);
                if (userApartment == null)
                {
                    return BadRequest(new { message = "Cư dân không có căn hộ liên kết. Vui lòng chọn căn hộ." });
                }
                apartmentId = userApartment.ApartmentId;
            }

            // Tạo DTO từ request
            var dto = new CreateAmenityBookingDto
            {
                AmenityId = request.AmenityId,
                PackageId = request.PackageId,
                ApartmentId = apartmentId,
                Notes = request.Notes
            };

            // Tạo booking cho cư dân (request.UserId)
            // Không tự động confirm - chỉ confirm sau khi thanh toán thành công
            var booking = await _bookingService.CreateBookingAsync(dto, request.UserId, apartmentId);

            // Reload booking để lấy thông tin mới nhất
            var createdBooking = await _bookingService.GetByIdAsync(booking.BookingId);
            if (createdBooking == null)
            {
                return StatusCode(500, new { message = "Không thể tải thông tin booking sau khi tạo" });
            }

            var response = new ApiResponse<AmenityBookingDto>
            {
                Data = createdBooking,
                Message = "Đã tạo đăng ký tiện ích thành công cho cư dân. Vui lòng thanh toán để xác nhận.",
                Success = true
            };

            return CreatedAtAction(nameof(GetBookingById), new { id = createdBooking.BookingId }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                errors = new { general = ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                errors = new { general = ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking for resident {UserId} by receptionist {OperatorId}",
                request?.UserId, GetCurrentUserId());
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Lễ tân quét nhanh và tự động check-in (không cần thực hiện thủ công)
    /// POST /api/amenitybooking/receptionist/scan
    /// </summary>
    [Authorize]
    [HttpPost("receptionist/scan")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ReceptionistScanResultDto>>> ReceptionistScan([FromForm] ReceptionistScanRequest request)
    {
        if (!HasReceptionistPrivilege())
        {
            return Forbid("Bạn không có quyền thực hiện thao tác này");
        }

        if (request?.FaceImage == null || request.FaceImage.Length == 0)
        {
            return BadRequest(new { message = "Face image is required" });
        }

        var operatorId = GetCurrentUserId();

        var identifyResult = await _faceRecognitionService.IdentifyFaceAsync(request.FaceImage);
        if (!identifyResult.IsIdentified || identifyResult.UserId == null)
        {
            var notFound = new ReceptionistScanResultDto
            {
                Success = false,
                UserId = identifyResult.UserId,
                ResidentName = identifyResult.FullName,
                AvatarUrl = identifyResult.AvatarUrl,
                Similarity = identifyResult.Similarity,
                Message = string.IsNullOrWhiteSpace(identifyResult.Message)
                    ? "Không tìm thấy cư dân phù hợp."
                    : identifyResult.Message
            };

            return Ok(new ApiResponse<ReceptionistScanResultDto>
            {
                Success = false,
                Message = notFound.Message,
                Data = notFound
            });
        }

        var activeBookings = await _bookingService.GetActiveBookingsByUserAsync(
            identifyResult.UserId.Value,
            request.AmenityId,
            DateTime.UtcNow.AddHours(7));

        var booking = activeBookings
            .OrderBy(b => b.StartDate)
            .ThenBy(b => b.EndDate)
            .FirstOrDefault();

        if (booking == null)
        {
            var result = new ReceptionistScanResultDto
            {
                Success = false,
                UserId = identifyResult.UserId,
                ResidentName = identifyResult.FullName,
                AvatarUrl = identifyResult.AvatarUrl,
                Similarity = identifyResult.Similarity,
                Message = "Đã nhận diện cư dân nhưng không có booking còn hiệu lực."
            };

            return Ok(new ApiResponse<ReceptionistScanResultDto>
            {
                Success = false,
                Message = result.Message,
                Data = result
            });
        }

        var record = await _checkInService.RecordCheckInAsync(
            booking.BookingId,
            identifyResult.UserId.Value,
            operatorId == Guid.Empty ? (Guid?)null : operatorId,
            true,
            "QuickScan",
            identifyResult.Similarity,
            identifyResult.Message,
            false);

        var successResult = new ReceptionistScanResultDto
        {
            Success = true,
            UserId = identifyResult.UserId,
            ResidentName = identifyResult.FullName,
            AvatarUrl = identifyResult.AvatarUrl,
            Similarity = identifyResult.Similarity,
            Booking = booking,
            CheckIn = record,
            Message = "Đã nhận diện và check-in thành công.",
            AlreadyCheckedInToday = false
        };

        return Ok(new ApiResponse<ReceptionistScanResultDto>
        {
            Success = true,
            Message = successResult.Message,
            Data = successResult
        });
    }
}
