using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketController : ControllerBase
{
    private readonly ITicketService _service;

    public TicketController(ITicketService service)
    {
        _service = service;
    }

    private BadRequestObjectResult ValidationProblemWithMessages()
    {
        var errors = ModelState
            .Where(kvp => kvp.Value?.Errors?.Count > 0)
            .SelectMany(kvp => kvp.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage))
            .Where(msg => !string.IsNullOrWhiteSpace(msg))
            .ToList();

        var message = errors.Count == 0
            ? "One or more validation errors occurred."
            : string.Join(" | ", errors);

        return BadRequest(new { message, errors });
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] TicketQueryDto query)
    {
        var (items, total) = await _service.SearchAsync(query);
        return Ok(new { total, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var item = await _service.GetAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblemWithMessages();
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.TicketId }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTicketDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblemWithMessages();
        try
        {
            var updated = await _service.UpdateAsync(dto);
            return updated == null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("status")]
    public async Task<IActionResult> ChangeStatus([FromBody] ChangeTicketStatusDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var updated = await _service.ChangeStatusAsync(dto);
            return updated == null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    [HttpGet("{ticketId:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid ticketId)
    {
        var items = await _service.GetCommentsAsync(ticketId);
        return Ok(items);
    }

    [HttpPost("comments")]
    public async Task<IActionResult> AddComment([FromBody] CreateTicketCommentDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await _service.AddCommentAsync(dto);
        return Ok(created);
    }

    // Ticket Attachments APIs
    [HttpGet("{ticketId:guid}/attachments")]
    public async Task<IActionResult> GetAttachments(Guid ticketId)
    {
        var items = await _service.GetAttachmentsAsync(ticketId);
        return Ok(items);
    }

    [HttpPost("attachments")]
    public async Task<IActionResult> AddAttachment([FromBody] CreateTicketAttachmentDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await _service.AddAttachmentAsync(dto);
        return Ok(created);
    }

    [HttpPost("attachments/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)] // ~100MB để hỗ trợ upload nhiều file
    public async Task<IActionResult> UploadAttachments(IFormFileCollection files, [FromForm] string ticketId)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded");

        if (string.IsNullOrEmpty(ticketId) || !Guid.TryParse(ticketId, out var ticketGuid))
            return BadRequest("Invalid ticket ID");

        try
        {
            var results = new List<object>();
            foreach (var file in files)
            {
                // Upload file first
                var fileResult = await _service.UploadFileAsync(file, "tickets", null);

                // Create attachment record
                var attachmentDto = new CreateTicketAttachmentDto
                {
                    TicketId = ticketGuid,
                    FileId = fileResult.FileId,
                    Note = file.FileName
                };

                var attachment = await _service.AddAttachmentAsync(attachmentDto);
                results.Add(attachment);
            }

            return Ok(new { message = "Files uploaded successfully", attachments = results });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {

            return StatusCode(500, new { message = $"Upload failed: {ex.Message}" });
        }
    }

    [HttpDelete("attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid attachmentId)
    {
        var ok = await _service.DeleteAttachmentAsync(attachmentId);
        return ok ? NoContent() : NotFound();
    }

    // File APIs
    [HttpPost("files")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)] // ~100MB
    public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string? subFolder = "tickets", [FromForm] string? uploadedBy = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var created = await _service.UploadFileAsync(file, subFolder ?? "tickets", uploadedBy);
        return Ok(created);
    }

    [HttpGet("files/{fileId:guid}")]
    public async Task<IActionResult> GetFile(Guid fileId)
    {
        var item = await _service.GetFileAsync(fileId);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpDelete("files/{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(Guid fileId)
    {
        var ok = await _service.DeleteFileAsync(fileId);
        return ok ? NoContent() : NotFound();
    }

    // Ticket related data APIs
    [HttpGet("{ticketId:guid}/invoice-details")]
    public async Task<IActionResult> GetInvoiceDetails(Guid ticketId)
    {
        var items = await _service.GetInvoiceDetailsAsync(ticketId);
        return Ok(items);
    }

    [HttpGet("{ticketId:guid}/voucher-items")]
    public async Task<IActionResult> GetVoucherItems(Guid ticketId)
    {
        var items = await _service.GetVoucherItemsAsync(ticketId);
        return Ok(items);
    }
}


