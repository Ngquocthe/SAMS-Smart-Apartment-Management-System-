using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces
{
    public interface IPaymentMethodService
    {
        Task<IReadOnlyList<PaymentMethodDto>> GetAllAsync();
        Task<IReadOnlyList<PaymentMethodDto>> GetActiveAsync();
        Task<PaymentMethodDto> GetByIdAsync(Guid id);
    Task<PaymentMethodDto> GetByCodeAsync(string code);
        Task<PaymentMethodDto> CreateAsync(CreatePaymentMethodDto dto);
        Task<PaymentMethodDto> UpdateAsync(Guid id, UpdatePaymentMethodDto dto);
        Task DeleteAsync(Guid id);
    }
}
