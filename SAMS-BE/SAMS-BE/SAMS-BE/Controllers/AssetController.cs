using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly ILogger<AssetController> _logger;

    public AssetController(IAssetService assetService, ILogger<AssetController> logger)
    {
        _assetService = assetService;
        _logger = logger;
    }

    #region Asset Management - View

    /// <summary>
    /// Lấy tất cả tài sản
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAllAssets()
    {
        try
        {
            var assets = await _assetService.GetAllAssetsAsync();
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all assets: {Message}", ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Lấy tài sản theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AssetDto>> GetAssetById(Guid id)
    {
        try
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null)
            {
                return NotFound(new { message = "Asset not found" });
            }
            return Ok(asset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting asset by ID: {AssetId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Tìm kiếm tài sản theo tên hoặc code
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> SearchAssets([FromQuery] string? searchTerm)
    {
        try
        {
            var assets = await _assetService.SearchAssetsAsync(searchTerm ?? "");
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching assets");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tài sản theo trạng thái
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssetsByStatus(string status)
    {
        try
        {
            var assets = await _assetService.GetAssetsByStatusAsync(status);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets by status: {Status}", status);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tài sản theo vị trí
    /// </summary>
    [HttpGet("location/{location}")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssetsByLocation(string location)
    {
        try
        {
            var assets = await _assetService.GetAssetsByLocationAsync(location);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets by location: {Location}", location);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tài sản theo danh mục
    /// </summary>
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssetsByCategory(Guid categoryId)
    {
        try
        {
            var assets = await _assetService.GetAssetsByCategoryAsync(categoryId);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets by category: {CategoryId}", categoryId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tài sản theo căn hộ
    /// </summary>
    [HttpGet("apartment/{apartmentId}")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssetsByApartment(Guid apartmentId)
    {
        try
        {
            var assets = await _assetService.GetAssetsByApartmentAsync(apartmentId);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets by apartment: {ApartmentId}", apartmentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tài sản theo block
    /// </summary>
    [HttpGet("block/{blockId}")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssetsByBlock(Guid blockId)
    {
        try
        {
            var assets = await _assetService.GetAssetsByBlockAsync(blockId);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets by block: {BlockId}", blockId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tài sản đã hết bảo hành
    /// </summary>
    [HttpGet("warranty/expired")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssetsWithExpiredWarranty()
    {
        try
        {
            var assets = await _assetService.GetAssetsWithExpiredWarrantyAsync();
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets with expired warranty");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tài sản sắp hết bảo hành trong N ngày
    /// </summary>
    [HttpGet("warranty/expiring")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssetsWithWarrantyExpiringInDays([FromQuery] int days = 30)
    {
        try
        {
            var assets = await _assetService.GetAssetsWithWarrantyExpiringInDaysAsync(days);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets with warranty expiring in {Days} days", days);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tài sản đang hoạt động
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetActiveAssets()
    {
        try
        {
            var assets = await _assetService.GetActiveAssetsAsync();
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting active assets");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy thống kê tài sản
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetAssetStatistics()
    {
        try
        {
            var totalCount = await _assetService.GetAssetCountAsync();
            var activeCount = await _assetService.GetAssetCountByStatusAsync("ACTIVE");
            var inactiveCount = await _assetService.GetAssetCountByStatusAsync("INACTIVE");
            var maintenanceCount = await _assetService.GetAssetCountByStatusAsync("MAINTENANCE");

            return Ok(new
            {
                totalAssets = totalCount,
                activeAssets = activeCount,
                inactiveAssets = inactiveCount,
                maintenanceAssets = maintenanceCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting asset statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Asset Categories

    /// <summary>
    /// Lấy tất cả danh mục tài sản
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<AssetCategoryDto>>> GetAllCategories()
    {
        try
        {
            var categories = await _assetService.GetAllCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all asset categories");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy danh mục tài sản theo ID
    /// </summary>
    [HttpGet("categories/{id}")]
    public async Task<ActionResult<AssetCategoryDto>> GetCategoryById(Guid id)
    {
        try
        {
            var category = await _assetService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Asset category not found" });
            }
            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting category by ID: {CategoryId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Asset Management - Create

    /// <summary>
    /// Tạo mới tài sản
    /// </summary>
    [HttpPost("createasset")]
    public async Task<ActionResult<AssetDto>> CreateAsset([FromBody] CreateAssetDto createAssetDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdAsset = await _assetService.CreateAssetAsync(createAssetDto);
            return CreatedAtAction(
                nameof(GetAssetById),
                new { id = createdAsset.AssetId },
                createdAsset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating asset: {Message}", ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    #endregion

    #region Asset Management - Update

    /// <summary>
    /// Cập nhật tài sản
    /// </summary>
    [HttpPut("updateasset/{id}")]
    public async Task<ActionResult<AssetDto>> UpdateAsset(Guid id, [FromBody] UpdateAssetDto updateAssetDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedAsset = await _assetService.UpdateAssetAsync(updateAssetDto, id);
            if (updatedAsset == null)
            {
                return NotFound(new { message = "Asset not found" });
            }

            return Ok(updatedAsset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating asset: {AssetId}, Message: {Message}", id, ex.Message);
            return StatusCode(500, new { 
                error = "Internal server error",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    #endregion

    #region Asset Management - Delete

    /// <summary>
    /// Xóa mềm tài sản
    /// </summary>
    [HttpDelete("softdelete/{id}")]
    public async Task<ActionResult> SoftDeleteAsset(Guid id)
    {
        try
        {
            var result = await _assetService.DeleteAssetAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Asset not found" });
            }

            return Ok(new { message = "Asset deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting asset: {AssetId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion
}

