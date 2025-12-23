using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IService
{
    public interface IFloorService
    {
        Task<CreateFloorsResponseDto> CreateFloorsAsync(CreateFloorsRequestDto request);
        Task<List<FloorResponseDto>> GetAllFloorsAsync();
        Task<FloorResponseDto?> GetFloorByIdAsync(string floorId);
        Task<FloorResponseDto> UpdateFloorAsync(Guid floorId, UpdateFloorRequestDto request);
        Task<bool> DeleteFloorAsync(Guid floorId);
        Task<CreateSingleFloorResponseDto> CreateSingleFloorAsync(CreateSingleFloorRequestDto request);
    }
}