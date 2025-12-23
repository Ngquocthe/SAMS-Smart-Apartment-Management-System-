using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface IAmenityBookingRepository
{
    Task<AmenityBooking?> GetByIdAsync(Guid bookingId);
    Task<IEnumerable<AmenityBooking>> GetAllAsync();
    Task<PagedResult<AmenityBooking>> GetPagedAsync(AmenityBookingQueryDto query);
    Task<IEnumerable<AmenityBooking>> GetByAmenityIdAsync(Guid amenityId);
    Task<IEnumerable<AmenityBooking>> GetByApartmentIdAsync(Guid apartmentId);
    Task<IEnumerable<AmenityBooking>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<AmenityBooking>> GetByStatusAsync(string status);
    Task<IEnumerable<AmenityBooking>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<AmenityBooking> CreateAsync(AmenityBooking booking);
    Task<AmenityBooking?> UpdateAsync(AmenityBooking booking);
    Task<bool> DeleteAsync(Guid bookingId);
    Task<bool> CancelAsync(Guid bookingId, string cancelReason);
    Task<bool> ConfirmAsync(Guid bookingId);
    Task<bool> CompleteAsync(Guid bookingId);
    Task<bool> UpdatePaymentStatusAsync(Guid bookingId, string paymentStatus);
    
    /// <summary>
    /// Kiểm tra xem có booking nào trùng lịch với booking mới không
    /// Trùng lịch: cùng amenityId, cùng userId, và có overlap về thời gian
    /// </summary>
    Task<IEnumerable<AmenityBooking>> GetOverlappingBookingsAsync(Guid amenityId, Guid userId, DateOnly startDate, DateOnly endDate, Guid? excludeBookingId = null);
}

