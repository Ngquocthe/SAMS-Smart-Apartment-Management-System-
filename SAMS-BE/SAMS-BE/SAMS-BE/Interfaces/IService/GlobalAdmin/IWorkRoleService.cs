using SAMS_BE.DTOs.Response.Staff;

namespace SAMS_BE.Interfaces.IService.GlobalAdmin
{
    public interface IWorkRoleService
    {
        Task<IReadOnlyList<WorkRoleOptionDto>> GetRolesAsync(
            string schema,
            string? search,
            bool includeInactive,
            CancellationToken ct);
    }
}
