using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Response;
using SAMS_BE.Interfaces.IService;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/amenity-packages")]
public class AmenityPackageController : ControllerBase
{
    private readonly IAmenityPackageService _packageService;
    private readonly ILogger<AmenityPackageController> _logger;

    public AmenityPackageController(
        IAmenityPackageService packageService,
        ILogger<AmenityPackageController> logger)
    {
        _packageService = packageService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy tất cả packages
    /// GET /api/amenity-packages
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AmenityPackageDto>>>> GetAllPackages()
    {
        try
        {
            var packages = await _packageService.GetAllPackagesAsync();
            var response = new ApiResponse<List<AmenityPackageDto>>
            {
                Data = packages.ToList(),
                Success = true,
                Message = "Retrieved all packages successfully"
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all packages");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy package theo ID
    /// GET /api/amenity-packages/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AmenityPackageDto>>> GetPackageById(Guid id)
    {
        try
        {
            var package = await _packageService.GetPackageByIdAsync(id);
            if (package == null)
            {
                return NotFound(new { message = "Package not found" });
            }

            var response = new ApiResponse<AmenityPackageDto>
            {
                Data = package,
                Success = true,
                Message = "Package retrieved successfully"
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting package by ID: {PackageId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy tất cả packages theo amenity ID
    /// GET /api/amenity-packages/by-amenity/{amenityId}
    /// </summary>
    [HttpGet("by-amenity/{amenityId}")]
    public async Task<ActionResult<ApiResponse<List<AmenityPackageDto>>>> GetPackagesByAmenityId(Guid amenityId)
    {
        try
        {
            var packages = await _packageService.GetPackagesByAmenityIdAsync(amenityId);
            var response = new ApiResponse<List<AmenityPackageDto>>
            {
                Data = packages.ToList(),
                Success = true,
                Message = "Retrieved packages for amenity successfully"
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting packages by amenity ID: {AmenityId}", amenityId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy các packages đang active theo amenity ID
    /// GET /api/amenity-packages/by-amenity/{amenityId}/active
    /// </summary>
    [HttpGet("by-amenity/{amenityId}/active")]
    public async Task<ActionResult<ApiResponse<List<AmenityPackageDto>>>> GetActivePackagesByAmenityId(Guid amenityId)
    {
        try
        {
            var packages = await _packageService.GetActivePackagesByAmenityIdAsync(amenityId);
            var response = new ApiResponse<List<AmenityPackageDto>>
            {
                Data = packages.ToList(),
                Success = true,
                Message = "Retrieved active packages for amenity successfully"
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting active packages by amenity ID: {AmenityId}", amenityId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy packages theo status
    /// GET /api/amenity-packages/by-status/{status}
    /// </summary>
    // [Authorize(Policy = "RequireManagerOrAdmin")]
    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<ApiResponse<List<AmenityPackageDto>>>> GetPackagesByStatus(string status)
    {
        try
        {
            var packages = await _packageService.GetPackagesByStatusAsync(status);
            var response = new ApiResponse<List<AmenityPackageDto>>
            {
                Data = packages.ToList(),
                Success = true,
                Message = $"Retrieved packages with status '{status}' successfully"
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting packages by status: {Status}", status);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Tạo package mới
    /// POST /api/amenity-packages
    /// </summary>
    // [Authorize(Policy = "RequireManagerOrAdmin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AmenityPackageDto>>> CreatePackage([FromBody] CreateAmenityPackageDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new 
                { 
                    message = "Bad Request",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var package = await _packageService.CreatePackageAsync(dto);
            var response = new ApiResponse<AmenityPackageDto>
            {
                Data = package,
                Success = true,
                Message = "Package created successfully"
            };

            return CreatedAtAction(nameof(GetPackageById), new { id = package.PackageId }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating package");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật package
    /// PUT /api/amenity-packages/{id}
    /// </summary>
    // [Authorize(Policy = "RequireManagerOrAdmin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<AmenityPackageDto>>> UpdatePackage(Guid id, [FromBody] UpdateAmenityPackageDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new 
                { 
                    message = "Bad Request",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var package = await _packageService.UpdatePackageAsync(dto, id);
            if (package == null)
            {
                return NotFound(new { message = "Package not found" });
            }

            var response = new ApiResponse<AmenityPackageDto>
            {
                Data = package,
                Success = true,
                Message = "Package updated successfully"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating package: {PackageId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Xóa package
    /// DELETE /api/amenity-packages/{id}
    /// </summary>
    // [Authorize(Policy = "RequireManagerOrAdmin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeletePackage(Guid id)
    {
        try
        {
            var result = await _packageService.DeletePackageAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Package not found" });
            }

            var response = new ApiResponse<object>
            {
                Data = new { packageId = id, deletedAt = DateTime.UtcNow },
                Success = true,
                Message = "Package deleted successfully"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting package: {PackageId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}

