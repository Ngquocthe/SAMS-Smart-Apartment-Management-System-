using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Request;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Mappers;
using SAMS_BE.Models;

namespace SAMS_BE.Services;

public class AmenityCheckInService : IAmenityCheckInService
{
    private readonly IAmenityCheckInRepository _checkInRepository;
    private readonly IAmenityBookingRepository _bookingRepository;
    private readonly ILogger<AmenityCheckInService> _logger;

    public AmenityCheckInService(
        IAmenityCheckInRepository checkInRepository,
        IAmenityBookingRepository bookingRepository,
        ILogger<AmenityCheckInService> logger)
    {
        _checkInRepository = checkInRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<AmenityCheckInDto> RecordCheckInAsync(
        Guid bookingId,
        Guid checkedInForUserId,
        Guid? checkedInByUserId,
        bool isSuccess,
        string resultStatus,
        float? similarity,
        string? message,
        bool isManualOverride,
        string? capturedImageUrl = null,
        string? notes = null)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId)
                      ?? throw new ArgumentException("Booking not found", nameof(bookingId));

        if (booking.UserId != checkedInForUserId)
        {
            _logger.LogWarning("Check-in booking mismatch: booking {BookingId} belongs to {BookingUserId} but request targeted {TargetUserId}",
                bookingId, booking.UserId, checkedInForUserId);
        }

        var now = DateTime.UtcNow.AddHours(7);
        var combinedMessage = CombineMessage(message, notes);

        var entity = new AmenityCheckIn
        {
            CheckInId = Guid.NewGuid(),
            BookingId = bookingId,
            CheckedInForUserId = checkedInForUserId,
            CheckedInByUserId = checkedInByUserId,
            IsSuccess = isSuccess,
            ResultStatus = resultStatus,
            Similarity = similarity,
            Message = combinedMessage,
            CapturedImageUrl = capturedImageUrl,
            IsManualOverride = isManualOverride,
            CheckedInAt = now,
            CreatedAt = now,
            CreatedBy = checkedInByUserId?.ToString()
        };

        var saved = await _checkInRepository.CreateAsync(entity);

        _logger.LogInformation("Amenity check-in recorded. Booking: {BookingId}, TargetUser: {TargetUserId}, Operator: {OperatorUserId}, Success: {Success}, Status: {Status}",
            bookingId, checkedInForUserId, checkedInByUserId, isSuccess, resultStatus);

        return saved.ToDto();
    }

    public async Task<PagedResult<AmenityCheckInDto>> GetPagedAsync(AmenityCheckInQueryDto query)
    {
        var paged = await _checkInRepository.GetPagedAsync(query);

        return new PagedResult<AmenityCheckInDto>
        {
            Items = paged.Items.ToDto(),
            TotalCount = paged.TotalCount,
            TotalItems = paged.TotalItems,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalPages = paged.TotalPages
        };
    }

    public async Task<IEnumerable<AmenityCheckInDto>> GetRecentAsync(int limit)
    {
        var result = await _checkInRepository.GetRecentAsync(limit);
        return result.ToDto();
    }

    public async Task<AmenityCheckInDto?> GetLatestByBookingIdAsync(Guid bookingId)
    {
        var entity = await _checkInRepository.GetLatestByBookingIdAsync(bookingId);
        return entity?.ToDto();
    }

    private static string? CombineMessage(string? message, string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return message;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return notes;
        }

        return $"{message} | {notes}";
    }
}


