using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs.Request.Building;
using SAMS_BE.DTOs.Response.Building;
using SAMS_BE.Infrastructure.Persistence.Global;
using SAMS_BE.Infrastructure.Persistence.Global.Models;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;

namespace SAMS_BE.Repositories.GlobalAdmin
{
    public class BuildingRepository : IBuildingRepository
    {
        private readonly GlobalDirectoryContext _db;
        private readonly IMapper _mapper;

        public BuildingRepository(GlobalDirectoryContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<bool> checkExistBuilding(CreateBuildingRequest buildingDto)
        {
            return await _db.buildings.AnyAsync(b => b.code == buildingDto.Code);
        }

        public async Task<IReadOnlyList<BuildingDto>> GetAllAsync(CancellationToken ct)
        {
            return await _db.Set<building>()
            .AsNoTracking()
                .Where(x => x.status == 1) // Chỉ lấy buildings đang active
                .OrderBy(x => x.building_name)
                .ProjectTo<BuildingDto>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<BuildingDto>> GetAllIncludingInactiveAsync(CancellationToken ct)
        {
            return await _db.Set<building>()
            .AsNoTracking()
                .OrderBy(x => x.building_name)
                .ProjectTo<BuildingDto>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<BuildingListDropdownDto>> GetAllForDropDownAsync(CancellationToken ct)
        {
            return await _db.Set<building>()
                 .AsNoTracking()
                 .Where(b => b.status == 1)
                 .OrderBy(x => x.building_name)
                 .Select(b => new BuildingListDropdownDto
                 {
                     Id = b.id,
                     BuildingName = b.building_name,
                     SchemaName = b.schema_name
                 })
                 .ToListAsync(ct);

        }

        public async Task<building?> GetBuildingBySchema(string schema)
        {
            return await _db.buildings
                .FirstOrDefaultAsync(b => b.schema_name.Equals(schema));
        }


        public async Task SaveBuilding(building building, CancellationToken ct)
        {
            await _db.buildings.AddAsync(building, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<building?> GetBuildingById(Guid id, CancellationToken ct)
        {
            return await _db.buildings
                .FirstOrDefaultAsync(b => b.id == id, ct);
        }

        public async Task UpdateBuilding(building building, CancellationToken ct)
        {
            _db.buildings.Update(building);
            await _db.SaveChangesAsync(ct);
        }
    }
}
