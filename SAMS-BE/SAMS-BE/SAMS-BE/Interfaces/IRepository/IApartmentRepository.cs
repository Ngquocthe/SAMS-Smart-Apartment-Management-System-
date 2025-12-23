using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository
{
    public interface IApartmentRepository
    {
        Task<List<Apartment>> CreateApartmentsAsync(List<Apartment> apartments);
        Task<List<Apartment>> GetApartmentsByFloorAsync(Guid floorId);
        Task<List<Apartment>> GetApartmentsByFloorNumberAsync(int floorNumber);
        Task<bool> FloorHasApartmentsAsync(Guid floorId);
        Task<bool> FloorHasApartmentsByNumberAsync(int floorNumber);
        Task<Apartment?> GetApartmentByIdAsync(Guid apartmentId);
        Task<Apartment?> GetApartmentByNumberAsync(string apartmentNumber);
        Task<List<Apartment>> GetAllApartmentsAsync();
        Task<Apartment> UpdateApartmentAsync(Apartment apartment);
        Task<bool> DeleteApartmentAsync(Guid apartmentId);
        Task<bool> ApartmentNumberExistsOnFloorAsync(string number, Guid floorId);
        Task<Floor?> GetFloorByNumberAsync(int floorNumber);
        Task<List<Apartment>> GetApartmentsByFloorNumbersAsync(List<int> floorNumbers);
        Task<List<Apartment>> UpdateApartmentsAsync(List<Apartment> apartments);
        IQueryable<Apartment> Query();
        IQueryable<ResidentApartment> QueryResidentApartments();
        IQueryable<ResidentProfile> QueryResidentProfiles();
        Task<List<Apartment>> GetActiveApartmentsAsync();
    }
}