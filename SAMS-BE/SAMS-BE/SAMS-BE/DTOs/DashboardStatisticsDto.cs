namespace SAMS_BE.DTOs
{
    public class DashboardStatisticsDto
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public DashboardChartDataDto ChartData { get; set; } = new();
        public List<DashboardActivityDto> RecentActivities { get; set; } = new();
        public List<DashboardAlertDto> Alerts { get; set; } = new();
    }

    public class DashboardStatsDto
    {
        public int TotalBuildings { get; set; }
        public int TotalApartments { get; set; }
        public int TotalResidents { get; set; }
        public decimal OccupancyRate { get; set; }
        public int MaintenanceRequests { get; set; }
        public int PendingTickets { get; set; }
        public int SecurityAlerts { get; set; }
        public int AnnouncementCount { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal AmenityRevenue { get; set; }
    }

    public class DashboardChartDataDto
    {
        public List<RevenueChartItemDto> Revenue { get; set; } = new();
    }

    public class RevenueChartItemDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }


    public class DashboardActivityDto
    {
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }

    public class DashboardAlertDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateOnly? ScheduledDate { get; set; }
        public TimeOnly? ScheduledTime { get; set; }
        public DateOnly? EndDate { get; set; }
        public TimeOnly? EndTime { get; set; }
    }
}

