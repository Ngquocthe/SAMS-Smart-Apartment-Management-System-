using SAMS_BE.DTOs.Request.Building;
using SAMS_BE.DTOs.Response.Building;
using SAMS_BE.Infrastructure.Persistence.Global.Models;

namespace SAMS_BE.Interfaces.IRepository.GlobalAdmin
{
    public interface IBuildingRepository
    {
        Task<IReadOnlyList<BuildingDto>> GetAllAsync(CancellationToken ct);

        Task<IReadOnlyList<BuildingDto>> GetAllIncludingInactiveAsync(CancellationToken ct);

        Task<IReadOnlyList<BuildingListDropdownDto>> GetAllForDropDownAsync(CancellationToken ct);

        Task SaveBuilding(building building, CancellationToken ct);

        Task<bool> checkExistBuilding(CreateBuildingRequest buildingDto);

        Task<building?> GetBuildingBySchema(string schema);

        Task<building?> GetBuildingById(Guid id, CancellationToken ct);

        Task UpdateBuilding(building building, CancellationToken ct);
    }
}
