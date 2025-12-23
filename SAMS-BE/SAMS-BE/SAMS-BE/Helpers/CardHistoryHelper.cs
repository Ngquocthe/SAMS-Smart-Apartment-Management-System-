using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Helpers;

public class CardHistoryHelper
{
    private readonly ICardHistoryService _cardHistoryService;
    private readonly ILogger<CardHistoryHelper> _logger;
    private readonly BuildingManagementContext _context;

    public CardHistoryHelper(ICardHistoryService cardHistoryService, ILogger<CardHistoryHelper> logger, BuildingManagementContext context)
    {
        _cardHistoryService = cardHistoryService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Lưu lịch sử thay đổi chủ sở hữu thẻ
    /// </summary>
    public async Task LogOwnerChangeAsync(Guid cardId, Guid? oldUserId, Guid? newUserId, string? oldUserName, string? newUserName, string changedBy, string? reason = null)
    {
        try
        {
            var dto = new CreateCardHistoryDto
            {
                CardId = cardId,
                EventCode = "OWNER_CHANGE",
                FieldName = "IssuedToUserId",
                OldValue = oldUserId?.ToString(),
                NewValue = newUserId?.ToString(),
                Description = $"Thay đổi chủ sở hữu từ '{oldUserName}' sang '{newUserName}'",
                CreatedBy = changedBy
            };

            if (!string.IsNullOrEmpty(reason))
            {
                dto.Description += $". Lý do: {reason}";
            }

            await _cardHistoryService.CreateCardHistoryAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging owner change for card {CardId}", cardId);
        }
    }

    /// <summary>
    /// Lưu lịch sử thay đổi căn hộ thẻ
    /// </summary>
    public async Task LogApartmentChangeAsync(Guid cardId, Guid? oldApartmentId, Guid? newApartmentId, string? oldApartmentNumber, string? newApartmentNumber, string changedBy, string? reason = null)
    {
        try
        {
            var dto = new CreateCardHistoryDto
            {
                CardId = cardId,
                EventCode = "APARTMENT_CHANGE",
                FieldName = "IssuedToApartmentId",
                OldValue = oldApartmentId?.ToString(),
                NewValue = newApartmentId?.ToString(),
                Description = $"Thay đổi căn hộ từ '{oldApartmentNumber}' sang '{newApartmentNumber}'",
                CreatedBy = changedBy
            };

            if (!string.IsNullOrEmpty(reason))
            {
                dto.Description += $". Lý do: {reason}";
            }

            await _cardHistoryService.CreateCardHistoryAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging apartment change for card {CardId}", cardId);
        }
    }

    /// <summary>
    /// Lưu lịch sử thay đổi trạng thái thẻ
    /// </summary>
    public async Task LogStatusChangeAsync(Guid cardId, string oldStatus, string newStatus, string changedBy, string? reason = null)
    {
        try
        {
            var dto = new CreateCardHistoryDto
            {
                CardId = cardId,
                EventCode = "STATUS_CHANGE",
                FieldName = "Status",
                OldValue = oldStatus,
                NewValue = newStatus,
                Description = $"Thay đổi trạng thái từ '{oldStatus}' sang '{newStatus}'",
                CreatedBy = changedBy
            };

            if (!string.IsNullOrEmpty(reason))
            {
                dto.Description += $". Lý do: {reason}";
            }

            await _cardHistoryService.CreateCardHistoryAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging status change for card {CardId}", cardId);
        }
    }

    /// <summary>
    /// Lưu lịch sử thay đổi ngày hết hạn thẻ
    /// </summary>
    public async Task LogExpiryChangeAsync(Guid cardId, DateTime? oldExpiryDate, DateTime? newExpiryDate, string changedBy, string? reason = null)
    {
        try
        {
            var dto = new CreateCardHistoryDto
            {
                CardId = cardId,
                EventCode = "EXPIRY_CHANGE",
                FieldName = "ExpiredDate",
                OldValue = oldExpiryDate?.ToString("yyyy-MM-dd"),
                NewValue = newExpiryDate?.ToString("yyyy-MM-dd"),
                Description = $"Thay đổi ngày hết hạn từ '{oldExpiryDate?.ToString("dd/MM/yyyy")}' sang '{newExpiryDate?.ToString("dd/MM/yyyy")}'",
                CreatedBy = changedBy
            };

            if (!string.IsNullOrEmpty(reason))
            {
                dto.Description += $". Lý do: {reason}";
            }

            await _cardHistoryService.CreateCardHistoryAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging expiry change for card {CardId}", cardId);
        }
    }

    /// <summary>
    /// Lưu lịch sử thay đổi quyền hạn thẻ
    /// Tạo bản ghi riêng cho mỗi chức năng được thêm/xóa để ngắn gọn và dễ hiểu
    /// </summary>
    public async Task LogCapabilityChangeAsync(Guid cardId, List<Guid> oldCapabilityIds, List<Guid> newCapabilityIds, string changedBy, string? reason = null)
    {
        try
        {
            // Lấy tên capabilities từ database
            var oldCapabilityNames = await GetCapabilityNamesAsync(oldCapabilityIds);
            var newCapabilityNames = await GetCapabilityNamesAsync(newCapabilityIds);

            // Tìm các chức năng được thêm (có trong mới nhưng không có trong cũ)
            var addedCapabilities = newCapabilityIds.Except(oldCapabilityIds).ToList();
            var addedCapabilityNames = await GetCapabilityNamesAsync(addedCapabilities);

            // Tìm các chức năng bị xóa (có trong cũ nhưng không có trong mới)
            var removedCapabilities = oldCapabilityIds.Except(newCapabilityIds).ToList();
            var removedCapabilityNames = await GetCapabilityNamesAsync(removedCapabilities);

            // Tạo bản ghi lịch sử cho mỗi chức năng được thêm
            foreach (var capabilityName in addedCapabilityNames)
            {
                var dto = new CreateCardHistoryDto
                {
                    CardId = cardId,
                    EventCode = "CAPABILITY_CHANGE",
                    FieldName = "Capabilities",
                    OldValue = string.Join(", ", oldCapabilityNames),
                    NewValue = string.Join(", ", newCapabilityNames),
                    Description = $"Thêm chức năng \"{capabilityName}\"",
                    CreatedBy = changedBy
                };

                if (!string.IsNullOrEmpty(reason))
                {
                    dto.Description += $". Lý do: {reason}";
                }

                await _cardHistoryService.CreateCardHistoryAsync(dto);
            }

            // Tạo bản ghi lịch sử cho mỗi chức năng bị xóa
            foreach (var capabilityName in removedCapabilityNames)
            {
                var dto = new CreateCardHistoryDto
                {
                    CardId = cardId,
                    EventCode = "CAPABILITY_CHANGE",
                    FieldName = "Capabilities",
                    OldValue = string.Join(", ", oldCapabilityNames),
                    NewValue = string.Join(", ", newCapabilityNames),
                    Description = $"Xóa chức năng \"{capabilityName}\"",
                    CreatedBy = changedBy
                };

                if (!string.IsNullOrEmpty(reason))
                {
                    dto.Description += $". Lý do: {reason}";
                }

                await _cardHistoryService.CreateCardHistoryAsync(dto);
            }

            // Nếu không có thay đổi nào (trường hợp edge case)
            if (!addedCapabilityNames.Any() && !removedCapabilityNames.Any())
            {
                var dto = new CreateCardHistoryDto
                {
                    CardId = cardId,
                    EventCode = "CAPABILITY_CHANGE",
                    FieldName = "Capabilities",
                    OldValue = string.Join(", ", oldCapabilityNames),
                    NewValue = string.Join(", ", newCapabilityNames),
                    Description = $"Thay đổi chức năng thẻ từ '{string.Join(", ", oldCapabilityNames)}' sang '{string.Join(", ", newCapabilityNames)}'",
                    CreatedBy = changedBy
                };

                if (!string.IsNullOrEmpty(reason))
                {
                    dto.Description += $". Lý do: {reason}";
                }

                await _cardHistoryService.CreateCardHistoryAsync(dto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging capability change for card {CardId}", cardId);
        }
    }

    /// <summary>
    /// Lấy tên capabilities từ danh sách ID
    /// </summary>
    private async Task<List<string>> GetCapabilityNamesAsync(List<Guid> capabilityIds)
    {
        try
        {
            if (!capabilityIds.Any())
                return new List<string>();

            var names = await _context.AccessCardTypes
                .Where(ct => capabilityIds.Contains(ct.CardTypeId))
                .Select(ct => ct.Name)
                .ToListAsync();
            return names;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting capability names for IDs: {Ids}", string.Join(",", capabilityIds));
            return capabilityIds.Select(id => $"ID: {id}").ToList();
        }
    }

    /// <summary>
    /// Lưu lịch sử tạo mới thẻ
    /// </summary>
    public async Task LogCardCreatedAsync(Guid cardId, string cardNumber, string createdBy, string? reason = null)
    {
        try
        {
            var dto = new CreateCardHistoryDto
            {
                CardId = cardId,
                EventCode = "CARD_CREATED",
                FieldName = "CardNumber",
                OldValue = null,
                NewValue = cardNumber,
                Description = $"Tạo mới thẻ '{cardNumber}'",
                CreatedBy = createdBy
            };

            if (!string.IsNullOrEmpty(reason))
            {
                dto.Description += $". Lý do: {reason}";
            }

            await _cardHistoryService.CreateCardHistoryAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging card creation for card {CardId}", cardId);
        }
    }

    /// <summary>
    /// Lưu lịch sử kích hoạt/vô hiệu hóa thẻ
    /// </summary>
    public async Task LogCardActivationAsync(Guid cardId, bool isActivated, string changedBy, string? reason = null)
    {
        try
        {
            var eventCode = isActivated ? "CARD_ACTIVATED" : "CARD_DEACTIVATED";
            var action = isActivated ? "Kích hoạt" : "Vô hiệu hóa";

            var dto = new CreateCardHistoryDto
            {
                CardId = cardId,
                EventCode = eventCode,
                FieldName = "Status",
                OldValue = isActivated ? "INACTIVE" : "ACTIVE",
                NewValue = isActivated ? "ACTIVE" : "INACTIVE",
                Description = $"{action} thẻ",
                CreatedBy = changedBy
            };

            if (!string.IsNullOrEmpty(reason))
            {
                dto.Description += $". Lý do: {reason}";
            }

            await _cardHistoryService.CreateCardHistoryAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging card activation for card {CardId}", cardId);
        }
    }

    /// <summary>
    /// Lưu lịch sử thay đổi số thẻ
    /// </summary>
    public async Task LogCardNumberChangeAsync(Guid cardId, string oldCardNumber, string newCardNumber, string changedBy, string? reason = null)
    {
        try
        {
            var dto = new CreateCardHistoryDto
            {
                CardId = cardId,
                EventCode = "CARD_NUMBER_CHANGE",
                FieldName = "CardNumber",
                OldValue = oldCardNumber,
                NewValue = newCardNumber,
                Description = $"Thay đổi số thẻ từ '{oldCardNumber}' sang '{newCardNumber}'",
                CreatedBy = changedBy
            };

            if (!string.IsNullOrEmpty(reason))
            {
                dto.Description += $". Lý do: {reason}";
            }

            await _cardHistoryService.CreateCardHistoryAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging card number change for card {CardId}", cardId);
        }
    }

    /// <summary>
    /// Lưu lịch sử thay đổi ngày cấp thẻ
    /// </summary>
    public async Task LogIssuedDateChangeAsync(Guid cardId, DateTime oldIssuedDate, DateTime newIssuedDate, string changedBy, string? reason = null)
    {
        try
        {
            var dto = new CreateCardHistoryDto
            {
                CardId = cardId,
                EventCode = "ISSUED_DATE_CHANGE",
                FieldName = "IssuedDate",
                OldValue = oldIssuedDate.ToString("yyyy-MM-dd"),
                NewValue = newIssuedDate.ToString("yyyy-MM-dd"),
                Description = $"Thay đổi ngày cấp từ '{oldIssuedDate.ToString("dd/MM/yyyy")}' sang '{newIssuedDate.ToString("dd/MM/yyyy")}'",
                CreatedBy = changedBy
            };

            if (!string.IsNullOrEmpty(reason))
            {
                dto.Description += $". Lý do: {reason}";
            }

            await _cardHistoryService.CreateCardHistoryAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging issued date change for card {CardId}", cardId);
        }
    }

    /// <summary>
    /// Lưu lịch sử thay đổi người cập nhật
    /// </summary>
    public async Task LogUpdatedByChangeAsync(Guid cardId, string? oldUpdatedBy, string newUpdatedBy, string changedBy, string? reason = null)
    {
        try
        {
            var dto = new CreateCardHistoryDto
            {
                CardId = cardId,
                EventCode = "UPDATED_BY_CHANGE",
                FieldName = "UpdatedBy",
                OldValue = oldUpdatedBy,
                NewValue = newUpdatedBy,
                Description = $"Thay đổi người cập nhật từ '{oldUpdatedBy}' sang '{newUpdatedBy}'",
                CreatedBy = changedBy
            };

            if (!string.IsNullOrEmpty(reason))
            {
                dto.Description += $". Lý do: {reason}";
            }

            await _cardHistoryService.CreateCardHistoryAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging updated by change for card {CardId}", cardId);
        }
    }


    /// <summary>
    /// Lưu lịch sử thay đổi tổng quát cho bất kỳ trường nào
    /// </summary>
    public async Task LogFieldChangeAsync(Guid cardId, string fieldName, string? oldValue, string? newValue, string eventCode, string changedBy, string? reason = null)
    {
        try
        {
            var dto = new CreateCardHistoryDto
            {
                CardId = cardId,
                EventCode = eventCode,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                Description = $"Thay đổi {GetFieldDisplayName(fieldName)} từ '{oldValue}' sang '{newValue}'",
                CreatedBy = changedBy
            };

            if (!string.IsNullOrEmpty(reason))
            {
                dto.Description += $". Lý do: {reason}";
            }

            await _cardHistoryService.CreateCardHistoryAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging field change for card {CardId}, field {FieldName}", cardId, fieldName);
        }
    }

    private static string GetFieldDisplayName(string fieldName)
    {
        return fieldName switch
        {
            "CardNumber" => "Số thẻ",
            "Status" => "Trạng thái",
            "IssuedToUserId" => "Chủ sở hữu",
            "IssuedToApartmentId" => "Căn hộ",
            "IssuedDate" => "Ngày cấp",
            "ExpiredDate" => "Ngày hết hạn",
            "UpdatedBy" => "Người cập nhật",
            "IsDelete" => "Trạng thái xóa",
            _ => fieldName
        };
    }
}
