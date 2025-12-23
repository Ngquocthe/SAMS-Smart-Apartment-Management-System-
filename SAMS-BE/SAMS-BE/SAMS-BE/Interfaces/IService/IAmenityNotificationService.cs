namespace SAMS_BE.Interfaces.IService;

public interface IAmenityNotificationService
{
    /// <summary>
    /// Tạo thông báo sau khi thanh toán thành công
    /// </summary>
    Task CreateBookingSuccessNotificationAsync(Guid bookingId);
}

