using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using SAMS_BE.Mappers;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Services;

public class CardHistoryService : ICardHistoryService
{
    private readonly ICardHistoryRepository _cardHistoryRepository;
    private readonly ILogger<CardHistoryService> _logger;
    private readonly BuildingManagementContext _context;

    public CardHistoryService(ICardHistoryRepository cardHistoryRepository, ILogger<CardHistoryService> logger, BuildingManagementContext context)
    {
        _cardHistoryRepository = cardHistoryRepository;
        _logger = logger;
        _context = context;
    }

    public async Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesAsync(CardHistoryQueryDto query)
    {
        try
        {
            var cardHistories = await _cardHistoryRepository.GetCardHistoriesAsync(query);
            var dtos = cardHistories.Select(ch => ch.ToResponseDto()).ToList();
            
            // Lấy tên người quản lý từ CreatedBy (email hoặc username)
            await PopulateCreatedByNameAsync(dtos);
            
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting card histories");
            throw;
        }
    }

    public async Task<CardHistoryResponseDto?> GetCardHistoryByIdAsync(Guid id)
    {
        try
        {
            var cardHistory = await _cardHistoryRepository.GetCardHistoryByIdAsync(id);
            if (cardHistory == null)
                return null;
                
            var dto = cardHistory.ToResponseDto();
            await PopulateCreatedByNameAsync(new List<CardHistoryResponseDto> { dto });
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting card history by ID: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByCardIdAsync(Guid cardId)
    {
        try
        {
            var cardHistories = await _cardHistoryRepository.GetCardHistoriesByCardIdAsync(cardId);
            var dtos = cardHistories.Select(ch => ch.ToResponseDto()).ToList();
            
            // Lấy tên người quản lý từ CreatedBy (email hoặc username)
            await PopulateCreatedByNameAsync(dtos);
            
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting card histories by card ID: {CardId}", cardId);
            throw;
        }
    }

    /// <summary>
    /// Populate CreatedByName từ CreatedBy (email hoặc username)
    /// </summary>
    private async Task PopulateCreatedByNameAsync(List<CardHistoryResponseDto> dtos)
    {
        try
        {
            // Lấy danh sách CreatedBy values (loại bỏ null và duplicate)
            var createdByValues = dtos
                .Where(d => !string.IsNullOrEmpty(d.CreatedBy))
                .Select(d => d.CreatedBy!)
                .Distinct()
                .ToList();

            if (!createdByValues.Any())
                return;

            var userMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            var users = await _context.Users
                .Where(u => createdByValues.Contains(u.Email) || createdByValues.Contains(u.Username))
                .ToListAsync();

            foreach (var user in users)
            {
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (!string.IsNullOrEmpty(fullName))
                {
                    if (!string.IsNullOrEmpty(user.Email))
                        userMap[user.Email] = fullName;
                    if (!string.IsNullOrEmpty(user.Username))
                        userMap[user.Username] = fullName;
                }
            }

            var remainingValues = createdByValues
                .Where(v => !userMap.ContainsKey(v))
                .ToList();

            if (remainingValues.Any())
            {
                var buildingManagerValues = remainingValues
                    .Where(v => v.Equals("buildingmanager", StringComparison.OrdinalIgnoreCase) || 
                               v.Equals("System", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (buildingManagerValues.Any())
                {
                    var buildingManagerUser = await _context.StaffProfiles
                        .Include(sp => sp.User)
                        .Include(sp => sp.Role)
                        .Where(sp => sp.User != null && 
                                    sp.IsActive &&
                                    (sp.Role.RoleKey.ToLower().Contains("building_manager") ||
                                     sp.Role.RoleKey.ToLower().Contains("buildingmanager") ||
                                     sp.Role.RoleName.ToLower().Contains("quản lý") ||
                                     sp.Role.RoleName.ToLower().Contains("building manager")))
                        .Select(sp => sp.User!)
                        .FirstOrDefaultAsync();

                    if (buildingManagerUser != null)
                    {
                        var fullName = $"{buildingManagerUser.FirstName} {buildingManagerUser.LastName}".Trim();
                        if (!string.IsNullOrEmpty(fullName))
                        {
                            foreach (var value in buildingManagerValues)
                            {
                                userMap[value] = fullName;
                            }
                        }
                    }
                }
            }

            // Map tên vào DTOs
            foreach (var dto in dtos)
            {
                if (!string.IsNullOrEmpty(dto.CreatedBy))
                {
                    if (userMap.TryGetValue(dto.CreatedBy, out var name))
                    {
                        dto.CreatedByName = name;
                    }
                    else
                    {
                        // Nếu không tìm thấy, set mặc định
                        dto.CreatedByName = dto.CreatedBy.Equals("System", StringComparison.OrdinalIgnoreCase) || 
                                          dto.CreatedBy.Equals("buildingmanager", StringComparison.OrdinalIgnoreCase)
                            ? "building management" 
                            : dto.CreatedBy;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while populating CreatedByName");
            // Không throw, chỉ log lỗi để không ảnh hưởng đến flow chính
        }
    }

    public async Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByUserIdAsync(Guid userId)
    {
        try
        {
            var cardHistories = await _cardHistoryRepository.GetCardHistoriesByUserIdAsync(userId);
            var dtos = cardHistories.Select(ch => ch.ToResponseDto()).ToList();
            await PopulateCreatedByNameAsync(dtos);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting card histories by user ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByApartmentIdAsync(Guid apartmentId)
    {
        try
        {
            var cardHistories = await _cardHistoryRepository.GetCardHistoriesByApartmentIdAsync(apartmentId);
            var dtos = cardHistories.Select(ch => ch.ToResponseDto()).ToList();
            await PopulateCreatedByNameAsync(dtos);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting card histories by apartment ID: {ApartmentId}", apartmentId);
            throw;
        }
    }

    public async Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByFieldNameAsync(string fieldName)
    {
        try
        {
            var cardHistories = await _cardHistoryRepository.GetCardHistoriesByFieldNameAsync(fieldName);
            var dtos = cardHistories.Select(ch => ch.ToResponseDto()).ToList();
            await PopulateCreatedByNameAsync(dtos);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting card histories by field name: {FieldName}", fieldName);
            throw;
        }
    }

    public async Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByEventCodeAsync(string eventCode)
    {
        try
        {
            var cardHistories = await _cardHistoryRepository.GetCardHistoriesByEventCodeAsync(eventCode);
            var dtos = cardHistories.Select(ch => ch.ToResponseDto()).ToList();
            await PopulateCreatedByNameAsync(dtos);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting card histories by event code: {EventCode}", eventCode);
            throw;
        }
    }

    public async Task<IEnumerable<CardHistoryResponseDto>> GetCardHistoriesByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var cardHistories = await _cardHistoryRepository.GetCardHistoriesByDateRangeAsync(fromDate, toDate);
            var dtos = cardHistories.Select(ch => ch.ToResponseDto()).ToList();
            await PopulateCreatedByNameAsync(dtos);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting card histories by date range: {FromDate} to {ToDate}", fromDate, toDate);
            throw;
        }
    }

    public async Task<CardHistoryResponseDto> CreateCardHistoryAsync(CreateCardHistoryDto dto)
    {
        try
        {
            var cardHistory = await _cardHistoryRepository.CreateCardHistoryAsync(dto);
            var responseDto = cardHistory.ToResponseDto();
            await PopulateCreatedByNameAsync(new List<CardHistoryResponseDto> { responseDto });
            return responseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating card history");
            throw;
        }
    }

    public async Task<bool> UpdateCardHistoryAsync(Guid id, CreateCardHistoryDto dto)
    {
        try
        {
            return await _cardHistoryRepository.UpdateCardHistoryAsync(id, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating card history: {Id}", id);
            throw;
        }
    }

    public async Task<bool> SoftDeleteCardHistoryAsync(Guid id)
    {
        try
        {
            return await _cardHistoryRepository.SoftDeleteCardHistoryAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while soft deleting card history: {Id}", id);
            throw;
        }
    }

    public async Task<PagedResult<CardHistoryResponseDto>> GetPagedCardHistoriesAsync(CardHistoryQueryDto query)
    {
        try
        {
            var pagedResult = await _cardHistoryRepository.GetPagedCardHistoriesAsync(query);
            var dtos = pagedResult.Items.Select(ch => ch.ToResponseDto()).ToList();
            await PopulateCreatedByNameAsync(dtos);
            
            return new PagedResult<CardHistoryResponseDto>
            {
                Items = dtos,
                TotalCount = pagedResult.TotalCount,
                TotalItems = pagedResult.TotalCount,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize,
                TotalPages = pagedResult.TotalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting paged card histories");
            throw;
        }
    }

    public async Task<IEnumerable<CardAccessSummaryDto>> GetCardAccessSummaryAsync(Guid? cardId = null, Guid? userId = null, Guid? apartmentId = null)
    {
        try
        {
            return await _cardHistoryRepository.GetCardAccessSummaryAsync(cardId, userId, apartmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting card access summary");
            throw;
        }
    }

    public async Task<IEnumerable<CardHistoryResponseDto>> GetRecentCardAccessAsync(Guid cardId, int limit = 10)
    {
        try
        {
            var query = new CardHistoryQueryDto
            {
                CardId = cardId,
                PageSize = limit,
                SortBy = "EventTimeUtc",
                SortDirection = "desc"
            };

            var cardHistories = await _cardHistoryRepository.GetCardHistoriesAsync(query);
            var dtos = cardHistories.Take(limit).Select(ch => ch.ToResponseDto()).ToList();
            await PopulateCreatedByNameAsync(dtos);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting recent card access for card ID: {CardId}", cardId);
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetAccessStatisticsAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var cardHistories = await _cardHistoryRepository.GetCardHistoriesByDateRangeAsync(fromDate, toDate);
            
            var statistics = new Dictionary<string, int>
            {
                ["TotalAccess"] = cardHistories.Count(),
                ["SuccessfulAccess"] = cardHistories.Count(ch => ch.EventCode != "DENIED_ACCESS"),
                ["FailedAccess"] = cardHistories.Count(ch => ch.EventCode == "DENIED_ACCESS"),
                ["UniqueCards"] = cardHistories.Select(ch => ch.CardId).Distinct().Count(),
                ["UniqueAreas"] = cardHistories.Where(ch => !string.IsNullOrEmpty(ch.FieldName))
                    .Select(ch => ch.FieldName!).Distinct().Count()
            };

            // Add event code statistics
            var eventCodeStats = cardHistories
                .GroupBy(ch => ch.EventCode)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var stat in eventCodeStats)
            {
                statistics[$"Event_{stat.Key}"] = stat.Value;
            }

            // Add field name statistics
            var fieldNameStats = cardHistories
                .Where(ch => !string.IsNullOrEmpty(ch.FieldName))
                .GroupBy(ch => ch.FieldName!)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var stat in fieldNameStats)
            {
                statistics[$"Field_{stat.Key}"] = stat.Value;
            }

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting access statistics from {FromDate} to {ToDate}", fromDate, toDate);
            throw;
        }
    }
}
