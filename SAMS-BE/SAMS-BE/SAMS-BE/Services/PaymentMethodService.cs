using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Models;

namespace SAMS_BE.Services
{
    public class PaymentMethodService : IPaymentMethodService
    {
   private readonly IPaymentMethodRepository _repository;

        public PaymentMethodService(IPaymentMethodRepository repository)
  {
         _repository = repository;
}

        public async Task<IReadOnlyList<PaymentMethodDto>> GetAllAsync()
        {
     var paymentMethods = await _repository.GetAllAsync();
return paymentMethods.Select(MapToDto).ToList();
     }

public async Task<IReadOnlyList<PaymentMethodDto>> GetActiveAsync()
   {
        var paymentMethods = await _repository.GetActiveAsync();
  return paymentMethods.Select(MapToDto).ToList();
   }

  public async Task<PaymentMethodDto> GetByIdAsync(Guid id)
     {
       var paymentMethod = await _repository.GetByIdAsync(id);
      if (paymentMethod == null)
 throw new KeyNotFoundException($"Payment Method with ID {id} not found.");

      return MapToDto(paymentMethod);
        }

      public async Task<PaymentMethodDto> GetByCodeAsync(string code)
        {
          var paymentMethod = await _repository.GetByCodeAsync(code);
      if (paymentMethod == null)
    throw new KeyNotFoundException($"Payment Method with code '{code}' not found.");

      return MapToDto(paymentMethod);
    }

   public async Task<PaymentMethodDto> CreateAsync(CreatePaymentMethodDto dto)
        {
  // Validate code uniqueness
            if (await _repository.ExistsByCodeAsync(dto.Code))
       throw new InvalidOperationException($"Payment Method with code '{dto.Code}' already exists.");

   var paymentMethod = new PaymentMethod
            {
      PaymentMethodId = Guid.NewGuid(),
    Code = dto.Code,
    Name = dto.Name,
   Active = dto.Active
};

     var created = await _repository.CreateAsync(paymentMethod);
  return MapToDto(created);
  }

        public async Task<PaymentMethodDto> UpdateAsync(Guid id, UpdatePaymentMethodDto dto)
   {
   var paymentMethod = await _repository.GetByIdAsync(id);
     if (paymentMethod == null)
       throw new KeyNotFoundException($"Payment Method with ID {id} not found.");

    if (dto.Name != null)
      paymentMethod.Name = dto.Name;

          if (dto.Active.HasValue)
          paymentMethod.Active = dto.Active.Value;

   var updated = await _repository.UpdateAsync(paymentMethod);
return MapToDto(updated);
  }

    public async Task DeleteAsync(Guid id)
      {
      var paymentMethod = await _repository.GetByIdAsync(id);
   if (paymentMethod == null)
   throw new KeyNotFoundException($"Payment Method with ID {id} not found.");

            await _repository.DeleteAsync(id);
        }

   // Helper method to map entity to DTO
        private static PaymentMethodDto MapToDto(PaymentMethod paymentMethod)
        {
     return new PaymentMethodDto
            {
           PaymentMethodId = paymentMethod.PaymentMethodId,
       Code = paymentMethod.Code,
      Name = paymentMethod.Name,
    Active = paymentMethod.Active
      };
      }
    }
}
