using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories
{
    public class ServiceTypeRepository : IServiceTypeRepository
    {
        private readonly BuildingManagementContext _context;
        public ServiceTypeRepository(BuildingManagementContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<ServiceType> Items, int Total)> ListAsync(ServiceTypeListQueryDto query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
            var sortBy = (query.SortBy ?? "Name").Trim();
            var sortDir = (query.SortDir ?? "asc").Trim().ToLowerInvariant();

            IQueryable<ServiceType> q = _context.ServiceTypes
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(x => x.Category);

            // Filter theo IsDelete: nếu NULL hoặc false thì hiển thị
            q = q.Where(x => x.IsDelete != true);

            if (query.IsActive.HasValue)
                q = q.Where(x => x.IsActive == query.IsActive.Value);

            if (query.CategoryId.HasValue)
            {
                q = q.Where(x => x.CategoryId == query.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var k = query.Q.Trim();
                q = q.Where(x => x.Code.Contains(k) || x.Name.Contains(k));
            }

            var total = await q.CountAsync();

            bool desc = sortDir == "desc";
            q = (sortBy.ToLowerInvariant()) switch
            {
                "code" => (desc ? q.OrderByDescending(x => x.Code) : q.OrderBy(x => x.Code)),
                "name" => (desc ? q.OrderByDescending(x => x.Name) : q.OrderBy(x => x.Name)),
                "category" => (desc ? q.OrderByDescending(x => x.Category.Name) : q.OrderBy(x => x.Category.Name)),
                "createdat" => (desc ? q.OrderByDescending(x => x.CreatedAt) : q.OrderBy(x => x.CreatedAt)),
                "updatedat" => (desc ? q.OrderByDescending(x => x.UpdatedAt) : q.OrderBy(x => x.UpdatedAt)),
                _ => (desc ? q.OrderByDescending(x => x.Name) : q.OrderBy(x => x.Name)),
            };

            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        public async Task<bool> CodeExistsAsync(string code)
        {
            return await _context.ServiceTypes
                .AsNoTracking()
                .AnyAsync(x => x.Code == code);
        }

        public async Task<ServiceType> CreateAsync(ServiceType entity)
        {
            await _context.ServiceTypes.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<ServiceType?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.ServiceTypes.Include(x => x.Category).SingleOrDefaultAsync(x => x.ServiceTypeId == id);
        }
        public async Task<ServiceType> UpdateAsync(ServiceType entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return entity;
        }

        // Get all services cần tính theo tháng:
        // - Các dịch vụ được tick "tính hàng tháng" (IsRecurring = true)
        // Logic kiểm tra điều kiện cụ thể (như số lượng xe, diện tích) nằm trong InvoiceService
        public async Task<List<ServiceType>> GetMonthlyRecurringServicesAsync()
        {
            return await _context.ServiceTypes
                .AsNoTracking()
                .Include(s => s.ServicePrices)
                .Where(s =>
                    (s.IsRecurring == true) && // Dịch vụ định kỳ
                    s.IsActive == true &&                               // Đang hoạt động
                    s.IsDelete != true)                                 // Không bị xóa
                .ToListAsync();
        }
    }
}
