using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class CardHistoryMapper
{
    public static CardHistoryDto ToDto(this CardHistory cardHistory)
    {
        return new CardHistoryDto
        {
            CardHistoryId = cardHistory.CardHistoryId,
            CardId = cardHistory.CardId,
            CardNumber = cardHistory.Card?.CardNumber ?? "",
            UserName = cardHistory.Card?.IssuedToUser != null ? 
                $"{cardHistory.Card.IssuedToUser.FirstName} {cardHistory.Card.IssuedToUser.LastName}" : null,
            ApartmentNumber = cardHistory.Card?.IssuedToApartment?.Number,
            CardTypeId = cardHistory.CardTypeId,
            CardTypeName = null, // Sẽ cần join với AccessCardType nếu cần
            EventCode = cardHistory.EventCode,
            EventName = GetEventName(cardHistory.EventCode),
            EventTimeUtc = cardHistory.EventTimeUtc,
            EventTimeLocal = cardHistory.EventTimeUtc.ToLocalTime(),
            EventTimeVietnam = cardHistory.EventTimeUtc, // DB đã lưu giờ VN rồi, lấy trực tiếp
            FieldName = cardHistory.FieldName,
            FieldDisplayName = GetFieldDisplayName(cardHistory.FieldName),
            OldValue = cardHistory.OldValue,
            NewValue = cardHistory.NewValue,
            Description = cardHistory.Description,
            ValidFrom = cardHistory.ValidFrom,
            ValidTo = cardHistory.ValidTo,
            CreatedBy = cardHistory.CreatedBy,
            CreatedAt = cardHistory.CreatedAt,
            CreatedAtVietnam = cardHistory.CreatedAt, // DB đã lưu giờ VN rồi, lấy trực tiếp
            IsDelete = cardHistory.IsDelete
        };
    }

    public static CardHistoryResponseDto ToResponseDto(this CardHistory cardHistory)
    {
        return new CardHistoryResponseDto
        {
            CardHistoryId = cardHistory.CardHistoryId,
            CardId = cardHistory.CardId,
            CardNumber = cardHistory.Card?.CardNumber ?? "",
            UserName = cardHistory.Card?.IssuedToUser != null ? 
                $"{cardHistory.Card.IssuedToUser.FirstName} {cardHistory.Card.IssuedToUser.LastName}" : null,
            ApartmentNumber = cardHistory.Card?.IssuedToApartment?.Number,
            CardTypeId = cardHistory.CardTypeId,
            CardTypeName = null, // Sẽ cần join với AccessCardType nếu cần
            EventCode = cardHistory.EventCode,
            EventName = GetEventName(cardHistory.EventCode),
            EventTimeUtc = cardHistory.EventTimeUtc,
            EventTimeLocal = cardHistory.EventTimeUtc.ToLocalTime(),
            EventTimeVietnam = cardHistory.EventTimeUtc, // DB đã lưu giờ VN rồi, lấy trực tiếp
            FieldName = cardHistory.FieldName,
            FieldDisplayName = GetFieldDisplayName(cardHistory.FieldName),
            OldValue = cardHistory.OldValue,
            NewValue = cardHistory.NewValue,
            Description = cardHistory.Description,
            ValidFrom = cardHistory.ValidFrom,
            ValidTo = cardHistory.ValidTo,
            CreatedBy = cardHistory.CreatedBy,
            CreatedByName = null, // Sẽ được populate trong Service
            CreatedAt = cardHistory.CreatedAt,
            CreatedAtVietnam = cardHistory.CreatedAt, // DB đã lưu giờ VN rồi, lấy trực tiếp
            IsDelete = cardHistory.IsDelete
        };
    }

    public static IEnumerable<CardHistoryDto> ToDto(this IEnumerable<CardHistory> cardHistories)
    {
        return cardHistories.Select(ch => ch.ToDto());
    }

    public static IEnumerable<CardHistoryResponseDto> ToResponseDto(this IEnumerable<CardHistory> cardHistories)
    {
        return cardHistories.Select(ch => ch.ToResponseDto());
    }

    public static CardHistory ToEntity(this CreateCardHistoryDto dto)
    {
        return new CardHistory
        {
            CardId = dto.CardId,
            CardTypeId = dto.CardTypeId,
            EventCode = dto.EventCode,
            EventTimeUtc = dto.EventTimeUtc ?? DateTime.UtcNow.AddHours(7), // Lưu giờ Việt Nam (UTC+7)
            FieldName = dto.FieldName,
            OldValue = dto.OldValue,
            NewValue = dto.NewValue,
            Description = dto.Description,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow.AddHours(7), // Lưu giờ Việt Nam (UTC+7)
            IsDelete = false
        };
    }

    private static string GetEventName(string eventCode)
    {
        return eventCode switch
        {
            "OWNER_CHANGE" => "Thay đổi chủ sở hữu",
            "APARTMENT_CHANGE" => "Thay đổi căn hộ",
            "CAPABILITY_CHANGE" => "Thay đổi quyền hạn",
            "STATUS_CHANGE" => "Thay đổi trạng thái",
            "EXPIRY_CHANGE" => "Thay đổi ngày hết hạn",
            "CARD_NUMBER_CHANGE" => "Thay đổi số thẻ",
            "ISSUED_DATE_CHANGE" => "Thay đổi ngày cấp",
            "UPDATED_BY_CHANGE" => "Thay đổi người cập nhật",
            "CARD_CREATED" => "Tạo mới thẻ",
            "CARD_ACTIVATED" => "Kích hoạt thẻ",
            "CARD_DEACTIVATED" => "Vô hiệu hóa thẻ",
            "CARD_RENEWED" => "Gia hạn thẻ",
            "CARD_SUSPENDED" => "Tạm ngưng thẻ",
            _ => eventCode
        };
    }

    private static string GetFieldDisplayName(string? fieldName)
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
            "Capabilities" => "Quyền hạn",
            _ => fieldName ?? ""
        };
    }
}
