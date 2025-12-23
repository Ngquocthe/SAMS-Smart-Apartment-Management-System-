using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/buildingmanagement")]
public class BuildingManagementController : ControllerBase
{
    private readonly IAmenityService _amenityService;
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<BuildingManagementController> _logger;

    public BuildingManagementController(
        IAmenityService amenityService, 
        IDashboardService dashboardService,
        ILogger<BuildingManagementController> logger)
    {
        _amenityService = amenityService;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    #region Amenity Management - View Only

    /// <summary>
    /// Lấy tất cả tiện ích
    /// </summary>
    [HttpGet("amenities")]
    public async Task<ActionResult<IEnumerable<AmenityDto>>> GetAllAmenities()
    {
        try
        {
            var amenities = await _amenityService.GetAllAmenitiesAsync();
            return Ok(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all amenities");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tiện ích theo ID
    /// </summary>
    [HttpGet("amenities/{id}")]
    public async Task<ActionResult<AmenityDto>> GetAmenityById(Guid id)
    {
        try
        {
            var amenity = await _amenityService.GetAmenityByIdAsync(id);
            if (amenity == null)
            {
                return NotFound(new { message = "Amenity not found" });
            }
            return Ok(amenity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenity by ID: {AmenityId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Tìm kiếm tiện ích theo tên hoặc code
    /// </summary>
    [HttpGet("amenities/search")]
    public async Task<ActionResult<IEnumerable<AmenityDto>>> SearchAmenities([FromQuery] string? searchTerm)
    {
        try
        {
            var amenities = await _amenityService.SearchAmenitiesAsync(searchTerm ?? "");
            return Ok(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching amenities");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tiện ích theo trạng thái
    /// </summary>
    [HttpGet("amenities/status/{status}")]
    public async Task<ActionResult<IEnumerable<AmenityDto>>> GetAmenitiesByStatus(string status)
    {
        try
        {
            var amenities = await _amenityService.GetAmenitiesByStatusAsync(status);
            return Ok(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenities by status: {Status}", status);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tiện ích theo vị trí
    /// </summary>
    [HttpGet("amenities/location/{location}")]
    public async Task<ActionResult<IEnumerable<AmenityDto>>> GetAmenitiesByLocation(string location)
    {
        try
        {
            var amenities = await _amenityService.GetAmenitiesByLocationAsync(location);
            return Ok(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenities by location: {Location}", location);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tiện ích theo danh mục
    /// </summary>
    [HttpGet("amenities/category/{categoryName}")]
    public async Task<ActionResult<IEnumerable<AmenityDto>>> GetAmenitiesByCategory(string categoryName)
    {
        try
        {
            var amenities = await _amenityService.GetAmenitiesByCategoryAsync(categoryName);
            return Ok(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenities by category: {CategoryName}", categoryName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tiện ích theo khoảng giá
    /// </summary>
    [HttpGet("amenities/price-range")]
    public async Task<ActionResult<IEnumerable<AmenityDto>>> GetAmenitiesByPriceRange([FromQuery] int minPrice, [FromQuery] int maxPrice)
    {
        try
        {
            var amenities = await _amenityService.GetAmenitiesByPriceRangeAsync(minPrice, maxPrice);
            return Ok(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenities by price range: {MinPrice}-{MaxPrice}", minPrice, maxPrice);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tiện ích có sẵn
    /// </summary>
    [HttpGet("amenities/available")]
    public async Task<ActionResult<IEnumerable<AmenityDto>>> GetAvailableAmenities()
    {
        try
        {
            var amenities = await _amenityService.GetAvailableAmenitiesAsync();
            return Ok(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting available amenities");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy tiện ích cần đặt trước
    /// </summary>
    [HttpGet("amenities/requiring-booking")]
    public async Task<ActionResult<IEnumerable<AmenityDto>>> GetAmenitiesRequiringBooking()
    {
        try
        {
            var amenities = await _amenityService.GetAmenitiesRequiringBookingAsync();
            return Ok(amenities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenities requiring booking");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy thống kê tiện ích
    /// </summary>
    [HttpGet("amenities/statistics")]
    public async Task<ActionResult> GetAmenityStatistics()
    {
        try
        {
            var totalCount = await _amenityService.GetAmenityCountAsync();
            var activeCount = await _amenityService.GetAmenityCountByStatusAsync("Active");
            var inactiveCount = await _amenityService.GetAmenityCountByStatusAsync("Inactive");
            var maintenanceCount = await _amenityService.GetAmenityCountByStatusAsync("MAINTENANCE");

            return Ok(new
            {
                totalAmenities = totalCount,
                activeAmenities = activeCount,
                inactiveAmenities = inactiveCount,
                maintenanceAmenities = maintenanceCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting amenity statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Amenity Management - Create

    /// <summary>
    /// Tạo mới tiện ích
    /// </summary>
    [HttpPost("createamenity")]
    public async Task<ActionResult<AmenityDto>> CreateAmenity([FromBody] CreateAmenityDto createAmenityDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdAmenity = await _amenityService.CreateAmenityAsync(createAmenityDto);

            return CreatedAtAction(
                nameof(GetAmenityById),
                new { id = createdAmenity.AmenityId },
                createdAmenity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating amenity");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cập nhật tiện ích
    /// </summary>
    [HttpPut("updateamenity/{id}")]
    public async Task<ActionResult<AmenityDto>> UpdateAmenity(Guid id, [FromBody] UpdateAmenityDto updateAmenityDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedAmenity = await _amenityService.UpdateAmenityAsync(updateAmenityDto, id);

            if (updatedAmenity == null)
            {
                return NotFound(new { message = "Amenity not found" });
            }

            return Ok(updatedAmenity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating amenity: {AmenityId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Xóa tiện ích
    /// </summary>
    [HttpDelete("deleteamenity/{id}")]
    public async Task<ActionResult> DeleteAmenity(Guid id)
    {
        try
        {
            var result = await _amenityService.DeleteAmenityAsync(id);

            if (!result)
            {
                return NotFound(new { message = "Amenity not found" });
            }

            return Ok(new { message = "Amenity deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting amenity: {AmenityId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Dashboard Statistics

    /// <summary>
    /// Lấy thống kê dashboard cho building manager
    /// </summary>
    [HttpGet("dashboard/statistics")]
    public async Task<ActionResult<DashboardStatisticsDto>> GetDashboardStatistics()
    {
        try
        {
            var statistics = await _dashboardService.GetDashboardStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting dashboard statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion
}