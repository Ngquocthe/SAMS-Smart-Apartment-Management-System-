using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface ICardHistoryRepository
{
    Task<IEnumerable<CardHistory>> GetCardHistoriesAsync(CardHistoryQueryDto query);
    Task<CardHistory?> GetCardHistoryByIdAsync(Guid id);
    Task<IEnumerable<CardHistory>> GetCardHistoriesByCardIdAsync(Guid cardId);
    Task<IEnumerable<CardHistory>> GetCardHistoriesByUserIdAsync(Guid userId);
    Task<IEnumerable<CardHistory>> GetCardHistoriesByApartmentIdAsync(Guid apartmentId);
    Task<IEnumerable<CardHistory>> GetCardHistoriesByFieldNameAsync(string fieldName);
    Task<IEnumerable<CardHistory>> GetCardHistoriesByEventCodeAsync(string eventCode);
    Task<IEnumerable<CardHistory>> GetCardHistoriesByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<CardHistory> CreateCardHistoryAsync(CreateCardHistoryDto dto);
    Task<bool> UpdateCardHistoryAsync(Guid id, CreateCardHistoryDto dto);
    Task<bool> SoftDeleteCardHistoryAsync(Guid id);
    Task<PagedResult<CardHistory>> GetPagedCardHistoriesAsync(CardHistoryQueryDto query);
    Task<IEnumerable<CardAccessSummaryDto>> GetCardAccessSummaryAsync(Guid? cardId = null, Guid? userId = null, Guid? apartmentId = null);
}
