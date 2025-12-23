using SAMS_BE.DTOs.Request.Building;
using SAMS_BE.DTOs.Response.Building;
using SAMS_BE.Infrastructure.Persistence.Global.Models;

namespace SAMS_BE.Interfaces.IService.IBuilding
{
    public interface IBuildingService
    {
        Task<building> CreateTenantAsync(CreateBuildingRequest req, CancellationToken ct);

        Task<IReadOnlyList<BuildingDto>> GetAllAsync(CancellationToken ct);

        Task<IReadOnlyList<BuildingDto>> GetAllIncludingInactiveAsync(CancellationToken ct);

        Task<IReadOnlyList<BuildingListDropdownDto>> GetAllForDropDownAsync(CancellationToken ct);

        Task<building?> GetBuildingBySchema(string schema);

        Task<BuildingDto?> GetByIdAsync(Guid id, CancellationToken ct);

        Task<building?> UpdateBuildingAsync(Guid id, UpdateBuildingRequest req, CancellationToken ct);
    }
}
