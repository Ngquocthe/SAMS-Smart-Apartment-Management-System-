using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Request;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories;

public class AmenityCheckInRepository : IAmenityCheckInRepository
{
    private readonly BuildingManagementContext _context;

    public AmenityCheckInRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    private IQueryable<AmenityCheckIn> BuildQuery()
    {
        return _context.AmenityCheckIns
            .Include(c => c.Booking)
                .ThenInclude(b => b.Amenity)
            .Include(c => c.Booking)
                .ThenInclude(b => b.Apartment)
            .Include(c => c.CheckedInForUser)
                .ThenInclude(u => u.ResidentProfile)
            .Include(c => c.CheckedInByUser)
                .ThenInclude(u => u!.ResidentProfile)
            .AsNoTracking();
    }

    public async Task<AmenityCheckIn> CreateAsync(AmenityCheckIn entity)
    {
        _context.AmenityCheckIns.Add(entity);
        await _context.SaveChangesAsync();

        return await _context.AmenityCheckIns
            .Include(c => c.Booking)
                .ThenInclude(b => b.Amenity)
            .Include(c => c.Booking)
                .ThenInclude(b => b.Apartment)
            .Include(c => c.CheckedInForUser)
                .ThenInclude(u => u.ResidentProfile)
            .Include(c => c.CheckedInByUser)
                .ThenInclude(u => u!.ResidentProfile)
            .FirstAsync(c => c.CheckInId == entity.CheckInId);
    }

    public async Task<PagedResult<AmenityCheckIn>> GetPagedAsync(AmenityCheckInQueryDto query)
    {
        var queryable = BuildQuery();

        if (query.BookingId.HasValue)
        {
            queryable = queryable.Where(c => c.BookingId == query.BookingId.Value);
        }

        if (query.AmenityId.HasValue)
        {
            queryable = queryable.Where(c => c.Booking.AmenityId == query.AmenityId.Value);
        }

        if (query.CheckedInForUserId.HasValue)
        {
            queryable = queryable.Where(c => c.CheckedInForUserId == query.CheckedInForUserId.Value);
        }

        if (query.CheckedInByUserId.HasValue)
        {
            queryable = queryable.Where(c => c.CheckedInByUserId == query.CheckedInByUserId.Value);
        }

        if (query.IsSuccess.HasValue)
        {
            queryable = queryable.Where(c => c.IsSuccess == query.IsSuccess.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ResultStatus))
        {
            queryable = queryable.Where(c => c.ResultStatus == query.ResultStatus);
        }

        if (query.FromDate.HasValue)
        {
            var from = query.FromDate.Value;
            queryable = queryable.Where(c => c.CheckedInAt >= from);
        }

        if (query.ToDate.HasValue)
        {
            var to = query.ToDate.Value;
            queryable = queryable.Where(c => c.CheckedInAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            queryable = queryable.Where(c =>
                (c.CheckedInForUser.FirstName + " " + c.CheckedInForUser.LastName).ToLower().Contains(keyword)
                || (c.CheckedInByUser != null && (c.CheckedInByUser.FirstName + " " + c.CheckedInByUser.LastName).ToLower().Contains(keyword))
                || (c.CheckedInForUser.ResidentProfile != null && c.CheckedInForUser.ResidentProfile.FullName.ToLower().Contains(keyword))
                || (c.CheckedInByUser != null && c.CheckedInByUser.ResidentProfile != null && c.CheckedInByUser.ResidentProfile.FullName.ToLower().Contains(keyword))
                || c.Booking.Amenity.Name.ToLower().Contains(keyword)
                || c.Booking.Apartment.Number.ToLower().Contains(keyword));
        }

        var totalCount = await queryable.CountAsync();

        var items = await queryable
            .OrderByDescending(c => c.CheckedInAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<AmenityCheckIn>
        {
            Items = items,
            TotalCount = totalCount,
            TotalItems = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<IEnumerable<AmenityCheckIn>> GetRecentAsync(int limit)
    {
        return await BuildQuery()
            .OrderByDescending(c => c.CheckedInAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<AmenityCheckIn?> GetLatestByBookingIdAsync(Guid bookingId)
    {
        return await BuildQuery()
            .Where(c => c.BookingId == bookingId)
            .OrderByDescending(c => c.CheckedInAt)
            .FirstOrDefaultAsync();
    }
}


