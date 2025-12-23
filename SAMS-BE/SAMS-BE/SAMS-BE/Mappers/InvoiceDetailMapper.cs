using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class InvoiceDetailMapper
{
    public static InvoiceDetailResponseDto ToDto(this InvoiceDetail entity)
    {
        return new InvoiceDetailResponseDto
        {
            InvoiceDetailId = entity.InvoiceDetailId,
            InvoiceId = entity.InvoiceId,
            ServiceId = entity.ServiceId,
            ServiceName = entity.Service?.Name,
            ServiceCode = entity.Service?.Code,
            ServiceUnit = entity.Service?.Unit,
            Description = entity.Description,
            Quantity = entity.Quantity,
            UnitPrice = entity.UnitPrice,
            Amount = entity.Amount ?? 0,
            VatRate = entity.VatRate,
            VatAmount = entity.VatAmount
        };
    }

    public static List<InvoiceDetailResponseDto> ToDtoList(this IEnumerable<InvoiceDetail> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }
}
