using Microsoft.Extensions.Logging;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using SAMS_BE.Tenant;

namespace SAMS_BE.Services;

public class AmenityNotificationService : IAmenityNotificationService
{
    private readonly IAmenityBookingRepository _bookingRepository;
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<AmenityNotificationService> _logger;

    public AmenityNotificationService(
        IAmenityBookingRepository bookingRepository,
        IAnnouncementRepository announcementRepository,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<AmenityNotificationService> logger)
    {
        _bookingRepository = bookingRepository;
        _announcementRepository = announcementRepository;
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Tạo thông báo sau khi thanh toán thành công
    /// </summary>
    public async Task CreateBookingSuccessNotificationAsync(Guid bookingId)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found for success notification", bookingId);
                return;
            }

            // Chỉ tạo thông báo cho booking đã thanh toán và đã xác nhận
            if (booking.PaymentStatus != "Paid" || booking.Status != "Confirmed")
            {
                _logger.LogInformation(
                    "Booking {BookingId} is not eligible for notification. PaymentStatus: {PaymentStatus}, Status: {Status}. Skipping notification.", 
                    bookingId, 
                    booking.PaymentStatus, 
                    booking.Status);
                return;
            }

            // Check xem đã có thông báo này chưa
            var alreadyExists = await _announcementRepository.ExistsAnnouncementByBookingIdAndTypeAsync(
                bookingId, 
                "AMENITY_BOOKING_SUCCESS");

            if (alreadyExists)
            {
                _logger.LogInformation("Success notification already exists for booking {BookingId}", bookingId);
                return;
            }

            var amenityName = booking.Amenity?.Name ?? "Tiện ích";
            var startDate = booking.StartDate.ToString("dd/MM/yyyy");
            var endDate = booking.EndDate.ToString("dd/MM/yyyy");

            var visibleFrom = DateTime.UtcNow.AddHours(7);
            var visibleTo = visibleFrom.Date.AddDays(1).AddSeconds(-1);

            var announcement = new Announcement
            {
                AnnouncementId = Guid.NewGuid(),
                Title = "Đăng ký tiện ích thành công",
                Content = $"Bạn vừa đăng ký thành công tiện ích \"{amenityName}\" từ ngày {startDate} đến ngày {endDate}.",
                VisibleFrom = visibleFrom,
                VisibleTo = visibleTo,
                VisibilityScope = "RESIDENT",
                Status = "ACTIVE",
                Type = "AMENITY_BOOKING_SUCCESS",
                IsPinned = false,
                BookingId = bookingId,
                CreatedBy = booking.UserId?.ToString(),
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            await _announcementRepository.CreateAnnouncementAsync(announcement);
            _logger.LogInformation("Created booking success notification for booking {BookingId}", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking success notification for booking {BookingId}", bookingId);
            throw;
        }
    }

}

