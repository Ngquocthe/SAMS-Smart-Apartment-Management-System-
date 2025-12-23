using SAMS_BE.Models;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Helpers
{
    /// <summary>
    /// Helper để lấy giá từ ServicePrice table
    /// </summary>
  public static class ServicePriceHelper
    {
        /// <summary>
        /// Lấy unit price hiện hành của service tại một thời điểm
        /// </summary>
        /// <param name="context">DbContext</param>
   /// <param name="serviceTypeId">Service Type ID</param>
        /// <param name="asOfDate">Ngày tính giá (mặc định = hôm nay)</param>
        /// <returns>Unit price hoặc null nếu không tìm thấy</returns>
        public static async Task<decimal?> GetCurrentPriceAsync(
BuildingManagementContext context,
        Guid serviceTypeId,
    DateOnly? asOfDate = null)
      {
            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var price = await context.ServicePrices
          .Where(sp => sp.ServiceTypeId == serviceTypeId
   && sp.EffectiveDate <= checkDate
        && (sp.EndDate == null || sp.EndDate >= checkDate)
          && sp.Status == "APPROVED")
    .OrderByDescending(sp => sp.EffectiveDate)
  .Select(sp => sp.UnitPrice)
   .FirstOrDefaultAsync();

            return price;
 }

        /// <summary>
 /// Kiểm tra xem service có giá hiện hành không
     /// </summary>
        public static async Task<bool> HasCurrentPriceAsync(
        BuildingManagementContext context,
   Guid serviceTypeId,
  DateOnly? asOfDate = null)
    {
 var price = await GetCurrentPriceAsync(context, serviceTypeId, asOfDate);
 return price.HasValue;
        }

        /// <summary>
        /// Lấy thông tin đầy đủ của giá hiện hành
      /// </summary>
        public static async Task<ServicePrice?> GetCurrentPriceInfoAsync(
 BuildingManagementContext context,
    Guid serviceTypeId,
            DateOnly? asOfDate = null)
     {
            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

     return await context.ServicePrices
          .Include(sp => sp.ServiceType)
                .Where(sp => sp.ServiceTypeId == serviceTypeId
     && sp.EffectiveDate <= checkDate
  && (sp.EndDate == null || sp.EndDate >= checkDate)
     && sp.Status == "APPROVED")
      .OrderByDescending(sp => sp.EffectiveDate)
  .FirstOrDefaultAsync();
        }
    }
}
