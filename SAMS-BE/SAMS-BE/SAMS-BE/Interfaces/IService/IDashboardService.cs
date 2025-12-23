using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService
{
    public interface IDashboardService
    {
        Task<DashboardStatisticsDto> GetDashboardStatisticsAsync();
    }
}

