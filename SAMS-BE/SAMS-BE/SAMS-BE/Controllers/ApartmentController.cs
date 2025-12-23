using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;

namespace SAMS_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApartmentController : ControllerBase
    {
        private readonly IApartmentService _apartmentService;

        public ApartmentController(IApartmentService apartmentService)
        {
            _apartmentService = apartmentService;
        }

        /// <summary>
        /// Tạo apartments cho một tầng
        /// </summary>
        /// <param name="request">Thông tin tạo apartments</param>
        /// <returns>Kết quả tạo apartments</returns>
        [HttpPost("create-apartment")]
        public async Task<ActionResult<CreateApartmentsResponseDto>> CreateApartments([FromBody] CreateApartmentsRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new CreateApartmentsResponseDto
                    {
                        Success = false,
                        Message = "Dữ liệu đầu vào không hợp lệ",
                        TotalCreated = 0
                    });
                }

                var result = await _apartmentService.CreateApartmentsAsync(request);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CreateApartmentsResponseDto
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}",
                    TotalCreated = 0
                });
            }
        }

        /// <summary>
        /// Tạo một căn hộ đơn lẻ trong tầng
        /// </summary>
        /// <param name="request">Thông tin căn hộ cần tạo</param>
        /// <returns>Căn hộ đã được tạo</returns>
        [HttpPost("create-single")]
        public async Task<ActionResult<ApartmentResponseDto>> CreateSingleApartment([FromBody] CreateSingleApartmentRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Dữ liệu đầu vào không hợp lệ");
                }

                var result = await _apartmentService.CreateSingleApartmentAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Không tìm thấy") || ex.Message.Contains("đã tồn tại"))
                {
                    return BadRequest(ex.Message);
                }
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Nhân bản apartments từ tầng gốc sang các tầng khác
        /// </summary>
        /// <param name="request">Thông tin nhân bản apartments</param>
        /// <returns>Kết quả nhân bản apartments</returns>
        [HttpPost("replicate")]
        public async Task<ActionResult<ReplicateApartmentsResponseDto>> ReplicateApartments([FromBody] ReplicateApartmentsRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ReplicateApartmentsResponseDto
                    {
                        Success = false,
                        Message = "Dữ liệu đầu vào không hợp lệ",
                        TotalReplicated = 0
                    });
                }

                var result = await _apartmentService.ReplicateApartmentsAsync(request);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ReplicateApartmentsResponseDto
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}",
                    TotalReplicated = 0
                });
            }
        }

        /// <summary>
        /// Lấy tất cả apartments
        /// </summary>
        /// <returns>Danh sách apartments</returns>
        [HttpGet]
        public async Task<ActionResult<List<ApartmentResponseDto>>> GetAllApartments()
        {
            try
            {
                var apartments = await _apartmentService.GetAllApartmentsAsync();
                return Ok(apartments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy apartments theo tầng
        /// </summary>
        /// <param name="floorNumber">Số tầng</param>
        /// <returns>Danh sách apartments của tầng</returns>
        [HttpGet("floor/{floorNumber}")]
        public async Task<ActionResult<List<ApartmentResponseDto>>> GetApartmentsByFloor(int floorNumber)
        {
            try
            {
                var apartments = await _apartmentService.GetApartmentsByFloorAsync(floorNumber);
                return Ok(apartments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy apartment theo số căn hộ (ví dụ: A0108)
        /// </summary>
        /// <param name="number">Số căn hộ</param>
        /// <returns>Thông tin apartment</returns>
        [HttpGet("number/{number}")]
        public async Task<ActionResult<ApartmentResponseDto>> GetApartmentByNumber(string number)
        {
            try
            {
                var apartment = await _apartmentService.GetApartmentByNumberAsync(number);
                if (apartment == null)
                {
                    return NotFound($"Không tìm thấy căn hộ {number}");
                }
                return Ok(apartment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy apartment theo ID
        /// </summary>
        /// <param name="id">Apartment ID</param>
        /// <returns>Thông tin apartment</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApartmentResponseDto>> GetApartmentById(Guid id)
        {
            try
            {
                var apartment = await _apartmentService.GetApartmentByIdAsync(id);
                if (apartment == null)
                {
                    return NotFound("Không tìm thấy apartment");
                }
                return Ok(apartment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy tóm tắt apartments theo tầng
        /// </summary>
        /// <returns>Tóm tắt apartments theo tầng</returns>
        [HttpGet("summary")]
        public async Task<ActionResult<List<FloorApartmentSummaryDto>>> GetFloorApartmentSummary()
        {
            try
            {
                var summary = await _apartmentService.GetFloorApartmentSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật apartment
        /// </summary>
        /// <param name="id">Apartment ID</param>
        /// <param name="updateDto">Thông tin cập nhật</param>
        /// <returns>Apartment đã cập nhật</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApartmentResponseDto>> UpdateApartment(Guid id, [FromBody] CreateApartmentDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Dữ liệu đầu vào không hợp lệ");
                }

                var updatedApartment = await _apartmentService.UpdateApartmentAsync(id, updateDto);
                return Ok(updatedApartment);
            }
            catch (InvalidOperationException ex)
            {
                // Validation error (e.g., cannot set INACTIVE with active residents)
                return BadRequest(new { error = ex.Message });
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
        /// Xóa apartment
        /// </summary>
        /// <param name="id">Apartment ID</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteApartment(Guid id)
        {
            try
            {
                var result = await _apartmentService.DeleteApartmentAsync(id);
                if (result)
                {
                    return Ok("Xóa apartment thành công");
                }
                return NotFound("Không tìm thấy apartment để xóa");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Refactor tên apartments từ prefix cũ sang prefix mới cho nhiều tầng
        /// </summary>
        /// <param name="request">Thông tin refactor</param>
        /// <returns>Kết quả refactor</returns>
        [HttpPut("refactor-names")]
        public async Task<ActionResult<RefactorApartmentNamesResponseDto>> RefactorApartmentNames([FromBody] RefactorApartmentNamesRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new RefactorApartmentNamesResponseDto
                    {
                        Success = false,
                        Message = "Dữ liệu đầu vào không hợp lệ",
                        TotalUpdated = 0
                    });
                }

                var result = await _apartmentService.RefactorApartmentNamesAsync(request);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RefactorApartmentNamesResponseDto
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}",
                    TotalUpdated = 0
                });
            }
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] string? number, [FromQuery] string? ownerName, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Nếu có ownerName, tìm theo tên chủ hộ
                if (!string.IsNullOrWhiteSpace(ownerName))
                {
                    var (items, total) = await _apartmentService.LookupByOwnerNameAsync(ownerName, page, pageSize);
                    return Ok(new { total, items });
                }

                // Nếu không có ownerName, tìm theo số căn hộ
                var (itemsByNumber, totalByNumber) = await _apartmentService.LookupAsync(number, page, pageSize);
                return Ok(new { total = totalByNumber, items = itemsByNumber });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{apartmentId:guid}/summary")]
        public async Task<IActionResult> GetSummary(Guid apartmentId)
        {
            try
            {
                var summary = await _apartmentService.GetSummaryAsync(apartmentId);
                return summary == null ? NotFound() : Ok(summary);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
