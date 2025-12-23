// Services/ServicePriceService.cs
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Mappers;
using SAMS_BE.Models;

namespace SAMS_BE.Services
{
    public class ServicePriceService : IServicePriceService
    {
        private readonly IServicePriceRepository _repo;
        private readonly IServiceTypeRepository _typeRepo;

        public ServicePriceService(IServicePriceRepository repo, IServiceTypeRepository typeRepo)
        { _repo = repo; _typeRepo = typeRepo; }

        public async Task<PagedResult<ServicePriceResponseDto>> ListAsync(Guid serviceTypeId, ServicePriceListQueryDto query)
        {
            if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
                throw new ArgumentException("FromDate must be <= ToDate.");

            var (items, total) = await _repo.ListAsync(serviceTypeId, query);
            var page = query.Page < 1 ? 1 : query.Page;
            var size = query.PageSize < 1 ? 20 : query.PageSize;

            return new PagedResult<ServicePriceResponseDto>
            {
                Items = items.Select(x => x.ToDto()),
                TotalItems = total,
                PageNumber = page,
                PageSize = size
            };
        }

        public async Task<ServicePriceResponseDto> CreateAsync(Guid serviceTypeId, CreateServicePriceDto dto, bool autoClosePrevious = true)
        {
            if (dto.UnitPrice <= 0) throw new ArgumentException("Unit price must be > 0.");
            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.EffectiveDate)
                throw new ArgumentException("EndDate must be >= EffectiveDate.");

            var st = await _typeRepo.GetByIdForUpdateAsync(serviceTypeId)
                     ?? throw new KeyNotFoundException("Service type not found.");
            if (st.IsDelete == true) throw new InvalidOperationException("Service type was deleted.");
            // if (!st.IsActive) throw new InvalidOperationException("Service type is inactive.");

            var overlap = await _repo.AnyOverlapAsync(serviceTypeId, dto.EffectiveDate, dto.EndDate);
            if (overlap)
            {
                var open = await _repo.GetOpenEndedAsync(serviceTypeId);
                var canAutoClose = autoClosePrevious && open != null && dto.EffectiveDate > open.EffectiveDate;
                if (!canAutoClose)
                    throw new InvalidOperationException("Price period overlaps existing records.");
            }

            var openEnded = await _repo.GetOpenEndedAsync(serviceTypeId);
            if (autoClosePrevious && openEnded != null && dto.EffectiveDate > openEnded.EffectiveDate)
            {
                openEnded.EndDate = dto.EffectiveDate.AddDays(-1);
                openEnded.UpdatedAt = DateTime.UtcNow;
                openEnded.ApprovedDate ??= DateTime.UtcNow;
                await _repo.UpdateAsync(openEnded);
            }

            var e = new ServicePrice
            {
                ServicePrices = Guid.NewGuid(),
                ServiceTypeId = serviceTypeId,
                UnitPrice = Math.Round(dto.UnitPrice, 2, MidpointRounding.AwayFromZero),
                EffectiveDate = dto.EffectiveDate,
                EndDate = dto.EndDate,
                Status = "APPROVED",
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                ApprovedDate = DateTime.UtcNow
            };

            await _repo.AddAsync(e);

            var reloaded = await _repo.GetByIdAsync(e.ServicePrices) ?? e;
            return reloaded.ToDto();
        }

        public async Task<ServicePriceResponseDto?> UpdateAsync(Guid priceId, UpdateServicePriceDto dto)
        {
            if (dto.UnitPrice <= 0) throw new ArgumentException("Unit price must be > 0.");
            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.EffectiveDate)
                throw new ArgumentException("EndDate must be >= EffectiveDate.");

            var e = await _repo.GetByIdAsync(priceId);
            if (e == null) return null;

            if (string.Equals(e.Status, "CANCELED", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This price is canceled and cannot be updated.");

            var overlap = await _repo.AnyOverlapAsync(e.ServiceTypeId, dto.EffectiveDate, dto.EndDate, excludeId: e.ServicePrices);
            if (overlap) throw new InvalidOperationException("Updated period overlaps existing records.");

            e.UnitPrice = Math.Round(dto.UnitPrice, 2, MidpointRounding.AwayFromZero);
            e.EffectiveDate = dto.EffectiveDate;
            e.EndDate = dto.EndDate;
            e.Notes = dto.Notes ?? e.Notes;
            e.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(e);
            var reloaded = await _repo.GetByIdAsync(e.ServicePrices) ?? e;
            return reloaded.ToDto();
        }

        public async Task<bool> CancelAsync(Guid priceId)
        {
            var e = await _repo.GetByIdAsync(priceId);
            if (e == null) return false;

            if (string.Equals(e.Status, "CANCELED", StringComparison.OrdinalIgnoreCase))
                return true; // idempotent

            e.Status = "CANCELED";
            e.EndDate ??= DateOnly.FromDateTime(DateTime.UtcNow.Date);
            e.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(e);
            return true;
        }

        public async Task<decimal?> GetCurrentPriceAsync(Guid serviceTypeId, DateOnly? asOfDate = null)
        {
            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var price = await _repo.GetCurrentPriceAsync(serviceTypeId, checkDate);
            return price?.UnitPrice;
        }
    }
}
