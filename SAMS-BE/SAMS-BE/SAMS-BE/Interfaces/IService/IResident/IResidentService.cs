using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Request.Resident;

namespace SAMS_BE.Interfaces.IService.IResident
{
    public interface IResidentService
    {
        Task<Guid> CreateResidentAsync(CreateResidentRequest dto, CancellationToken ct);
        
        Task<Guid> AddExistingResidentToBuildingAsync(AddExistingResidentRequest dto, CancellationToken ct);

        Task<DTOs.Response.Resident.ResidentDto?> GetResidentByIdAsync(Guid residentId, CancellationToken ct = default);

        Task<IEnumerable<DTOs.Response.Resident.ResidentDto>> GetResidentsByApartmentIdAsync(Guid apartmentId, CancellationToken ct = default);

        Task<PagedResult<DTOs.Response.Resident.ResidentDto>> GetResidentsPagedAsync(
            string schema,
            int pageNumber,
            int pageSize,
            Guid? apartmentId = null,
            string? q = null,
            string? sortBy = null,
            string? sortDir = null,
            CancellationToken ct = default);
    }
}
