using AutoMapper;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using static SAMS_BE.Helpers.DateTimeHelper;

namespace SAMS_BE.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly IAnnouncementRepository _announcementRepository;
        private readonly IMapper _mapper;

        public AnnouncementService(IAnnouncementRepository announcementRepository, IMapper mapper)
        {
            _announcementRepository = announcementRepository;
            _mapper = mapper;
        }

        public async Task<AnnouncementResponseDto?> GetAnnouncementByIdAsync(Guid announcementId, Guid? userId = null)
        {
            try
            {
                var announcement = await _announcementRepository.GetAnnouncementByIdAsync(announcementId);
                if (announcement == null)
                    return null;

                var responseDto = _mapper.Map<AnnouncementResponseDto>(announcement);
                responseDto.IsActive = IsAnnouncementActive(announcement);
                
                if (userId.HasValue)
                {
                    responseDto.IsRead = await _announcementRepository.IsAnnouncementReadByUserAsync(announcementId, userId.Value);
                }
                
                return responseDto;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting announcement: {ex.Message}");
            }
        }

        public async Task<AnnouncementListResponseDto> GetAllAnnouncementsAsync(int pageNumber = 1, int pageSize = 10, List<string>? excludeTypes = null)
        {
            try
            {
                var allAnnouncements = await _announcementRepository.GetAllAnnouncementsAsync(excludeTypes);
                var totalCount = allAnnouncements.Count;

                var paginatedAnnouncements = allAnnouncements
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var announcementDtos = paginatedAnnouncements.Select(a =>
                {
                    var dto = _mapper.Map<AnnouncementResponseDto>(a);
                    dto.IsActive = IsAnnouncementActive(a);
                    return dto;
                }).ToList();

                return new AnnouncementListResponseDto
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Announcements = announcementDtos
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting announcements: {ex.Message}");
            }
        }

        public async Task<List<AnnouncementResponseDto>> GetActiveAnnouncementsAsync()
        {
            try
            {
                // Sử dụng giờ Việt Nam để lấy thông báo đang active
                var activeAnnouncements = await _announcementRepository.GetActiveAnnouncementsAsync(VietnamNow);
                return activeAnnouncements.Select(a =>
                {
                    var dto = _mapper.Map<AnnouncementResponseDto>(a);
                    dto.IsActive = true; // They are already active if returned from this method
                    return dto;
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting active announcements: {ex.Message}");
            }
        }

        public async Task<List<AnnouncementResponseDto>> GetAnnouncementsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var announcements = await _announcementRepository.GetAnnouncementsByDateRangeAsync(startDate, endDate);
                return announcements.Select(a =>
                {
                    var dto = _mapper.Map<AnnouncementResponseDto>(a);
                    dto.IsActive = IsAnnouncementActive(a);
                    return dto;
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting announcements by date range: {ex.Message}");
            }
        }

        public async Task<List<AnnouncementResponseDto>> GetAnnouncementsByVisibilityScopeAsync(string visibilityScope)
        {
            try
            {
                var announcements = await _announcementRepository.GetAnnouncementsByVisibilityScopeAsync(visibilityScope);
                return announcements.Select(a =>
                {
                    var dto = _mapper.Map<AnnouncementResponseDto>(a);
                    dto.IsActive = IsAnnouncementActive(a);
                    return dto;
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting announcements by visibility scope: {ex.Message}");
            }
        }

        public async Task<AnnouncementResponseDto> CreateAnnouncementAsync(CreateAnnouncementDto request, string? createdBy = null)
        {
            try
            {
                // Validate dates - sử dụng giờ Việt Nam
                var now = VietnamNow;
                
                if (request.VisibleTo.HasValue && request.VisibleTo.Value < request.VisibleFrom)
                    throw new ArgumentException("VisibleTo phải bằng hoặc sau VisibleFrom");

                // Determine status based on VisibleFrom
                string status;
                if (request.VisibleFrom > now)
                {
                    status = "SCHEDULED"; // Future announcement
                }
                else
                {
                    status = "ACTIVE"; // Current or past announcement
                }

                // FE gửi datetime theo giờ Việt Nam, lưu trực tiếp vào DB
                var announcement = new Announcement
                {
                    AnnouncementId = Guid.NewGuid(),
                    Title = request.Title,
                    Content = request.Content,
                    VisibleFrom = request.VisibleFrom,
                    VisibleTo = request.VisibleTo,
                    VisibilityScope = request.VisibilityScope,
                    Status = status, // Auto-set based on VisibleFrom
                    IsPinned = request.IsPinned,
                    Type = request.Type,
                    CreatedAt = now,
                    CreatedBy = createdBy ?? "System"
                };

                var createdAnnouncement = await _announcementRepository.CreateAnnouncementAsync(announcement);
                
                var responseDto = _mapper.Map<AnnouncementResponseDto>(createdAnnouncement);
                responseDto.IsActive = IsAnnouncementActive(createdAnnouncement);
                return responseDto;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating announcement: {ex.Message}");
            }
        }

        public async Task<AnnouncementResponseDto?> UpdateAnnouncementAsync(UpdateAnnouncementDto request, string? updatedBy = null)
        {
            try
            {
                // Validate dates - chỉ validate VisibleTo > VisibleFrom
                var now = VietnamNow;
                
                if (request.VisibleTo.HasValue && request.VisibleTo.Value < request.VisibleFrom)
                    throw new ArgumentException("VisibleTo phải bằng hoặc sau VisibleFrom");

                var existingAnnouncement = await _announcementRepository.GetAnnouncementByIdAsync(request.AnnouncementId);
                if (existingAnnouncement == null)
                    return null;

                // Determine status based on VisibleFrom (only for ANNOUNCEMENT and EVENT types)
                string status;
                var allowedTypes = new List<string> { "ANNOUNCEMENT", "EVENT" };
                if (request.Type == null || allowedTypes.Contains(request.Type))
                {
                    // Auto-set status for user-created announcements
                    if (request.VisibleFrom > now)
                    {
                        status = "SCHEDULED"; // Future announcement
                    }
                    else if (request.VisibleTo.HasValue && request.VisibleTo.Value < now)
                    {
                        status = "EXPIRED"; // Past announcement
                    }
                    else
                    {
                        status = "ACTIVE"; // Current announcement
                    }
                }
                else
                {
                    // Keep existing status for system announcements
                    status = existingAnnouncement.Status;
                }

                // FE gửi datetime theo giờ Việt Nam, lưu trực tiếp vào DB
                existingAnnouncement.Title = request.Title;
                existingAnnouncement.Content = request.Content;
                existingAnnouncement.VisibleFrom = request.VisibleFrom;
                existingAnnouncement.VisibleTo = request.VisibleTo;
                existingAnnouncement.VisibilityScope = request.VisibilityScope;
                existingAnnouncement.Status = status; // Auto-set based on VisibleFrom
                existingAnnouncement.IsPinned = request.IsPinned;
                existingAnnouncement.Type = request.Type;
                existingAnnouncement.UpdatedAt = now;
                existingAnnouncement.UpdatedBy = updatedBy ?? "System";

                var updatedAnnouncement = await _announcementRepository.UpdateAnnouncementAsync(existingAnnouncement);
                
                var responseDto = _mapper.Map<AnnouncementResponseDto>(updatedAnnouncement);
                responseDto.IsActive = IsAnnouncementActive(updatedAnnouncement);
                return responseDto;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating announcement: {ex.Message}");
            }
        }

        public async Task<bool> DeleteAnnouncementAsync(Guid announcementId)
        {
            try
            {
                return await _announcementRepository.DeleteAnnouncementAsync(announcementId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting announcement: {ex.Message}");
            }
        }

        public async Task<List<AnnouncementResponseDto>> GetUnreadAnnouncementsForUserAsync(Guid userId, string? visibilityScope = null, List<string>? includeTypes = null)
        {
            try
            {
                var unreadAnnouncements = await _announcementRepository.GetUnreadAnnouncementsForUserAsync(userId, visibilityScope, includeTypes);
                return unreadAnnouncements.Select(a =>
                {
                    var dto = _mapper.Map<AnnouncementResponseDto>(a);
                    dto.IsActive = IsAnnouncementActive(a);
                    dto.IsRead = false; // These are unread by definition
                    return dto;
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting unread announcements: {ex.Message}");
            }
        }

        public async Task<int> GetUnreadAnnouncementCountForUserAsync(Guid userId, string? visibilityScope = null, List<string>? includeTypes = null)
        {
            try
            {
                return await _announcementRepository.GetUnreadAnnouncementCountForUserAsync(userId, visibilityScope, includeTypes);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting unread announcement count: {ex.Message}");
            }
        }

        public async Task<bool> MarkAnnouncementAsReadAsync(Guid announcementId, Guid userId)
        {
            try
            {
                return await _announcementRepository.MarkAnnouncementAsReadAsync(announcementId, userId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error marking announcement as read: {ex.Message}");
            }
        }

        private bool IsAnnouncementActive(Announcement announcement)
        {
            // Sử dụng giờ Việt Nam để kiểm tra thông báo có đang active không
            var vietnamToday = VietnamNow.Date;
            return announcement.Status == "ACTIVE" &&
                   announcement.VisibleFrom.Date <= vietnamToday &&
                   (announcement.VisibleTo == null || announcement.VisibleTo.Value.Date >= vietnamToday);
        }

        public async Task<int> ExpireAnnouncementsAsync()
        {
            try
            {
                return await _announcementRepository.ExpireAnnouncementsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error expiring announcements: {ex.Message}");
            }
        }
    }
}
