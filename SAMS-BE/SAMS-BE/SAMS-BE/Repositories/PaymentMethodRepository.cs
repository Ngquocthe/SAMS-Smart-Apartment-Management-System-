using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories
{
    public class PaymentMethodRepository : IPaymentMethodRepository
    {
        private readonly BuildingManagementContext _context;

        public PaymentMethodRepository(BuildingManagementContext context)
 {
            _context = context;
        }

        public async Task<IReadOnlyList<PaymentMethod>> GetAllAsync()
  {
         return await _context.PaymentMethods
             .AsNoTracking()
           .OrderBy(pm => pm.Name)
   .ToListAsync();
     }

        public async Task<IReadOnlyList<PaymentMethod>> GetActiveAsync()
        {
      return await _context.PaymentMethods
        .AsNoTracking()
     .Where(pm => pm.Active)
       .OrderBy(pm => pm.Name)
   .ToListAsync();
        }

    public async Task<PaymentMethod?> GetByIdAsync(Guid id)
        {
            return await _context.PaymentMethods
     .AsNoTracking()
      .FirstOrDefaultAsync(pm => pm.PaymentMethodId == id);
        }

        public async Task<PaymentMethod?> GetByCodeAsync(string code)
        {
    return await _context.PaymentMethods
     .AsNoTracking()
    .FirstOrDefaultAsync(pm => pm.Code == code);
    }

        public async Task<PaymentMethod> CreateAsync(PaymentMethod paymentMethod)
        {
            await _context.PaymentMethods.AddAsync(paymentMethod);
       await _context.SaveChangesAsync();
            return paymentMethod;
        }

 public async Task<PaymentMethod> UpdateAsync(PaymentMethod paymentMethod)
        {
   _context.PaymentMethods.Update(paymentMethod);
            await _context.SaveChangesAsync();
            return paymentMethod;
     }

        public async Task DeleteAsync(Guid id)
        {
        var paymentMethod = await _context.PaymentMethods.FindAsync(id);
     if (paymentMethod != null)
            {
        _context.PaymentMethods.Remove(paymentMethod);
          await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByCodeAsync(string code)
        {
      return await _context.PaymentMethods
     .AnyAsync(pm => pm.Code == code);
        }
    }
}
