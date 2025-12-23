using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
    public interface IServiceTypeRepository
    {
        Task<(IReadOnlyList<ServiceType> Items, int Total)> ListAsync(ServiceTypeListQueryDto query);
        Task<bool> CodeExistsAsync(string code);
        Task<ServiceType> CreateAsync(ServiceType entity);
        Task<ServiceType?> GetByIdForUpdateAsync(Guid id);
        Task<ServiceType> UpdateAsync(ServiceType entity);
        /// <summary>
        /// NEW: Lấy tất cả services định kỳ hàng tháng (bất kể bắt buộc hay không)
        /// Logic kiểm tra điều kiện (như số lượng xe) sẽ nằm trong InvoiceService
        /// </summary>
        Task<List<ServiceType>> GetMonthlyRecurringServicesAsync();
    }
}
