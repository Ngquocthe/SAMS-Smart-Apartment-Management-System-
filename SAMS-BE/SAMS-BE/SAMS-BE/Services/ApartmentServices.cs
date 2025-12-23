using AutoMapper;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Services
{
    public class ApartmentServices : IApartmentService
    {
        private readonly IApartmentRepository _apartmentRepository;
        private readonly IFloorRepository _floorRepository;
        private readonly IMapper _mapper;

        public ApartmentServices(
            IApartmentRepository apartmentRepository,
            IFloorRepository floorRepository,
            IMapper mapper)
        {
            _apartmentRepository = apartmentRepository;
            _floorRepository = floorRepository;
            _mapper = mapper;
        }

        public async Task<CreateApartmentsResponseDto> CreateApartmentsAsync(CreateApartmentsRequestDto request)
        {
            try
            {
                // Validate tầng có tồn tại không
                var sourceFloor = await _apartmentRepository.GetFloorByNumberAsync(request.SourceFloorNumber);
                if (sourceFloor == null)
                {
                    return new CreateApartmentsResponseDto
                    {
                        Success = false,
                        Message = $"Không tìm thấy tầng {request.SourceFloorNumber}",
                        TotalCreated = 0
                    };
                }

                var apartmentsToCreate = new List<Apartment>();
                var createdApartmentResponses = new List<ApartmentResponseDto>();
                var skippedApartments = new List<string>(); // Track apartments đã tồn tại

                // Tạo apartments
                foreach (var apartmentDto in request.Apartments)
                {
                    // Generate apartment number theo format: BuildingCode + FloorNumber + Number
                    // Ví dụ: A + 08 + 01 = A0801
                    var apartmentNumber = $"{request.BuildingCode}{request.SourceFloorNumber:D2}{apartmentDto.Number.PadLeft(2, '0')}";

                    // Check duplicate number
                    var exists = await _apartmentRepository.ApartmentNumberExistsOnFloorAsync(apartmentNumber, sourceFloor.FloorId);
                    if (exists)
                    {
                        skippedApartments.Add(apartmentNumber);
                        continue; // Skip duplicate
                    }

                    // Map từ DTO sang model
                    var apartment = _mapper.Map<Apartment>(apartmentDto);
                    apartment.ApartmentId = Guid.NewGuid();
                    apartment.FloorId = sourceFloor.FloorId;
                    apartment.Number = apartmentNumber;
                    apartment.CreatedBy = "System"; // TODO: Get from current user
                    apartment.CreatedAt = DateTime.UtcNow;

                    apartmentsToCreate.Add(apartment);

                    // Prepare response
                    var responseDto = _mapper.Map<ApartmentResponseDto>(apartment);
                    responseDto.FloorNumber = sourceFloor.FloorNumber;
                    responseDto.FloorName = sourceFloor.Name;
                    createdApartmentResponses.Add(responseDto);
                }

                if (apartmentsToCreate.Count == 0)
                {
                    var message = "Không có apartment nào được tạo.";
                    if (skippedApartments.Count > 0)
                    {
                        message += $" Các căn hộ sau đã tồn tại: {string.Join(", ", skippedApartments)}";
                    }

                    return new CreateApartmentsResponseDto
                    {
                        Success = false,
                        Message = message,
                        TotalCreated = 0
                    };
                }

                // Lưu vào database
                await _apartmentRepository.CreateApartmentsAsync(apartmentsToCreate);

                var successMessage = $"Tạo thành công {apartmentsToCreate.Count} apartments cho tầng {request.SourceFloorNumber}";
                if (skippedApartments.Count > 0)
                {
                    successMessage += $". Bỏ qua {skippedApartments.Count} căn đã tồn tại: {string.Join(", ", skippedApartments)}";
                }

                return new CreateApartmentsResponseDto
                {
                    Success = true,
                    Message = successMessage,
                    CreatedApartments = createdApartmentResponses,
                    TotalCreated = apartmentsToCreate.Count
                };
            }
            catch (Exception ex)
            {
                return new CreateApartmentsResponseDto
                {
                    Success = false,
                    Message = $"Lỗi khi tạo apartments: {ex.Message}",
                    TotalCreated = 0
                };
            }
        }

        public async Task<ApartmentResponseDto> CreateSingleApartmentAsync(CreateSingleApartmentRequestDto request)
        {
            try
            {
                // Validate tầng có tồn tại không
                var floor = await _apartmentRepository.GetFloorByNumberAsync(request.FloorNumber);
                if (floor == null)
                {
                    throw new Exception($"Không tìm thấy tầng {request.FloorNumber}");
                }

                // Generate apartment number theo format: BuildingCode + FloorNumber + ApartmentNumber
                // Ví dụ: A + 08 + 01 = A0801
                var apartmentNumber = $"{request.BuildingCode}{request.FloorNumber:D2}{request.ApartmentNumber}";

                // Check xem apartment đã tồn tại chưa
                var exists = await _apartmentRepository.ApartmentNumberExistsOnFloorAsync(apartmentNumber, floor.FloorId);
                if (exists)
                {
                    throw new Exception($"Căn hộ {apartmentNumber} đã tồn tại trên tầng {request.FloorNumber}");
                }

                // Tạo apartment mới
                var apartment = new Apartment
                {
                    ApartmentId = Guid.NewGuid(),
                    FloorId = floor.FloorId,
                    Number = apartmentNumber,
                    AreaM2 = request.AreaM2,
                    Bedrooms = request.Bedrooms,
                    Status = request.Status,
                    Image = request.Image,
                    Type = request.Type,
                    CreatedBy = "System", // TODO: Get from current user
                    CreatedAt = DateTime.UtcNow
                };

                // Lưu vào database
                await _apartmentRepository.CreateApartmentsAsync(new List<Apartment> { apartment });

                // Prepare response
                var responseDto = _mapper.Map<ApartmentResponseDto>(apartment);
                responseDto.FloorNumber = floor.FloorNumber;
                responseDto.FloorName = floor.Name;

                return responseDto;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo căn hộ: {ex.Message}", ex);
            }
        }

        public async Task<ReplicateApartmentsResponseDto> ReplicateApartmentsAsync(ReplicateApartmentsRequestDto request)
        {
            try
            {
                // Lấy apartments từ tầng gốc
                var sourceApartments = await _apartmentRepository.GetApartmentsByFloorNumberAsync(request.SourceFloorNumber);
                if (sourceApartments.Count == 0)
                {
                    return new ReplicateApartmentsResponseDto
                    {
                        Success = false,
                        Message = $"Tầng {request.SourceFloorNumber} không có apartments để nhân bản",
                        TotalReplicated = 0
                    };
                }

                var replicatedApartments = new List<ApartmentResponseDto>();
                var skippedFloors = new List<int>();
                var skippedApartments = new Dictionary<int, List<string>>(); // Track apartments đã tồn tại theo từng tầng
                var totalReplicated = 0;

                // Nhân bản cho từng tầng đích
                foreach (var targetFloorNumber in request.TargetFloorNumbers)
                {
                    // Tìm tầng đích theo floor number 
                    var targetFloor = await _apartmentRepository.GetFloorByNumberAsync(targetFloorNumber);
                    if (targetFloor == null)
                    {
                        skippedFloors.Add(targetFloorNumber);
                        continue;
                    }

                    var apartmentsForThisFloor = new List<Apartment>();
                    var skippedForThisFloor = new List<string>();

                    // Lấy danh sách apartments hiện có trên tầng đích
                    var existingApartments = await _apartmentRepository.GetApartmentsByFloorAsync(targetFloor.FloorId);
                    var existingNumbers = existingApartments.Select(a => a.Number).ToHashSet();

                    // Clone apartments từ tầng gốc
                    foreach (var sourceApartment in sourceApartments)
                    {
                        // Parse apartment number để tách apartment suffix
                        // Format: BuildingCode + FloorNumber(2 digits) + ApartmentNumber(2 digits)
                        // Ví dụ: A0101 → BuildingCode="A", FloorNumber="01", ApartmentNumber="01"
                        var originalNumber = sourceApartment.Number;
                        var apartmentNumber = ExtractApartmentNumber(originalNumber, request.BuildingCode);

                        // Generate new apartment number: BuildingCode + TargetFloor(2 digits) + ApartmentNumber(2 digits)
                        var newApartmentNumber = $"{request.BuildingCode}{targetFloorNumber:D2}{apartmentNumber}";

                        // Kiểm tra xem apartment này đã tồn tại trên tầng đích chưa
                        if (existingNumbers.Contains(newApartmentNumber))
                        {
                            skippedForThisFloor.Add(newApartmentNumber);
                            continue; // Bỏ qua apartment đã tồn tại
                        }

                        // Tạo apartment mới thay vì dùng AutoMapper để tránh vấn đề với FloorId
                        var newApartment = new Apartment
                        {
                            ApartmentId = Guid.NewGuid(),
                            FloorId = targetFloor.FloorId, // Gán FloorId của target floor
                            Number = newApartmentNumber,
                            AreaM2 = sourceApartment.AreaM2,
                            Bedrooms = sourceApartment.Bedrooms,
                            Status = sourceApartment.Status,
                            Image = sourceApartment.Image,
                            Type = sourceApartment.Type,
                            CreatedBy = "System",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = null,
                            UpdatedBy = null
                        };

                        // Debug log để kiểm tra
                        Console.WriteLine($"Clone: {sourceApartment.Number} (FloorId: {sourceApartment.FloorId}) → {newApartment.Number} (TargetFloorId: {newApartment.FloorId}, TargetFloorNumber: {targetFloorNumber})");

                        apartmentsForThisFloor.Add(newApartment);
                    }

                    // Track apartments đã bỏ qua
                    if (skippedForThisFloor.Count > 0)
                    {
                        skippedApartments[targetFloorNumber] = skippedForThisFloor;
                    }

                    // Lưu apartments cho tầng này
                    if (apartmentsForThisFloor.Count > 0)
                    {
                        await _apartmentRepository.CreateApartmentsAsync(apartmentsForThisFloor);

                        // Add to response
                        foreach (var apartment in apartmentsForThisFloor)
                        {
                            var responseDto = _mapper.Map<ApartmentResponseDto>(apartment);
                            responseDto.FloorNumber = targetFloor.FloorNumber;
                            responseDto.FloorName = targetFloor.Name;
                            replicatedApartments.Add(responseDto);
                        }

                        totalReplicated += apartmentsForThisFloor.Count;
                    }
                }

                var successMessage = $"Nhân bản thành công {totalReplicated} apartments";
                if (skippedFloors.Count > 0)
                {
                    successMessage += $". Bỏ qua tầng không tồn tại: {string.Join(", ", skippedFloors)}";
                }
                if (skippedApartments.Count > 0)
                {
                    successMessage += $". Một số căn hộ đã tồn tại và được bỏ qua";
                    foreach (var kvp in skippedApartments)
                    {
                        successMessage += $" (Tầng {kvp.Key}: {string.Join(", ", kvp.Value)})";
                    }
                }

                return new ReplicateApartmentsResponseDto
                {
                    Success = totalReplicated > 0,
                    Message = successMessage,
                    ReplicatedApartments = replicatedApartments,
                    TotalReplicated = totalReplicated,
                    SkippedFloors = skippedFloors,
                    SkippedReason = skippedFloors.Count > 0 ? "Tầng không tồn tại" : null
                };
            }
            catch (Exception ex)
            {
                return new ReplicateApartmentsResponseDto
                {
                    Success = false,
                    Message = $"Lỗi khi nhân bản apartments: {ex.Message}",
                    TotalReplicated = 0
                };
            }
        }

        public async Task<List<ApartmentResponseDto>> GetAllApartmentsAsync()
        {
            try
            {
                var apartments = await _apartmentRepository.GetAllApartmentsAsync();
                return apartments.Select(MapToResponseDto).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách apartments: {ex.Message}", ex);
            }
        }

        public async Task<List<ApartmentResponseDto>> GetApartmentsByFloorAsync(int floorNumber)
        {
            try
            {
                var apartments = await _apartmentRepository.GetApartmentsByFloorNumberAsync(floorNumber);
                return apartments.Select(MapToResponseDto).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy apartments của tầng {floorNumber}: {ex.Message}", ex);
            }
        }

        public async Task<ApartmentResponseDto?> GetApartmentByIdAsync(Guid apartmentId)
        {
            try
            {
                var apartment = await _apartmentRepository.GetApartmentByIdAsync(apartmentId);
                return apartment != null ? MapToResponseDto(apartment) : null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin apartment: {ex.Message}", ex);
            }
        }

        public async Task<ApartmentResponseDto?> GetApartmentByNumberAsync(string apartmentNumber)
        {
            try
            {
                var apartment = await _apartmentRepository.GetApartmentByNumberAsync(apartmentNumber);
                return apartment != null ? MapToResponseDto(apartment) : null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin apartment theo số: {ex.Message}", ex);
            }
        }

        public async Task<List<FloorApartmentSummaryDto>> GetFloorApartmentSummaryAsync()
        {
            try
            {
                var floors = await _floorRepository.GetAllFloorsAsync();
                var summaries = new List<FloorApartmentSummaryDto>();

                foreach (var floor in floors)
                {
                    var apartments = await _apartmentRepository.GetApartmentsByFloorAsync(floor.FloorId);
                    var apartmentDtos = apartments.Select(MapToResponseDto).ToList();

                    summaries.Add(new FloorApartmentSummaryDto
                    {
                        FloorNumber = floor.FloorNumber,
                        FloorName = floor.Name ?? string.Empty,
                        ApartmentCount = apartments.Count,
                        HasApartments = apartments.Count > 0,
                        Apartments = apartmentDtos
                    });
                }

                return summaries.OrderBy(s => s.FloorNumber).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy tóm tắt apartments theo tầng: {ex.Message}", ex);
            }
        }

        public async Task<ApartmentResponseDto> UpdateApartmentAsync(Guid apartmentId, CreateApartmentDto updateDto)
        {
            try
            {
                var apartment = await _apartmentRepository.GetApartmentByIdAsync(apartmentId);
                if (apartment == null)
                    throw new Exception("Không tìm thấy apartment");

                // Validate: Không cho phép update status thành INACTIVE nếu còn cư dân đang ở
                if (updateDto.Status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase) && 
                    !apartment.Status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    // Kiểm tra xem có cư dân đang ở không (end_date = null hoặc trong tương lai)
                    var activeResidents = apartment.ResidentApartments?
                        .Where(ra => ra.EndDate == null || ra.EndDate >= DateOnly.FromDateTime(DateTime.Today))
                        .ToList();

                    if (activeResidents != null && activeResidents.Any())
                    {
                        var residentNames = activeResidents
                            .Select(ra => ra.Resident?.FullName ?? "Unknown")
                            .ToList();
                        
                        throw new InvalidOperationException(
                            $"Không thể chuyển căn hộ sang trạng thái INACTIVE vì còn cư dân đang ở.");
                    }
                }

                // Update properties
                apartment.AreaM2 = updateDto.AreaM2;
                apartment.Bedrooms = updateDto.Bedrooms;
                apartment.Status = updateDto.Status;
                apartment.Image = updateDto.Image;
                apartment.Type = updateDto.Type;
                apartment.UpdatedAt = DateTime.UtcNow;
                apartment.UpdatedBy = "System"; // TODO: Get from current user

                var updatedApartment = await _apartmentRepository.UpdateApartmentAsync(apartment);
                return _mapper.Map<ApartmentResponseDto>(updatedApartment);
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
                return await _apartmentRepository.DeleteApartmentAsync(apartmentId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa apartment: {ex.Message}", ex);
            }
        }

        public async Task<RefactorApartmentNamesResponseDto> RefactorApartmentNamesAsync(RefactorApartmentNamesRequestDto request)
        {
            try
            {
                // Lấy tất cả apartments từ các tầng được chọn
                var apartments = await _apartmentRepository.GetApartmentsByFloorNumbersAsync(request.FloorNumbers);

                if (apartments.Count == 0)
                {
                    return new RefactorApartmentNamesResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy apartments trong các tầng được chọn",
                        TotalUpdated = 0
                    };
                }

                var updateResults = new List<ApartmentUpdateResult>();
                var apartmentsToUpdate = new List<Apartment>();
                var processedFloors = new HashSet<int>();
                var skippedFloors = new List<int>();

                foreach (var apartment in apartments)
                {
                    processedFloors.Add(apartment.Floor.FloorNumber);

                    // Kiểm tra prefix nếu có OldPrefix được cung cấp
                    if (!string.IsNullOrEmpty(request.OldPrefix) &&
                        !apartment.Number.StartsWith(request.OldPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        updateResults.Add(new ApartmentUpdateResult
                        {
                            ApartmentId = apartment.ApartmentId,
                            OldNumber = apartment.Number,
                            NewNumber = apartment.Number, // Không thay đổi
                            FloorNumber = apartment.Floor.FloorNumber,
                            Updated = false,
                            ErrorMessage = $"Apartment {apartment.Number} không có prefix '{request.OldPrefix}'"
                        });
                        continue;
                    }

                    try
                    {
                        string newNumber;

                        if (string.IsNullOrEmpty(request.OldPrefix))
                        {
                            // Không có OldPrefix - tự động phát hiện và thay thế building code
                            // Giả sử format: BuildingCode + số (VD: A0801, B123, string456)

                            // Tìm vị trí đầu tiên của số
                            int firstDigitIndex = -1;
                            for (int i = 0; i < apartment.Number.Length; i++)
                            {
                                if (char.IsDigit(apartment.Number[i]))
                                {
                                    firstDigitIndex = i;
                                    break;
                                }
                            }

                            if (firstDigitIndex > 0)
                            {
                                // Có building code phía trước số
                                var numberPart = apartment.Number.Substring(firstDigitIndex);
                                newNumber = $"{request.NewBuildingCode}{numberPart}";
                            }
                            else if (firstDigitIndex == 0)
                            {
                                // Toàn bộ là số, thêm building code vào đầu
                                newNumber = $"{request.NewBuildingCode}{apartment.Number}";
                            }
                            else
                            {
                                // Không có số, thay thế toàn bộ
                                newNumber = $"{request.NewBuildingCode}{apartment.Floor.FloorNumber:D2}01";
                            }
                        }
                        else
                        {
                            // Có OldPrefix - thay thế prefix cũ bằng prefix mới
                            var suffixPart = apartment.Number.Substring(request.OldPrefix.Length);

                            // Kiểm tra xem suffix đã có format đúng chưa (ví dụ: 0801)
                            if (suffixPart.Length >= 4 && suffixPart.All(char.IsDigit))
                            {
                                // Nếu đã có format tầng + số căn (ví dụ: 0801)
                                newNumber = $"{request.NewBuildingCode}{suffixPart}";
                            }
                            else
                            {
                                // Nếu chưa có format đúng, tạo mới theo format chuẩn
                                // Lấy 2 số cuối làm số căn
                                var apartmentNum = suffixPart.Length >= 2 ? suffixPart.Substring(suffixPart.Length - 2) : suffixPart.PadLeft(2, '0');
                                newNumber = $"{request.NewBuildingCode}{apartment.Floor.FloorNumber:D2}{apartmentNum}";
                            }
                        }

                        // Kiểm tra xem number mới có bị trùng không (chỉ với apartments khác)
                        var duplicateApartment = apartments.FirstOrDefault(a => a.Number == newNumber && a.ApartmentId != apartment.ApartmentId);
                        if (duplicateApartment != null)
                        {
                            updateResults.Add(new ApartmentUpdateResult
                            {
                                ApartmentId = apartment.ApartmentId,
                                OldNumber = apartment.Number,
                                NewNumber = newNumber,
                                FloorNumber = apartment.Floor.FloorNumber,
                                Updated = false,
                                ErrorMessage = $"Number {newNumber} sẽ bị trùng với apartment khác"
                            });
                            continue;
                        }

                        // Update apartment
                        var oldNumber = apartment.Number;
                        apartment.Number = newNumber;
                        apartment.UpdatedAt = DateTime.UtcNow;
                        apartment.UpdatedBy = "System"; // TODO: Get from current user

                        apartmentsToUpdate.Add(apartment);

                        updateResults.Add(new ApartmentUpdateResult
                        {
                            ApartmentId = apartment.ApartmentId,
                            OldNumber = oldNumber,
                            NewNumber = newNumber,
                            FloorNumber = apartment.Floor.FloorNumber,
                            Updated = true
                        });
                    }
                    catch (Exception ex)
                    {
                        updateResults.Add(new ApartmentUpdateResult
                        {
                            ApartmentId = apartment.ApartmentId,
                            OldNumber = apartment.Number,
                            NewNumber = apartment.Number,
                            FloorNumber = apartment.Floor.FloorNumber,
                            Updated = false,
                            ErrorMessage = $"Lỗi xử lý: {ex.Message}"
                        });
                    }
                }

                // Lưu những apartments đã được update
                if (apartmentsToUpdate.Count > 0)
                {
                    await _apartmentRepository.UpdateApartmentsAsync(apartmentsToUpdate);
                }

                // Kiểm tra tầng nào không có apartments
                foreach (var floorNumber in request.FloorNumbers)
                {
                    if (!processedFloors.Contains(floorNumber))
                    {
                        skippedFloors.Add(floorNumber);
                    }
                }

                var successCount = updateResults.Count(r => r.Updated);
                var oldPrefixText = string.IsNullOrEmpty(request.OldPrefix) ? "tự động" : $"từ prefix '{request.OldPrefix}'";
                var message = $"Đã cập nhật {successCount}/{updateResults.Count} apartments {oldPrefixText} thành '{request.NewBuildingCode}'";

                if (skippedFloors.Count > 0)
                {
                    message += $". Tầng không có apartments: {string.Join(", ", skippedFloors)}";
                }

                return new RefactorApartmentNamesResponseDto
                {
                    Success = successCount > 0,
                    Message = message,
                    UpdatedApartments = updateResults,
                    TotalUpdated = successCount,
                    ProcessedFloors = processedFloors.ToList(),
                    SkippedFloors = skippedFloors
                };
            }
            catch (Exception ex)
            {
                return new RefactorApartmentNamesResponseDto
                {
                    Success = false,
                    Message = $"Lỗi khi refactor apartment names: {ex.Message}",
                    TotalUpdated = 0
                };
            }
        }

        /// <summary>
        /// Tách apartment number từ full apartment number
        /// Format: BuildingCode + FloorNumber(2 digits) + ApartmentNumber(2 digits)
        /// Ví dụ: A0101 → ApartmentNumber = "01"
        /// </summary>
        private string ExtractApartmentNumber(string fullApartmentNumber, string buildingCode)
        {
            if (string.IsNullOrEmpty(fullApartmentNumber) || string.IsNullOrEmpty(buildingCode))
            {
                return "01"; // Default fallback
            }

            // Nếu có building code prefix và đủ độ dài
            if (fullApartmentNumber.StartsWith(buildingCode) &&
                fullApartmentNumber.Length >= buildingCode.Length + 4)
            {
                // Format chuẩn: BuildingCode + FloorNumber(2 digits) + ApartmentNumber(2 digits)
                // Ví dụ: A0101 → skip "A" + skip "01" + lấy "01"
                return fullApartmentNumber.Substring(buildingCode.Length + 2, 2);
            }

            // Fallback: lấy 2 số cuối
            if (fullApartmentNumber.Length >= 2)
            {
                return fullApartmentNumber.Substring(fullApartmentNumber.Length - 2);
            }

            // Fallback cuối: pad with zero
            return fullApartmentNumber.PadLeft(2, '0');
        }

        public async Task<(IEnumerable<ApartmentLookupDto> items, int total)> LookupAsync(string? number, int page, int pageSize)
        {
            try
            {
                // Tìm kiếm theo số căn hộ - trả về TẤT CẢ căn hộ, kể cả không có chủ hộ
                var query = from a in _apartmentRepository.Query()
                                // Join với chủ hộ (IsPrimary = true và đang ở)
                            join ra in _apartmentRepository.QueryResidentApartments() on a.ApartmentId equals ra.ApartmentId into raJoin
                            from ra in raJoin.Where(x => x.IsPrimary && (x.EndDate == null || x.EndDate >= DateOnly.FromDateTime(DateTime.Today))).DefaultIfEmpty()
                            join rp in _apartmentRepository.QueryResidentProfiles() on ra.ResidentId equals rp.ResidentId into rpJoin
                            from rp in rpJoin.DefaultIfEmpty()
                            where string.IsNullOrEmpty(number) || a.Number.ToLower().Contains(number.ToLower())
                            select new ApartmentLookupDto
                            {
                                ApartmentId = a.ApartmentId,
                                Number = a.Number,
                                OwnerName = rp.FullName // Có thể null nếu không có chủ hộ
                            };

                var total = await query.CountAsync();

                // Lấy tất cả items trước để sort
                var allItems = await query.ToListAsync();

                // Ưu tiên exact match trước, sau đó mới sort theo alphabet
                if (!string.IsNullOrEmpty(number))
                {
                    var numberLower = number.ToLower();
                    allItems = allItems.OrderBy(x =>
                    {
                        var numLower = x.Number.ToLower();
                        // Exact match = 0, contains = 1, không khớp = 2
                        if (numLower == numberLower) return 0;
                        if (numLower.Contains(numberLower)) return 1;
                        return 2;
                    }).ThenBy(x => x.Number).ToList();
                }
                else
                {
                    allItems = allItems.OrderBy(x => x.Number).ToList();
                }

                // Pagination sau khi sort
                var items = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                return (items, total);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tìm kiếm apartments: {ex.Message}", ex);
            }
        }

        public async Task<(IEnumerable<ApartmentLookupDto> items, int total)> LookupByOwnerNameAsync(string? ownerName, int page, int pageSize)
        {
            try
            {
                // Tìm kiếm theo TẤT CẢ cư dân, không chỉ chủ căn hộ
                // Mỗi cư dân khớp sẽ tạo ra một record cho căn hộ của họ
                // Cần join thêm với chủ hộ thực sự để trả về đúng thông tin
                var query = from a in _apartmentRepository.Query()
                            join raMatched in _apartmentRepository.QueryResidentApartments() on a.ApartmentId equals raMatched.ApartmentId
                            join rpMatched in _apartmentRepository.QueryResidentProfiles() on raMatched.ResidentId equals rpMatched.ResidentId
                            where string.IsNullOrEmpty(ownerName) || (rpMatched.FullName != null && rpMatched.FullName.ToLower().Contains(ownerName.ToLower()))
                            // Chỉ lấy các cư dân đang ở (EndDate null hoặc trong tương lai)
                            where raMatched.EndDate == null || raMatched.EndDate >= DateOnly.FromDateTime(DateTime.Today)
                            // Join với chủ hộ thực sự
                            join raOwner in _apartmentRepository.QueryResidentApartments() on a.ApartmentId equals raOwner.ApartmentId into raOwnerJoin
                            from raOwner in raOwnerJoin.Where(x => x.IsPrimary && (x.EndDate == null || x.EndDate >= DateOnly.FromDateTime(DateTime.Today))).DefaultIfEmpty()
                            join rpOwner in _apartmentRepository.QueryResidentProfiles() on raOwner.ResidentId equals rpOwner.ResidentId into rpOwnerJoin
                            from rpOwner in rpOwnerJoin.DefaultIfEmpty()
                            orderby a.Number, rpMatched.FullName
                            select new ApartmentLookupDto
                            {
                                ApartmentId = a.ApartmentId,
                                Number = a.Number,
                                OwnerName = rpOwner.FullName, // Tên chủ hộ thực sự
                                MatchedResidentName = rpMatched.FullName // Tên cư dân khớp (có thể không phải chủ hộ)
                            };

                var total = await query.CountAsync();
                var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                return (items, total);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tìm kiếm apartments theo tên cư dân: {ex.Message}", ex);
            }
        }

        public async Task<ApartmentSummaryDto?> GetSummaryAsync(Guid apartmentId)
        {
            try
            {
                var query = from a in _apartmentRepository.Query()
                            where a.ApartmentId == apartmentId
                            join ra in _apartmentRepository.QueryResidentApartments() on a.ApartmentId equals ra.ApartmentId into raJoin
                            from ra in raJoin.Where(x => x.IsPrimary).DefaultIfEmpty()
                            join rp in _apartmentRepository.QueryResidentProfiles() on ra.ResidentId equals rp.ResidentId into rpJoin
                            from rp in rpJoin.DefaultIfEmpty()
                            select new ApartmentSummaryDto
                            {
                                ApartmentId = a.ApartmentId,
                                Number = a.Number,
                                OwnerName = rp.FullName,
                                OwnerUserId = null // Có thể join thêm bảng users nếu cần
                            };

                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin apartment: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Map Apartment entity to ApartmentResponseDto với đầy đủ thông tin chủ hộ, số lượng cư dân và phương tiện
        /// </summary>
        private ApartmentResponseDto MapToResponseDto(Apartment apartment)
        {
            var dto = _mapper.Map<ApartmentResponseDto>(apartment);

            // Tìm chủ hộ (is_primary = true và end_date = null)
            var primaryResident = apartment.ResidentApartments?
                .FirstOrDefault(ra => ra.IsPrimary && ra.EndDate == null);

            if (primaryResident?.Resident != null)
            {
                dto.OwnerInfo = new OwnerInfoDto
                {
                    ResidentId = primaryResident.Resident.ResidentId,
                    FullName = primaryResident.Resident.FullName,
                    Phone = primaryResident.Resident.Phone,
                    Email = primaryResident.Resident.Email
                };
            }

            // Lấy danh sách tất cả cư dân đang ở (end_date = null hoặc trong tương lai)
            var activeResidents = apartment.ResidentApartments?
                .Where(ra => ra.EndDate == null || ra.EndDate >= DateOnly.FromDateTime(DateTime.Today))
                .OrderByDescending(ra => ra.IsPrimary) // Owner trước, family member sau
                .ThenBy(ra => ra.StartDate)
                .ToList() ?? new List<ResidentApartment>();

            dto.Residents = activeResidents.Select(ra => new ResidentInfoDto
            {
                ResidentId = ra.ResidentId,
                FullName = ra.Resident?.FullName,
                Phone = ra.Resident?.Phone,
                Email = ra.Resident?.Email,
                RelationType = ra.RelationType,
                IsPrimary = ra.IsPrimary,
                StartDate = ra.StartDate.ToDateTime(TimeOnly.MinValue),
                EndDate = ra.EndDate?.ToDateTime(TimeOnly.MinValue)
            }).ToList();

            // Đếm số lượng cư dân (các resident có end_date = null)
            dto.ResidentCount = apartment.ResidentApartments?
                .Count(ra => ra.EndDate == null) ?? 0;

            // Đếm số lượng phương tiện (chỉ đếm xe có status = ACTIVE)
            dto.VehicleCount = apartment.Vehicles?.Count(v => v.Status == "ACTIVE") ?? 0;

            return dto;
        }
    }
}