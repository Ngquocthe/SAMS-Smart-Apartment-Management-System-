using SAMS_BE.DTOs.Response.Staff;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Interfaces.IService.GlobalAdmin;
using SAMS_BE.Utils;
using System.Data.Common;

namespace SAMS_BE.Services.GlobalAdmin
{
    public sealed class WorkRoleService(IWorkRoleRepository repo) : IWorkRoleService
    {
        public async Task<IReadOnlyList<WorkRoleOptionDto>> GetRolesAsync(
            string schema, string? search, bool includeInactive, CancellationToken ct)
        {
            try
            {
                var items = await repo.GetRolesAsync(schema, search, includeInactive, ct);

                return items;
            }
            catch (DbException ex) when (DbExceptionUtils.IsMissingSchemaOrTable(ex))
            {
                return Array.Empty<WorkRoleOptionDto>();
            }
            catch (Exception ex) when (DbExceptionUtils.IsMissingSchemaOrTableDeep(ex))
            {
                return Array.Empty<WorkRoleOptionDto>();
            }
        }
    }
}
