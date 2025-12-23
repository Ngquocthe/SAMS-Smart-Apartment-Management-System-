using SAMS_BE.Infrastructure.Persistence.Global.Models;
namespace SAMS_BE.Interfaces.IRepository.GlobalAdmin
{
    public interface IAdminUserRepository
    {
        Task<user_registry?> GetByIdAsync(Guid id);

        Task CreateUserRegistryAsync(user_registry entity, CancellationToken ct);

        Task<bool> ExistsUsernameAsync(string username, CancellationToken ct);
        Task<bool> ExistsEmailAsync(string email, CancellationToken ct);
    }
}
