using Microsoft.EntityFrameworkCore;
using SAMS_BE.Infrastructure.Persistence.Global;
using SAMS_BE.Infrastructure.Persistence.Global.Models;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories.GlobalAdmin
{
    public class AdminUserRepository : IAdminUserRepository
    {

        private readonly GlobalDirectoryContext _db;

        public AdminUserRepository(GlobalDirectoryContext db)
        {
            _db = db;
        }

        public async Task CreateUserRegistryAsync(user_registry entity, CancellationToken ct)
        {
            _db.user_registries.Add(entity);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsEmailAsync(string email, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            email = email.Trim();

            return await _db.user_registries
                .AsNoTracking()
                .AnyAsync(x => x.email == email, ct);
        }

        public async Task<bool> ExistsUsernameAsync(string username, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            username = username.Trim();

            return await _db.user_registries
                .AsNoTracking()
                .AnyAsync(x => x.username == username, ct);
        }

        public async Task<user_registry?> GetByIdAsync(Guid id)
        {
            return await _db.user_registries.AsNoTracking().FirstOrDefaultAsync(x => x.keycloak_user_id == id);
        }
    }
}
