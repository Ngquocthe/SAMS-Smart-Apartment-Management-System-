using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories;

public class AmenityBookingRepository : IAmenityBookingRepository
{
    private readonly BuildingManagementContext _context;

    public AmenityBookingRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    public async Task<AmenityBooking?> GetByIdAsync(Guid bookingId)
    {
        return await _context.AmenityBookings
            .Include(b => b.Amenity)
            .Include(b => b.Package)
            .Include(b => b.Apartment)
            .Include(b => b.User)
                .ThenInclude(u => u.ResidentProfile)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);
    }

    public async Task<IEnumerable<AmenityBooking>> GetAllAsync()
    {
        return await _context.AmenityBookings
            .Include(b => b.Amenity)
            .Include(b => b.Package)
            .Include(b => b.Apartment)
            .Include(b => b.User)
                .ThenInclude(u => u.ResidentProfile)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<PagedResult<AmenityBooking>> GetPagedAsync(AmenityBookingQueryDto query)
    {
        var queryable = _context.AmenityBookings
            .Include(b => b.Amenity)
            .Include(b => b.Package)
            .Include(b => b.Apartment)
            .Include(b => b.User)
                .ThenInclude(u => u.ResidentProfile)
            .AsQueryable();

        // Apply filters
        if (query.AmenityId.HasValue)
        {
            queryable = queryable.Where(b => b.AmenityId == query.AmenityId.Value);
        }

        if (query.ApartmentId.HasValue)
        {
            queryable = queryable.Where(b => b.ApartmentId == query.ApartmentId.Value);
        }

        if (query.UserId.HasValue)
        {
            queryable = queryable.Where(b => b.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            queryable = queryable.Where(b => b.Status == query.Status);
        }

        if (!string.IsNullOrEmpty(query.PaymentStatus))
        {
            queryable = queryable.Where(b => b.PaymentStatus == query.PaymentStatus);
        }

        if (query.FromDate.HasValue)
        {
            var fromDateOnly = DateOnly.FromDateTime(query.FromDate.Value);
            queryable = queryable.Where(b => b.StartDate >= fromDateOnly);
        }

        if (query.ToDate.HasValue)
        {
            var toDateOnly = DateOnly.FromDateTime(query.ToDate.Value);
            queryable = queryable.Where(b => b.EndDate <= toDateOnly);
        }

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Apply sorting
        queryable = query.SortBy?.ToLower() switch
        {
            "startdate" => query.SortOrder?.ToLower() == "asc" 
                ? queryable.OrderBy(b => b.StartDate) 
                : queryable.OrderByDescending(b => b.StartDate),
            "enddate" => query.SortOrder?.ToLower() == "asc" 
                ? queryable.OrderBy(b => b.EndDate) 
                : queryable.OrderByDescending(b => b.EndDate),
            "createdat" => query.SortOrder?.ToLower() == "asc" 
                ? queryable.OrderBy(b => b.CreatedAt) 
                : queryable.OrderByDescending(b => b.CreatedAt),
            "price" => query.SortOrder?.ToLower() == "asc" 
                ? queryable.OrderBy(b => b.TotalPrice) 
                : queryable.OrderByDescending(b => b.TotalPrice),
            _ => queryable.OrderByDescending(b => b.StartDate)
        };

        // Apply pagination
        var items = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<AmenityBooking>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<IEnumerable<AmenityBooking>> GetByAmenityIdAsync(Guid amenityId)
    {
        return await _context.AmenityBookings
            .Include(b => b.Package)
            .Include(b => b.Apartment)
            .Include(b => b.User)
                .ThenInclude(u => u.ResidentProfile)
            .Where(b => b.AmenityId == amenityId)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<AmenityBooking>> GetByApartmentIdAsync(Guid apartmentId)
    {
        return await _context.AmenityBookings
            .Include(b => b.Amenity)
            .Include(b => b.Package)
            .Include(b => b.User)
                .ThenInclude(u => u.ResidentProfile)
            .Where(b => b.ApartmentId == apartmentId)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<AmenityBooking>> GetByUserIdAsync(Guid userId)
    {
        return await _context.AmenityBookings
            .Include(b => b.Amenity)
            .Include(b => b.Package)
            .Include(b => b.Apartment)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<AmenityBooking>> GetByStatusAsync(string status)
    {
        return await _context.AmenityBookings
            .Include(b => b.Amenity)
            .Include(b => b.Package)
            .Include(b => b.Apartment)
            .Include(b => b.User)
                .ThenInclude(u => u.ResidentProfile)
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<AmenityBooking>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var fromDateOnly = DateOnly.FromDateTime(fromDate);
        var toDateOnly = DateOnly.FromDateTime(toDate);
        
        return await _context.AmenityBookings
            .Include(b => b.Amenity)
            .Include(b => b.Package)
            .Include(b => b.Apartment)
            .Include(b => b.User)
                .ThenInclude(u => u.ResidentProfile)
            .Where(b => b.StartDate >= fromDateOnly && b.EndDate <= toDateOnly)
            .OrderBy(b => b.StartDate)
            .ToListAsync();
    }

    public async Task<AmenityBooking> CreateAsync(AmenityBooking booking)
    {
        _context.AmenityBookings.Add(booking);
        await _context.SaveChangesAsync();
        
        // Reload with includes
        return (await GetByIdAsync(booking.BookingId))!;
    }

    public async Task<AmenityBooking?> UpdateAsync(AmenityBooking booking)
    {
        var existing = await _context.AmenityBookings.FindAsync(booking.BookingId);
        if (existing == null)
        {
            return null;
        }

        existing.PackageId = booking.PackageId;
        existing.StartDate = booking.StartDate;
        existing.EndDate = booking.EndDate;
        existing.Price = booking.Price;
        existing.TotalPrice = booking.TotalPrice;
        existing.Status = booking.Status;
        existing.Notes = booking.Notes;
        existing.PaymentStatus = booking.PaymentStatus;
        // dùng giờ Việt Nam (UTC+7)
        existing.UpdatedAt = DateTime.UtcNow.AddHours(7);
        existing.UpdatedBy = booking.UpdatedBy;

        await _context.SaveChangesAsync();
        
        // Reload with includes
        return await GetByIdAsync(existing.BookingId);
    }

    public async Task<bool> DeleteAsync(Guid bookingId)
    {
        var booking = await _context.AmenityBookings.FindAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        _context.AmenityBookings.Remove(booking);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelAsync(Guid bookingId, string cancelReason)
    {
        var booking = await _context.AmenityBookings.FindAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        booking.Status = "Cancelled";
        // Note: CancellationReason stored in Notes field
        if (!string.IsNullOrEmpty(cancelReason))
        {
            booking.Notes = $"Cancelled: {cancelReason}";
        }
        booking.UpdatedAt = DateTime.UtcNow.AddHours(7);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ConfirmAsync(Guid bookingId)
    {
        var booking = await _context.AmenityBookings.FindAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        booking.Status = "Confirmed";
        booking.UpdatedAt = DateTime.UtcNow.AddHours(7);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteAsync(Guid bookingId)
    {
        var booking = await _context.AmenityBookings.FindAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        booking.Status = "Completed";
        booking.UpdatedAt = DateTime.UtcNow.AddHours(7);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdatePaymentStatusAsync(Guid bookingId, string paymentStatus)
    {
        var booking = await _context.AmenityBookings.FindAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        booking.PaymentStatus = paymentStatus;
        booking.UpdatedAt = DateTime.UtcNow.AddHours(7);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AmenityBooking>> GetOverlappingBookingsAsync(Guid amenityId, Guid userId, DateOnly startDate, DateOnly endDate, Guid? excludeBookingId = null)
    {
        // Chỉ check các booking đang active (Pending, Confirmed, Completed)
        // Không check Cancelled vì đã bị hủy
        var activeStatuses = new[] { "Pending", "Confirmed", "Completed" };
        
        var query = _context.AmenityBookings
            .Include(b => b.Amenity)
            .Include(b => b.Package)
            .Where(b => b.AmenityId == amenityId 
                     && b.UserId == userId
                     && activeStatuses.Contains(b.Status) // Chỉ check các booking đang active
                     && (
                         // Công thức chuẩn để check overlap: (start1 <= end2) && (end1 >= start2)
                         // Tức là: booking hiện có bắt đầu trước khi booking mới kết thúc
                         // VÀ booking hiện có kết thúc sau khi booking mới bắt đầu
                         (b.StartDate <= endDate && b.EndDate >= startDate)
                     ));

        if (excludeBookingId.HasValue)
        {
            query = query.Where(b => b.BookingId != excludeBookingId.Value);
        }

        return await query.ToListAsync();
    }
}

