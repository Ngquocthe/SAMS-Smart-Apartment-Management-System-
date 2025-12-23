using SAMS_BE.DTOs;
using SAMS_BE.Models;
using SAMS_BE.Mappers;

namespace SAMS_BE.Mappers;

public static class InvoiceMapper
{
    public static Invoice ToEntity(this CreateInvoiceDto dto)
    {
        return new Invoice
        {
            InvoiceId = Guid.NewGuid(),
            InvoiceNo = dto.InvoiceNo.Trim(),
            ApartmentId = dto.ApartmentId,
            IssueDate = dto.IssueDate,
            DueDate = dto.DueDate,
            Status = dto.Status.Trim().ToUpperInvariant(),
            SubtotalAmount = 0,
            TaxAmount = 0,
            TotalAmount = 0,
            Note = dto.Note?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    public static InvoiceResponseDto ToDto(this Invoice entity)
    {
        return new InvoiceResponseDto
        {
            InvoiceId = entity.InvoiceId,
            InvoiceNo = entity.InvoiceNo,
            ApartmentId = entity.ApartmentId,
            IssueDate = entity.IssueDate,
            DueDate = entity.DueDate,
            Status = entity.Status,
            SubtotalAmount = entity.SubtotalAmount,
            TaxAmount = entity.TaxAmount,
            TotalAmount = entity.TotalAmount,
            Note = entity.Note,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            TicketId = entity.TicketId,  // TicketId đã được chuyển lên Invoice
            Details = (entity.InvoiceDetails != null)
                ? entity.InvoiceDetails.ToDtoList()
                : new List<InvoiceDetailResponseDto>()
        };
    }

    public static List<InvoiceResponseDto> ToDtoList(this IEnumerable<Invoice> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }
}