using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories;

public class AccessCardRepository : IAccessCardRepository
{
    private readonly BuildingManagementContext _context;

    public AccessCardRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AccessCard>> GetAccessCardsWithDetailsAsync()
    {
        return await _context.AccessCards
            .Include(ac => ac.IssuedToUser)
            .Include(ac => ac.IssuedToApartment)
            .Include(ac => ac.AccessCardCapabilities)
                .ThenInclude(acc => acc.CardType)
            .Where(ac => !ac.IsDelete)
            .OrderByDescending(ac => ac.CreatedAt)
            .ToListAsync();
    }

    public async Task<AccessCard?> GetAccessCardWithDetailsByIdAsync(Guid id)
    {
        return await _context.AccessCards
            .Include(ac => ac.IssuedToUser)
            .Include(ac => ac.IssuedToApartment)
            .Include(ac => ac.AccessCardCapabilities)
                .ThenInclude(acc => acc.CardType)
            .Where(ac => ac.CardId == id && !ac.IsDelete)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<AccessCard>> GetAccessCardsByUserIdAsync(Guid userId)
    {
        return await _context.AccessCards
            .Include(ac => ac.IssuedToUser)
            .Include(ac => ac.IssuedToApartment)
            .Include(ac => ac.AccessCardCapabilities)
                .ThenInclude(acc => acc.CardType)
            .Where(ac => ac.IssuedToUserId == userId && !ac.IsDelete)
            .OrderByDescending(ac => ac.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AccessCard>> GetAccessCardsByApartmentIdAsync(Guid apartmentId)
    {
        return await _context.AccessCards
            .Include(ac => ac.IssuedToUser)
            .Include(ac => ac.IssuedToApartment)
            .Include(ac => ac.AccessCardCapabilities)
                .ThenInclude(acc => acc.CardType)
            .Where(ac => ac.IssuedToApartmentId == apartmentId && !ac.IsDelete)
            .OrderByDescending(ac => ac.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AccessCard>> GetAccessCardsByStatusAsync(string status)
    {
        return await _context.AccessCards
            .Include(ac => ac.IssuedToUser)
            .Include(ac => ac.IssuedToApartment)
            .Include(ac => ac.AccessCardCapabilities)
                .ThenInclude(acc => acc.CardType)
            .Where(ac => ac.Status == status && !ac.IsDelete)
            .OrderByDescending(ac => ac.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AccessCard>> GetAccessCardsByCardTypeAsync(Guid cardTypeId)
    {
        return await _context.AccessCards
            .Include(ac => ac.IssuedToUser)
            .Include(ac => ac.IssuedToApartment)
            .Include(ac => ac.AccessCardCapabilities)
                .ThenInclude(acc => acc.CardType)
            .Where(ac => ac.AccessCardCapabilities.Any(acc => acc.CardTypeId == cardTypeId) && !ac.IsDelete)
            .OrderByDescending(ac => ac.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsCardNumberExistsAsync(string cardNumber, Guid? excludeId = null)
    {
        var query = _context.AccessCards.Where(ac => ac.CardNumber == cardNumber && !ac.IsDelete);
        
        if (excludeId.HasValue)
        {
            query = query.Where(ac => ac.CardId != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<AccessCard> CreateAccessCardAsync(AccessCard accessCard, List<Guid> cardTypeIds)
    {
        try
        {
            // Set default values
            accessCard.CardId = Guid.NewGuid();
            if (accessCard.IssuedDate == default(DateTime))
            {
                accessCard.IssuedDate = DateTime.UtcNow;
            }
            accessCard.CreatedAt = DateTime.UtcNow;
            accessCard.IsDelete = false;

            await _context.AccessCards.AddAsync(accessCard);
            await _context.SaveChangesAsync();

            // Tạo capabilities cho thẻ
            foreach (var cardTypeId in cardTypeIds)
            {
                var capability = new AccessCardCapability
                {
                    CardId = accessCard.CardId,
                    CardTypeId = cardTypeId,
                    IsEnabled = true,
                    ValidFrom = DateTime.UtcNow.AddHours(7), // Giờ Việt Nam (UTC+7)
                    CreatedAt = DateTime.UtcNow.AddHours(7), // Giờ Việt Nam (UTC+7)
                    CreatedBy = accessCard.CreatedBy
                };
                _context.AccessCardCapabilities.Add(capability);
            }

            await _context.SaveChangesAsync();
            return accessCard;
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi tạo access card: {ex.Message}", ex);
        }
    }

    public async Task<AccessCard?> UpdateAccessCardAsync(Guid id, AccessCard accessCard, List<Guid>? cardTypeIds = null)
{
    try
    {
        var existingCard = await _context.AccessCards
            .Where(ac => ac.CardId == id && !ac.IsDelete)
            .FirstOrDefaultAsync();

        if (existingCard == null)
        {
            return null;
        }

        // Update only the fields that are provided (not null)
        if (!string.IsNullOrEmpty(accessCard.CardNumber))
        {
            existingCard.CardNumber = accessCard.CardNumber;
        }
        if (!string.IsNullOrEmpty(accessCard.Status))
        {
            existingCard.Status = accessCard.Status;
        }
        if (accessCard.IssuedToUserId.HasValue)
        {
            existingCard.IssuedToUserId = accessCard.IssuedToUserId;
        }
        if (accessCard.IssuedToApartmentId.HasValue)
        {
            existingCard.IssuedToApartmentId = accessCard.IssuedToApartmentId;
        }
        // Luôn cập nhật IssuedDate nếu có giá trị (không check default vì có thể là ngày hợp lệ)
        // Sử dụng một cách tiếp cận khác: check xem có phải là giá trị mới không
        // Nếu IssuedDate khác với giá trị hiện tại, thì cập nhật
        if (accessCard.IssuedDate != default(DateTime) && accessCard.IssuedDate != existingCard.IssuedDate)
        {
            existingCard.IssuedDate = accessCard.IssuedDate;
        }
        if (accessCard.ExpiredDate.HasValue)
        {
            existingCard.ExpiredDate = accessCard.ExpiredDate;
        }
        if (!string.IsNullOrEmpty(accessCard.UpdatedBy))
        {
            existingCard.UpdatedBy = accessCard.UpdatedBy;
        }

        existingCard.UpdatedAt = DateTime.UtcNow.AddHours(7); // Giờ Việt Nam (UTC+7)

        // ⬇️ SỬA CHỖ NÀY: Thêm check Any()
        // Xử lý capabilities nếu có VÀ không rỗng
        if (cardTypeIds != null && cardTypeIds.Any())
        {
            // Xóa tất cả capabilities cũ
            var existingCapabilities = await _context.AccessCardCapabilities
                .Where(acc => acc.CardId == id)
                .ToListAsync();
            _context.AccessCardCapabilities.RemoveRange(existingCapabilities);

            // Thêm capabilities mới
            foreach (var cardTypeId in cardTypeIds)
            {
                var capability = new AccessCardCapability
                {
                    CardId = id,
                    CardTypeId = cardTypeId,
                    IsEnabled = true,
                    ValidFrom = DateTime.UtcNow.AddHours(7), // Giờ Việt Nam (UTC+7)
                    CreatedAt = DateTime.UtcNow.AddHours(7), // Giờ Việt Nam (UTC+7)
                    CreatedBy = accessCard.UpdatedBy ?? "buildingmanager"
                };
                _context.AccessCardCapabilities.Add(capability);
            }
        }

        await _context.SaveChangesAsync();
        return existingCard;
    }
    catch (Exception ex)
    {
        throw new Exception($"Lỗi khi cập nhật access card: {ex.Message}", ex);
    }
}

    public async Task<bool> SoftDeleteAccessCardAsync(Guid id)
    {
        try
        {
            var accessCard = await _context.AccessCards
                .Where(ac => ac.CardId == id && !ac.IsDelete)
                .FirstOrDefaultAsync();

            if (accessCard == null)
            {
                return false;
            }

            // Soft delete - chỉ đánh dấu IsDelete = true
            accessCard.IsDelete = true;
            accessCard.UpdatedAt = DateTime.UtcNow;
            accessCard.UpdatedBy = "buildingmanager";

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi xóa mềm access card: {ex.Message}", ex);
        }
    }
}
