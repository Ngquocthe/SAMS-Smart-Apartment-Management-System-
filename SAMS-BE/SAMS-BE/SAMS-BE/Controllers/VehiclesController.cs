using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces.IService;
using System.Security.Claims;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(
        IVehicleService vehicleService,
        ILogger<VehiclesController> logger)
    {
        _vehicleService = vehicleService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy tất cả xe (admin)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MyVehicleDto>>> GetAllVehicles()
    {
        try
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync();
            return Ok(vehicles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all vehicles");
            return StatusCode(500, "Lỗi khi lấy danh sách xe");
        }
    }

    /// <summary>
    /// Lấy danh sách xe của cư dân hiện tại
    /// </summary>
    [HttpGet("my-vehicles")]
    public async Task<ActionResult<IEnumerable<MyVehicleDto>>> GetMyVehicles()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Không thể xác định người dùng");
            }

            var vehicles = await _vehicleService.GetMyVehiclesAsync(userId);
            return Ok(vehicles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user vehicles");
            return StatusCode(500, "Lỗi khi lấy danh sách xe");
        }
    }

    /// <summary>
    /// Update status xe và đóng ticket liên quan
    /// </summary>
    [HttpPut("{vehicleId}/status")]
    public async Task<ActionResult<MyVehicleDto>> UpdateVehicleStatus(Guid vehicleId, [FromBody] UpdateVehicleStatusDto dto)
    {
        try
        {
            var updatedVehicle = await _vehicleService.UpdateVehicleStatusAsync(vehicleId, dto);
            return Ok(updatedVehicle);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid vehicle update request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vehicle status");
            return StatusCode(500, "Lỗi khi cập nhật trạng thái xe");
        }
    }

    /// <summary>
    /// Tạo ticket hủy đăng ký xe
    /// </summary>
    [HttpPost("cancel")]
    public async Task<ActionResult<ResidentTicketDto>> CreateCancelVehicleTicket([FromBody] CreateCancelVehicleTicketDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Không thể xác định người dùng");
            }

            var ticket = await _vehicleService.CreateCancelVehicleTicketAsync(dto, userId);
            return Ok(ticket);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid vehicle cancellation request");
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized vehicle cancellation attempt");
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for vehicle cancellation");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cancel vehicle ticket");
            return StatusCode(500, "Lỗi khi tạo ticket hủy xe");
        }
    }

    /// <summary>
    /// Tạo phiếu đăng ký xe cho cư dân (Manager)
    /// Manager tạo ticket đăng ký xe thay cho cư dân
    /// </summary>
    /// <param name="residentId">ID của cư dân cần đăng ký xe</param>
    /// <param name="dto">Thông tin phiếu đăng ký xe (phải có ApartmentId)</param>
    /// <returns>Phiếu đăng ký xe đã tạo</returns>
    [HttpPost("register/{residentId}")]
    public async Task<ActionResult<ResidentTicketDto>> CreateVehicleRegistrationForResident(
        Guid residentId,
        [FromBody] CreateVehicleRegistrationTicketDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for vehicle registration");
            return BadRequest(ModelState);
        }

        try
        {
            // Lấy managerId từ claims
            var managerId = UserClaimsHelper.GetUserIdOrThrow(User);

            // Validate ApartmentId is required for manager registration
            if (!dto.ApartmentId.HasValue)
            {
                _logger.LogWarning("ApartmentId is required for manager vehicle registration");
                return BadRequest(new { message = "ApartmentId is required for manager registration" });
            }

            // Log request details
            _logger.LogInformation("Manager {ManagerId} creating vehicle registration ticket for Resident {ResidentId}", 
                managerId, residentId);
            _logger.LogInformation("Subject: {Subject}", dto.Subject);
            _logger.LogInformation("License Plate: {LicensePlate}", dto.VehicleInfo.LicensePlate);
            _logger.LogInformation("Vehicle Type ID: {VehicleTypeId}", dto.VehicleInfo.VehicleTypeId);
            _logger.LogInformation("Apartment ID: {ApartmentId}", dto.ApartmentId.Value);
            
            if (dto.AttachmentFileIds != null && dto.AttachmentFileIds.Any())
            {
                _logger.LogInformation("Attachment File IDs: {FileIds}", string.Join(", ", dto.AttachmentFileIds));
                _logger.LogInformation("Total attachments: {Count}", dto.AttachmentFileIds.Count);
            }
            else
            {
                _logger.LogInformation("No attachments provided");
            }

            var ticket = await _vehicleService.CreateVehicleRegistrationTicketForManagerAsync(dto, residentId, managerId);

            _logger.LogInformation("Vehicle registration ticket created successfully. Ticket ID: {TicketId}", ticket.TicketId);

            return CreatedAtAction(
                nameof(GetAllVehicles),
                new { id = ticket.TicketId },
                ticket);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for vehicle registration");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create vehicle registration ticket");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle registration ticket for resident");
            return StatusCode(500, new { message = "An error occurred while creating the ticket" });
        }
    }

    /// <summary>
    /// Lấy danh sách ticket hủy gửi xe (Manager/Admin)
    /// </summary>
    /// <param name="status">Lọc theo trạng thái (optional)</param>
    /// <param name="fromDate">Lọc từ ngày (optional)</param>
    /// <param name="toDate">Lọc đến ngày (optional)</param>
    /// <param name="page">Số trang (default: 1)</param>
    /// <param name="pageSize">Số items mỗi trang (default: 20)</param>
    [HttpGet("cancel-tickets")]
    public async Task<ActionResult<object>> GetCancelVehicleTickets(
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Getting cancel vehicle tickets. Status: {Status}, FromDate: {FromDate}, ToDate: {ToDate}, Page: {Page}, PageSize: {PageSize}",
                status ?? "All", fromDate, toDate, page, pageSize);

            var (items, total) = await _vehicleService.GetCancelVehicleTicketsAsync(
                status, fromDate, toDate, page, pageSize);

            var response = new
            {
                items = items,
                total = total,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            };

            _logger.LogInformation("Found {Total} cancel vehicle tickets", total);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cancel vehicle tickets");
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách ticket hủy xe" });
        }
    }
}
