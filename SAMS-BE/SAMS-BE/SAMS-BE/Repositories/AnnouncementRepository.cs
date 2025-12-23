using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using static SAMS_BE.Helpers.DateTimeHelper;

namespace SAMS_BE.Repositories
{
    public class AnnouncementRepository : IAnnouncementRepository
    {
        private readonly BuildingManagementContext _context;

        public AnnouncementRepository(BuildingManagementContext context)
        {
            _context = context;
        }

        public async Task<Announcement?> GetAnnouncementByIdAsync(Guid announcementId)
        {
            return await _context.Announcements
                .FirstOrDefaultAsync(a => a.AnnouncementId == announcementId);
        }

        public async Task<List<Announcement>> GetAllAnnouncementsAsync(List<string>? excludeTypes = null)
        {
            var allowedTypes = new List<string> { "ANNOUNCEMENT", "EVENT" };
            
            var query = _context.Announcements
                .Where(a =>  
                           (a.Type == null || allowedTypes.Contains(a.Type))); // Only ANNOUNCEMENT and EVENT types
            
            if (excludeTypes != null && excludeTypes.Any())
            {
                query = query.Where(a => a.Type == null || !excludeTypes.Contains(a.Type));
            }
            
            return await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Announcement>> GetActiveAnnouncementsAsync(DateTime currentDate)
        {
            // Active announcements: visible from <= current date <= visible to (or visible to is null)
            return await _context.Announcements
                .Where(a => a.VisibleFrom.Date <= currentDate.Date &&
                           (a.VisibleTo == null || a.VisibleTo.Value.Date >= currentDate.Date) &&
                           a.Status == "ACTIVE")
                .OrderByDescending(a => a.VisibleFrom)
                .ToListAsync();
        }

        public async Task<List<Announcement>> GetAnnouncementsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Announcements
                .Where(a => a.VisibleFrom.Date >= startDate.Date && a.VisibleFrom.Date <= endDate.Date)
                .OrderByDescending(a => a.VisibleFrom)
                .ToListAsync();
        }

