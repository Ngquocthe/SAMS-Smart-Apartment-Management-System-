using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository
{
    public interface IAnnouncementRepository
    {
        Task<Announcement?> GetAnnouncementByIdAsync(Guid announcementId);
        Task<List<Announcement>> GetAllAnnouncementsAsync(List<string>? excludeTypes = null);
        Task<List<Announcement>> GetActiveAnnouncementsAsync(DateTime currentDate);
        Task<List<Announcement>> GetAnnouncementsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<Announcement>> GetAnnouncementsByVisibilityScopeAsync(string visibilityScope);
        Task<List<Announcement>> GetAnnouncementsByStatusAsync(string status);
        Task<Announcement> CreateAnnouncementAsync(Announcement announcement);
        Task<Announcement> UpdateAnnouncementAsync(Announcement announcement);
        Task<bool> DeleteAnnouncementAsync(Guid announcementId);
        Task<int> GetTotalAnnouncementCountAsync();
        Task<List<Announcement>> GetUnreadAnnouncementsForUserAsync(Guid userId, string? visibilityScope = null, List<string>? includeTypes = null);
        Task<int> GetUnreadAnnouncementCountForUserAsync(Guid userId, string? visibilityScope = null, List<string>? includeTypes = null);
        Task<bool> MarkAnnouncementAsReadAsync(Guid announcementId, Guid userId);
        Task<bool> IsAnnouncementReadByUserAsync(Guid announcementId, Guid userId);
        Task<bool> ExistsAnnouncementByBookingIdAndTypeAsync(Guid bookingId, string announcementType);
        Task<List<Announcement>> GetUnreadAnnouncementsByUserAndTypeAsync(Guid userId, string announcementType, DateTime fromDate, DateTime toDate);
        Task<int> ExpireAnnouncementsAsync();
    }
}
