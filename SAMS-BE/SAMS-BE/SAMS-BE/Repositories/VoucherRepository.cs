using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories
{
    public class VoucherRepository : Interfaces.IRepository.IVoucherRepository
    {
        private readonly BuildingManagementContext _context;

        public VoucherRepository(BuildingManagementContext context)
        {
            _context = context;
        }

        public async Task<Voucher> CreateAsync(Voucher voucher)
        {
            await _context.Vouchers.AddAsync(voucher);
            await _context.SaveChangesAsync();
            return voucher;
        }

        public async Task<Voucher?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.Vouchers
               .SingleOrDefaultAsync(v => v.VoucherId == id);
        }

        public async Task<Voucher?> GetByIdAsync(Guid id)
        {
            return await _context.Vouchers
           .AsNoTracking()
          .Include(v => v.VoucherItems)
                          .ThenInclude(i => i.ServiceType)
               .SingleOrDefaultAsync(v => v.VoucherId == id);
        }

        public async Task<Voucher?> GetByIdWithItemsAsync(Guid id)
        {
            return await _context.Vouchers
                .Include(v => v.VoucherItems)
                    .ThenInclude(i => i.ServiceType)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VoucherId == id);
        }

        public async Task<(IReadOnlyList<Voucher> Items, int Total)> ListAsync(VoucherListQueryDto query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
            var sortBy = (query.SortBy ?? "Date").Trim().ToLowerInvariant();
            var sortDir = (query.SortDir ?? "desc").Trim().ToLowerInvariant() == "desc";

            IQueryable<Voucher> vouchers = _context.Vouchers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var k = query.Search.Trim();
                vouchers = vouchers.Where(v => v.VoucherNumber.Contains(k));
            }

            // Type filter kh�ng c�n c?n thi?t v� ch? c� PAYMENT
            // Nh?ng v?n gi? ?? t??ng th�ch ng??c v?i frontend
            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                vouchers = vouchers.Where(v => v.Type == query.Type);
            }
            else
            {
                // Force ch? l?y PAYMENT
                vouchers = vouchers.Where(v => v.Type == VoucherHelper.TYPE_PAYMENT);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                vouchers = vouchers.Where(v => v.Status == query.Status);
            }

            if (query.DateFrom.HasValue)
            {
                vouchers = vouchers.Where(v => v.Date >= query.DateFrom.Value);
            }

            if (query.DateTo.HasValue)
            {
                vouchers = vouchers.Where(v => v.Date <= query.DateTo.Value);
            }

            var total = await vouchers.CountAsync();

            vouchers = sortBy switch
            {
                "vouchernumber" => sortDir ? vouchers.OrderByDescending(v => v.VoucherNumber) : vouchers.OrderBy(v => v.VoucherNumber),
                "date" => sortDir ? vouchers.OrderByDescending(v => v.Date) : vouchers.OrderBy(v => v.Date),
                "totalamount" => sortDir ? vouchers.OrderByDescending(v => v.TotalAmount) : vouchers.OrderBy(v => v.TotalAmount),
                _ => sortDir ? vouchers.OrderByDescending(v => v.Date) : vouchers.OrderBy(v => v.Date),
            };

            var items = await vouchers.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        public async Task<Voucher> UpdateAsync(Voucher entity)
        {
            _context.Vouchers.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(Guid id)
        {
            var voucher = await _context.Vouchers
                        .Include(v => v.VoucherItems)
                  .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher != null)
            {
                _context.Vouchers.Remove(voucher);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Voucher> AddVoucherAsync(Voucher voucher)
        {
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
            return voucher;
        }

        public async Task<VoucherItem> AddVoucherItemAsync(VoucherItem item)
        {
            _context.VoucherItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<List<(Guid VoucherId, string VoucherNumber, decimal Amount)>> GetVouchersByTicketAsync(Guid ticketId)
        {
            return await _context.Vouchers
                .Where(v => v.TicketId == ticketId)
                .Select(v => new ValueTuple<Guid, string, decimal>(
                    v.VoucherId,
                    v.VoucherNumber,
                    v.TotalAmount
                ))
                .ToListAsync();
        }

        public async Task<Guid?> GetVoucherIdByTicketAsync(Guid ticketId)
        {
            return await _context.Vouchers
                .Where(v => v.TicketId == ticketId)
                .Select(v => (Guid?)v.VoucherId)
                .FirstOrDefaultAsync();
        }

        public async Task<Guid?> GetVoucherIdByHistoryAsync(Guid historyId)
        {
            return await _context.Vouchers
                .Where(v => v.HistoryId == historyId)
                .Select(v => (Guid?)v.VoucherId)
                .FirstOrDefaultAsync();
        }

        public async Task<VoucherItem?> GetItemByIdAsync(Guid itemId)
        {
            return await _context.VoucherItems
                .Include(i => i.Voucher)
                .Include(i => i.ServiceType)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.VoucherItemsId == itemId);
        }

        public async Task<Voucher> UpdateVoucherAsync(Voucher voucher)
        {
            _context.Vouchers.Update(voucher);
            await _context.SaveChangesAsync();
            return voucher;
        }

        public async Task<VoucherItem> UpdateVoucherItemAsync(VoucherItem item)
        {
            _context.VoucherItems.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }
    }
}


