using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Request;

namespace SAMS_BE.Interfaces.IService;

public interface IAmenityCheckInService
{
    Task<AmenityCheckInDto> RecordCheckInAsync(
        Guid bookingId,
        Guid checkedInForUserId,
        Guid? checkedInByUserId,
        bool isSuccess,
        string resultStatus,
        float? similarity,
        string? message,
        bool isManualOverride,
        string? capturedImageUrl = null,
        string? notes = null);

    Task<PagedResult<AmenityCheckInDto>> GetPagedAsync(AmenityCheckInQueryDto query);
    Task<IEnumerable<AmenityCheckInDto>> GetRecentAsync(int limit);
    Task<AmenityCheckInDto?> GetLatestByBookingIdAsync(Guid bookingId);
}


