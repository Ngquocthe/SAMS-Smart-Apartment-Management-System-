using SAMS_BE.DTOs;
using SAMS_BE.Models;
using Microsoft.AspNetCore.Http;

namespace SAMS_BE.Interfaces.IService;

public interface ITicketService
{
    Task<(IEnumerable<TicketDto> items, int total)> SearchAsync(TicketQueryDto dto);
    Task<TicketDto?> GetAsync(Guid id);
    Task<TicketDto> CreateAsync(CreateTicketDto dto);
    Task<TicketDto?> UpdateAsync(UpdateTicketDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<TicketDto?> ChangeStatusAsync(ChangeTicketStatusDto dto);

    // Comments
    Task<IEnumerable<TicketCommentDto>> GetCommentsAsync(Guid ticketId);
    Task<TicketCommentDto> AddCommentAsync(CreateTicketCommentDto dto);

    // Ticket Attachments
    Task<IEnumerable<TicketAttachmentDto>> GetAttachmentsAsync(Guid ticketId);
    Task<TicketAttachmentDto> AddAttachmentAsync(CreateTicketAttachmentDto dto);
    Task<bool> DeleteAttachmentAsync(Guid attachmentId);

    // Files
    Task<FileDto> UploadFileAsync(IFormFile file, string subFolder, string? uploadedBy);
    Task<FileDto?> GetFileAsync(Guid fileId);
    Task<bool> DeleteFileAsync(Guid fileId);

    // Ticket related data
    Task<IEnumerable<InvoiceDetailDto>> GetInvoiceDetailsAsync(Guid ticketId);
    Task<IEnumerable<VoucherItemDto>> GetVoucherItemsAsync(Guid ticketId);
}


