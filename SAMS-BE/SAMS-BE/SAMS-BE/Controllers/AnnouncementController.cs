using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Helpers;
using System.Security.Claims;

namespace SAMS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnnouncementController : ControllerBase
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        /// <summary>
        /// Lấy tất cả thông báo với phân trang
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<AnnouncementListResponseDto>> GetAllAnnouncements(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? excludeTypes = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;

                List<string>? excludeTypesList = null;
                if (!string.IsNullOrEmpty(excludeTypes))
                {
                    excludeTypesList = excludeTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .ToList();
                }
                // Không có excludeTypes mặc định - hiển thị tất cả thông báo

                var result = await _announcementService.GetAllAnnouncementsAsync(pageNumber, pageSize, excludeTypesList);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông báo theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AnnouncementResponseDto>> GetAnnouncementById(Guid id)
        {
            try
            {
                Guid? userId = null;
                if (User?.Identity?.IsAuthenticated == true)
                {
                    try
                    {
                        userId = UserClaimsHelper.GetUserIdOrThrow(User);
                    }
                    catch
                    {
                        // User không có userId hợp lệ, tiếp tục với userId = null
                    }
                }

                var announcement = await _announcementService.GetAnnouncementByIdAsync(id, userId);
                if (announcement == null)
                    return NotFound(new { message = "Thông báo không tìm thấy" });

                return Ok(announcement);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy các thông báo đang hoạt động (hiện tại có thể hiển thị)
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<List<AnnouncementResponseDto>>> GetActiveAnnouncements()
        {
            try
            {
                var announcements = await _announcementService.GetActiveAnnouncementsAsync();
                return Ok(announcements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông báo theo khoảng thời gian
        /// </summary>
        [HttpGet("date-range")]
        public async Task<ActionResult<List<AnnouncementResponseDto>>> GetAnnouncementsByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                    return BadRequest(new { message = "Ngày bắt đầu phải trước ngày kết thúc" });

                var announcements = await _announcementService.GetAnnouncementsByDateRangeAsync(startDate, endDate);
                return Ok(announcements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông báo theo phạm vi hiển thị (visibility scope)
        /// </summary>
        [HttpGet("scope/{scope}")]
        public async Task<ActionResult<List<AnnouncementResponseDto>>> GetAnnouncementsByVisibilityScope(string scope)
        {
            try
            {
                var announcements = await _announcementService.GetAnnouncementsByVisibilityScopeAsync(scope);
                return Ok(announcements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo thông báo mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AnnouncementResponseDto>> CreateAnnouncement([FromBody] CreateAnnouncementDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _announcementService.CreateAnnouncementAsync(request, "CurrentUser");
                return CreatedAtAction(nameof(GetAnnouncementById), new { id = result.AnnouncementId }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông báo
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<AnnouncementResponseDto>> UpdateAnnouncement(
            Guid id,
            [FromBody] UpdateAnnouncementDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != request.AnnouncementId)
                    return BadRequest(new { message = "ID không khớp" });

                var result = await _announcementService.UpdateAnnouncementAsync(request, "CurrentUser");
                if (result == null)
                    return NotFound(new { message = "Thông báo không tìm thấy" });

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa thông báo
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAnnouncement(Guid id)
        {
            try
            {
                var result = await _announcementService.DeleteAnnouncementAsync(id);
                if (!result)
                    return NotFound(new { message = "Thông báo không tìm thấy" });

                return Ok(new { message = "Xóa thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy số lượng thông báo chưa đọc của user hiện tại
        /// </summary>
        [HttpGet("unread/count")]
        public async Task<ActionResult<int>> GetUnreadAnnouncementCount([FromQuery] string? scope = null, [FromQuery] string? includeTypes = null)
        {
            try
            {
                if (User?.Identity?.IsAuthenticated != true)
                {
                    return Unauthorized(new { message = "Chưa đăng nhập" });
                }

                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                
                List<string>? includeTypesList = null;
                if (!string.IsNullOrEmpty(includeTypes))
                {
                    includeTypesList = includeTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .ToList();
                }

                var count = await _announcementService.GetUnreadAnnouncementCountForUserAsync(userId, scope, includeTypesList);
                return Ok(new { count });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách thông báo chưa đọc của user hiện tại
        /// </summary>
        [HttpGet("unread")]
        public async Task<ActionResult<List<AnnouncementResponseDto>>> GetUnreadAnnouncements([FromQuery] string? scope = null, [FromQuery] string? includeTypes = null)
        {
            try
            {
                if (User?.Identity?.IsAuthenticated != true)
                {
                    return Unauthorized(new { message = "Chưa đăng nhập" });
                }

                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                
                List<string>? includeTypesList = null;
                if (!string.IsNullOrEmpty(includeTypes))
                {
                    includeTypesList = includeTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .ToList();
                }

                var announcements = await _announcementService.GetUnreadAnnouncementsForUserAsync(userId, scope, includeTypesList);
                return Ok(announcements);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy số lượng thông báo bảo trì chưa đọc của user hiện tại (dành cho màn hình quản lý tài sản)
        /// </summary>
        [HttpGet("unread/maintenance/count")]
        public async Task<ActionResult<int>> GetMaintenanceUnreadCount([FromQuery] string? scope = null)
        {
            try
            {
                if (User?.Identity?.IsAuthenticated != true)
                {
                    return Unauthorized(new { message = "Chưa đăng nhập" });
                }

                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var includeTypesList = new List<string> { "MAINTENANCE_REMINDER", "MAINTENANCE_ASSIGNMENT" };
                
                var count = await _announcementService.GetUnreadAnnouncementCountForUserAsync(userId, scope, includeTypesList);
                return Ok(new { count });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách thông báo bảo trì chưa đọc của user hiện tại (dành cho màn hình quản lý tài sản)
        /// </summary>
        [HttpGet("unread/maintenance")]
        public async Task<ActionResult<List<AnnouncementResponseDto>>> GetMaintenanceUnreadAnnouncements([FromQuery] string? scope = null)
        {
            try
            {
                if (User?.Identity?.IsAuthenticated != true)
                {
                    return Unauthorized(new { message = "Chưa đăng nhập" });
                }

                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var includeTypesList = new List<string> { "MAINTENANCE_REMINDER", "MAINTENANCE_ASSIGNMENT" };
                
                var announcements = await _announcementService.GetUnreadAnnouncementsForUserAsync(userId, scope, includeTypesList);
                return Ok(announcements);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Đánh dấu thông báo là đã đọc
        /// </summary>
        [HttpPost("{id}/mark-as-read")]
        public async Task<ActionResult> MarkAnnouncementAsRead(Guid id)
        {
            try
            {
                if (User?.Identity?.IsAuthenticated != true)
                {
                    return Unauthorized(new { message = "Chưa đăng nhập" });
                }

                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _announcementService.MarkAnnouncementAsReadAsync(id, userId);
                
                if (!result)
                    return NotFound(new { message = "Thông báo không tìm thấy" });

                return Ok(new { message = "Đã đánh dấu đã đọc" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