        public async Task<List<Announcement>> GetAnnouncementsByVisibilityScopeAsync(string visibilityScope)
        {
            return await _context.Announcements
                .Where(a => a.VisibilityScope == visibilityScope)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Announcement>> GetAnnouncementsByStatusAsync(string status)
        {
            return await _context.Announcements
                .Where(a => a.Status == status)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Announcement> CreateAnnouncementAsync(Announcement announcement)
        {
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            return announcement;
        }

        public async Task<Announcement> UpdateAnnouncementAsync(Announcement announcement)
        {
            _context.Announcements.Update(announcement);
            await _context.SaveChangesAsync();
            return announcement;
        }

        public async Task<bool> DeleteAnnouncementAsync(Guid announcementId)
        {
            var announcement = await GetAnnouncementByIdAsync(announcementId);
            if (announcement == null)
                return false;

            // Soft delete - set status to INACTIVE instead of removing
            announcement.Status = "INACTIVE";
            announcement.UpdatedAt = VietnamNow;
            _context.Announcements.Update(announcement);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalAnnouncementCountAsync()
        {
            return await _context.Announcements.CountAsync();
        }

        public async Task<List<Announcement>> GetUnreadAnnouncementsForUserAsync(Guid userId, string? visibilityScope = null, List<string>? includeTypes = null)
        {
            var now = VietnamNow;
            var readAnnouncementIds = await _context.AnnouncementReads
                .Where(ar => ar.UserId == userId)
                .Select(ar => ar.AnnouncementId)
                .ToListAsync();

            var query = _context.Announcements
                .Where(a => a.Status == "ACTIVE" &&
                           a.VisibleFrom <= now &&
                           (a.VisibleTo == null || a.VisibleTo >= now) &&
                           !readAnnouncementIds.Contains(a.AnnouncementId));

            if (!string.IsNullOrEmpty(visibilityScope))
            {
                query = query.Where(a => a.VisibilityScope == visibilityScope);
            }

            // Filter theo loại thông báo nếu có (để chỉ lấy thông báo bảo trì ở màn hình quản lý tài sản)
            if (includeTypes != null && includeTypes.Any())
            {
                query = query.Where(a => a.Type != null && includeTypes.Contains(a.Type));
            }

            // Filter thông báo amenity theo CreatedBy (chỉ hiển thị thông báo của cư dân đó)
            // Các thông báo khác (như MAINTENANCE_REMINDER) không cần filter theo CreatedBy
            var userIdString = userId.ToString();
            query = query.Where(a => 
                (a.Type != null && (a.Type == "AMENITY_BOOKING_SUCCESS" || 
                                   a.Type == "AMENITY_EXPIRATION_REMINDER" || 
                                   a.Type == "AMENITY_EXPIRED" ||
                                   a.Type == "AMENITY_BOOKING_CONFLICT" ||
                                   a.Type == "AMENITY_MAINTENANCE_REMINDER")) 
                    ? a.CreatedBy == userIdString 
                    : true);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadAnnouncementCountForUserAsync(Guid userId, string? visibilityScope = null, List<string>? includeTypes = null)
        {
            var now = VietnamNow;
            var readAnnouncementIds = await _context.AnnouncementReads
                .Where(ar => ar.UserId == userId)
                .Select(ar => ar.AnnouncementId)
                .ToListAsync();

            var query = _context.Announcements
                .Where(a => a.Status == "ACTIVE" &&
                           a.VisibleFrom <= now &&
                           (a.VisibleTo == null || a.VisibleTo >= now) &&
                           !readAnnouncementIds.Contains(a.AnnouncementId));

            if (!string.IsNullOrEmpty(visibilityScope))
            {
                query = query.Where(a => a.VisibilityScope == visibilityScope);
            }

            // Filter theo loại thông báo nếu có (để chỉ lấy thông báo bảo trì ở màn hình quản lý tài sản)
            if (includeTypes != null && includeTypes.Any())
            {
                query = query.Where(a => a.Type != null && includeTypes.Contains(a.Type));
            }

            // Filter thông báo amenity theo CreatedBy (chỉ hiển thị thông báo của cư dân đó)
            // Các thông báo khác (như MAINTENANCE_REMINDER) không cần filter theo CreatedBy
            var userIdString = userId.ToString();
            query = query.Where(a => 
                (a.Type != null && (a.Type == "AMENITY_BOOKING_SUCCESS" || 
                                   a.Type == "AMENITY_EXPIRATION_REMINDER" || 
                                   a.Type == "AMENITY_EXPIRED" ||
                                   a.Type == "AMENITY_BOOKING_CONFLICT" ||
                                   a.Type == "AMENITY_MAINTENANCE_REMINDER")) 
                    ? a.CreatedBy == userIdString 
                    : true);

            return await query.CountAsync();
        }

        public async Task<bool> MarkAnnouncementAsReadAsync(Guid announcementId, Guid userId)
        {
            var existingRead = await _context.AnnouncementReads
                .FirstOrDefaultAsync(ar => ar.AnnouncementId == announcementId && ar.UserId == userId);

            if (existingRead != null)
            {
                return true; // Already marked as read
            }

            var announcementRead = new AnnouncementRead
            {
                AnnouncementReadId = Guid.NewGuid(),
                AnnouncementId = announcementId,
                UserId = userId,
                ReadAt = VietnamNow
            };

            _context.AnnouncementReads.Add(announcementRead);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsAnnouncementReadByUserAsync(Guid announcementId, Guid userId)
        {
            return await _context.AnnouncementReads
                .AnyAsync(ar => ar.AnnouncementId == announcementId && ar.UserId == userId);
        }

        public async Task<bool> ExistsAnnouncementByBookingIdAndTypeAsync(Guid bookingId, string announcementType)
        {
            return await _context.Announcements
                .AnyAsync(a => a.BookingId == bookingId && a.Type == announcementType);
        }

        public async Task<List<Announcement>> GetUnreadAnnouncementsByUserAndTypeAsync(Guid userId, string announcementType, DateTime fromDate, DateTime toDate)
        {
            var readAnnouncementIds = await _context.AnnouncementReads
                .Where(ar => ar.UserId == userId)
                .Select(ar => ar.AnnouncementId)
                .ToListAsync();

            var userIdString = userId.ToString();

            return await _context.Announcements
                .Where(a => a.Type == announcementType &&
                           a.CreatedBy == userIdString &&
                           a.Status == "ACTIVE" &&
                           a.VisibleFrom >= fromDate &&
                           a.VisibleFrom <= toDate &&
                           !readAnnouncementIds.Contains(a.AnnouncementId))
                .ToListAsync();
        }

        public async Task<int> ExpireAnnouncementsAsync()
        {
            var now = VietnamNow;
            int updatedCount = 0;
            
            var allowedTypes = new List<string> { "ANNOUNCEMENT", "EVENT" };
            
            // 1. SCHEDULED → ACTIVE: VisibleFrom <= now and status is SCHEDULED
            var scheduledToActive = await _context.Announcements
                .Where(a => a.Status == "SCHEDULED" && 
                           a.VisibleFrom <= now &&
                           (a.Type == null || allowedTypes.Contains(a.Type))) // Only ANNOUNCEMENT and EVENT
                .ToListAsync();

            foreach (var announcement in scheduledToActive)
            {
                announcement.Status = "ACTIVE";
                announcement.UpdatedAt = now;
                updatedCount++;
            }

            // 2. ACTIVE → EXPIRED: VisibleTo < now and status is ACTIVE
            var activeToExpired = await _context.Announcements
                .Where(a => a.Status == "ACTIVE" && 
                           a.VisibleTo != null && 
                           a.VisibleTo < now &&
                           (a.Type == null || allowedTypes.Contains(a.Type))) // Only ANNOUNCEMENT and EVENT
                .ToListAsync();

            foreach (var announcement in activeToExpired)
            {
                announcement.Status = "EXPIRED";
                announcement.UpdatedAt = now;
                updatedCount++;
            }

            // 3. Any non-INACTIVE announcement in valid time range → ACTIVE
            var shouldBeActive = await _context.Announcements
                .Where(a => a.Status != "INACTIVE" && 
                           a.Status != "ACTIVE" &&
                           a.Status != "EXPIRED" &&
                           a.VisibleFrom <= now &&
                           (a.VisibleTo == null || a.VisibleTo >= now) &&
                           (a.Type == null || allowedTypes.Contains(a.Type))) // Only ANNOUNCEMENT and EVENT
                .ToListAsync();

            foreach (var announcement in shouldBeActive)
            {
                announcement.Status = "ACTIVE";
                announcement.UpdatedAt = now;
                updatedCount++;
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return updatedCount;
        }
    }
}
