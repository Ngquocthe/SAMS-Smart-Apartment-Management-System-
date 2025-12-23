using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;

namespace SAMS_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FloorController : ControllerBase
    {
        private readonly IFloorService _floorService;

        public FloorController(IFloorService floorService)
        {
            _floorService = floorService;
        }

        /// <summary>
        /// Tạo một tầng đơn lẻ
        /// </summary>
        /// <param name="request">Thông tin tạo tầng</param>
        /// <returns>Kết quả tạo tầng</returns>
        [HttpPost("create-single")]
        public async Task<ActionResult<CreateSingleFloorResponseDto>> CreateSingleFloor([FromBody] CreateSingleFloorRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new CreateSingleFloorResponseDto
                    {
                        Success = false,
                        Message = "Dữ liệu đầu vào không hợp lệ"
                    });
                }

                var result = await _floorService.CreateSingleFloorAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CreateSingleFloorResponseDto
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Tạo nhiều tầng cho một tòa nhà
        /// </summary>
        /// <param name="request">Thông tin tạo tầng</param>
        /// <returns>Kết quả tạo tầng</returns>
        [HttpPost("create-floors")]
        public async Task<ActionResult<CreateFloorsResponseDto>> CreateFloors([FromBody] CreateFloorsRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new CreateFloorsResponseDto
                    {
                        Success = false,
                        Message = "Dữ liệu đầu vào không hợp lệ",
                        TotalCreated = 0
                    });
                }

                var result = await _floorService.CreateFloorsAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CreateFloorsResponseDto
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}",
                    TotalCreated = 0
                });
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả các tầng
        /// </summary>
        /// <returns>Danh sách tầng</returns>
        [HttpGet]
        public async Task<ActionResult<List<FloorResponseDto>>> GetAllFloors()
        {
            try
            {
                var floors = await _floorService.GetAllFloorsAsync();
                return Ok(floors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy thông tin tầng theo ID
        /// </summary>
        /// <param name="id">Floor ID</param>
        /// <returns>Thông tin tầng</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<FloorResponseDto>> GetFloorById(string id)
        {
            try
            {
                var floor = await _floorService.GetFloorByIdAsync(id);
                if (floor == null)
                {
                    return NotFound("Không tìm thấy tầng");
                }
                return Ok(floor);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật thông tin tầng
        /// </summary>
        /// <param name="id">Floor ID</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Thông tin tầng đã cập nhật</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<FloorResponseDto>> UpdateFloor(Guid id, [FromBody] UpdateFloorRequestDto request)
        {
            try
            {
                var updatedFloor = await _floorService.UpdateFloorAsync(id, request);
                return Ok(updatedFloor);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Không tìm thấy"))
                {
                    return NotFound(ex.Message);
                }
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa tầng
        /// </summary>
        /// <param name="id">Floor ID</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteFloor(Guid id)
        {
            try
            {
                var result = await _floorService.DeleteFloorAsync(id);
                if (result)
                {
                    return Ok(new { success = true, message = "Xóa tầng thành công" });
                }
                return NotFound(new { success = false, message = "Không tìm thấy tầng để xóa" });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("đã có căn hộ"))
                {
                    return BadRequest(new { success = false, message = ex.Message });
                }
                return StatusCode(500, new { success = false, message = $"Lỗi server: {ex.Message}" });
            }
        }
    }
}
