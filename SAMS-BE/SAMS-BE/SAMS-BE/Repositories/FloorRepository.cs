using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories
{
    public class FloorRepository : IFloorRepository
    {
        private readonly BuildingManagementContext _context;

        public FloorRepository(BuildingManagementContext context)
        {
            _context = context;
        }

        public async Task<List<Floor>> CreateFloorsAsync(List<Floor> floors)
        {
            try
            {
                await _context.Floors.AddRangeAsync(floors);
                await _context.SaveChangesAsync();
                return floors;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo các tầng: {ex.Message}", ex);
            }
        }

        public async Task<Floor?> GetByFloorIdAsync(string floorId)
        {
            if (Guid.TryParse(floorId, out Guid guidFloorId))
            {
                return await _context.Floors
                    .Include(f => f.Apartments)
                    .FirstOrDefaultAsync(f => f.FloorId == guidFloorId);
            }
            return null;
        }

        public async Task<List<Floor>> GetAllFloorsAsync()
        {
            return await _context.Floors
                .Include(f => f.Apartments)
                .OrderBy(f => f.FloorNumber)
                .ToListAsync();
        }

        public async Task<bool> FloorExistsAsync(string floorId)
        {
            if (Guid.TryParse(floorId, out Guid guidFloorId))
            {
                return await _context.Floors.AnyAsync(f => f.FloorId == guidFloorId);
            }
            return false;
        }

        public async Task<Floor> UpdateFloorAsync(Floor floor)
        {
            try
            {
                _context.Floors.Update(floor);
                await _context.SaveChangesAsync();
                return floor;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật tầng: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFloorAsync(Guid floorId)
        {
            try
            {
                var floor = await _context.Floors.FindAsync(floorId);
                if (floor == null)
                    return false;

                _context.Floors.Remove(floor);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa tầng: {ex.Message}", ex);
            }
        }

        public async Task<bool> FloorNumberExistsAsync(int floorNumber)
        {
            return await _context.Floors.AnyAsync(f => f.FloorNumber == floorNumber);
        }

        public async Task<Floor> CreateSingleFloorAsync(Floor floor)
        {
            try
            {
                await _context.Floors.AddAsync(floor);
                await _context.SaveChangesAsync();
                return floor;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo tầng: {ex.Message}", ex);
            }
        }

        public async Task<bool> FloorHasApartmentsAsync(Guid floorId)
        {
            return await _context.Apartments.AnyAsync(a => a.FloorId == floorId);
        }
    }
}