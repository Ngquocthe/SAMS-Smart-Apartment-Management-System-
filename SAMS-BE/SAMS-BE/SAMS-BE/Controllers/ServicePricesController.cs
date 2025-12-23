// Controllers/ServicePricesController.cs
using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;

namespace SAMS_BE.Controllers
{
    [ApiController]
    [Route("api/servicetypes/{serviceTypeId:guid}/prices")]
    public class ServicePricesController : Controller
    {
        private readonly IServicePriceService _svc;
        public ServicePricesController(IServicePriceService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<PagedResult<ServicePriceResponseDto>>> List(
            Guid serviceTypeId, [FromQuery] ServicePriceListQueryDto query)
            => Ok(await _svc.ListAsync(serviceTypeId, query));

        [HttpPost]
        public async Task<ActionResult<ServicePriceResponseDto>> Create(
            Guid serviceTypeId, [FromBody] CreateServicePriceDto dto, [FromQuery] bool autoClosePrevious = true)
        {
            var res = await _svc.CreateAsync(serviceTypeId, dto, autoClosePrevious);
            return CreatedAtAction(nameof(List), new { serviceTypeId }, res);
        }

        [HttpPut("{priceId:guid}")]
        public async Task<ActionResult<ServicePriceResponseDto>> Update(
            Guid serviceTypeId, Guid priceId, [FromBody] UpdateServicePriceDto dto)
        {
            var res = await _svc.UpdateAsync(priceId, dto);
            return res is null ? NotFound() : Ok(res);
        }

        // “Delete” = Cancel (giữ lịch sử)
        [HttpDelete("{priceId:guid}")]
        public async Task<IActionResult> Cancel(Guid serviceTypeId, Guid priceId)
            => await _svc.CancelAsync(priceId) ? NoContent() : NotFound();
    }
}
