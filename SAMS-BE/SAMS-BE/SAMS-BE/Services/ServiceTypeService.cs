using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Mappers;
using SAMS_BE.Helpers;

namespace SAMS_BE.Services
{
    public class ServiceTypeService : IServiceTypeService
    {
        private readonly IServiceTypeRepository _repository;
        private readonly ILogger<ServiceTypeService> _logger;

        public ServiceTypeService(IServiceTypeRepository repository, ILogger<ServiceTypeService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<PagedResult<ServiceTypeResponseDto>> ListAsync(ServiceTypeListQueryDto query)
        {
            var (entities, total) = await _repository.ListAsync(query);
            return new PagedResult<ServiceTypeResponseDto>
            {
                Items = entities.Select(e => e.ToDto()),
                TotalItems = total,
                PageNumber = query.Page <= 0 ? 1 : query.Page,
                PageSize = query.PageSize <= 0 ? 20 : query.PageSize
            };
        }

        public async Task<ServiceTypeResponseDto> CreateAsync(CreateServiceTypeDto dto)
        {
            var normalizedCode = ServiceTypeValidation.NormalizeCode(dto.Code);
            ServiceTypeValidation.ValidateCode(normalizedCode);

            var warnings = ServiceTypeValidation.ValidateBusinessRules(dto, normalizedCode);
            foreach (var w in warnings)
                _logger.LogWarning("Create ServiceType warning: {Warning}", w);

            if (await _repository.CodeExistsAsync(normalizedCode))
            {
                _logger.LogWarning("Duplicate service type code pre-check: {Code}", normalizedCode);
                throw new InvalidOperationException($"Service type with code '{normalizedCode}' already exists.");
            }
            dto.Code = normalizedCode;
            var entity = dto.ToEntity();
            try
            {
                await _repository.CreateAsync(entity);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "DB update error when creating service type: {Code}", normalizedCode);
                throw new InvalidOperationException($"Service type with code '{normalizedCode}' may already exist.");
            }

            _logger.LogInformation("Created service type: {Id} ({Code})", entity.ServiceTypeId, entity.Code);
            return entity.ToDto();
        }

        public async Task<ServiceTypeResponseDto?> GetByIdAsync(Guid id)
        {
            var tracked = await _repository.GetByIdForUpdateAsync(id);
            return tracked?.ToDto();
        }

        public async Task<ServiceTypeResponseDto?> UpdateAsync(Guid id, UpdateServiceTypeDto dto)
        {
            var entity = await _repository.GetByIdForUpdateAsync(id);
            if (entity == null) return null;

            if (dto.IsMandatory && !dto.IsRecurring)
                throw new ArgumentException("Mandatory service must also be Recurring.");

            entity.Name = dto.Name.Trim();
            entity.CategoryId = dto.CategoryId;
            entity.Unit = dto.Unit.Trim();
            entity.IsMandatory = dto.IsMandatory;
            entity.IsRecurring = dto.IsRecurring;

            // Only update IsActive if explicitly provided
            if (dto.IsActive.HasValue)
            {
                entity.IsActive = dto.IsActive.Value;
            }

            entity.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(entity);
            return updated.ToDto();
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var entity = await _repository.GetByIdForUpdateAsync(id);
            if (entity == null) return false;

            if (!entity.IsActive) return true;
            entity.IsDelete = true;
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);
            return true;
        }
        public async Task<bool> SetActiveAsync(Guid id, bool isActive)
        {
            var entity = await _repository.GetByIdForUpdateAsync(id);
            if (entity == null) return false;

            if (entity.IsDelete == true) throw new InvalidOperationException("This service type was deleted and cannot be (de)activated.");

            if (entity.IsActive == isActive) return true;
            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);
            return true;
        }
        public async Task<IEnumerable<OptionDto>> GetAllOptionsAsync()
        {
            var query = new ServiceTypeListQueryDto
            {
                Q = null,
                Page = 1,
                PageSize = int.MaxValue, // trả tất cả
                IsActive = true
            };
            var (entities, _) = await _repository.ListAsync(query);
            return entities.Select(st => new OptionDto
            {
                Value = st.ServiceTypeId,
                Label = st.Name
            });
        }
    }
}
