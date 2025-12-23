using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IService;

public interface ICardHistoryService
{
    Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesAsync(CardHistoryQueryDto query);
    Task<CardHistoryResponseDto?> GetCardHistoryByIdAsync(Guid id);
    Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByCardIdAsync(Guid cardId);
    Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByUserIdAsync(Guid userId);
    Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByApartmentIdAsync(Guid apartmentId);
    Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByFieldNameAsync(string fieldName);
    Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByEventCodeAsync(string eventCode);
    Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<CardHistoryResponseDto> CreateCardHistoryAsync(CreateCardHistoryDto dto);
    Task<bool> UpdateCardHistoryAsync(Guid id, CreateCardHistoryDto dto);
    Task<bool> SoftDeleteCardHistoryAsync(Guid id);
    Task<PagedResult<CardHistoryResponseDto>> GetPagedCardHistoriesAsync(CardHistoryQueryDto query);
    Task<IEnumerable<CardAccessSummaryDto>> GetCardAccessSummaryAsync(Guid? cardId = null, Guid? userId = null, Guid? apartmentId = null);
    Task<IEnumerable<CardHistoryResponseDto>> GetRecentCardAccessAsync(Guid cardId, int limit = 10);
    Task<Dictionary<string, int>> GetAccessStatisticsAsync(DateTime fromDate, DateTime toDate);
}
