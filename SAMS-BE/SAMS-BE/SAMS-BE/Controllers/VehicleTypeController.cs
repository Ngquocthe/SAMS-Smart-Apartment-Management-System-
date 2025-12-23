using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleTypeController : ControllerBase
    {
        private readonly BuildingManagementContext _context;

        public VehicleTypeController(BuildingManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách tất cả loại xe
        /// </summary>
        /// <returns>Danh sách vehicle types</returns>
        [HttpGet]
        public async Task<ActionResult<List<VehicleTypeDto>>> GetAllVehicleTypes()
        {
            try
            {
                var vehicleTypes = await _context.VehicleTypes
                    .Select(vt => new VehicleTypeDto
                    {
                        VehicleTypeId = vt.VehicleTypeId,
                        Code = vt.Code,
                        Name = vt.Name
                    })
                    .OrderBy(vt => vt.Name)
                    .ToListAsync();

                return Ok(vehicleTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }
    }
}
