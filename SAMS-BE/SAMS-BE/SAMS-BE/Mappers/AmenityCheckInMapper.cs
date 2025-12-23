using System;
using System.Linq;
using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class AmenityCheckInMapper
{
    public static AmenityCheckInDto ToDto(this AmenityCheckIn entity)
    {
        var booking = entity.Booking;
        var amenity = booking?.Amenity;
        var apartment = booking?.Apartment;
        var residentProfile = entity.CheckedInForUser.ResidentProfile;
        var operatorProfile = entity.CheckedInByUser?.ResidentProfile;

        return new AmenityCheckInDto
        {
            CheckInId = entity.CheckInId,
            BookingId = entity.BookingId,
            AmenityId = amenity?.AmenityId ?? Guid.Empty,
            AmenityName = amenity?.Name ?? string.Empty,
            ApartmentId = apartment?.ApartmentId ?? Guid.Empty,
            ApartmentCode = apartment?.Number ?? string.Empty,
            CheckedInForUserId = entity.CheckedInForUserId,
            CheckedInForFullName = residentProfile?.FullName
                ?? $"{entity.CheckedInForUser.FirstName} {entity.CheckedInForUser.LastName}".Trim(),
            CheckedInForPhone = entity.CheckedInForUser.Phone,
            CheckedInByUserId = entity.CheckedInByUserId,
            CheckedInByFullName = operatorProfile?.FullName
                ?? (entity.CheckedInByUser is null
                    ? null
                    : $"{entity.CheckedInByUser.FirstName} {entity.CheckedInByUser.LastName}".Trim()),
            Similarity = entity.Similarity,
            IsSuccess = entity.IsSuccess,
            ResultStatus = entity.ResultStatus,
            IsManualOverride = entity.IsManualOverride,
            Message = entity.Message,
            CheckedInAt = entity.CheckedInAt,
            BookingStatus = booking?.Status ?? string.Empty,
            CapturedImageUrl = entity.CapturedImageUrl
        };
    }

    public static IEnumerable<AmenityCheckInDto> ToDto(this IEnumerable<AmenityCheckIn> entities)
        => entities.Select(ToDto);
}


