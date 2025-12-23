using SAMS_BE.DTOs.Request.Staff;
using SAMS_BE.DTOs.Response.Staff;

namespace SAMS_BE.Interfaces.IRepository.GlobalAdmin
{
    public interface IStaffRepository
    {
        Task<(List<StaffListItemDto> Items, int Total)> SearchAsync(string schema, StaffQuery query, CancellationToken ct);
        Task<StaffDetailDto?> GetDetailAsync(string schema, Guid staffCode, CancellationToken ct);
        Task<bool> UpdateAsync(string schema, Guid staffCode, StaffUpdateDto dto, string avatarUrl, string cardUrl, CancellationToken ct);
        Task<bool> ActivateAsync(string schema, Guid staffCode, CancellationToken ct);
        Task<bool> DeactivateAsync(string schema, Guid staffCode, DateTime? date, CancellationToken ct);
        Task<bool> ExistsTaxCodeAsync(string schema, string? taxCode, CancellationToken ct);
        Task<bool> ExistsSocialInsuranceNoAsync(string schema, string? socialInsuranceNo, CancellationToken ct);
    }
}
