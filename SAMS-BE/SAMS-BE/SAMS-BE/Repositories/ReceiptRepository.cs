using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories
{
    public class ReceiptRepository : IReceiptRepository
    {
        private readonly BuildingManagementContext _context;

        public ReceiptRepository(BuildingManagementContext context)
        {
            _context = context;
        }

        public async Task<Receipt> CreateAsync(Receipt receipt)
        {
            await _context.Receipts.AddAsync(receipt);
            await _context.SaveChangesAsync();
            return receipt;
        }

        public async Task<Receipt?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.Receipts
                .Include(r => r.Invoice)
              .Include(r => r.Method)
          .SingleOrDefaultAsync(r => r.ReceiptId == id);
        }

        public async Task<Receipt?> GetByIdAsync(Guid id)
        {
            return await _context.Receipts
            .AsNoTracking()
                .Include(r => r.Invoice)
              .ThenInclude(i => i.Apartment)
                    .Include(r => r.Method)
              .Include(r => r.CreatedByNavigation)
                            .SingleOrDefaultAsync(r => r.ReceiptId == id);
        }

        public async Task<Receipt?> GetByInvoiceIdAsync(Guid invoiceId)
        {
            return await _context.Receipts
               .AsNoTracking()
           .Include(r => r.Invoice)
          .Include(r => r.Method)
         .FirstOrDefaultAsync(r => r.InvoiceId == invoiceId);
        }

        public async Task<(IReadOnlyList<Receipt> Items, int Total)> ListAsync(ReceiptListQueryDto query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
            var sortBy = (query.SortBy ?? "ReceivedDate").Trim().ToLowerInvariant();
            var sortDir = (query.SortDir ?? "desc").Trim().ToLowerInvariant() == "desc";

            IQueryable<Receipt> receipts = _context.Receipts
                           .AsNoTracking()
                  .Include(r => r.Invoice)
                 .ThenInclude(i => i.Apartment)
             .Include(r => r.Method);


            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var k = query.Search.Trim();
                receipts = receipts.Where(r => r.ReceiptNo.Contains(k));
            }

            if (query.InvoiceId.HasValue)
            {
                receipts = receipts.Where(r => r.InvoiceId == query.InvoiceId.Value);
            }

            if (query.MethodId.HasValue)
            {
                receipts = receipts.Where(r => r.MethodId == query.MethodId.Value);
            }

            if (query.ReceivedFrom.HasValue)
            {
                receipts = receipts.Where(r => r.ReceivedDate >= query.ReceivedFrom.Value);
            }

            if (query.ReceivedTo.HasValue)
            {
                receipts = receipts.Where(r => r.ReceivedDate <= query.ReceivedTo.Value);
            }

            var total = await receipts.CountAsync();

            receipts = sortBy switch
            {
                "receiptno" => sortDir ? receipts.OrderByDescending(r => r.ReceiptNo) : receipts.OrderBy(r => r.ReceiptNo),
                "receiveddate" => sortDir ? receipts.OrderByDescending(r => r.ReceivedDate) : receipts.OrderBy(r => r.ReceivedDate),
                "amounttotal" => sortDir ? receipts.OrderByDescending(r => r.AmountTotal) : receipts.OrderBy(r => r.AmountTotal),
                _ => sortDir ? receipts.OrderByDescending(r => r.ReceivedDate) : receipts.OrderBy(r => r.ReceivedDate),
            };

            var items = await receipts.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, total);
        }

        public async Task<Receipt> UpdateAsync(Receipt entity)
        {
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(Guid id)
        {
            var receipt = await _context.Receipts.FindAsync(id);
            if (receipt != null)
            {
                _context.Receipts.Remove(receipt);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> InvoiceExistsAsync(Guid invoiceId)
        {
            return await _context.Invoices.AnyAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task<bool> PaymentMethodExistsAsync(Guid methodId)
        {
            return await _context.PaymentMethods.AnyAsync(pm => pm.PaymentMethodId == methodId);
        }
    }
}
