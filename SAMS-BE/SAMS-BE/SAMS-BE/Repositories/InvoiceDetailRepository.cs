using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories
{
    public class InvoiceDetailRepository : IInvoiceDetailRepository
    {
        private readonly BuildingManagementContext _context;

        public InvoiceDetailRepository(BuildingManagementContext context)
        {
            _context = context;
        }

        public async Task<InvoiceDetail> CreateAsync(InvoiceDetail detail)
        {
            await _context.InvoiceDetails.AddAsync(detail);
            await _context.SaveChangesAsync();
            return detail;
        }

        public async Task DeleteAsync(InvoiceDetail entity)
        {
            _context.InvoiceDetails.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<InvoiceDetail?> GetByIdAsync(Guid id)
        {
            return await _context.InvoiceDetails
             .AsNoTracking()
              .Include(d => d.Service)
        .Include(d => d.Invoice)
           .FirstOrDefaultAsync(d => d.InvoiceDetailId == id);
        }

        public async Task<InvoiceDetail?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.InvoiceDetails
                      .Include(d => d.Service)
                 .Include(d => d.Invoice)
                    .FirstOrDefaultAsync(d => d.InvoiceDetailId == id);
        }

        public async Task<List<InvoiceDetail>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            return await _context.InvoiceDetails
              .AsNoTracking()
        .Include(d => d.Service)
     .Where(d => d.InvoiceId == invoiceId)
      .OrderBy(d => d.Service.Name)
         .ToListAsync();
        }

        public async Task<bool> InvoiceExistsAsync(Guid invoiceId)
        {
            return await _context.Invoices.AnyAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task<(IReadOnlyList<InvoiceDetail> Items, int Total)> ListAsync(InvoiceDetailListQueryDto query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
            var sortBy = (query.SortBy ?? "ServiceName").Trim().ToLowerInvariant();
            var sortDir = (query.SortDir ?? "asc").Trim().ToLowerInvariant() == "desc";

            IQueryable<InvoiceDetail> details = _context.InvoiceDetails
            .AsNoTracking()
               .Include(d => d.Service)
          .Include(d => d.Invoice);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var k = query.Search.Trim();
                details = details.Where(d =>
                  d.Service.Name.Contains(k) ||
                      d.Service.Code.Contains(k) ||
                   (d.Description != null && d.Description.Contains(k))
                 );
            }

            if (query.InvoiceId.HasValue)
            {
                details = details.Where(d => d.InvoiceId == query.InvoiceId.Value);
            }

            if (query.ServiceId.HasValue)
            {
                details = details.Where(d => d.ServiceId == query.ServiceId.Value);
            }

            var total = await details.CountAsync();

            details = sortBy switch
            {
                "servicename" => sortDir ? details.OrderByDescending(x => x.Service.Name) : details.OrderBy(x => x.Service.Name),
                "quantity" => sortDir ? details.OrderByDescending(x => x.Quantity) : details.OrderBy(x => x.Quantity),
                "amount" => sortDir ? details.OrderByDescending(x => x.Amount) : details.OrderBy(x => x.Amount),
                _ => sortDir ? details.OrderByDescending(x => x.Service.Name) : details.OrderBy(x => x.Service.Name),
            };

            var items = await details.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        public async Task<bool> ServiceExistsAsync(Guid serviceId)
        {
            return await _context.ServiceTypes
     .AnyAsync(s => s.ServiceTypeId == serviceId && s.IsActive == true && s.IsDelete != true);

        }

        public async Task<InvoiceDetail> UpdateAsync(InvoiceDetail entity)
        {
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}
