using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByUsernameAsync(string username);
        Task<(List<User> items, int total)> LookupByUsernameAsync(string? username, int page, int pageSize);
        Task<List<User>> GetByIdsAsync(IEnumerable<Guid> ids);

        Task CreateUserAsync(string schema, User user, CancellationToken ct);

        Task DeleteUserAsync(string schema, Guid userId, CancellationToken ct);

        Task<bool> ExistsUsernameAsync(string schema, string username, CancellationToken ct);
        Task<bool> ExistsEmailAsync(string schema, string email, CancellationToken ct);
        Task<bool> ExistsPhoneAsync(string schema, string phone, CancellationToken ct);
    }
}
