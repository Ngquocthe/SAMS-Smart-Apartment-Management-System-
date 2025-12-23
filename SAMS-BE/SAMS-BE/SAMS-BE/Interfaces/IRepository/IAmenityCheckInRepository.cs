using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Request;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface IAmenityCheckInRepository
{
    Task<AmenityCheckIn> CreateAsync(AmenityCheckIn entity);
    Task<PagedResult<AmenityCheckIn>> GetPagedAsync(AmenityCheckInQueryDto query);
    Task<IEnumerable<AmenityCheckIn>> GetRecentAsync(int limit);
    Task<AmenityCheckIn?> GetLatestByBookingIdAsync(Guid bookingId);
}


