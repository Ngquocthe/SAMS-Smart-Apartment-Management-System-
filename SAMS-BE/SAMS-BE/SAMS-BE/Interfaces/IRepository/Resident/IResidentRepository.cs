using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository.Resident
{
    public interface IResidentRepository
    {
        Task CreateResidentAsync(string schema, ResidentProfile residentProfile, CancellationToken ct);

        Task<bool> ExistsEmailAsync(string schema, string email, CancellationToken ct);

        Task<bool> ExistsPhoneAsync(string schema, string phone, CancellationToken ct);

        Task<bool> ExistsIdNumberAsync(string schema, string idNumber, CancellationToken ct);

        Task<bool> ApartmentExistsAsync(string schema, Guid apartmentId, CancellationToken ct);

        Task<(bool hasOwner, bool hasPrimary)> GetApartmentOwnershipStatusAsync(string schema, Guid apartmentId, CancellationToken ct);

        Task<ResidentProfile?> GetResidentByIdNumberAsync(string schema, string idNumber, CancellationToken ct);

        Task<ResidentProfile?> GetByIdAsync(Guid residentId, CancellationToken ct = default);
        Task<ResidentProfile?> GetWithApartmentsAsync(Guid residentId, CancellationToken ct = default);
        Task<PagedResult<ResidentProfile>> GetResidentsPagedAsync(
            string schema,
            int pageNumber,
            int pageSize,
            Guid? apartmentId = null,
            string? q = null,
            string? sortBy = null,
            string? sortDir = null,
            CancellationToken ct = default);

        Task<IEnumerable<ResidentProfile>> GetByApartmentIdAsync(Guid apartmentId, CancellationToken ct = default);
    }
}
