using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces
{
    public interface IServiceTypeService
    {
        Task<PagedResult<ServiceTypeResponseDto>> ListAsync(ServiceTypeListQueryDto query);
        Task<ServiceTypeResponseDto> CreateAsync(CreateServiceTypeDto dto);
        Task<ServiceTypeResponseDto?> UpdateAsync(Guid id, UpdateServiceTypeDto dto);
        Task<ServiceTypeResponseDto?> GetByIdAsync(Guid id);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> SetActiveAsync(Guid id, bool isActive);
        Task<IEnumerable<OptionDto>> GetAllOptionsAsync();
    }
}
