using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs.Response;
using SAMS_BE.Interfaces.IService;
using System.Security.Claims;
using SAMS_BE.Interfaces.IRepository;

namespace SAMS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepo;

        public UsersController(IUserService userService, IUserRepository userRepo)
        {
            _userService = userService;
            _userRepo = userRepo;
        }

        /// <summary>
        /// Lấy căn hộ hiện tại của user từ token (primary hoặc fallback gần nhất)
        /// GET /api/users/me/apartment
        /// </summary>
        [HttpGet("me/apartment")]
        public async Task<ActionResult<object>> GetMyApartment()
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("user_id")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId) || userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var apartment = await _userService.GetUserPrimaryApartmentAsync(userId);
            if (apartment == null)
            {
                return NotFound(new { message = "User chưa liên kết căn hộ" });
            }

            return Ok(new
            {
                apartmentId = apartment.ApartmentId,
                apartmentNumber = apartment.Number
            });
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<LoginUserDto>> GetById(Guid id)
        {
            var list = User.Claims.Select(c => new { c.Type, c.Value });

            var dto = await _userService.GetLoginUserAsync(id);
            return dto is null ? NotFound(new { message = "User not found" }) : Ok(dto);
        }

        // Simple lookup by username to support FE assigning tickets
        [HttpGet("by-username/{username}")]
        public async Task<IActionResult> GetByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return BadRequest("username is required");
            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null) return NotFound(new { message = "User not found" });
            return Ok(new { userId = user.UserId, username = user.Username });
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] string? username, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (items, total) = await _userRepo.LookupByUsernameAsync(username, page, pageSize);
            var result = items.Select(u => new { userId = u.UserId, username = u.Username, email = u.Email, phone = u.Phone, dob = u.Dob });
            return Ok(new { total, items = result });
        }

        /// <summary>
        /// Lấy thông tin user công khai (public info) để hiển thị - không cần authorization check
        /// Dùng cho việc hiển thị tên người tạo/đóng ticket, maintenance history, etc.
        /// GET /api/users/public/{id}
        /// </summary>
        [HttpGet("public/{id:guid}")]
        public async Task<IActionResult> GetPublicInfo(Guid id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Chỉ trả về thông tin công khai để hiển thị
            return Ok(new
            {
                userId = user.UserId,
                username = user.Username,
                firstName = user.FirstName,
                lastName = user.LastName,
                fullName = $"{user.FirstName} {user.LastName}".Trim(),
                avatarUrl = user.AvatarUrl
            });
        }
    }
}
