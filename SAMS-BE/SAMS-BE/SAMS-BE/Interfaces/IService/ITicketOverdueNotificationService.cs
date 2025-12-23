using System.Threading.Tasks;

namespace SAMS_BE.Interfaces.IService
{
    public interface ITicketOverdueNotificationService
    {
        /// <summary>
        /// Kiểm tra các ticket sắp đến hạn hoàn thành dự kiến (trong vòng 24h tới)
        /// và gửi email thông báo cho lễ tân.
        /// </summary>
        Task CheckAndNotifyOverdueTicketsAsync();
    }
}












