using System;
using System.Linq;
using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Request;
using SAMS_BE.DTOs.Response;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Interfaces.IMail;
using SAMS_BE.Mappers;
using SAMS_BE.Tenant;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Services;

public class AmenityBookingService : IAmenityBookingService
{
    private readonly IAmenityBookingRepository _bookingRepository;
    private readonly IAmenityRepository _amenityRepository;
    private readonly IAmenityPackageRepository _packageRepository;
    private readonly IAmenityNotificationService _notificationService;
    private readonly IAssetMaintenanceScheduleRepository _scheduleRepository;
    private readonly IEmailSender _emailSender;
    private readonly IUserRepository _userRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly Models.BuildingManagementContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly Microsoft.Extensions.Logging.ILogger<AmenityBookingService> _logger;

    public AmenityBookingService(
        IAmenityBookingRepository bookingRepository,
        IAmenityRepository amenityRepository,
        IAmenityPackageRepository packageRepository,
        IAmenityNotificationService notificationService,
        IAssetMaintenanceScheduleRepository scheduleRepository,
        IEmailSender emailSender,
        IUserRepository userRepository,
        IBuildingRepository buildingRepository,
        ITenantContextAccessor tenantContextAccessor,
        Models.BuildingManagementContext context,
        IWebHostEnvironment env,
        Microsoft.Extensions.Logging.ILogger<AmenityBookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _amenityRepository = amenityRepository;
        _packageRepository = packageRepository;
        _notificationService = notificationService;
        _scheduleRepository = scheduleRepository;
        _emailSender = emailSender;
        _userRepository = userRepository;
        _buildingRepository = buildingRepository;
        _tenantContextAccessor = tenantContextAccessor;
        _context = context;
        _env = env;
        _logger = logger;
    }

