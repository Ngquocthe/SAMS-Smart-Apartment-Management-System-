using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly BuildingManagementContext _context;

    public InvoiceRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    // === Methods from feature/ticket-management ===

    public async Task<Invoice> AddInvoiceAsync(Invoice invoice)
    {
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
    {
        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task<Invoice?> GetByIdAsyncInvoice(Guid invoiceId)
    {
        return await _context.Invoices
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Service)
            .Include(i => i.Apartment)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
    }

    public async Task<InvoiceDetail> AddInvoiceDetailAsync(InvoiceDetail detail)
    {
        _context.InvoiceDetails.Add(detail);
        await _context.SaveChangesAsync();
        return detail;
    }

    public async Task<List<(Guid InvoiceId, string InvoiceNo, decimal Amount)>> GetInvoicesByTicketAsync(Guid ticketId)
    {
        return await _context.Invoices
            .Where(i => i.TicketId == ticketId)
            .Select(i => new ValueTuple<Guid, string, decimal>(
                i.InvoiceId,
                i.InvoiceNo,
                i.TotalAmount
            ))
            .ToListAsync();
    }

    public async Task<Guid?> GetInvoiceIdByTicketAsync(Guid ticketId)
    {
        return await _context.Invoices
            .Where(i => i.TicketId == ticketId)
            .Select(i => (Guid?)i.InvoiceId)
            .FirstOrDefaultAsync();
    }

    public async Task<InvoiceDetail?> GetDetailByIdAsync(Guid detailId)
    {
        return await _context.InvoiceDetails
            .Include(d => d.Service)
            .Include(d => d.Invoice)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.InvoiceDetailId == detailId);
    }

    public async Task<InvoiceDetail> UpdateInvoiceDetailAsync(InvoiceDetail detail)
    {
        _context.InvoiceDetails.Update(detail);
        await _context.SaveChangesAsync();
        return detail;
    }

    // === Methods from develop (core invoice management) ===

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        await _context.Invoices.AddAsync(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task<Invoice?> GetByIdForUpdateAsync(Guid id)
    {
        return await _context.Invoices
            .Include(x => x.Apartment)
            .SingleOrDefaultAsync(x => x.InvoiceId == id);
    }

    public async Task<Invoice?> GetByIdAsync(Guid id)
    {
        return await _context.Invoices
            .AsNoTracking()
            .Include(x => x.Apartment)
            .Include(x => x.InvoiceDetails)
            .ThenInclude(d => d.Service)
            .SingleOrDefaultAsync(x => x.InvoiceId == id);
    }

    public async Task<Invoice?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _context.Invoices
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Service)
            .Include(i => i.Apartment)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);
    }

    public async Task<(IReadOnlyList<Invoice> Items, int Total)> ListAsync(InvoiceListQueryDto query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        var sortBy = (query.SortBy ?? "DueDate").Trim().ToLowerInvariant();
        var sortDir = (query.SortDir ?? "desc").Trim().ToLowerInvariant() == "desc";

        IQueryable<Invoice> invoices = _context.Invoices.AsNoTracking().Include(i => i.Apartment);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var k = query.Search.Trim();
            invoices = invoices.Where(i => i.InvoiceNo.Contains(k));
        }

        if (query.ApartmentId.HasValue)
        {
            invoices = invoices.Where(i => i.ApartmentId == query.ApartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            invoices = invoices.Where(i => i.Status == query.Status);
        }

        if (query.DueFrom.HasValue)
        {
            invoices = invoices.Where(i => i.DueDate >= query.DueFrom.Value);
        }

        if (query.DueTo.HasValue)
        {
            invoices = invoices.Where(i => i.DueDate <= query.DueTo.Value);
        }

        var total = await invoices.CountAsync();

        invoices = sortBy switch
        {
            "invoiceno" => sortDir ? invoices.OrderByDescending(x => x.InvoiceNo) : invoices.OrderBy(x => x.InvoiceNo),
            "issuedate" => sortDir ? invoices.OrderByDescending(x => x.IssueDate) : invoices.OrderBy(x => x.IssueDate),
            "duedate" => sortDir ? invoices.OrderByDescending(x => x.DueDate) : invoices.OrderBy(x => x.DueDate),
            "totalamount" => sortDir ? invoices.OrderByDescending(x => x.TotalAmount) : invoices.OrderBy(x => x.TotalAmount),
            _ => sortDir ? invoices.OrderByDescending(x => x.DueDate) : invoices.OrderBy(x => x.DueDate),
        };

        var items = await invoices.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Invoice> UpdateAsync(Invoice entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Invoices.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Invoice?> CheckExistingMonthlyInvoiceAsync(Guid apartmentId, int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _context.Invoices
            .FirstOrDefaultAsync(i =>
                i.ApartmentId == apartmentId &&
                i.IssueDate >= startDate &&
                i.IssueDate <= endDate);
    }

    public async Task<Invoice> CreateWithDetailsAsync(Invoice invoice, List<InvoiceDetail> details)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            foreach (var detail in details)
            {
                detail.InvoiceId = invoice.InvoiceId;
                _context.InvoiceDetails.Add(detail);
            }
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return invoice;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Invoice>> GetOverdueInvoicesAsync(DateOnly today)
    {
        return await _context.Invoices
            .Where(i => i.Status == "ISSUED" && i.DueDate < today)
            .ToListAsync();
    }

    // ✅ NEW: Delete invoice (only DRAFT allowed)
    public async Task DeleteAsync(Guid id)
    {
        var invoice = await _context.Invoices
        .Include(i => i.InvoiceDetails)
       .FirstOrDefaultAsync(i => i.InvoiceId == id);

        if (invoice != null)
        {
            // Cascade delete will automatically delete InvoiceDetails
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
        }
    }

    // ✅ NEW: Get invoices by apartment and statuses with details
    public async Task<List<Invoice>> GetByApartmentAndStatusesWithDetailsAsync(Guid apartmentId, IEnumerable<string> statuses)
    {
        var statusSet = statuses.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return await _context.Invoices
            .AsNoTracking()
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Service)
            .Where(i => i.ApartmentId == apartmentId && statusSet.Contains(i.Status))
            .OrderByDescending(i => i.DueDate)
            .ToListAsync();
    }

    // ✅ NEW: Get invoices by user and statuses with details
    public async Task<List<Invoice>> GetByUserAndStatusesWithDetailsAsync(Guid userId, IEnumerable<string> statuses)
    {
        var statusSet = statuses.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Find resident by userId
        var residentIds = await _context.ResidentProfiles
            .Where(r => r.UserId == userId)
            .Select(r => r.ResidentId)
            .ToListAsync();

        if (!residentIds.Any()) return new List<Invoice>();

        // Find current/primary apartments of resident(s)
        var apartmentIds = await _context.ResidentApartments
            .Where(ra => residentIds.Contains(ra.ResidentId) && (ra.EndDate == null) && ra.IsPrimary)
            .Select(ra => ra.ApartmentId)
            .ToListAsync();

        if (!apartmentIds.Any()) return new List<Invoice>();

        return await _context.Invoices
            .AsNoTracking()
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Service)
            .Where(i => apartmentIds.Contains(i.ApartmentId) && statusSet.Contains(i.Status))
            .OrderByDescending(i => i.DueDate)
            .ToListAsync();
    }
}
