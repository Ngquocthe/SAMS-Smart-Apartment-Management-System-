using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService;

public interface IAccessCardService
{
    Task<IEnumerable<AccessCardDto>> GetAccessCardsWithDetailsAsync();
    Task<AccessCardDto?> GetAccessCardWithDetailsByIdAsync(Guid id);
    Task<IEnumerable<AccessCardDto>> GetAccessCardsByUserIdAsync(Guid userId);
    Task<IEnumerable<AccessCardDto>> GetAccessCardsByApartmentIdAsync(Guid apartmentId);
    Task<IEnumerable<AccessCardDto>> GetAccessCardsByStatusAsync(string status);
    Task<IEnumerable<AccessCardDto>> GetAccessCardsByCardTypeAsync(Guid cardTypeId);
    Task<bool> IsCardNumberExistsAsync(string cardNumber, Guid? excludeId = null);
    Task<AccessCardDto> CreateAccessCardAsync(CreateAccessCardDto createAccessCardDto);
    Task<AccessCardDto?> UpdateAccessCardAsync(Guid id, UpdateAccessCardDto updateAccessCardDto);
    Task<bool> SoftDeleteAccessCardAsync(Guid id);
    Task<IEnumerable<CardTypeDto>> GetCardTypesAsync();
    Task<IEnumerable<AccessCardCapabilityDto>> GetCardCapabilitiesAsync(Guid cardId);
    Task<bool> UpdateCardCapabilitiesAsync(Guid cardId, List<Guid> cardTypeIds);
}
