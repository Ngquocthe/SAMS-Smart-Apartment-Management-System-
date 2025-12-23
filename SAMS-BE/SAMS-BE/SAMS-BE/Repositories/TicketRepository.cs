using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly BuildingManagementContext _context;

    public TicketRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    public IQueryable<Ticket> Query()
    {
        return _context.Tickets
            .Include(x => x.CreatedByUser)
            .Include(x => x.Apartment)
            .AsQueryable();
    }

    public async Task<Ticket?> GetByIdAsync(Guid id)
    {
        // Load ticket cơ bản trước
        var ticket = await _context.Tickets
            .Include(x => x.CreatedByUser)
            .Include(x => x.Apartment)
            .Include(x => x.TicketComments)
            .FirstOrDefaultAsync(x => x.TicketId == id);

        if (ticket != null)
        {
            // Load attachments riêng biệt để tránh lỗi casting với UploadedBy
            var attachmentData = await _context.TicketAttachments
                .AsNoTracking()
                .Where(a => a.TicketId == id)
                .Select(a => new
                {
                    a.AttachmentId,
                    a.TicketId,
                    a.FileId,
                    a.Note,
                    a.UploadedAt
                })
                .ToListAsync();

            var attachments = new List<TicketAttachment>();
            foreach (var data in attachmentData)
            {
                var attachment = new TicketAttachment
                {
                    AttachmentId = data.AttachmentId,
                    TicketId = data.TicketId,
                    FileId = data.FileId,
                    UploadedBy = null, // Không load từ DB
                    Note = data.Note,
                    UploadedAt = data.UploadedAt
                };

                // Load File riêng biệt
                attachment.File = await _context.Files
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FileId == data.FileId);

                attachments.Add(attachment);
            }

            ticket.TicketAttachments = attachments;
        }

        return ticket;
    }

    public async Task<Ticket> AddAsync(Ticket entity)
    {
        _context.Tickets.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(Ticket entity)
    {
        // Detach File entities và navigation properties để tránh lỗi tracking conflict
        if (entity.TicketAttachments != null)
        {
            foreach (var attachment in entity.TicketAttachments)
            {
                if (attachment.File != null)
                {
                    var fileEntry = _context.Entry(attachment.File);
                    if (fileEntry.State != EntityState.Detached)
                    {
                        fileEntry.State = EntityState.Detached;
                    }
                }
                // Detach attachment nếu đã được track
                var attachmentEntry = _context.Entry(attachment);
                if (attachmentEntry.State != EntityState.Detached)
                {
                    attachmentEntry.State = EntityState.Detached;
                }
            }
        }

        // Detach các navigation properties khác nếu cần
        if (entity.CreatedByUser != null)
        {
            var userEntry = _context.Entry(entity.CreatedByUser);
            if (userEntry.State != EntityState.Detached)
            {
                userEntry.State = EntityState.Detached;
            }
        }

        if (entity.Apartment != null)
        {
            var apartmentEntry = _context.Entry(entity.Apartment);
            if (apartmentEntry.State != EntityState.Detached)
            {
                apartmentEntry.State = EntityState.Detached;
            }
        }

        // Chỉ update ticket entity, không include navigation properties
        var existingTicket = await _context.Tickets.FindAsync(entity.TicketId);
        if (existingTicket != null)
        {
            // Chỉ update các trường scalar properties, không touch navigation properties
            existingTicket.UpdatedAt = entity.UpdatedAt;
            existingTicket.HasInvoice = entity.HasInvoice;
            existingTicket.Status = entity.Status;
            existingTicket.Description = entity.Description;
            existingTicket.Priority = entity.Priority;
            existingTicket.ClosedAt = entity.ClosedAt;
            existingTicket.ExpectedCompletionAt = entity.ExpectedCompletionAt;
            existingTicket.Subject = entity.Subject;
            existingTicket.Category = entity.Category;
            existingTicket.Scope = entity.Scope;
            existingTicket.ApartmentId = entity.ApartmentId;
            existingTicket.CreatedByUserId = entity.CreatedByUserId;
        }
        else
        {
            // Nếu ticket chưa tồn tại, tạo mới nhưng detach navigation properties trước
            var ticketToAdd = new Ticket
            {
                TicketId = entity.TicketId,
                CreatedByUserId = entity.CreatedByUserId,
                Category = entity.Category,
                Priority = entity.Priority,
                Subject = entity.Subject,
                Description = entity.Description,
                Status = entity.Status,
                ExpectedCompletionAt = entity.ExpectedCompletionAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ClosedAt = entity.ClosedAt,
                Scope = entity.Scope,
                ApartmentId = entity.ApartmentId,
                HasInvoice = entity.HasInvoice
            };
            _context.Tickets.Add(ticketToAdd);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.Tickets.FirstOrDefaultAsync(x => x.TicketId == id);
        if (item != null)
        {
            _context.Tickets.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public IQueryable<TicketComment> QueryComments()
    {
        return _context.TicketComments.AsQueryable();
    }

    public async Task<TicketComment> AddCommentAsync(TicketComment comment)
    {
        _context.TicketComments.Add(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    // Ticket Attachments
    public IQueryable<TicketAttachment> QueryAttachments()
    {
        return _context.TicketAttachments.AsQueryable();
    }

    public async Task<TicketAttachment?> GetAttachmentByIdAsync(Guid attachmentId)
    {
        // Load attachment mà không select UploadedBy
        var attachmentData = await _context.TicketAttachments
            .AsNoTracking()
            .Where(a => a.AttachmentId == attachmentId)
            .Select(a => new
            {
                a.AttachmentId,
                a.TicketId,
                a.FileId,
                a.Note,
                a.UploadedAt
            })
            .FirstOrDefaultAsync();

        if (attachmentData == null)
        {
            return null;
        }

        var attachment = new TicketAttachment
        {
            AttachmentId = attachmentData.AttachmentId,
            TicketId = attachmentData.TicketId,
            FileId = attachmentData.FileId,
            UploadedBy = null, // Không load từ DB
            Note = attachmentData.Note,
            UploadedAt = attachmentData.UploadedAt
        };

        // Load File riêng biệt
        attachment.File = await _context.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FileId == attachmentData.FileId);

        return attachment;
    }

    public async Task<List<TicketAttachment>> GetAttachmentsByTicketIdAsync(Guid ticketId)
    {
        // Load attachments mà không select UploadedBy
        var attachmentData = await _context.TicketAttachments
            .AsNoTracking()
            .Where(x => x.TicketId == ticketId)
            .Select(a => new
            {
                a.AttachmentId,
                a.TicketId,
                a.FileId,
                a.Note,
                a.UploadedAt
            })
            .OrderBy(x => x.UploadedAt)
            .ToListAsync();

        var attachments = new List<TicketAttachment>();
        foreach (var data in attachmentData)
        {
            var attachment = new TicketAttachment
            {
                AttachmentId = data.AttachmentId,
                TicketId = data.TicketId,
                FileId = data.FileId,
                UploadedBy = null, // Không load từ DB
                Note = data.Note,
                UploadedAt = data.UploadedAt
            };

            // Load File riêng biệt
            attachment.File = await _context.Files
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FileId == data.FileId);

            attachments.Add(attachment);
        }

        return attachments;
    }

    public async Task<TicketAttachment> AddAttachmentAsync(TicketAttachment attachment)
    {
        _context.TicketAttachments.Add(attachment);
        await _context.SaveChangesAsync();
        return attachment;
    }

    public async Task DeleteAttachmentAsync(Guid attachmentId)
    {
        var item = await _context.TicketAttachments.FirstOrDefaultAsync(x => x.AttachmentId == attachmentId);
        if (item != null)
        {
            _context.TicketAttachments.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    // Files
    public IQueryable<Models.File> QueryFiles()
    {
        return _context.Files.AsQueryable();
    }

    public async Task<Models.File?> GetFileByIdAsync(Guid fileId)
    {
        return await _context.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FileId == fileId);
    }

    public async Task<Models.File> AddFileAsync(Models.File file)
    {
        _context.Files.Add(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task DeleteFileAsync(Guid fileId)
    {
        var item = await _context.Files.FirstOrDefaultAsync(x => x.FileId == fileId);
        if (item != null)
        {
            _context.Files.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    // Invoice Details
    public IQueryable<InvoiceDetail> QueryInvoiceDetails()
    {
        return _context.InvoiceDetails.AsQueryable();
    }

    // Voucher Items
    public IQueryable<VoucherItem> QueryVoucherItems()
    {
        return _context.VoucherItems.AsQueryable();
    }
}


