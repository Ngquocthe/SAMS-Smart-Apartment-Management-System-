using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository
{
    public interface IFloorRepository
    {
        Task<List<Floor>> CreateFloorsAsync(List<Floor> floors);
        Task<Floor?> GetByFloorIdAsync(string floorId);
        Task<List<Floor>> GetAllFloorsAsync();
        Task<bool> FloorExistsAsync(string floorId);
        Task<Floor> UpdateFloorAsync(Floor floor);
        Task<bool> DeleteFloorAsync(Guid floorId);
        Task<bool> FloorNumberExistsAsync(int floorNumber);
        Task<Floor> CreateSingleFloorAsync(Floor floor);
        Task<bool> FloorHasApartmentsAsync(Guid floorId);
    }
}