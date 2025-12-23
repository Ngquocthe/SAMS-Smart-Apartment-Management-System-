using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Response;
using SAMS_BE.DTOs.Request;
using System;

namespace SAMS_BE.Interfaces.IService;

public interface IAmenityBookingService
{
    Task<AmenityBookingDto?> GetByIdAsync(Guid bookingId);
    Task<IEnumerable<AmenityBookingDto>> GetAllAsync();
    Task<PagedResult<AmenityBookingDto>> GetPagedAsync(AmenityBookingQueryDto query);
    Task<IEnumerable<AmenityBookingDto>> GetByAmenityIdAsync(Guid amenityId);
    Task<IEnumerable<AmenityBookingDto>> GetByApartmentIdAsync(Guid apartmentId);
    Task<IEnumerable<AmenityBookingDto>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<AmenityBookingDto>> GetMyBookingsAsync(Guid userId);
    Task<AmenityBookingDto> CreateBookingAsync(CreateAmenityBookingDto dto, Guid userId, Guid apartmentId);
    Task<AmenityBookingDto?> UpdateBookingAsync(Guid bookingId, UpdateAmenityBookingDto dto, Guid userId);
    Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, string? cancelReason = null, bool isAdminOrReceptionist = false);
    Task<bool> ConfirmBookingAsync(Guid bookingId, Guid adminUserId);
    Task<bool> CompleteBookingAsync(Guid bookingId, Guid adminUserId);
    Task<bool> UpdatePaymentStatusAsync(Guid bookingId, string paymentStatus, Guid adminUserId);
    Task<AvailabilityCheckResponse> CheckAvailabilityAsync(Guid amenityId);
    Task<PriceCalculationResponse> CalculatePriceAsync(CalculatePriceRequest request);
    Task<IEnumerable<AmenityBookingDto>> GetActiveBookingsByUserAsync(Guid userId, Guid? amenityId = null, DateTime? referenceTime = null);
    Task<PagedResult<RegisteredResidentDto>> GetRegisteredResidentsAsync(AmenityBookingQueryDto query);

    // Background job: Tự động cập nhật trạng thái từ Confirmed → Completed khi EndDate đã qua
    Task<int> UpdateExpiredBookingsAsync();
}

