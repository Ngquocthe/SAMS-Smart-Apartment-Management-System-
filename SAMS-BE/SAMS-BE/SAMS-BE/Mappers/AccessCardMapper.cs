using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class AccessCardMapper
{
    public static AccessCardDto ToDto(this AccessCard accessCard)
    {
        return new AccessCardDto
        {
            CardId = accessCard.CardId,
            CardNumber = accessCard.CardNumber,
            Status = accessCard.Status,
            IssuedToUserId = accessCard.IssuedToUserId,
            IssuedToUserName = accessCard.IssuedToUser != null ? 
                $"{accessCard.IssuedToUser.FirstName} {accessCard.IssuedToUser.LastName}" : null,
            IssuedToApartmentId = accessCard.IssuedToApartmentId,
            IssuedToApartmentNumber = accessCard.IssuedToApartment?.Number,
            IssuedDate = accessCard.IssuedDate,
            ExpiredDate = accessCard.ExpiredDate,
            CreatedAt = accessCard.CreatedAt,
            UpdatedAt = accessCard.UpdatedAt,
            CreatedBy = accessCard.CreatedBy,
            UpdatedBy = accessCard.UpdatedBy,
            IsDelete = accessCard.IsDelete,
            Capabilities = accessCard.AccessCardCapabilities?.Select(acc => new AccessCardCapabilityDto
            {
                CardCapabilityId = acc.CardCapabilityId,
                CardId = acc.CardId,
                CardTypeId = acc.CardTypeId,
                CardTypeName = acc.CardType?.Name,
                CardTypeCode = acc.CardType?.Code,
                IsEnabled = acc.IsEnabled,
                ValidFrom = acc.ValidFrom,
                ValidTo = acc.ValidTo,
                CreatedAt = acc.CreatedAt,
                UpdatedAt = acc.UpdatedAt,
                CreatedBy = acc.CreatedBy,
                UpdatedBy = acc.UpdatedBy
            }).ToList() ?? new List<AccessCardCapabilityDto>()
        };
    }

    public static IEnumerable<AccessCardDto> ToDto(this IEnumerable<AccessCard> accessCards)
    {
        return accessCards.Select(ac => ac.ToDto());
    }

    public static AccessCard ToEntity(this CreateAccessCardDto createDto)
    {
        return new AccessCard
        {
            CardNumber = createDto.CardNumber,
            Status = createDto.Status,
            IssuedToUserId = createDto.IssuedToUserId,
            IssuedToApartmentId = createDto.IssuedToApartmentId,
            ExpiredDate = createDto.ExpiredDate,
            CreatedBy = createDto.CreatedBy,
            IssuedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsDelete = false
        };
    }

    public static AccessCard ToEntity(this UpdateAccessCardDto updateDto)
    {
        return new AccessCard
        {
            CardNumber = updateDto.CardNumber ?? string.Empty,
            Status = updateDto.Status ?? string.Empty,
            IssuedToUserId = updateDto.IssuedToUserId,
            IssuedToApartmentId = updateDto.IssuedToApartmentId,
            ExpiredDate = updateDto.ExpiredDate,
            UpdatedBy = updateDto.UpdatedBy,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
