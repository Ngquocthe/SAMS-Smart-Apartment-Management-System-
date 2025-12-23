using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using System.Security.Claims;
using SAMS_BE.Helpers;
using SAMS_BE.Models;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccessCardController : ControllerBase
    {
        private readonly IAccessCardService _accessCardService;
        private readonly ICardHistoryService _cardHistoryService;
        private readonly BuildingManagementContext _context;
        
        public AccessCardController(IAccessCardService accessCardService, ICardHistoryService cardHistoryService, BuildingManagementContext context)
        {
            _accessCardService = accessCardService;
            _cardHistoryService = cardHistoryService;
            _context = context;
        }

        private async Task<string?> GetCurrentUserIdentifierAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst("sub")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("user_id")?.Value;

                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == userId);

                    if (user != null)
                    {
                        return user.Email ?? user.Username;
                    }
                }

                var email = User.FindFirst(ClaimTypes.Email)?.Value 
                    ?? User.FindFirst("email")?.Value;
                
                if (!string.IsNullOrEmpty(email))
                {
                    return email;
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("preferred_username")?.Value
                    ?? User.FindFirst("username")?.Value;

                return username;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get all access cards with details
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<AccessCardDto>>> GetAllAccessCards()
        {
            try
            {
                var accessCards = await _accessCardService.GetAccessCardsWithDetailsAsync();
                return Ok(accessCards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Get access card by ID with details
        /// </summary>
        [HttpGet("get/{id}")]
        public async Task<ActionResult<AccessCardDto>> GetAccessCardById(Guid id)
        {
            try
            {
                var accessCard = await _accessCardService.GetAccessCardWithDetailsByIdAsync(id);
                if (accessCard == null)
                {
                    return NotFound("Không tìm thấy access card");
                }
                return Ok(accessCard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Get access cards by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<AccessCardDto>>> GetAccessCardsByUserId(Guid userId)
        {
            try
            {
                var accessCards = await _accessCardService.GetAccessCardsByUserIdAsync(userId);
                return Ok(accessCards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Get access cards by apartment ID
        /// </summary>
        [HttpGet("apartment/{apartmentId}")]
        public async Task<ActionResult<List<AccessCardDto>>> GetAccessCardsByApartmentId(Guid apartmentId)
        {
            try
            {
                var accessCards = await _accessCardService.GetAccessCardsByApartmentIdAsync(apartmentId);
                return Ok(accessCards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Get access cards by status
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<ActionResult<List<AccessCardDto>>> GetAccessCardsByStatus(string status)
        {
            try
            {
                var accessCards = await _accessCardService.GetAccessCardsByStatusAsync(status);
                return Ok(accessCards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Get access cards by card type ID
        /// </summary>
        [HttpGet("card-type/{cardTypeId}")]
        public async Task<ActionResult<List<AccessCardDto>>> GetAccessCardsByCardType(Guid cardTypeId)
        {
            try
            {
                var accessCards = await _accessCardService.GetAccessCardsByCardTypeAsync(cardTypeId);
                return Ok(accessCards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new access card
        /// </summary>
        [HttpPost("createcard")]
        public async Task<ActionResult<AccessCardDto>> CreateAccessCard([FromBody] CreateAccessCardDto createAccessCardDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Dữ liệu đầu vào không hợp lệ");
                }

                if (string.IsNullOrEmpty(createAccessCardDto.CreatedBy) || createAccessCardDto.CreatedBy == "buildingmanager")
                {
                    var currentUserIdentifier = await GetCurrentUserIdentifierAsync();
                    createAccessCardDto.CreatedBy = currentUserIdentifier ?? "buildingmanager";
                }

                var createdAccessCard = await _accessCardService.CreateAccessCardAsync(createAccessCardDto);
                return CreatedAtAction(nameof(GetAccessCardById), new { id = createdAccessCard.CardId }, createdAccessCard);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Update an existing access card
        /// </summary>
        [HttpPut("updatecard/{id}")]
        public async Task<ActionResult<AccessCardDto>> UpdateAccessCard(Guid id, [FromBody] UpdateAccessCardDto updateAccessCardDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Dữ liệu đầu vào không hợp lệ");
                }

                if (string.IsNullOrEmpty(updateAccessCardDto.UpdatedBy) || updateAccessCardDto.UpdatedBy == "buildingmanager")
                {
                    var currentUserIdentifier = await GetCurrentUserIdentifierAsync();
                    updateAccessCardDto.UpdatedBy = currentUserIdentifier ?? "buildingmanager";
                }

                var existingCard = await _accessCardService.GetAccessCardWithDetailsByIdAsync(id);
                if (existingCard == null)
                {
                    return NotFound($"Không tìm thấy access card với ID: {id}");
                }

                var updatedAccessCard = await _accessCardService.UpdateAccessCardAsync(id, updateAccessCardDto);
                if (updatedAccessCard == null)
                {
                    return NotFound("Không thể cập nhật access card");
                }

                return Ok(updatedAccessCard);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Soft delete an access card
        /// </summary>
        [HttpDelete("softdelete/{id}")]
        public async Task<ActionResult> SoftDeleteAccessCard(Guid id)
        {
            try
            {
                var result = await _accessCardService.SoftDeleteAccessCardAsync(id);
                if (!result)
                {
                    return NotFound("Không tìm thấy access card để xóa");
                }

                return Ok(new { message = "Access card đã được xóa thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpGet("card-types")]
        public async Task<ActionResult<IEnumerable<CardTypeDto>>> GetCardTypes()
        {
            try
            {
                var cardTypes = await _accessCardService.GetCardTypesAsync();
                return Ok(cardTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{cardId}/capabilities")]
        public async Task<ActionResult<IEnumerable<AccessCardCapabilityDto>>> GetCardCapabilities(Guid cardId)
        {
            try
            {
                var capabilities = await _accessCardService.GetCardCapabilitiesAsync(cardId);
                return Ok(capabilities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPost("createcard-with-capabilities")]
        public async Task<ActionResult<AccessCardDto>> CreateAccessCardWithCapabilities([FromBody] CreateAccessCardDto createAccessCardDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrEmpty(createAccessCardDto.CreatedBy) || createAccessCardDto.CreatedBy == "buildingmanager")
                {
                    var currentUserIdentifier = await GetCurrentUserIdentifierAsync();
                    createAccessCardDto.CreatedBy = currentUserIdentifier ?? "buildingmanager";
                }

                var accessCard = await _accessCardService.CreateAccessCardAsync(createAccessCardDto);
                return CreatedAtAction(nameof(GetAccessCardById), new { id = accessCard.CardId }, accessCard);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPut("updatecard-with-capabilities/{id}")]
        public async Task<ActionResult<AccessCardDto>> UpdateAccessCardWithCapabilities(Guid id, [FromBody] UpdateAccessCardDto updateAccessCardDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrEmpty(updateAccessCardDto.UpdatedBy) || updateAccessCardDto.UpdatedBy == "buildingmanager")
                {
                    var currentUserIdentifier = await GetCurrentUserIdentifierAsync();
                    updateAccessCardDto.UpdatedBy = currentUserIdentifier ?? "buildingmanager";
                }

                var accessCard = await _accessCardService.UpdateAccessCardAsync(id, updateAccessCardDto);
                if (accessCard == null)
                {
                    return NotFound("Không tìm thấy access card để cập nhật");
                }

                return Ok(accessCard);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPut("update-capabilities/{cardId}")]
        public async Task<ActionResult> UpdateCardCapabilities(Guid cardId, [FromBody] List<Guid> cardTypeIds)
        {
            try
            {
                if (cardTypeIds == null || !cardTypeIds.Any())
                {
                    return BadRequest("Danh sách cardTypeIds không được rỗng");
                }

                var success = await _accessCardService.UpdateCardCapabilitiesAsync(cardId, cardTypeIds);
                if (!success)
                {
                    return NotFound("Không tìm thấy access card để cập nhật capabilities");
                }

                return Ok(new { message = "Capabilities đã được cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // ========== CARD HISTORY ENDPOINTS ==========

        /// <summary>
        /// Get card history with filtering and pagination
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<PagedResult<CardHistoryResponseDto>>> GetCardHistory([FromQuery] CardHistoryQueryDto query)
        {
            try
            {
                var result = await _cardHistoryService.GetPagedCardHistoriesAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Get card history by card ID
        /// </summary>
        [HttpGet("{cardId}/history")]
        public async Task<ActionResult<IEnumerable<CardHistoryResponseDto>>> GetCardHistoryByCardId(Guid cardId)
        {
            try
            {
                var cardHistories = await _cardHistoryService.GetCardHistoriesByCardIdAsync(cardId);
                return Ok(cardHistories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Get recent card access (last 10 entries)
        /// </summary>
        [HttpGet("{cardId}/recent-access")]
        public async Task<ActionResult<IEnumerable<CardHistoryResponseDto>>> GetRecentCardAccess(Guid cardId, [FromQuery] int limit = 10)
        {
            try
            {
                var recentAccess = await _cardHistoryService.GetRecentCardAccessAsync(cardId, limit);
                return Ok(recentAccess);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Get card access summary
        /// </summary>
        [HttpGet("access-summary")]
        public async Task<ActionResult<IEnumerable<CardAccessSummaryDto>>> GetCardAccessSummary(
            [FromQuery] Guid? cardId = null, 
            [FromQuery] Guid? userId = null, 
            [FromQuery] Guid? apartmentId = null)
        {
            try
            {
                var summary = await _cardHistoryService.GetCardAccessSummaryAsync(cardId, userId, apartmentId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Get access statistics for date range
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<Dictionary<string, int>>> GetAccessStatistics(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate)
        {
            try
            {
                var statistics = await _cardHistoryService.GetAccessStatisticsAsync(fromDate, toDate);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Get card history by field name (owner, apartment, status, etc.)
        /// </summary>
        [HttpGet("history/field/{fieldName}")]
        public async Task<ActionResult<IEnumerable<CardHistoryResponseDto>>> GetCardHistoryByFieldName(string fieldName)
        {
            try
            {
                var cardHistories = await _cardHistoryService.GetCardHistoriesByFieldNameAsync(fieldName);
                return Ok(cardHistories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Get card history by event code
        /// </summary>
        [HttpGet("history/event/{eventCode}")]
        public async Task<ActionResult<IEnumerable<CardHistoryResponseDto>>> GetCardHistoryByEventCode(string eventCode)
        {
            try
            {
                var cardHistories = await _cardHistoryService.GetCardHistoriesByEventCodeAsync(eventCode);
                return Ok(cardHistories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Create new card history entry
        /// </summary>
        [HttpPost("history")]
        public async Task<ActionResult<CardHistoryResponseDto>> CreateCardHistory([FromBody] CreateCardHistoryDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Dữ liệu đầu vào không hợp lệ");
                }

                var cardHistory = await _cardHistoryService.CreateCardHistoryAsync(dto);
                return CreatedAtAction(nameof(GetCardHistoryByCardId), new { cardId = dto.CardId }, cardHistory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
    }
}
