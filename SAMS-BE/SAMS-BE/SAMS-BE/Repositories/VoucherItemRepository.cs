using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories
{
    public class VoucherItemRepository : IVoucherItemRepository
    {
        private readonly BuildingManagementContext _context;

        public VoucherItemRepository(BuildingManagementContext context)
        {
            _context = context;
        }

        public async Task<VoucherItem> CreateAsync(VoucherItem item)
        {
            await _context.VoucherItems.AddAsync(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<VoucherItem?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.VoucherItems
            .Include(vi => vi.Voucher)
                      .Include(vi => vi.ServiceType)
               .Include(vi => vi.Apartment)
               .SingleOrDefaultAsync(vi => vi.VoucherItemsId == id);
        }

        public async Task<VoucherItem?> GetByIdAsync(Guid id)
        {
            return await _context.VoucherItems
               .AsNoTracking()
          .Include(vi => vi.Voucher)
        .Include(vi => vi.ServiceType)
           .Include(vi => vi.Apartment)
           .SingleOrDefaultAsync(vi => vi.VoucherItemsId == id);
        }

        public async Task<(IReadOnlyList<VoucherItem> Items, int Total)> ListAsync(VoucherItemListQueryDto query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
            var sortBy = (query.SortBy ?? "CreatedAt").Trim().ToLowerInvariant();
            var sortDir = (query.SortDir ?? "desc").Trim().ToLowerInvariant() == "desc";

            IQueryable<VoucherItem> items = _context.VoucherItems
              .AsNoTracking()
                 .Include(vi => vi.Voucher)
             .Include(vi => vi.ServiceType)
                     .Include(vi => vi.Apartment);

            if (query.VoucherId.HasValue)
            {
                items = items.Where(vi => vi.VoucherId == query.VoucherId.Value);
            }

            if (query.ServiceTypeId.HasValue)
            {
                items = items.Where(vi => vi.ServiceTypeId == query.ServiceTypeId.Value);
            }

            if (query.ApartmentId.HasValue)
            {
                items = items.Where(vi => vi.ApartmentId == query.ApartmentId.Value);
            }

            if (query.TicketId.HasValue)
            {
                items = items.Where(vi => vi.Voucher.TicketId == query.TicketId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var k = query.Search.Trim();
                items = items.Where(vi =>
                vi.Description != null && vi.Description.Contains(k));
            }

            var total = await items.CountAsync();

            items = sortBy switch
            {
                "servicetypename" => sortDir
                  ? items.OrderByDescending(vi => vi.ServiceType!.Name)
           : items.OrderBy(vi => vi.ServiceType!.Name),
                "amount" => sortDir
                 ? items.OrderByDescending(vi => vi.Amount)
                       : items.OrderBy(vi => vi.Amount),
                "createdat" => sortDir
            ? items.OrderByDescending(vi => vi.CreatedAt)
                   : items.OrderBy(vi => vi.CreatedAt),
                _ => sortDir
                  ? items.OrderByDescending(vi => vi.CreatedAt)
                : items.OrderBy(vi => vi.CreatedAt),
            };

            var result = await items
                    .Skip((page - 1) * pageSize)
            .Take(pageSize)
                .ToListAsync();

            return (result, total);
        }

        public async Task<VoucherItem> UpdateAsync(VoucherItem entity)
        {
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(VoucherItem entity)
        {
            _context.VoucherItems.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> VoucherExistsAsync(Guid voucherId)
        {
            return await _context.Vouchers.AnyAsync(v => v.VoucherId == voucherId);
        }

        public async Task<bool> ServiceTypeExistsAsync(Guid serviceTypeId)
        {
            return await _context.ServiceTypes.AnyAsync(st => st.ServiceTypeId == serviceTypeId);
        }

        public async Task<List<VoucherItem>> GetByVoucherIdAsync(Guid voucherId)
        {
            return await _context.VoucherItems
            .AsNoTracking()
          .Include(vi => vi.ServiceType)
          .Include(vi => vi.Apartment)
           .Where(vi => vi.VoucherId == voucherId)
           .ToListAsync();
        }
    }
}
