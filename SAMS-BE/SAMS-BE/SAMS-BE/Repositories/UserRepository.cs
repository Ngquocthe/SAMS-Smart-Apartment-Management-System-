using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using SAMS_BE.Tenant;

namespace SAMS_BE.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly BuildingManagementContext _db;
        private readonly ITenantContextAccessor schemaSwitcher;

        public UserRepository(BuildingManagementContext db, ITenantContextAccessor schemaSwitcher)
        {
            _db = db;
            this.schemaSwitcher = schemaSwitcher;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == username);
        }

        public async Task<(List<User> items, int total)> LookupByUsernameAsync(string? username, int page, int pageSize)
        {
            // Chỉ lấy users có staff profile
            var q = from sp in _db.StaffProfiles.AsNoTracking()
                    join u in _db.Users.AsNoTracking() on sp.UserId equals u.UserId
                    select u;

            if (!string.IsNullOrWhiteSpace(username))
            {
                q = q.Where(u => u.Username.Contains(username));
            }

            var total = await q.CountAsync();
            var items = await q.OrderBy(u => u.Username)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();
            return (items, total);
        }

        public async Task<List<User>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var idList = ids?.Distinct().ToList();
            if (idList == null || idList.Count == 0)
                return new List<User>();

            return await _db.Users
                .AsNoTracking()
                .Where(u => idList.Contains(u.UserId))
                .ToListAsync();
        }

        public async Task CreateUserAsync(string schema, User user, CancellationToken ct)
        {
            schemaSwitcher.SetSchema(schema);

            await _db.Users.AddAsync(user, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteUserAsync(string schema, Guid userId, CancellationToken ct)
        {
            schemaSwitcher.SetSchema(schema);

            var entity = await _db.Users.FindAsync([userId], ct);
            if (entity is null) return;

            _db.Users.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsUsernameAsync(string schema, string username, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            schemaSwitcher.SetSchema(schema);
            username = username.Trim();

            return await _db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Username == username, ct);
        }

        public async Task<bool> ExistsEmailAsync(string schema, string email, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            schemaSwitcher.SetSchema(schema);
            email = email.Trim();

            return await _db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == email, ct);
        }

        public async Task<bool> ExistsPhoneAsync(string schema, string phone, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            schemaSwitcher.SetSchema(schema);
            phone = phone.Trim();

            return await _db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Phone == phone, ct);
        }
    }
}
