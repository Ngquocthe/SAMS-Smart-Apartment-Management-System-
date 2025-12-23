using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;

namespace SAMS_BE.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class VoucherItemController : Controller
  {
    private readonly IVoucherItemService _service;
    private readonly ILogger<VoucherItemController> _logger;

    public VoucherItemController(IVoucherItemService service, ILogger<VoucherItemController> logger)
    {
      _service = service;
      _logger = logger;
    }

    // POST /api/VoucherItem
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVoucherItemDto dto)
    {
      try
      {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.VoucherItemsId }, result);
      }
      catch (KeyNotFoundException ex)
      {
        return NotFound(new { message = ex.Message, error = ex.Message });
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new { message = ex.Message, error = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating voucher item");
        return StatusCode(500, new { error = "An error occurred while creating the voucher item." });
      }
    }

    // GET /api/VoucherItem/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
      try
      {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
      }
      catch (KeyNotFoundException ex)
      {
        return NotFound(new { error = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving voucher item with ID: {VoucherItemId}", id);
        return StatusCode(500, new { error = "An error occurred while retrieving the voucher item." });
      }
    }

    // GET /api/VoucherItem?voucherId=...&serviceTypeId=...&apartmentId=...&ticketId=...&search=...&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] VoucherItemListQueryDto query)
    {
      try
      {
        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 200) query.PageSize = 20;

        var result = await _service.ListAsync(query);
        return Ok(result);
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new { error = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error listing voucher items");
        return StatusCode(500, new { error = "An error occurred while listing voucher items." });
      }
    }

    // GET /api/VoucherItem/voucher/{voucherId}
    [HttpGet("voucher/{voucherId}")]
    public async Task<IActionResult> GetByVoucherId(Guid voucherId)
    {
      try
      {
        var result = await _service.GetByVoucherIdAsync(voucherId);
        return Ok(result);
      }
      catch (KeyNotFoundException ex)
      {
        return NotFound(new { error = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving voucher items for voucher ID: {VoucherId}", voucherId);
        return StatusCode(500, new { error = "An error occurred while retrieving voucher items." });
      }
    }

    // PUT /api/VoucherItem/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVoucherItemDto dto)
    {
      try
      {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
      }
      catch (KeyNotFoundException ex)
      {
        return NotFound(new { error = ex.Message });
      }
      catch (InvalidOperationException ex)
      {
        return BadRequest(new { error = ex.Message });
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new { error = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating voucher item with ID: {VoucherItemId}", id);
        return StatusCode(500, new { error = "An error occurred while updating the voucher item." });
      }
    }

    // DELETE /api/VoucherItem/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
      try
      {
        await _service.DeleteAsync(id);
        return NoContent(); // 204 No Content
      }
      catch (KeyNotFoundException ex)
      {
        return NotFound(new { error = ex.Message });
      }
      catch (InvalidOperationException ex)
      {
        return BadRequest(new { error = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting voucher item with ID: {VoucherItemId}", id);
        return StatusCode(500, new { message = "An error occurred while deleting the voucher item.", error = "An error occurred while deleting the voucher item." });
      }
    }
  }
}
