using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using SAMS_BE.Mappers;

namespace SAMS_BE.Repositories;

public class CardHistoryRepository : ICardHistoryRepository
{
    private readonly BuildingManagementContext _context;

    public CardHistoryRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CardHistory>> GetCardHistoriesAsync(CardHistoryQueryDto query)
    {
        var cardHistories = _context.CardHistories
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToUser)
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToApartment)
            .Where(ch => !ch.IsDelete);

        // Apply filters
        if (query.CardId.HasValue)
            cardHistories = cardHistories.Where(ch => ch.CardId == query.CardId.Value);

        if (query.UserId.HasValue)
            cardHistories = cardHistories.Where(ch => ch.Card.IssuedToUserId == query.UserId.Value);

        if (query.ApartmentId.HasValue)
            cardHistories = cardHistories.Where(ch => ch.Card.IssuedToApartmentId == query.ApartmentId.Value);

        if (!string.IsNullOrEmpty(query.EventCode))
            cardHistories = cardHistories.Where(ch => ch.EventCode == query.EventCode);

        if (!string.IsNullOrEmpty(query.FieldName))
            cardHistories = cardHistories.Where(ch => ch.FieldName == query.FieldName);

        if (query.FromDate.HasValue)
            cardHistories = cardHistories.Where(ch => ch.EventTimeUtc >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            cardHistories = cardHistories.Where(ch => ch.EventTimeUtc <= query.ToDate.Value);

        // Apply sorting
        if (!string.IsNullOrEmpty(query.SortBy))
        {
            switch (query.SortBy.ToLower())
            {
                case "eventtimeutc":
                    cardHistories = query.SortDirection?.ToLower() == "asc" 
                        ? cardHistories.OrderBy(ch => ch.EventTimeUtc)
                        : cardHistories.OrderByDescending(ch => ch.EventTimeUtc);
                    break;
                case "fieldname":
                    cardHistories = query.SortDirection?.ToLower() == "asc"
                        ? cardHistories.OrderBy(ch => ch.FieldName)
                        : cardHistories.OrderByDescending(ch => ch.FieldName);
                    break;
                case "eventcode":
                    cardHistories = query.SortDirection?.ToLower() == "asc"
                        ? cardHistories.OrderBy(ch => ch.EventCode)
                        : cardHistories.OrderByDescending(ch => ch.EventCode);
                    break;
                default:
                    cardHistories = cardHistories.OrderByDescending(ch => ch.EventTimeUtc);
                    break;
            }
        }

        return await cardHistories.ToListAsync();
    }

    public async Task<CardHistory?> GetCardHistoryByIdAsync(Guid id)
    {
        return await _context.CardHistories
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToUser)
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToApartment)
            .Where(ch => ch.CardHistoryId == id && !ch.IsDelete)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CardHistory>> GetCardHistoriesByCardIdAsync(Guid cardId)
    {
        return await _context.CardHistories
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToUser)
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToApartment)
            .Where(ch => ch.CardId == cardId && !ch.IsDelete)
            .OrderByDescending(ch => ch.EventTimeUtc)
            .ToListAsync();
    }

    public async Task<IEnumerable<CardHistory>> GetCardHistoriesByUserIdAsync(Guid userId)
    {
        return await _context.CardHistories
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToUser)
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToApartment)
            .Where(ch => ch.Card.IssuedToUserId == userId && !ch.IsDelete)
            .OrderByDescending(ch => ch.EventTimeUtc)
            .ToListAsync();
    }

    public async Task<IEnumerable<CardHistory>> GetCardHistoriesByApartmentIdAsync(Guid apartmentId)
    {
        return await _context.CardHistories
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToUser)
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToApartment)
            .Where(ch => ch.Card.IssuedToApartmentId == apartmentId && !ch.IsDelete)
            .OrderByDescending(ch => ch.EventTimeUtc)
            .ToListAsync();
    }

    public async Task<IEnumerable<CardHistory>> GetCardHistoriesByFieldNameAsync(string fieldName)
    {
        return await _context.CardHistories
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToUser)
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToApartment)
            .Where(ch => ch.FieldName == fieldName && !ch.IsDelete)
            .OrderByDescending(ch => ch.EventTimeUtc)
            .ToListAsync();
    }

    public async Task<IEnumerable<CardHistory>> GetCardHistoriesByEventCodeAsync(string eventCode)
    {
        return await _context.CardHistories
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToUser)
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToApartment)
            .Where(ch => ch.EventCode == eventCode && !ch.IsDelete)
            .OrderByDescending(ch => ch.EventTimeUtc)
            .ToListAsync();
    }

    public async Task<IEnumerable<CardHistory>> GetCardHistoriesByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.CardHistories
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToUser)
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToApartment)
            .Where(ch => ch.EventTimeUtc >= fromDate && ch.EventTimeUtc <= toDate && !ch.IsDelete)
            .OrderByDescending(ch => ch.EventTimeUtc)
            .ToListAsync();
    }

    public async Task<CardHistory> CreateCardHistoryAsync(CreateCardHistoryDto dto)
    {
        var cardHistory = dto.ToEntity();
        _context.CardHistories.Add(cardHistory);
        await _context.SaveChangesAsync();
        return cardHistory;
    }

    public async Task<bool> UpdateCardHistoryAsync(Guid id, CreateCardHistoryDto dto)
    {
        var existingHistory = await _context.CardHistories
            .Where(ch => ch.CardHistoryId == id && !ch.IsDelete)
            .FirstOrDefaultAsync();

        if (existingHistory == null)
            return false;

        existingHistory.EventCode = dto.EventCode;
        existingHistory.EventTimeUtc = dto.EventTimeUtc ?? DateTime.UtcNow.AddHours(7); // Lưu giờ Việt Nam (UTC+7)
        existingHistory.FieldName = dto.FieldName;
        existingHistory.OldValue = dto.OldValue;
        existingHistory.NewValue = dto.NewValue;
        existingHistory.Description = dto.Description;
        existingHistory.ValidFrom = dto.ValidFrom;
        existingHistory.ValidTo = dto.ValidTo;
        existingHistory.CreatedBy = dto.CreatedBy;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SoftDeleteCardHistoryAsync(Guid id)
    {
        var cardHistory = await _context.CardHistories
            .Where(ch => ch.CardHistoryId == id && !ch.IsDelete)
            .FirstOrDefaultAsync();

        if (cardHistory == null)
            return false;

        cardHistory.IsDelete = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResult<CardHistory>> GetPagedCardHistoriesAsync(CardHistoryQueryDto query)
    {
        var cardHistories = _context.CardHistories
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToUser)
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToApartment)
            .Where(ch => !ch.IsDelete);

        // Apply filters (same as GetCardHistoriesAsync)
        if (query.CardId.HasValue)
            cardHistories = cardHistories.Where(ch => ch.CardId == query.CardId.Value);

        if (query.UserId.HasValue)
            cardHistories = cardHistories.Where(ch => ch.Card.IssuedToUserId == query.UserId.Value);

        if (query.ApartmentId.HasValue)
            cardHistories = cardHistories.Where(ch => ch.Card.IssuedToApartmentId == query.ApartmentId.Value);

        if (!string.IsNullOrEmpty(query.EventCode))
            cardHistories = cardHistories.Where(ch => ch.EventCode == query.EventCode);

        if (!string.IsNullOrEmpty(query.FieldName))
            cardHistories = cardHistories.Where(ch => ch.FieldName == query.FieldName);

        if (query.FromDate.HasValue)
            cardHistories = cardHistories.Where(ch => ch.EventTimeUtc >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            cardHistories = cardHistories.Where(ch => ch.EventTimeUtc <= query.ToDate.Value);

        // Apply sorting
        if (!string.IsNullOrEmpty(query.SortBy))
        {
            switch (query.SortBy.ToLower())
            {
                case "eventtimeutc":
                    cardHistories = query.SortDirection?.ToLower() == "asc"
                        ? cardHistories.OrderBy(ch => ch.EventTimeUtc)
                        : cardHistories.OrderByDescending(ch => ch.EventTimeUtc);
                    break;
                case "fieldname":
                    cardHistories = query.SortDirection?.ToLower() == "asc"
                        ? cardHistories.OrderBy(ch => ch.FieldName)
                        : cardHistories.OrderByDescending(ch => ch.FieldName);
                    break;
                case "eventcode":
                    cardHistories = query.SortDirection?.ToLower() == "asc"
                        ? cardHistories.OrderBy(ch => ch.EventCode)
                        : cardHistories.OrderByDescending(ch => ch.EventCode);
                    break;
                default:
                    cardHistories = cardHistories.OrderByDescending(ch => ch.EventTimeUtc);
                    break;
            }
        }

        var totalCount = await cardHistories.CountAsync();
        var items = await cardHistories
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<CardHistory>
        {
            Items = items,
            TotalCount = totalCount,
            TotalItems = totalCount,
            PageNumber = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
        };
    }

    public async Task<IEnumerable<CardAccessSummaryDto>> GetCardAccessSummaryAsync(Guid? cardId = null, Guid? userId = null, Guid? apartmentId = null)
    {
        var query = _context.CardHistories
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToUser)
            .Include(ch => ch.Card)
                .ThenInclude(c => c.IssuedToApartment)
            .Where(ch => !ch.IsDelete);

        if (cardId.HasValue)
            query = query.Where(ch => ch.CardId == cardId.Value);

        if (userId.HasValue)
            query = query.Where(ch => ch.Card.IssuedToUserId == userId.Value);

        if (apartmentId.HasValue)
            query = query.Where(ch => ch.Card.IssuedToApartmentId == apartmentId.Value);

        var histories = await query.ToListAsync();

        return histories
            .GroupBy(ch => ch.CardId)
            .Select(g => new CardAccessSummaryDto
            {
                CardId = g.Key,
                CardNumber = g.First().Card.CardNumber,
                UserName = g.First().Card.IssuedToUser != null ? 
                    $"{g.First().Card.IssuedToUser.FirstName} {g.First().Card.IssuedToUser.LastName}" : null,
                ApartmentNumber = g.First().Card.IssuedToApartment?.Number,
                TotalAccess = g.Count(),
                SuccessfulAccess = g.Count(ch => ch.EventCode != "DENIED_ACCESS"),
                FailedAccess = g.Count(ch => ch.EventCode == "DENIED_ACCESS"),
                LastAccessTime = g.Max(ch => ch.EventTimeUtc),
                AccessedAreas = g                .Where(ch => !string.IsNullOrEmpty(ch.FieldName))
                .Select(ch => ch.FieldName!)
                    .Distinct()
                    .ToList(),
                EventTypes = g.Select(ch => ch.EventCode)
                    .Distinct()
                    .ToList()
            })
            .ToList();
    }
}
