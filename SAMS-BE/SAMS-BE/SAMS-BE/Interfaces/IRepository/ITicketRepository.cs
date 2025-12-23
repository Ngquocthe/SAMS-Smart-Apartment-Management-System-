using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface ITicketRepository
{
    IQueryable<Ticket> Query();
    Task<Ticket?> GetByIdAsync(Guid id);
    Task<Ticket> AddAsync(Ticket entity);
    Task UpdateAsync(Ticket entity);
    Task DeleteAsync(Guid id);

    // Comments
    IQueryable<TicketComment> QueryComments();
    Task<TicketComment> AddCommentAsync(TicketComment comment);

    // Ticket Attachments
    IQueryable<TicketAttachment> QueryAttachments();
    Task<TicketAttachment?> GetAttachmentByIdAsync(Guid attachmentId);
    Task<List<TicketAttachment>> GetAttachmentsByTicketIdAsync(Guid ticketId);
    Task<TicketAttachment> AddAttachmentAsync(TicketAttachment attachment);
    Task DeleteAttachmentAsync(Guid attachmentId);

    // Files
    IQueryable<Models.File> QueryFiles();
    Task<Models.File?> GetFileByIdAsync(Guid fileId);
    Task<Models.File> AddFileAsync(Models.File file);
    Task DeleteFileAsync(Guid fileId);

    // Invoice Details
    IQueryable<InvoiceDetail> QueryInvoiceDetails();

    // Voucher Items
    IQueryable<VoucherItem> QueryVoucherItems();
}


