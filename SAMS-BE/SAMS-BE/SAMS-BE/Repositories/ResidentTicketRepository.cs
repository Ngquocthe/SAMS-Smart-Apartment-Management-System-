using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using SAMS_BE.DTOs;

namespace SAMS_BE.Repositories;

public class ResidentTicketRepository : IResidentTicketRepository
{
    private readonly BuildingManagementContext _context;

    public ResidentTicketRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    public IQueryable<Ticket> Query()
    {
        return _context.Tickets
            .Include(t => t.CreatedByUser)
            .Include(t => t.Apartment)
            .AsQueryable();
    }

    public async Task<Ticket?> GetByIdAsync(Guid id)
    {
        // Load ticket cơ bản trước
        var ticket = await _context.Tickets
            .Include(t => t.CreatedByUser)
            .Include(t => t.Apartment)
            .FirstOrDefaultAsync(t => t.TicketId == id);

        if (ticket != null)
        {
            // Load attachments riêng biệt để tránh lỗi casting
            // KHÔNG select UploadedBy vì trong DB là varchar, không phải uniqueidentifier
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

            // Load comments riêng biệt
            var comments = await _context.TicketComments
                .Where(c => c.TicketId == id)
                .OrderBy(c => c.CommentTime)
                .ToListAsync();

            ticket.TicketComments = comments;


            // Load vehicle nếu là ticket đăng ký xe
            if ((ticket.Category == "Đăng ký xe" || ticket.Category == "VehicleRegistration") && ticket.VehicleId.HasValue)
            {
                ticket.Vehicle = await _context.Vehicles
                    .Include(v => v.VehicleType)
                    .FirstOrDefaultAsync(v => v.VehicleId == ticket.VehicleId.Value);
            }
        }

        return ticket;
    }

    public async Task<Ticket> AddAsync(Ticket entity)
    {
        await _context.Tickets.AddAsync(entity);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(entity.TicketId) ?? entity;
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

    public IQueryable<TicketComment> QueryComments()
    {
        return _context.TicketComments.AsQueryable();
    }

    public async Task<TicketComment> AddCommentAsync(TicketComment comment)
    {
        await _context.TicketComments.AddAsync(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    public IQueryable<TicketAttachment> QueryAttachments()
    {
        // Không dùng Include để tránh lỗi casting
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

        // Load File và Ticket riêng biệt
        attachment.File = await _context.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FileId == attachmentData.FileId);

        attachment.Ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TicketId == attachmentData.TicketId);

        return attachment;
    }

    public async Task<TicketAttachment> AddAttachmentAsync(TicketAttachment attachment)
    {
        await _context.TicketAttachments.AddAsync(attachment);
        await _context.SaveChangesAsync();
        return attachment;
    }

    public async Task DeleteAttachmentAsync(Guid attachmentId)
    {
        var attachment = await _context.TicketAttachments.FindAsync(attachmentId);
        if (attachment != null)
        {
            _context.TicketAttachments.Remove(attachment);
            await _context.SaveChangesAsync();
        }
    }

    public IQueryable<Models.File> QueryFiles()
    {
        return _context.Files.AsQueryable();
    }

    public async Task<Models.File?> GetFileByIdAsync(Guid fileId)
    {
        return await _context.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FileId == fileId);
    }

    public async Task<Models.File> AddFileAsync(Models.File file)
    {
        await _context.Files.AddAsync(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task<List<ResidentTicketInvoiceDto>> GetInvoicesByTicketIdAsync(Guid ticketId)
    {
        return await _context.Invoices
            .AsNoTracking()
            .Where(i => i.TicketId == ticketId)
            .Select(i => new ResidentTicketInvoiceDto
            {
                InvoiceId = i.InvoiceId,
                InvoiceNo = i.InvoiceNo,
                Status = i.Status,
                TotalAmount = i.TotalAmount,
                DueDate = i.DueDate
            })
            .ToListAsync();
    }

    public async Task<Vehicle?> GetVehicleByLicensePlateAsync(string licensePlate)
    {
        return await _context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.LicensePlate == licensePlate);
    }

    public async Task<Vehicle> AddVehicleAsync(Vehicle vehicle)
    {
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    public async Task<ResidentProfile?> GetResidentProfileByUserIdAsync(Guid userId)
    {
        return await _context.ResidentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(rp => rp.UserId == userId);
    }
}
