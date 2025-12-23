using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface IAccessCardRepository
{
    Task<IEnumerable<AccessCard>> GetAccessCardsWithDetailsAsync();
    Task<AccessCard?> GetAccessCardWithDetailsByIdAsync(Guid id);
    Task<IEnumerable<AccessCard>> GetAccessCardsByUserIdAsync(Guid userId);
    Task<IEnumerable<AccessCard>> GetAccessCardsByApartmentIdAsync(Guid apartmentId);
    Task<IEnumerable<AccessCard>> GetAccessCardsByStatusAsync(string status);
    Task<IEnumerable<AccessCard>> GetAccessCardsByCardTypeAsync(Guid cardTypeId);
    Task<bool> IsCardNumberExistsAsync(string cardNumber, Guid? excludeId = null);
    Task<AccessCard> CreateAccessCardAsync(AccessCard accessCard, List<Guid> cardTypeIds);
    Task<AccessCard?> UpdateAccessCardAsync(Guid id, AccessCard accessCard, List<Guid>? cardTypeIds = null);
    Task<bool> SoftDeleteAccessCardAsync(Guid id);
}
