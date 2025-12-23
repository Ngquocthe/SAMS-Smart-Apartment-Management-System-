using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Enums;
using SAMS_BE.Constants;
using System.Security.Claims;

namespace SAMS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _service;

        public DocumentsController(IDocumentService service)
        {
            _service = service;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("user_id")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] DocumentQueryDto query)
        {
            var (items, total) = await _service.SearchAsync(query);
            return Ok(new { total, items });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var doc = await _service.GetAsync(id);
            return doc == null ? NotFound() : Ok(doc);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(104_857_600)] // ~100MB
        public async Task<IActionResult> Create([FromForm] CreateDocumentDto dto, IFormFile file, [FromForm] string? note)
        {
            if (file == null || file.Length == 0) return BadRequest("Vui lòng đính kèm file cho phiên bản đầu tiên.");
            try
            {
                var actorId = GetCurrentUserId();
                var doc = await _service.CreateWithFirstVersionAsync(dto, file, note, actorId);
                return CreatedAtAction(nameof(Get), new { id = doc.DocumentId }, doc);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/versions")]
        [RequestSizeLimit(104_857_600)] // ~100MB
        public async Task<IActionResult> UploadVersion(Guid id, IFormFile file, [FromForm] string? note, [FromForm] string? createdBy)
        {
            if (file == null || file.Length == 0) return BadRequest(new { message = "File rỗng." });
            try
            {
                var actorId = GetCurrentUserId();
                var ver = await _service.AddVersionAsync(id, file, note, createdBy, actorId);
                return ver == null ? NotFound() : Ok(ver);
            }
            catch (ArgumentException ex)
            {
                // Lỗi validate file (loại file, kích thước, extension nguy hiểm, ...)
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Lỗi nghiệp vụ (không cho upload khi đang chờ duyệt / đã ngừng hiển thị, ...)
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("files/{fileId:guid}")]
        public async Task<IActionResult> Download(Guid fileId)
        {
            var result = await _service.DownloadAsync(fileId);
            if (result == null) return NotFound();
            var (stream, mime, name) = result.Value;
            return File(stream, mime, name);
        }

        // Inline view for browsers (force inline Content-Disposition)
        [HttpGet("files/{fileId:guid}/view")]
        public async Task<IActionResult> ViewInline(Guid fileId)
        {
            var result = await _service.DownloadAsync(fileId);
            if (result == null) return NotFound();
            var (stream, mime, name) = result.Value;
            // Set Content-Disposition inline; filename for better UX
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{name}\"";
            return File(stream, mime);
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetAllLatest([FromQuery] string? status)
        {
            var items = await _service.GetAllWithLatestVersionAsync(status);
            return Ok(items);
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeDocumentStatusDto dto)
        {
            try
            {
                var ok = await _service.ChangeStatusAsync(id, dto.Status, dto.ActorId, dto.Detail);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}/versions/{versionNo:int}")]
        public async Task<IActionResult> GetVersion(Guid id, int versionNo)
        {
            var ver = await _service.GetVersionAsync(id, versionNo);
            return ver == null ? NotFound() : Ok(ver);
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var actorId = GetCurrentUserId();
                var ok = await _service.UpdateMetadataAsync(id, dto, actorId);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete(Guid id, [FromQuery] Guid? actorId, [FromQuery] string? reason)
        {
            var effectiveActorId = actorId ?? GetCurrentUserId();
            var ok = await _service.SoftDeleteAsync(id, effectiveActorId, reason);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> Restore(Guid id, [FromBody] RequestRestoreDto? dto)
        {
            var actorId = GetCurrentUserId();
            var ok = await _service.RestoreAsync(id, actorId, dto?.Reason);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("{id:guid}/logs")]
        public async Task<IActionResult> GetLogs(Guid id)
        {
            var logs = await _service.GetLogsAsync(id);
            return Ok(logs);
        }

        [HttpGet("resident")]
        public async Task<IActionResult> GetResidentDocuments([FromQuery] DocumentQueryDto query)
        {
            var docs = await _service.GetResidentDocumentsAsync(query);
            return Ok(docs);
        }

        [HttpGet("receptionist")]
        public async Task<IActionResult> GetReceptionistDocuments([FromQuery] DocumentQueryDto query)
        {
            var docs = await _service.GetReceptionistDocumentsAsync(query);
            return Ok(docs);
        }

        [HttpGet("accountant")]
        public async Task<IActionResult> GetAccountingDocuments([FromQuery] DocumentQueryDto query)
        {
            var docs = await _service.GetAccountingDocumentsAsync(query);
            return Ok(docs);
        }

        [HttpGet("{id:guid}/versions")]
        public async Task<IActionResult> GetAllVersions(Guid id)
        {
            var versions = await _service.GetAllVersionsAsync(id);
            return Ok(versions);
        }

        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            var categories = Enum.GetValues<DocumentCategory>()
                .Select(category => new DocumentCategoryDto
                {
                    Value = category.ToString(),
                    DisplayName = category.ToString() // Đơn giản - không cần mapping
                })
                .ToList();

            return Ok(categories);
        }
    }
}