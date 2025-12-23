using Microsoft.AspNetCore.Mvc;
using SAMS_BE.Interfaces.IService;
using System.Security.Claims;

namespace SAMS_BE.Controllers
{
    [ApiController]
    [Route("api/Resident")] // Giữ tương thích với FE hiện tại
    public class ResidentController : ControllerBase
    {
        private readonly IUserService _userService;

        public ResidentController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Tương thích cũ: GET /api/Resident/apartment → trả về căn hộ hiện tại của user từ token
        /// </summary>
        [HttpGet("apartment")]
        public async Task<ActionResult<object>> GetCurrentApartment()
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
    }
}


