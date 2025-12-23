using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService
{
    public interface IApartmentService
    {
        Task<CreateApartmentsResponseDto> CreateApartmentsAsync(CreateApartmentsRequestDto request);
        Task<ApartmentResponseDto> CreateSingleApartmentAsync(CreateSingleApartmentRequestDto request);
        Task<ReplicateApartmentsResponseDto> ReplicateApartmentsAsync(ReplicateApartmentsRequestDto request);
        Task<List<ApartmentResponseDto>> GetAllApartmentsAsync();
        Task<List<ApartmentResponseDto>> GetApartmentsByFloorAsync(int floorNumber);
        Task<ApartmentResponseDto?> GetApartmentByIdAsync(Guid apartmentId);
        Task<ApartmentResponseDto?> GetApartmentByNumberAsync(string apartmentNumber);
        Task<List<FloorApartmentSummaryDto>> GetFloorApartmentSummaryAsync();
        Task<ApartmentResponseDto> UpdateApartmentAsync(Guid apartmentId, CreateApartmentDto updateDto);
        Task<bool> DeleteApartmentAsync(Guid apartmentId);
        Task<RefactorApartmentNamesResponseDto> RefactorApartmentNamesAsync(RefactorApartmentNamesRequestDto request);
        Task<(IEnumerable<ApartmentLookupDto> items, int total)> LookupAsync(string? number, int page, int pageSize);
        Task<(IEnumerable<ApartmentLookupDto> items, int total)> LookupByOwnerNameAsync(string? ownerName, int page, int pageSize);
        Task<ApartmentSummaryDto?> GetSummaryAsync(Guid apartmentId);
    }
}