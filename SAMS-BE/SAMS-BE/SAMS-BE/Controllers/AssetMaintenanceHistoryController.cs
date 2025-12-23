using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Helpers;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/asset-maintenance-history")]
public class AssetMaintenanceHistoryController : ControllerBase
{
    private readonly IAssetMaintenanceHistoryService _historyService;
    private readonly ILogger<AssetMaintenanceHistoryController> _logger;

    public AssetMaintenanceHistoryController(
        IAssetMaintenanceHistoryService historyService,
        ILogger<AssetMaintenanceHistoryController> logger)
    {
        _historyService = historyService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy tất cả lịch sử bảo trì
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetMaintenanceHistoryDto>>> GetAllHistories()
    {
        try
        {
            var histories = await _historyService.GetAllHistoriesAsync();
            return Ok(histories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all histories: {Message}", ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Lấy lịch sử bảo trì theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AssetMaintenanceHistoryDto>> GetHistoryById(Guid id)
    {
        try
        {
            var history = await _historyService.GetHistoryByIdAsync(id);
            if (history == null)
            {
                return NotFound(new { message = "History not found" });
            }
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting history by ID: {HistoryId}, Message: {Message}", id, ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Lấy lịch sử bảo trì theo Asset ID
    /// </summary>
    [HttpGet("asset/{assetId}")]
    public async Task<ActionResult<IEnumerable<AssetMaintenanceHistoryDto>>> GetHistoriesByAssetId(Guid assetId)
    {
        try
        {
            var histories = await _historyService.GetHistoriesByAssetIdAsync(assetId);
            return Ok(histories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting histories by asset ID: {AssetId}, Message: {Message}", assetId, ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Lấy lịch sử bảo trì theo Schedule ID
    /// </summary>
    [HttpGet("schedule/{scheduleId}")]
    public async Task<ActionResult<IEnumerable<AssetMaintenanceHistoryDto>>> GetHistoriesByScheduleId(Guid scheduleId)
    {
        try
        {
            var histories = await _historyService.GetHistoriesByScheduleIdAsync(scheduleId);
            return Ok(histories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting histories by schedule ID: {ScheduleId}, Message: {Message}", scheduleId, ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Tạo lịch sử bảo trì mới (sau khi hoàn thành bảo trì)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssetMaintenanceHistoryDto>> CreateHistory([FromBody] CreateAssetMaintenanceHistoryDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var history = await _historyService.CreateHistoryAsync(createDto);
            
            return CreatedAtAction(nameof(GetHistoryById), new { id = history.HistoryId }, history);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating history: {Message}", ex.Message);
            return BadRequest(new { 
                error = "Validation error",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating history: {Message}", ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Cập nhật lịch sử bảo trì
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<AssetMaintenanceHistoryDto>> UpdateHistory(Guid id, [FromBody] UpdateAssetMaintenanceHistoryDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var history = await _historyService.UpdateHistoryAsync(updateDto, id);
            if (history == null)
            {
                return NotFound(new { message = "History not found" });
            }

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating history: {HistoryId}, Message: {Message}", id, ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }
}