    public async Task<AmenityBookingDto?> GetByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        return booking?.ToDto();
    }

    public async Task<IEnumerable<AmenityBookingDto>> GetAllAsync()
    {
        var bookings = await _bookingRepository.GetAllAsync();
        return bookings.ToDto();
    }

    public async Task<PagedResult<AmenityBookingDto>> GetPagedAsync(AmenityBookingQueryDto query)
    {
        var pagedResult = await _bookingRepository.GetPagedAsync(query);

        return new PagedResult<AmenityBookingDto>
        {
            Items = pagedResult.Items.ToDto().ToList(),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };
    }

    public async Task<IEnumerable<AmenityBookingDto>> GetByAmenityIdAsync(Guid amenityId)
    {
        var bookings = await _bookingRepository.GetByAmenityIdAsync(amenityId);
        return bookings.ToDto();
    }

    public async Task<IEnumerable<AmenityBookingDto>> GetByApartmentIdAsync(Guid apartmentId)
    {
        var bookings = await _bookingRepository.GetByApartmentIdAsync(apartmentId);
        return bookings.ToDto();
    }

    public async Task<IEnumerable<AmenityBookingDto>> GetByUserIdAsync(Guid userId)
    {
        var bookings = await _bookingRepository.GetByUserIdAsync(userId);
        return bookings.ToDto();
    }

    public async Task<IEnumerable<AmenityBookingDto>> GetMyBookingsAsync(Guid userId)
    {
        return await GetByUserIdAsync(userId);
    }

    public async Task<AmenityBookingDto> CreateBookingAsync(CreateAmenityBookingDto dto, Guid userId, Guid apartmentId)
    {
        // 1. Kiểm tra amenity có tồn tại không
        var amenity = await _amenityRepository.GetAmenityByIdAsync(dto.AmenityId);
        if (amenity == null)
        {
            throw new ArgumentException("Amenity not found");
        }

        // 2. Kiểm tra amenity có đang active không
        if (amenity.Status != "ACTIVE")
        {
            throw new InvalidOperationException("Amenity is not available for booking");
        }

        // 3. Kiểm tra package có tồn tại không
        var package = await _packageRepository.GetPackageByIdAsync(dto.PackageId);
        if (package == null)
        {
            throw new ArgumentException("Package not found");
        }

        // 4. Kiểm tra package có thuộc amenity không
        if (package.AmenityId != dto.AmenityId)
        {
            throw new ArgumentException("Package does not belong to the specified amenity");
        }

        // 5. Kiểm tra package có active không
        if (package.Status != "ACTIVE")
        {
            throw new InvalidOperationException("Package is not available");
        }

        // 5.1 Kiểm tra tiện ích có đang bảo trì không
        if (amenity.AssetId.HasValue)
        {
            var isUnderMaintenance = await _scheduleRepository.IsAssetUnderMaintenanceAsync(amenity.AssetId.Value);
            if (isUnderMaintenance)
            {
                throw new InvalidOperationException($"Tiện ích {amenity.Name} hiện đang trong thời gian bảo trì. Vui lòng quay lại sau khi bảo trì hoàn tất.");
            }
        }

        // 6. Tính toán startDate và endDate từ package
        var booking = dto.ToEntity(userId, package.Price, package.MonthCount, package.DurationDays, package.PeriodUnit);
        booking.ApartmentId = apartmentId;

        // 7. Kiểm tra trùng lịch với các booking hiện có của user này
        var overlappingBookings = await _bookingRepository.GetOverlappingBookingsAsync(
            dto.AmenityId,
            userId,
            booking.StartDate,
            booking.EndDate);

        if (overlappingBookings.Any())
        {
            var conflict = overlappingBookings.First();
            var amenityName = conflict.Amenity?.Name ?? "tiện ích";
            var message = $"Bạn đang sử dụng {amenityName} trong khoảng thời gian " +
                          $"{conflict.StartDate:dd/MM/yyyy} - {conflict.EndDate:dd/MM/yyyy}.";
            throw new InvalidOperationException(message);
        }

        // 8. Tạo booking
        var createdBooking = await _bookingRepository.CreateAsync(booking);
        return createdBooking.ToDto();

    }

    public async Task<AmenityBookingDto?> UpdateBookingAsync(Guid bookingId, UpdateAmenityBookingDto dto, Guid userId)
    {
        var existingBooking = await _bookingRepository.GetByIdAsync(bookingId);
        if (existingBooking == null)
        {
            return null;
        }

        // Chỉ cho phép update nếu status là Pending
        if (existingBooking.Status != "Pending")
        {
            throw new InvalidOperationException("Can only update pending bookings");
        }

        // Kiểm tra package mới
        var package = await _packageRepository.GetPackageByIdAsync(dto.PackageId);
        if (package == null)
        {
            throw new ArgumentException("Package not found");
        }

        if (package.AmenityId != existingBooking.AmenityId)
        {
            throw new ArgumentException("Cannot change to a package from a different amenity");
        }

        if (package.Status != "ACTIVE")
        {
            throw new InvalidOperationException("Package is not available");
        }

        // Cập nhật booking
        existingBooking.UpdateEntity(dto, userId, package.Price, package.MonthCount, package.DurationDays, package.PeriodUnit);

        var updatedBooking = await _bookingRepository.UpdateAsync(existingBooking);
        return updatedBooking?.ToDto();
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, string? cancelReason = null, bool isAdminOrReceptionist = false)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        // Kiểm tra quyền: Chỉ người tạo booking hoặc admin/receptionist mới được hủy
        if (booking.UserId != userId && !isAdminOrReceptionist)
        {
            throw new UnauthorizedAccessException("You are not authorized to cancel this booking");
        }

        // Chỉ cho phép hủy booking có status là Pending hoặc Confirmed
        if (booking.Status != "Pending" && booking.Status != "Confirmed")
        {
            throw new InvalidOperationException("Cannot cancel booking with current status");
        }

        return await _bookingRepository.CancelAsync(bookingId, cancelReason ?? "Cancelled by user");
    }

    public async Task<bool> ConfirmBookingAsync(Guid bookingId, Guid adminUserId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        if (booking.Status != "Pending")
        {
            throw new InvalidOperationException("Can only confirm pending bookings");
        }

        var result = await _bookingRepository.ConfirmAsync(bookingId);

        // Gửi email thông báo đăng ký thành công
        if (result && booking.UserId.HasValue)
        {
            try
            {
                // Lấy thông tin user và resident để lấy email
                var user = await _userRepository.GetByIdAsync(booking.UserId.Value);
                var resident = await _context.ResidentProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(rp => rp.UserId == booking.UserId.Value);

                string? recipientEmail = null;
                string residentName = "Quý khách";

                if (resident != null)
                {
                    recipientEmail = resident.Email;
                    residentName = resident.FullName ?? residentName;
                }
                else if (user != null)
                {
                    recipientEmail = user.Email;
                    residentName = $"{user.FirstName} {user.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(residentName))
                    {
                        residentName = user.Username ?? "Quý khách";
                    }
                }

                // Chỉ gửi email nếu tìm được địa chỉ email
                if (!string.IsNullOrWhiteSpace(recipientEmail))
                {
                    // Lấy thông tin amenity và package
                    var amenity = await _amenityRepository.GetAmenityByIdAsync(booking.AmenityId);
                    var package = await _packageRepository.GetPackageByIdAsync(booking.PackageId);

                    // Xác định loại booking dựa trên thông tin package
                    string bookingType = package?.Name ?? "Đăng ký tiện ích";
                    if (package != null)
                    {
                        // Xác định loại dựa trên PeriodUnit hoặc MonthCount
                        if (!string.IsNullOrWhiteSpace(package.PeriodUnit))
                        {
                            bookingType = package.PeriodUnit.ToLower() switch
                            {
                                "hour" => "Theo giờ",
                                "day" => "Theo ngày",
                                "month" => "Theo tháng",
                                _ => package.Name
                            };
                        }
                        else if (package.MonthCount > 0)
                        {
                            bookingType = "Theo tháng";
                        }
                    }

                    // Format thông tin thời gian
                    string timeInfo = $"{booking.StartDate:dd/MM/yyyy} - {booking.EndDate:dd/MM/yyyy}";
                    string startDate = booking.StartDate.ToString("dd/MM/yyyy");
                    string endDate = booking.EndDate.ToString("dd/MM/yyyy");

                    // Tạo mã giao dịch (sử dụng bookingId)
                    string transactionCode = $"NOAH-{booking.BookingId.ToString().Substring(0, 8).ToUpper()}";

                    // Thời gian thanh toán (giờ hiện tại - Vietnam timezone)
                    string paymentTime = DateTime.UtcNow.AddHours(7).ToString("HH:mm dd/MM/yyyy");

                    // Phương thức thanh toán và trạng thái
                    string paymentMethod = "QR Banking";
                    string status = "Đã thanh toán";

                    // Build email body
                    var htmlBody = await BuildAmenityBookingSuccessEmailBodyAsync(
                        residentName,
                        amenity?.Name ?? "Tiện ích",
                        bookingType,
                        timeInfo,
                        startDate,
                        endDate,
                        booking.TotalPrice,
                        transactionCode,
                        paymentTime,
                        paymentMethod,
                        status,
                        CancellationToken.None);

                    // Gửi email
                    await _emailSender.SendEmailAsync(
                        recipientEmail,
                        "[NOAH] Xác nhận đăng ký tiện ích thành công",
                        htmlBody);

                    _logger.LogInformation(
                        "Sent booking success email to {Email} for booking {BookingId}",
                        recipientEmail,
                        bookingId);
                }
                else
                {
                    _logger.LogWarning(
                        "Could not send booking success email for booking {BookingId} - No email address found",
                        bookingId);
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không throw để không làm gián đoạn flow xác nhận booking
                _logger.LogError(ex,
                    "Failed to send booking success email for booking {BookingId}",
                    bookingId);
            }
        }

        return result;
    }

    public async Task<bool> CompleteBookingAsync(Guid bookingId, Guid adminUserId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        if (booking.Status != "Confirmed")
        {
            throw new InvalidOperationException("Can only complete confirmed bookings");
        }

        return await _bookingRepository.CompleteAsync(bookingId);
    }

    public async Task<bool> UpdatePaymentStatusAsync(Guid bookingId, string paymentStatus, Guid adminUserId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        var result = await _bookingRepository.UpdatePaymentStatusAsync(bookingId, paymentStatus);

        // Nếu thanh toán thành công, tạo thông báo
        if (result && paymentStatus == "Paid")
        {
            // Đợi 1.5 giây để đảm bảo tất cả updates (confirm + payment status) đã được commit vào DB
            await Task.Delay(1500);

            // Reload booking để có status mới nhất (bao gồm cả PaymentStatus và Status)
            var updatedBooking = await _bookingRepository.GetByIdAsync(bookingId);
            if (updatedBooking != null)
            {
                try
                {
                    // Log để debug
                    _logger.LogInformation(
                        "Attempting to create notification for booking {BookingId}. PaymentStatus: {PaymentStatus}, Status: {Status}",
                        bookingId,
                        updatedBooking.PaymentStatus,
                        updatedBooking.Status);

                    // Tạo thông báo - NotificationService sẽ check lại PaymentStatus == "Paid" && Status == "Confirmed"
                    await _notificationService.CreateBookingSuccessNotificationAsync(bookingId);
                }
                catch (Exception ex)
                {
                    // Log error nhưng không throw để không ảnh hưởng đến flow thanh toán
                    _logger.LogWarning(ex, "Failed to create notification for booking {BookingId}, but payment was successful", bookingId);
                }
            }
        }

        return result;
    }

    public async Task<AvailabilityCheckResponse> CheckAvailabilityAsync(Guid amenityId)
    {
        // Với hệ thống packages theo tháng, không cần check time slot hay capacity
        // Chỉ cần kiểm tra amenity có active và có packages không
        var amenity = await _amenityRepository.GetAmenityByIdAsync(amenityId);

        if (amenity == null)
        {
            return new AvailabilityCheckResponse
            {
                IsAvailable = false,
                Message = "Amenity not found",
                ConflictingBookings = new List<ConflictingBookingInfo>()
            };
        }

        if (amenity.Status != "ACTIVE")
        {
            return new AvailabilityCheckResponse
            {
                IsAvailable = false,
                Message = "Amenity is not available",
                ConflictingBookings = new List<ConflictingBookingInfo>()
            };
        }

        return new AvailabilityCheckResponse
        {
            IsAvailable = true,
            Message = "Available",
            ConflictingBookings = new List<ConflictingBookingInfo>()
        };
    }

    public async Task<PriceCalculationResponse> CalculatePriceAsync(CalculatePriceRequest request)
    {
        try
        {
            if (request.PackageId == Guid.Empty)
            {
                throw new ArgumentException("PackageId is required");
            }

            var package = await _packageRepository.GetPackageByIdAsync(request.PackageId);
            
            if (package == null)
            {
                throw new ArgumentException("Package not found");
            }

            // Tạo chi tiết hiển thị dựa trên period_unit
            string details;
            if (package.PeriodUnit == "Day" && package.DurationDays.HasValue)
            {
                details = $"Package: {package.Name} ({package.DurationDays.Value} day(s))";
            }
            else
            {
                details = $"Package: {package.Name} ({package.MonthCount} month(s))";
            }

            return new PriceCalculationResponse
            {
                BasePrice = package.Price,
                TotalPrice = package.Price,
                Details = details
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating price for PackageId: {request?.PackageId}");
            throw;
        }
    }

    public async Task<IEnumerable<AmenityBookingDto>> GetActiveBookingsByUserAsync(Guid userId, Guid? amenityId = null, DateTime? referenceTime = null)
    {
        var bookings = await _bookingRepository.GetByUserIdAsync(userId);
        if (bookings == null || !bookings.Any())
        {
            return Enumerable.Empty<AmenityBookingDto>();
        }

        var targetDate = DateOnly.FromDateTime((referenceTime ?? DateTime.UtcNow).Date);

        var activeStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Confirmed",
            "Completed" // Cho phép đánh dấu Completed nhưng vẫn trong thời hạn
        };

        var activeBookings = bookings
            .Where(b =>
                !b.IsDelete &&
                activeStatuses.Contains(b.Status ?? string.Empty) &&
                b.StartDate <= targetDate &&
                b.EndDate >= targetDate);

        if (amenityId.HasValue && amenityId.Value != Guid.Empty)
        {
            activeBookings = activeBookings.Where(b => b.AmenityId == amenityId.Value);
        }

        return activeBookings.ToDto();
    }

    public async Task<PagedResult<RegisteredResidentDto>> GetRegisteredResidentsAsync(AmenityBookingQueryDto query)
    {
        // Lấy tất cả booking với filter (không phân trang để group)
        var allBookingsResult = await _bookingRepository.GetPagedAsync(new AmenityBookingQueryDto
        {
            PageNumber = 1,
            PageSize = int.MaxValue, // Lấy tất cả để group
            Status = query.Status,
            PaymentStatus = query.PaymentStatus,
            AmenityId = query.AmenityId,
            ApartmentId = query.ApartmentId,
            FromDate = query.FromDate,
            ToDate = query.ToDate,
        });

        // Group by UserId để lấy unique residents
        var residentGroups = allBookingsResult.Items
            .Where(b => b.UserId.HasValue && !b.IsDelete)
            .GroupBy(b => b.UserId!.Value)
            .ToList();

        var residents = new List<RegisteredResidentDto>();

        foreach (var group in residentGroups)
        {
            var userId = group.Key;
            var bookings = group.ToList();
            var firstBooking = bookings.First();
            var user = firstBooking.User;

            if (user == null) continue;

            var activeStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Confirmed",
                "Completed"
            };

            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var activeBookings = bookings
                .Where(b => activeStatuses.Contains(b.Status ?? string.Empty) &&
                            b.StartDate <= today &&
                            b.EndDate >= today)
                .ToList();

            var fullName = user.ResidentProfile?.FullName ?? $"{user.LastName} {user.FirstName}".Trim();

            residents.Add(new RegisteredResidentDto
            {
                UserId = userId,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                FullName = fullName,
                AvatarUrl = user.AvatarUrl,
                ApartmentCode = firstBooking.Apartment?.Number,
                ApartmentId = firstBooking.ApartmentId,
                TotalBookings = bookings.Count,
                ActiveBookings = activeBookings.Count,
                Bookings = bookings.ToDto().ToList(),
                HasFaceRegistered = user.FaceEmbedding != null && user.FaceEmbedding.Length > 0
            });
        }

        // Apply sorting
        var sortedResidents = query.SortBy?.ToLower() switch
        {
            "fullname" => query.SortOrder?.ToLower() == "asc"
                ? residents.OrderBy(r => r.FullName).ToList()
                : residents.OrderByDescending(r => r.FullName).ToList(),
            "totalbookings" => query.SortOrder?.ToLower() == "asc"
                ? residents.OrderBy(r => r.TotalBookings).ToList()
                : residents.OrderByDescending(r => r.TotalBookings).ToList(),
            "activebookings" => query.SortOrder?.ToLower() == "asc"
                ? residents.OrderBy(r => r.ActiveBookings).ToList()
                : residents.OrderByDescending(r => r.ActiveBookings).ToList(),
            _ => residents.OrderByDescending(r => r.TotalBookings).ToList()
        };

        // Apply pagination
        var totalCount = sortedResidents.Count;
        var paginatedResidents = sortedResidents
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResult<RegisteredResidentDto>
        {
            Items = paginatedResidents,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    /// <summary>
    /// Background job: Tự động cập nhật trạng thái từ Confirmed → Completed khi EndDate đã qua
    /// Chạy mỗi ngày lúc 00:00 để đảm bảo cập nhật ngay khi hết hạn
    /// </summary>
    public async Task<int> UpdateExpiredBookingsAsync()
    {
        try
        {
            // Get all buildings
            var buildings = await _buildingRepository.GetAllAsync(CancellationToken.None);
            
            int totalUpdatedCount = 0;

            foreach (var building in buildings)
            {
                try
                {
                    // Set schema for current building
                    _tenantContextAccessor.SetSchema(building.SchemaName);
                    
                    // Lấy tất cả bookings có status = "Confirmed"
                    var confirmedBookings = await _bookingRepository.GetByStatusAsync("Confirmed");
                    if (confirmedBookings == null || !confirmedBookings.Any())
                    {
                        continue;
                    }

                    // Lấy ngày hiện tại (chỉ phần ngày, bỏ qua giờ)
                    var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)); // Convert to Vietnam timezone

                    // Lọc các booking đã hết hạn (EndDate <= today)
                    // Bao gồm cả ngày kết thúc - booking sẽ hoàn tất vào cuối ngày kết thúc
                    var expiredBookings = confirmedBookings
                        .Where(b => b.EndDate <= today && !b.IsDelete)
                        .ToList();

                    if (!expiredBookings.Any())
                    {
                        continue;
                    }

                    int updatedCount = 0;
                    foreach (var booking in expiredBookings)
                    {
                        var success = await _bookingRepository.CompleteAsync(booking.BookingId);
                        if (success)
                        {
                            updatedCount++;
                        }
                    }

                    totalUpdatedCount += updatedCount;
                    
                    if (updatedCount > 0)
                    {
                        _logger.LogInformation(
                            "Completed {UpdatedCount} expired bookings for building: {BuildingName}",
                            updatedCount,
                            building.BuildingName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating expired bookings for building {BuildingName}", building.BuildingName);
                }
            }

            return totalUpdatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating expired bookings");
            throw;
        }
    }

    private async Task<string> BuildAmenityBookingSuccessEmailBodyAsync(
        string residentName,
        string amenityName,
        string bookingType,
        string timeInfo,
        string startDate,
        string endDate,
        decimal totalAmount,
        string transactionCode,
        string paymentTime,
        string paymentMethod,
        string status,
        CancellationToken ct = default)
    {
        var templatePath = Path.Combine(
            _env.ContentRootPath,
            "EmailTemplates",
            "AmenityBookingSuccessEmail.html");

        var template = await File.ReadAllTextAsync(templatePath, ct);

        var html = template
            .Replace("{{CompanyName}}", "NOAH")
            .Replace("{{AppName}}", "NOAH Building Management")
            .Replace("{{ResidentName}}", residentName)
            .Replace("{{AmenityName}}", amenityName)
            .Replace("{{BookingType}}", bookingType)
            .Replace("{{TimeInfo}}", timeInfo)
            .Replace("{{StartDate}}", startDate)
            .Replace("{{EndDate}}", endDate)
            .Replace("{{TotalAmount}}", totalAmount.ToString("N0") + " VNĐ")
            .Replace("{{TransactionCode}}", transactionCode)
            .Replace("{{PaymentTime}}", paymentTime)
            .Replace("{{PaymentMethod}}", paymentMethod)
            .Replace("{{Status}}", status)
            .Replace("{{MyBookingsUrl}}", "https://noahbuilding.me/resident/my-bookings")
            .Replace("{{SupportEmail}}", "support@noahbuilding.me");

        return html;
    }
}

