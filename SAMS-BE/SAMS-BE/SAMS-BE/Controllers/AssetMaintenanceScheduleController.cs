using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Helpers;
using System.Security.Claims;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/asset-maintenance-schedules")]
public class AssetMaintenanceScheduleController : ControllerBase
{
    private readonly IAssetMaintenanceScheduleService _scheduleService;
    private readonly ILogger<AssetMaintenanceScheduleController> _logger;

    public AssetMaintenanceScheduleController(
        IAssetMaintenanceScheduleService scheduleService,
        ILogger<AssetMaintenanceScheduleController> logger)
    {
        _scheduleService = scheduleService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy tất cả lịch bảo trì
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetMaintenanceScheduleDto>>> GetAllSchedules()
    {
        try
        {
            var schedules = await _scheduleService.GetAllSchedulesAsync();
            return Ok(schedules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all schedules: {Message}", ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Lấy lịch bảo trì theo ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetMaintenanceScheduleDto>> GetScheduleById(Guid id)
    {
        try
        {
            var schedule = await _scheduleService.GetScheduleByIdAsync(id);
            if (schedule == null)
            {
                return NotFound(new { message = "Schedule not found" });
            }
            return Ok(schedule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting schedule by ID: {ScheduleId}, Message: {Message}", id, ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Lấy lịch bảo trì theo Asset ID
    /// </summary>
    [HttpGet("asset/{assetId}")]
    public async Task<ActionResult<IEnumerable<AssetMaintenanceScheduleDto>>> GetSchedulesByAssetId(Guid assetId)
    {
        try
        {
            var schedules = await _scheduleService.GetSchedulesByAssetIdAsync(assetId);
            return Ok(schedules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting schedules by asset ID: {AssetId}, Message: {Message}", assetId, ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Lấy lịch bảo trì theo Status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<AssetMaintenanceScheduleDto>>> GetSchedulesByStatus(string status)
    {
        try
        {
            var schedules = await _scheduleService.GetSchedulesByStatusAsync(status);
            return Ok(schedules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting schedules by status: {Status}, Message: {Message}", status, ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Tạo lịch bảo trì mới
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssetMaintenanceScheduleDto>> CreateSchedule([FromBody] CreateAssetMaintenanceScheduleDto createDto)
    {
        try
        {
            // Kiểm tra quyền lễ tân hoặc quản lý
            if (!HasReceptionistPrivilege())
            {
                return StatusCode(403, new { error = "Forbidden", message = "Chỉ quản lý tòa nhà và lễ tân mới được phép tạo lịch bảo trì" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Guid? createdBy = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("sub")?.Value
                                 ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                 ?? User.FindFirst("user_id")?.Value;
                
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    createdBy = userId;
                }
            }

            var schedule = await _scheduleService.CreateScheduleAsync(createDto, createdBy);
            
            return CreatedAtAction(nameof(GetScheduleById), new { id = schedule.ScheduleId }, schedule);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating schedule: {Message}", ex.Message);
            return BadRequest(new { 
                error = "Validation error",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating schedule: {Message}", ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Cập nhật lịch bảo trì
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AssetMaintenanceScheduleDto>> UpdateSchedule(Guid id, [FromBody] UpdateAssetMaintenanceScheduleDto updateDto)
    {
        try
        {
            // Kiểm tra quyền lễ tân hoặc quản lý
            if (!HasReceptionistPrivilege())
            {
                return StatusCode(403, new { error = "Forbidden", message = "Chỉ quản lý tòa nhà và lễ tân mới được phép cập nhật lịch bảo trì" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var schedule = await _scheduleService.UpdateScheduleAsync(updateDto, id);
            if (schedule == null)
            {
                return NotFound(new { message = "Schedule not found" });
            }

            return Ok(schedule);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to update schedule: {ScheduleId}, Message: {Message}", id, ex.Message);
            return StatusCode(403, new { 
                error = "Forbidden",
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating schedule: {ScheduleId}, Message: {Message}", id, ex.Message);
            return BadRequest(new { 
                error = "Validation error",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating schedule: {ScheduleId}, Message: {Message}", id, ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Xóa lịch bảo trì
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteSchedule(Guid id)
    {
        try
        {
            // Kiểm tra quyền lễ tân hoặc quản lý
            if (!HasReceptionistPrivilege())
            {
                return StatusCode(403, new { error = "Forbidden", message = "Chỉ quản lý tòa nhà và lễ tân mới được phép xóa lịch bảo trì" });
            }

            var result = await _scheduleService.DeleteScheduleAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Schedule not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting schedule: {ScheduleId}, Message: {Message}", id, ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Lấy các lịch bảo trì sắp đến hạn (để gửi nhắc nhở)
    /// </summary>
    [HttpGet("due-for-reminder")]
    public async Task<ActionResult<IEnumerable<AssetMaintenanceScheduleDto>>> GetSchedulesDueForReminder()
    {
        try
        {
            var schedules = await _scheduleService.GetSchedulesDueForReminderAsync();
            return Ok(schedules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting schedules due for reminder: {Message}", ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Lấy các lịch bảo trì đến hạn (để tạo ticket)
    /// </summary>
    [HttpGet("due-for-maintenance")]
    public async Task<ActionResult<IEnumerable<AssetMaintenanceScheduleDto>>> GetSchedulesDueForMaintenance()
    {
        try
        {
            var schedules = await _scheduleService.GetSchedulesDueForMaintenanceAsync();
            return Ok(schedules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting schedules due for maintenance: {Message}", ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Tìm kiếm và lọc lịch bảo trì
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<AssetMaintenanceScheduleDto>>> SearchSchedules(
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? assetId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? startDateFrom = null,
        [FromQuery] DateOnly? startDateTo = null)
    {
        try
        {
            var schedules = await _scheduleService.SearchSchedulesAsync(searchTerm, assetId, status, startDateFrom, startDateTo);
            return Ok(schedules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching schedules: {Message}", ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Trigger job hoàn thành bảo trì thủ công (để test/debug)
    /// </summary>
    [HttpPost("trigger-complete-job")]
    public async Task<ActionResult> TriggerCompleteJob()
    {
        try
        {
            await _scheduleService.CompleteMaintenanceJobAsync();
            return Ok(new { message = "Complete maintenance job triggered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while triggering complete job: {Message}", ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Trigger job bắt đầu bảo trì thủ công (để test/debug)
    /// </summary>
    [HttpPost("trigger-start-job")]
    public async Task<ActionResult> TriggerStartJob()
    {
        try
        {
            await _scheduleService.StartMaintenanceJobAsync();
            return Ok(new { message = "Start maintenance job triggered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while triggering start job: {Message}", ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Kiểm tra quyền lễ tân hoặc quản lý
    /// </summary>
    private bool HasReceptionistPrivilege()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
        {
            return false;
        }

        return User.IsInRole("receptionist")
               || User.IsInRole("global_admin")
               || User.IsInRole("building_admin")
               || User.IsInRole("building-manager")
               || User.IsInRole("building_management");
    }
}

