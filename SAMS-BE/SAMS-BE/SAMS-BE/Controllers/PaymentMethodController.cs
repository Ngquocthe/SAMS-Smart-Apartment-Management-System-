using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;

namespace SAMS_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentMethodController : ControllerBase
    {
        private readonly IPaymentMethodService _service;
        private readonly ILogger<PaymentMethodController> _logger;

  public PaymentMethodController(
     IPaymentMethodService service,
      ILogger<PaymentMethodController> logger)
      {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// L?y t?t c? Payment Methods
        /// </summary>
  /// <returns>Danh sách Payment Methods</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
   {
       try
    {
     var result = await _service.GetAllAsync();
     return Ok(result);
         }
catch (Exception ex)
   {
  _logger.LogError(ex, "Error retrieving all payment methods");
      return StatusCode(500, new { error = "An error occurred while retrieving payment methods." });
  }
    }

        /// <summary>
        /// L?y danh sách Payment Methods ?ang active
        /// </summary>
        /// <returns>Danh sách Payment Methods active</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
          try
         {
      var result = await _service.GetActiveAsync();
   return Ok(result);
        }
 catch (Exception ex)
       {
    _logger.LogError(ex, "Error retrieving active payment methods");
        return StatusCode(500, new { error = "An error occurred while retrieving active payment methods." });
   }
        }

        /// <summary>
        /// L?y Payment Method theo ID
        /// </summary>
  /// <param name="id">Payment Method ID</param>
  /// <returns>Payment Method detail</returns>
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
        _logger.LogError(ex, "Error retrieving payment method with ID: {PaymentMethodId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the payment method." });
         }
        }

        /// <summary>
        /// L?y Payment Method theo Code
        /// </summary>
        /// <param name="code">Payment Method Code</param>
      /// <returns>Payment Method detail</returns>
        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetByCode(string code)
  {
            try
        {
        var result = await _service.GetByCodeAsync(code);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
        {
       return NotFound(new { error = ex.Message });
      }
    catch (Exception ex)
{
       _logger.LogError(ex, "Error retrieving payment method with code: {Code}", code);
       return StatusCode(500, new { error = "An error occurred while retrieving the payment method." });
       }
        }

        /// <summary>
    /// T?o Payment Method m?i
        /// </summary>
        /// <param name="dto">Thông tin Payment Method</param>
   /// <returns>Payment Method ?ã t?o</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentMethodDto dto)
        {
     try
            {
            if (!ModelState.IsValid)
             {
          return BadRequest(ModelState);
       }

       var result = await _service.CreateAsync(dto);
         return CreatedAtAction(nameof(GetById), new { id = result.PaymentMethodId }, result);
            }
            catch (InvalidOperationException ex)
         {
        return BadRequest(new { error = ex.Message });
     }
        catch (Exception ex)
       {
        _logger.LogError(ex, "Error creating payment method");
      return StatusCode(500, new { error = "An error occurred while creating the payment method." });
  }
        }

        /// <summary>
        /// C?p nh?t Payment Method
   /// </summary>
        /// <param name="id">Payment Method ID</param>
        /// <param name="dto">Thông tin c?p nh?t</param>
   /// <returns>Payment Method ?ã c?p nh?t</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentMethodDto dto)
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
            catch (Exception ex)
      {
      _logger.LogError(ex, "Error updating payment method with ID: {PaymentMethodId}", id);
     return StatusCode(500, new { error = "An error occurred while updating the payment method." });
    }
        }

        /// <summary>
        /// Xóa Payment Method
    /// </summary>
        /// <param name="id">Payment Method ID</param>
        /// <returns>No content</returns>
      [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
    try
   {
            await _service.DeleteAsync(id);
            return NoContent();
            }
            catch (KeyNotFoundException ex)
      {
            return NotFound(new { error = ex.Message });
      }
   catch (Exception ex)
            {
           _logger.LogError(ex, "Error deleting payment method with ID: {PaymentMethodId}", id);
     return StatusCode(500, new { error = "An error occurred while deleting the payment method." });
 }
        }
    }
}
