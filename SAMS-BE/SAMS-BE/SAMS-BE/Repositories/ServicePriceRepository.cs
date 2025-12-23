using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories
{
    public class ServicePriceRepository : IServicePriceRepository
    {
        private readonly BuildingManagementContext _context;
        public ServicePriceRepository(BuildingManagementContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<ServicePrice> Items, int Total)> ListAsync(Guid serviceTypeId, ServicePriceListQueryDto query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var size = query.PageSize < 1 ? 20 : query.PageSize;
            var sortBy = (query.SortBy ?? "EffectiveDate").Trim().ToLowerInvariant();
            var desc = (query.SortDir ?? "desc").Trim().ToLowerInvariant() == "desc";

            IQueryable<ServicePrice> q = _context.ServicePrices.AsNoTracking()
                .Include(st => st.ServiceType)
                .Include(st => st.CreatedByNavigation)
                .Include(st => st.ApprovedByNavigation)
                .Where(sp => sp.ServiceTypeId == serviceTypeId);

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var k = query.Q.Trim();
                q = q.Where(x =>
                    (x.ServiceType.Code.Contains(k) || x.ServiceType.Name.Contains(k)) ||
                    (x.Notes != null && x.Notes.Contains(k)));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
                q = q.Where(x => x.Status == query.Status);

            if (query.FromDate.HasValue)
                q = q.Where(x => x.EndDate == null || x.EndDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                q = q.Where(x => x.EffectiveDate <= query.ToDate.Value);

            var total = await q.CountAsync();

            q = sortBy switch
            {
                "unitprice" => desc ? q.OrderByDescending(x => x.UnitPrice) : q.OrderBy(x => x.UnitPrice),
                "enddate" => desc ? q.OrderByDescending(x => x.EndDate) : q.OrderBy(x => x.EndDate),
                "createdat" => desc ? q.OrderByDescending(x => x.CreatedAt) : q.OrderBy(x => x.CreatedAt),
                _ => desc ? q.OrderByDescending(x => x.EffectiveDate) : q.OrderBy(x => x.EffectiveDate),
            };

            var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
            return (items, total);
        }

        public Task<ServicePrice?> GetByIdAsync(Guid priceId)
            => _context.ServicePrices
                  .Include(x => x.ServiceType)
                  .Include(x => x.CreatedByNavigation)
                  .Include(x => x.ApprovedByNavigation)
                  .FirstOrDefaultAsync(x => x.ServicePrices == priceId);

        public Task<ServicePrice?> GetOpenEndedAsync(Guid serviceTypeId)
            => _context.ServicePrices.FirstOrDefaultAsync(x => x.ServiceTypeId == serviceTypeId && x.EndDate == null);

        public Task<bool> AnyOverlapAsync(Guid serviceTypeId, DateOnly start, DateOnly? end, Guid? excludeId = null)
            => _context.ServicePrices.AnyAsync(p =>
                p.ServiceTypeId == serviceTypeId &&
                (excludeId == null || p.ServicePrices != excludeId.Value) &&
                (end == null || p.EffectiveDate <= end.Value) &&
                (p.EndDate == null || p.EndDate >= start));

        public async Task AddAsync(ServicePrice entity)
        {
            await _context.ServicePrices.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(ServicePrice entity) => _context.SaveChangesAsync();

        // ✅ NEW: Get current active price for a service on a specific date
        public async Task<ServicePrice?> GetCurrentPriceAsync(Guid serviceTypeId, DateOnly effectiveDate)
        {
            return await _context.ServicePrices
             .AsNoTracking()
      .Where(p => p.ServiceTypeId == serviceTypeId
       && p.Status == "APPROVED"
   && p.EffectiveDate <= effectiveDate
         && (p.EndDate == null || p.EndDate >= effectiveDate))
        .OrderByDescending(p => p.EffectiveDate)
        .FirstOrDefaultAsync();
        }

        // ✅ NEW: Get latest price for a service (newest by EffectiveDate)
        public async Task<ServicePrice?> GetLatestPriceAsync(Guid serviceTypeId)
        {
            return await _context.ServicePrices
       .AsNoTracking()
        .Where(p => p.ServiceTypeId == serviceTypeId && p.Status == "APPROVED")
       .OrderByDescending(p => p.EffectiveDate)
          .FirstOrDefaultAsync();
        }
    }
}
