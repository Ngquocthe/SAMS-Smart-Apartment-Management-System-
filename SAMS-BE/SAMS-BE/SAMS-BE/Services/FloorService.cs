using AutoMapper;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using static SAMS_BE.Helpers.DateTimeHelper;

namespace SAMS_BE.Services
{
    public class FloorService : IFloorService
    {
        private readonly IFloorRepository _floorRepository;
        private readonly IMapper _mapper;

        public FloorService(IFloorRepository floorRepository, IMapper mapper)
        {
            _floorRepository = floorRepository;
            _mapper = mapper;
        }

        public async Task<CreateFloorsResponseDto> CreateFloorsAsync(CreateFloorsRequestDto request)
        {
            try
            {
                var floorsToCreate = new List<Floor>();
                var skippedFloors = new List<int>();
                var now = VietnamNow;

                // Generate floor numbers based on floor type
                List<int> floorNumbers;
                if (request.FloorType == Enums.FloorType.BASEMENT)
                {
                    // For basement: create negative floor numbers (-1, -2, -3, ...)
                    floorNumbers = Enumerable.Range(1, request.Count)
                        .Select(i => -i)
                        .ToList();
                }
                else
                {
                    // For other types: create positive floor numbers starting from StartFloor
                    int startFloor = request.StartFloor ?? 1;
                    floorNumbers = Enumerable.Range(startFloor, request.Count).ToList();
                }

                foreach (var floorNumber in floorNumbers)
                {
                    // Skip excluded floors
                    if (request.ExcludeFloors != null && request.ExcludeFloors.Contains(floorNumber))
                    {
                        continue;
                    }

                    // Check if floor number already exists
                    var floorExists = await _floorRepository.FloorNumberExistsAsync(floorNumber);
                    if (floorExists)
                    {
                        skippedFloors.Add(floorNumber);
                        continue;
                    }

                    var floorTypeName = request.FloorType.ToString();
                    var floorDisplayName = GetFloorDisplayName(floorNumber, request.FloorType);

                    var newFloor = new Floor
                    {
                        FloorId = Guid.NewGuid(),
                        FloorNumber = floorNumber,
                        Name = floorDisplayName,
                        FloorType = floorTypeName,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    floorsToCreate.Add(newFloor);
                }

                if (floorsToCreate.Count == 0)
                {
                    return new CreateFloorsResponseDto
                    {
                        Success = false,
                        Message = "Không có tầng nào được tạo. Tất cả các tầng đã tồn tại hoặc bị loại trừ.",
                        TotalCreated = 0,
                        SkippedFloors = skippedFloors
                    };
                }

                // Create floors in database
                var createdFloors = await _floorRepository.CreateFloorsAsync(floorsToCreate);
                var createdFloorsResponse = _mapper.Map<List<FloorResponseDto>>(createdFloors);

                var message = $"Tạo thành công {createdFloors.Count} tầng";
                if (skippedFloors.Any())
                {
                    message += $", bỏ qua {skippedFloors.Count} tầng đã tồn tại";
                }

                return new CreateFloorsResponseDto
                {
                    Success = true,
                    Message = message,
                    CreatedFloors = createdFloorsResponse,
                    TotalCreated = createdFloors.Count,
                    SkippedFloors = skippedFloors
                };
            }
            catch (Exception ex)
            {
                return new CreateFloorsResponseDto
                {
                    Success = false,
                    Message = $"Lỗi khi tạo tầng: {ex.Message}",
                    TotalCreated = 0
                };
            }
        }

        public async Task<List<FloorResponseDto>> GetAllFloorsAsync()
        {
            try
            {
                var floors = await _floorRepository.GetAllFloorsAsync();
                return _mapper.Map<List<FloorResponseDto>>(floors);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách tầng: {ex.Message}", ex);
            }
        }

        public async Task<FloorResponseDto?> GetFloorByIdAsync(string floorId)
        {
            try
            {
                var floor = await _floorRepository.GetByFloorIdAsync(floorId);
                if (floor == null)
                    return null;

                return _mapper.Map<FloorResponseDto>(floor);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin tầng: {ex.Message}", ex);
            }
        }

        public async Task<FloorResponseDto> UpdateFloorAsync(Guid floorId, UpdateFloorRequestDto request)
        {
            try
            {
                var floor = await _floorRepository.GetByFloorIdAsync(floorId.ToString());
                if (floor == null)
                    throw new Exception("Không tìm thấy tầng");

                // Update Name if provided
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    floor.Name = request.Name;
                }

                // Update FloorType if provided
                if (request.FloorType.HasValue)
                {
                    floor.FloorType = request.FloorType.Value.ToString();
                }

                floor.UpdatedAt = VietnamNow;
                var updatedFloor = await _floorRepository.UpdateFloorAsync(floor);

                return _mapper.Map<FloorResponseDto>(updatedFloor);
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
                // Check if floor has apartments
                var hasApartments = await _floorRepository.FloorHasApartmentsAsync(floorId);
                if (hasApartments)
                {
                    throw new Exception("Không thể xóa tầng này vì đã có căn hộ");
                }

                return await _floorRepository.DeleteFloorAsync(floorId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa tầng: {ex.Message}", ex);
            }
        }

        public async Task<CreateSingleFloorResponseDto> CreateSingleFloorAsync(CreateSingleFloorRequestDto request)
        {
            try
            {
                // Check if floor number already exists
                var floorExists = await _floorRepository.FloorNumberExistsAsync(request.FloorNumber);
                if (floorExists)
                {
                    return new CreateSingleFloorResponseDto
                    {
                        Success = false,
                        Message = $"Tầng {request.FloorNumber} đã tồn tại"
                    };
                }

                var now = VietnamNow;
                var floorTypeName = request.FloorType.ToString();
                var floorDisplayName = request.Name ?? GetFloorDisplayName(request.FloorNumber, request.FloorType);

                // Create new floor
                var newFloor = new Floor
                {
                    FloorId = Guid.NewGuid(),
                    FloorNumber = request.FloorNumber,
                    Name = floorDisplayName,
                    FloorType = floorTypeName,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                // Save to database
                var createdFloor = await _floorRepository.CreateSingleFloorAsync(newFloor);

                return new CreateSingleFloorResponseDto
                {
                    Success = true,
                    Message = $"Tạo thành công tầng {createdFloor.FloorNumber}",
                    CreatedFloor = _mapper.Map<FloorResponseDto>(createdFloor)
                };
            }
            catch (Exception ex)
            {
                return new CreateSingleFloorResponseDto
                {
                    Success = false,
                    Message = $"Lỗi khi tạo tầng: {ex.Message}"
                };
            }
        }

        private string GetFloorDisplayName(int floorNumber, Enums.FloorType floorType)
        {
            if (floorType == Enums.FloorType.BASEMENT)
            {
                return $"Tầng hầm B{Math.Abs(floorNumber)}";
            }
            else
            {
                return $"Tầng {floorNumber}";
            }
        }
    }
}
