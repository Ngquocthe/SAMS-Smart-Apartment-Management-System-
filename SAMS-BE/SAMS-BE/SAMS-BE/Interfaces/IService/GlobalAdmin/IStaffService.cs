using SAMS_BE.DTOs.Request.Staff;
using SAMS_BE.DTOs.Response.Staff;

namespace SAMS_BE.Interfaces.IService.GlobalAdmin
{
    public interface IStaffService
    {
        Task<(List<StaffListItemDto> Items, int Total, int Page, int PageSize)>
            SearchAsync(string schema, StaffQuery query, CancellationToken ct);

        Task<StaffDetailDto?> GetDetailAsync(string schema, Guid staffCode, CancellationToken ct);
        Task<bool> UpdateAsync(string schema, Guid staffCode, StaffUpdateDto dto, CancellationToken ct);
        Task<bool> ActivateAsync(string schema, Guid staffCode, CancellationToken ct);
        Task<bool> DeactivateAsync(string schema, Guid staffCode, DateTime? date, CancellationToken ct);
        Task<Guid> CreateAsync(string schema, StaffCreateRequest dto, CancellationToken ct);
    }
}
