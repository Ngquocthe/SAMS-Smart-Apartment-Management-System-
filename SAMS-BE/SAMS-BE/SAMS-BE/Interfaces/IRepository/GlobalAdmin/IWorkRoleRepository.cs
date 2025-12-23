using SAMS_BE.DTOs.Response.Staff;

namespace SAMS_BE.Interfaces.IRepository.GlobalAdmin
{
    public interface IWorkRoleRepository
    {
        Task<List<WorkRoleOptionDto>> GetRolesAsync(
            string schema,
            string? search,
            bool includeInactive,
            CancellationToken ct);
    }
}
