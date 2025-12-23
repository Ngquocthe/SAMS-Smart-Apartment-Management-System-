using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces.IService;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/resident-tickets")]
[Authorize] // Yêu cầu authentication
public class ResidentTicketController : ControllerBase
{
    private readonly IResidentTicketService _service;
    private readonly ILogger<ResidentTicketController> _logger;

    public ResidentTicketController(
        IResidentTicketService service,
        ILogger<ResidentTicketController> logger)
    {
        _service = service;
        _logger = logger;
    }


    [HttpPost("maintenance")]
    public async Task<IActionResult> CreateMaintenanceTicket([FromBody] CreateMaintenanceTicketDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Lấy userId từ claims
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var ticket = await _service.CreateMaintenanceTicketAsync(dto, userId);

            return CreatedAtAction(
                nameof(GetTicketById),
                new { id = ticket.TicketId },
                ticket);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create maintenance ticket");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance ticket");
            return StatusCode(500, new { message = "An error occurred while creating the ticket" });
        }
    }

    /// <summary>
    /// Tạo phiếu khiếu nại (Complaint ticket)
    /// Category: Complaint, Scope: BUILDING
    /// </summary>
    /// <param name="dto">Thông tin phiếu khiếu nại</param>
    /// <returns>Phiếu khiếu nại đã tạo</returns>
    [HttpPost("complaint")]
    public async Task<IActionResult> CreateComplaintTicket([FromBody] CreateComplaintTicketDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Lấy userId từ claims
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var ticket = await _service.CreateComplaintTicketAsync(dto, userId);

            return CreatedAtAction(
                nameof(GetTicketById),
                new { id = ticket.TicketId },
                ticket);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create complaint ticket");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating complaint ticket");
            return StatusCode(500, new { message = "An error occurred while creating the ticket" });
        }
    }

    /// <summary>
    /// Tạo phiếu đăng ký xe (Vehicle Registration ticket)
    /// Category: VehicleRegistration, Scope: APARTMENT
    /// </summary>
    /// <param name="dto">Thông tin phiếu đăng ký xe</param>
    /// <returns>Phiếu đăng ký xe đã tạo</returns>
    [HttpPost("vehicle-registration")]
    public async Task<IActionResult> CreateVehicleRegistrationTicket([FromBody] CreateVehicleRegistrationTicketDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for vehicle registration");
            return BadRequest(ModelState);
        }

        try
        {
            // Lấy userId từ claims
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            // Log request details
            _logger.LogInformation("Creating vehicle registration ticket for user {UserId}", userId);
            _logger.LogInformation("Subject: {Subject}", dto.Subject);
            _logger.LogInformation("License Plate: {LicensePlate}", dto.VehicleInfo.LicensePlate);
            _logger.LogInformation("Vehicle Type ID: {VehicleTypeId}", dto.VehicleInfo.VehicleTypeId);
            _logger.LogInformation("Apartment ID: {ApartmentId}", dto.ApartmentId?.ToString() ?? "null (auto-detect)");
            
            if (dto.AttachmentFileIds != null && dto.AttachmentFileIds.Any())
            {
                _logger.LogInformation("Attachment File IDs: {FileIds}", string.Join(", ", dto.AttachmentFileIds));
                _logger.LogInformation("Total attachments: {Count}", dto.AttachmentFileIds.Count);
            }
            else
            {
                _logger.LogInformation("No attachments provided");
            }

            var ticket = await _service.CreateVehicleRegistrationTicketAsync(dto, userId);

            _logger.LogInformation("Vehicle registration ticket created successfully. Ticket ID: {TicketId}", ticket.TicketId);

            return CreatedAtAction(
                nameof(GetTicketById),
                new { id = ticket.TicketId },
                ticket);
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
            _logger.LogError(ex, "Error creating vehicle registration ticket");
            return StatusCode(500, new { message = "An error occurred while creating the ticket" });
        }
    }

    /// <summary>
    /// Lấy danh sách tickets của resident hiện tại
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <returns>Danh sách tickets</returns>
    [HttpGet("my-tickets")]
    public async Task<IActionResult> GetMyTickets([FromQuery] ResidentTicketQueryDto query)
    {
        try
        {
            // Lấy userId từ claims
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var (items, total) = await _service.GetMyTicketsAsync(query, userId);

            return Ok(new { total, items });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tickets");
            return StatusCode(500, new { message = "An error occurred while retrieving tickets" });
        }
    }

    /// <summary>
    /// Lấy chi tiết ticket theo ID
    /// </summary>
    /// <param name="id">Ticket ID</param>
    /// <returns>Chi tiết ticket</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicketById(Guid id)
    {
        try
        {
            // Lấy userId từ claims
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var ticket = await _service.GetTicketByIdAsync(id, userId);

            if (ticket == null)
            {
                return NotFound(new { message = "Ticket not found" });
            }

            return Ok(ticket);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to ticket {TicketId}", id);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket {TicketId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the ticket" });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetMyStatistics()
    {
        try
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var stats = await _service.GetMyTicketStatisticsAsync(userId);
            return Ok(stats);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resident ticket statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving ticket statistics" });
        }
    }

    /// <summary>
    /// Lấy hóa đơn liên quan đến ticket
    /// </summary>
    [HttpGet("{ticketId:guid}/invoices")]
    public async Task<IActionResult> GetTicketInvoices(Guid ticketId)
    {
        try
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var invoices = await _service.GetInvoicesForTicketAsync(ticketId, userId);
            return Ok(invoices);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid ticket");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket invoices");
            return StatusCode(500, new { message = "An error occurred while retrieving ticket invoices" });
        }
    }

    /// <summary>
    /// Thêm comment vào ticket
    /// </summary>
    /// <param name="dto">Comment data</param>
    /// <returns>Comment đã tạo</returns>
    [HttpPost("comments")]
    public async Task<IActionResult> AddComment([FromBody] CreateResidentTicketCommentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Lấy userId từ claims
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var comment = await _service.AddCommentAsync(dto, userId);

            return Ok(comment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
            return StatusCode(500, new { message = "An error occurred while adding the comment" });
        }
    }

    /// <summary>
    /// Lấy danh sách comments của ticket
    /// </summary>
    /// <param name="ticketId">Ticket ID</param>
    /// <returns>Danh sách comments</returns>
    [HttpGet("{ticketId:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid ticketId)
    {
        try
        {
            // Lấy userId từ claims
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var comments = await _service.GetCommentsAsync(ticketId, userId);

            return Ok(comments);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments");
            return StatusCode(500, new { message = "An error occurred while retrieving comments" });
        }
    }

    /// <summary>
    /// Upload file cho ticket
    /// </summary>
    /// <param name="file">File to upload</param>
    /// <returns>File info</returns>
    [HttpPost("files/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)] // ~100MB
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        try
        {
            // Lấy userId từ claims
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var fileDto = await _service.UploadFileAsync(file, userId);

            return Ok(fileDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid file");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { message = "An error occurred while uploading the file" });
        }
    }

    /// <summary>
    /// Thêm attachment vào ticket
    /// </summary>
    /// <param name="ticketId">Ticket ID</param>
    /// <param name="fileId">File ID</param>
    /// <param name="note">Note (optional)</param>
    /// <returns>Attachment đã tạo</returns>
    [HttpPost("{ticketId:guid}/attachments")]
    public async Task<IActionResult> AddAttachment(
        Guid ticketId,
        [FromBody] AddAttachmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Lấy userId từ claims
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var attachment = await _service.AddAttachmentAsync(
                ticketId,
                request.FileId,
                request.Note,
                userId);

            return Ok(attachment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding attachment");
            return StatusCode(500, new { message = "An error occurred while adding the attachment" });
        }
    }

    /// <summary>
    /// Lấy danh sách attachments của ticket
    /// </summary>
    /// <param name="ticketId">Ticket ID</param>
    /// <returns>Danh sách attachments</returns>
    [HttpGet("{ticketId:guid}/attachments")]
    public async Task<IActionResult> GetAttachments(Guid ticketId)
    {
        try
        {
            // Lấy userId từ claims
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var attachments = await _service.GetAttachmentsAsync(ticketId, userId);

            return Ok(attachments);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachments");
            return StatusCode(500, new { message = "An error occurred while retrieving attachments" });
        }
    }

    /// <summary>
    /// Xóa attachment (chỉ người upload mới được xóa)
    /// </summary>
    /// <param name="attachmentId">Attachment ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid attachmentId)
    {
        try
        {
            // Lấy userId từ claims
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);

            var success = await _service.DeleteAttachmentAsync(attachmentId, userId);

            if (!success)
            {
                return NotFound(new { message = "Attachment not found" });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment");
            return StatusCode(500, new { message = "An error occurred while deleting the attachment" });
        }
    }
}

/// <summary>
/// Request model cho việc thêm attachment
/// </summary>
public class AddAttachmentRequest
{
    [Required]
    public Guid FileId { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
