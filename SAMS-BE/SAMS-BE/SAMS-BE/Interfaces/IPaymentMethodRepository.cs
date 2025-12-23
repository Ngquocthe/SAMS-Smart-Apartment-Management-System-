using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
  public interface IPaymentMethodRepository
    {
        Task<IReadOnlyList<PaymentMethod>> GetAllAsync();
        Task<IReadOnlyList<PaymentMethod>> GetActiveAsync();
        Task<PaymentMethod?> GetByIdAsync(Guid id);
        Task<PaymentMethod?> GetByCodeAsync(string code);
      Task<PaymentMethod> CreateAsync(PaymentMethod paymentMethod);
        Task<PaymentMethod> UpdateAsync(PaymentMethod paymentMethod);
        Task DeleteAsync(Guid id);
  Task<bool> ExistsByCodeAsync(string code);
    }
}
