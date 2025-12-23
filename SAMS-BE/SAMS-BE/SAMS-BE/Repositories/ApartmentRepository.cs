using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories
{
    public class ApartmentRepository : IApartmentRepository
    {
        private readonly BuildingManagementContext _context;

        public ApartmentRepository(BuildingManagementContext context)
        {
            _context = context;
        }

        public async Task<List<Apartment>> CreateApartmentsAsync(List<Apartment> apartments)
        {
            try
            {
                await _context.Apartments.AddRangeAsync(apartments);
                await _context.SaveChangesAsync();
                return apartments;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo apartments: {ex.Message}", ex);
            }
        }

        public async Task<List<Apartment>> GetApartmentsByFloorAsync(Guid floorId)
        {
            return await _context.Apartments
                .Include(a => a.Floor)
                .Include(a => a.ResidentApartments)
                    .ThenInclude(ra => ra.Resident)
                .Include(a => a.Vehicles)
                .Where(a => a.FloorId == floorId)
                .OrderBy(a => a.Number)
                .ToListAsync();
        }

        public async Task<List<Apartment>> GetApartmentsByFloorNumberAsync(int floorNumber)
        {
            return await _context.Apartments
                .Include(a => a.Floor)
                .Include(a => a.ResidentApartments)
                    .ThenInclude(ra => ra.Resident)
                .Include(a => a.Vehicles)
                .Where(a => a.Floor.FloorNumber == floorNumber)
                .OrderBy(a => a.Number)
                .ToListAsync();
        }

        public async Task<bool> FloorHasApartmentsAsync(Guid floorId)
        {
            return await _context.Apartments.AnyAsync(a => a.FloorId == floorId);
        }

        public async Task<bool> FloorHasApartmentsByNumberAsync(int floorNumber)
        {
            return await _context.Apartments
                .Include(a => a.Floor)
                .AnyAsync(a => a.Floor.FloorNumber == floorNumber);
        }

        public async Task<Apartment?> GetApartmentByIdAsync(Guid apartmentId)
        {
            return await _context.Apartments
                .Include(a => a.Floor)
                .Include(a => a.ResidentApartments)
                    .ThenInclude(ra => ra.Resident)
                .Include(a => a.Vehicles)
                .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId);
        }

        public async Task<Apartment?> GetApartmentByNumberAsync(string apartmentNumber)
        {
            return await _context.Apartments
                .Include(a => a.Floor)
                .Include(a => a.ResidentApartments)
                    .ThenInclude(ra => ra.Resident)
                .Include(a => a.Vehicles)
                .FirstOrDefaultAsync(a => a.Number == apartmentNumber);
        }

        public async Task<List<Apartment>> GetAllApartmentsAsync()
        {
            return await _context.Apartments
                .Include(a => a.Floor)
                .Include(a => a.ResidentApartments)
                    .ThenInclude(ra => ra.Resident)
                .Include(a => a.Vehicles)
                .OrderBy(a => a.Floor.FloorNumber)
                .ThenBy(a => a.Number)
                .ToListAsync();
        }

        public async Task<Apartment> UpdateApartmentAsync(Apartment apartment)
        {
            try
            {
                _context.Apartments.Update(apartment);
                await _context.SaveChangesAsync();
                return apartment;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật apartment: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteApartmentAsync(Guid apartmentId)
        {
            try
            {
                var apartment = await _context.Apartments.FindAsync(apartmentId);
                if (apartment == null)
                    return false;

                _context.Apartments.Remove(apartment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa apartment: {ex.Message}", ex);
            }
        }

        public async Task<bool> ApartmentNumberExistsOnFloorAsync(string number, Guid floorId)
        {
            return await _context.Apartments
                .AnyAsync(a => a.Number == number && a.FloorId == floorId);
        }

        public async Task<Floor?> GetFloorByNumberAsync(int floorNumber)
        {
            return await _context.Floors
                .FirstOrDefaultAsync(f => f.FloorNumber == floorNumber);
        }



        public async Task<List<Apartment>> GetApartmentsByFloorNumbersAsync(List<int> floorNumbers)
        {
            return await _context.Apartments
                .Include(a => a.Floor)
                .Where(a => floorNumbers.Contains(a.Floor.FloorNumber))
                .OrderBy(a => a.Floor.FloorNumber)
                .ThenBy(a => a.Number)
                .ToListAsync();
        }

        public async Task<List<Apartment>> UpdateApartmentsAsync(List<Apartment> apartments)
        {
            try
            {
                _context.Apartments.UpdateRange(apartments);
                await _context.SaveChangesAsync();
                return apartments;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật danh sách apartments: {ex.Message}", ex);
            }
        }
        public IQueryable<Apartment> Query()
        {
            return _context.Apartments.AsQueryable();
        }
        public IQueryable<ResidentApartment> QueryResidentApartments()
        {
            return _context.ResidentApartments.AsQueryable();
        }

        public IQueryable<ResidentProfile> QueryResidentProfiles()
        {
            return _context.ResidentProfiles.AsQueryable();
        }
        // Get apartments “đang có cư dân ở” để dùng cho auto invoice:
        // - Có bản ghi trong resident_apartments
        // - relation_type thông thường là OWNER (hoặc các loại ở thực tế)
        // - end_date NULL hoặc lớn hơn ngày hiện tại (chưa kết thúc ở)
        public async Task<List<Apartment>> GetActiveApartmentsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            return await _context.Apartments
                .Include(a => a.Floor)
                .Where(a =>
                    _context.ResidentApartments.Any(ra =>
                        ra.ApartmentId == a.ApartmentId &&
                        ra.RelationType == "OWNER" &&
                        ra.IsPrimary &&
                        (ra.EndDate == null || ra.EndDate > today)))
                .OrderBy(a => a.Floor.FloorNumber)
                .ThenBy(a => a.Number)
                .ToListAsync();
        }
    }
}