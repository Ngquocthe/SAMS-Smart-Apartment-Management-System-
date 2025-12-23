using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService
{
    public interface IAnnouncementService
    {
        Task<AnnouncementResponseDto?> GetAnnouncementByIdAsync(Guid announcementId, Guid? userId = null);
        Task<AnnouncementListResponseDto> GetAllAnnouncementsAsync(int pageNumber = 1, int pageSize = 10, List<string>? excludeTypes = null);
        Task<List<AnnouncementResponseDto>> GetActiveAnnouncementsAsync();
        Task<List<AnnouncementResponseDto>> GetAnnouncementsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<AnnouncementResponseDto>> GetAnnouncementsByVisibilityScopeAsync(string visibilityScope);
        Task<AnnouncementResponseDto> CreateAnnouncementAsync(CreateAnnouncementDto request, string? createdBy = null);
        Task<AnnouncementResponseDto?> UpdateAnnouncementAsync(UpdateAnnouncementDto request, string? updatedBy = null);
        Task<bool> DeleteAnnouncementAsync(Guid announcementId);
        Task<List<AnnouncementResponseDto>> GetUnreadAnnouncementsForUserAsync(Guid userId, string? visibilityScope = null, List<string>? includeTypes = null);
        Task<int> GetUnreadAnnouncementCountForUserAsync(Guid userId, string? visibilityScope = null, List<string>? includeTypes = null);
        Task<bool> MarkAnnouncementAsReadAsync(Guid announcementId, Guid userId);
        Task<int> ExpireAnnouncementsAsync();
    }
}
