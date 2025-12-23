using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using System.Collections.Generic;

namespace SAMS_BE.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly BuildingManagementContext _context;
        private readonly IBuildingRepository _buildingRepository;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            BuildingManagementContext context,
            IBuildingRepository buildingRepository,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _buildingRepository = buildingRepository;
            _logger = logger;
        }

        public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync()
        {
            try
            {
                var stats = new DashboardStatsDto();
                var chartData = new DashboardChartDataDto();
                var recentActivities = new List<DashboardActivityDto>();
                var alerts = new List<DashboardAlertDto>();

                // Get total buildings (from global context)
                var buildings = await _buildingRepository.GetAllAsync(CancellationToken.None);
                stats.TotalBuildings = buildings.Count;

                // Get total apartments
                stats.TotalApartments = await _context.Apartments.CountAsync();

                // Get total residents (all residents in ResidentProfiles table)
                // Đếm tất cả resident profiles trong database
                stats.TotalResidents = await _context.ResidentProfiles.CountAsync();

                // Calculate occupancy rate
                // Tỷ lệ lấp đầy = (Số căn hộ có người ở / Tổng số căn hộ) * 100%
                // Một căn hộ được coi là "có người ở" nếu có ít nhất 1 resident đang ở (EndDate == null)
                // Không nhất thiết phải là primary resident, vì có thể có căn hộ có nhiều residents
                var occupiedApartments = await _context.ResidentApartments
                    .Where(ra => ra.EndDate == null) // Resident đang ở (chưa chuyển đi)
                    .Select(ra => ra.ApartmentId)
                    .Distinct() // Đếm unique apartment IDs
                    .CountAsync();

                stats.OccupancyRate = stats.TotalApartments > 0
                    ? Math.Round((occupiedApartments * 100.0m) / stats.TotalApartments, 2)
                    : 0;

                // Get pending tickets (status Mới tạo, Đã tiếp nhận, Đang xử lý)
                stats.PendingTickets = await _context.Tickets
                    .Where(t => t.Status == "Mới tạo" || t.Status == "Đã tiếp nhận" || t.Status == "Đang xử lý")
                    .CountAsync();

                // Get security alerts (tickets with category An ninh and status not Đã đóng)
                stats.SecurityAlerts = await _context.Tickets
                    .Where(t => t.Category == "An ninh" && t.Status != "Đã đóng")
                    .CountAsync();

                // Count active announcements (news/events), excluding system-generated maintenance/amenity types
                var excludedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "MAINTENANCE_REMINDER",
                    "MAINTENANCE_ASSIGNMENT",
                    "MAINTENANCE_COMPLETED",
                    "AMENITY_BOOKING_SUCCESS",
                    "AMENITY_BOOKING_CONFLICT",
                    "AMENITY_EXPIRATION_REMINDER",
                    "AMENITY_EXPIRED",
                    "AMENITY_MAINTENANCE_REMINDER",
                    "ASSET_MAINTENANCE_NOTICE"
                };

                var now = DateTime.Now;
                stats.AnnouncementCount = await _context.Announcements
                    .Where(a => (a.Status == "ACTIVE" || a.Status == "Hoạt động") &&
                                a.VisibleFrom <= now &&
                                (a.VisibleTo == null || a.VisibleTo >= now) &&
                                !excludedTypes.Contains(a.Type ?? string.Empty))
                    .CountAsync();

                // Get monthly revenue (current month - invoices with status PAID)
                var currentMonth = DateTime.Now;
                var firstDayOfMonth = new DateOnly(currentMonth.Year, currentMonth.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                stats.MonthlyRevenue = await _context.Invoices
                    .Where(i => i.Status == "PAID" &&
                                i.IssueDate >= firstDayOfMonth &&
                                i.IssueDate <= lastDayOfMonth)
                    .SumAsync(i => i.TotalAmount);

                // Get amenity revenue (current month - bookings with payment_status = "Paid")
                var firstDayOfMonthDateTime = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                var lastDayOfMonthDateTime = firstDayOfMonthDateTime.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

                stats.AmenityRevenue = await _context.AmenityBookings
                    .Where(b => (b.PaymentStatus == "Paid" || b.PaymentStatus == "Đã thanh toán") &&
                                b.CreatedAt >= firstDayOfMonthDateTime &&
                                b.CreatedAt <= lastDayOfMonthDateTime)
                    .SumAsync(b => (decimal)b.TotalPrice);

                // Get revenue chart data (last 6 months)
                var currentDate = DateTime.Now;
                var monthNames = new[] { "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6",
                                         "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12" };

                // Initialize all 6 months with 0, using format "Tháng X/YYYY" for clarity
                var revenueChartData = new List<RevenueChartItemDto>();
                for (int i = 5; i >= 0; i--)
                {
                    var checkDate = currentDate.AddMonths(-i);
                    revenueChartData.Add(new RevenueChartItemDto
                    {
                        Month = $"{monthNames[checkDate.Month - 1]}/{checkDate.Year}",
                        Amount = 0
                    });
                }

                // Get actual revenue data for last 6 months
                var sixMonthsAgo = currentDate.AddMonths(-5);
                var startDate = new DateOnly(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

                // Get Invoice revenue
                var invoiceRevenueData = await _context.Invoices
                    .Where(i => i.Status == "PAID" && i.IssueDate >= startDate)
                    .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Amount = g.Sum(i => i.TotalAmount)
                    })
                    .ToListAsync();

                // Get Amenity revenue for last 6 months
                var sixMonthsAgoDateTime = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);
                var amenityRevenueData = await _context.AmenityBookings
                    .Where(b => (b.PaymentStatus == "Paid" || b.PaymentStatus == "Đã thanh toán") &&
                                b.CreatedAt >= sixMonthsAgoDateTime)
                    .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Amount = g.Sum(b => (decimal)b.TotalPrice)
                    })
                    .ToListAsync();

                // Update revenue chart with Invoice data
                foreach (var revenue in invoiceRevenueData)
                {
                    var monthIndex = revenue.Month - 1;
                    var monthLabel = $"{monthNames[monthIndex]}/{revenue.Year}";
                    var chartItem = revenueChartData.FirstOrDefault(r => r.Month == monthLabel);
                    if (chartItem != null)
                    {
                        chartItem.Amount += revenue.Amount;
                    }
                }

                // Update revenue chart with Amenity data
                foreach (var revenue in amenityRevenueData)
                {
                    var monthIndex = revenue.Month - 1;
                    var monthLabel = $"{monthNames[monthIndex]}/{revenue.Year}";
                    var chartItem = revenueChartData.FirstOrDefault(r => r.Month == monthLabel);
                    if (chartItem != null)
                    {
                        chartItem.Amount += revenue.Amount;
                    }
                }

                chartData.Revenue = revenueChartData;

                // Get recent activities (recent tickets from residents only)
                var ticketData = await _context.Tickets
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(10)
                    .Select(t => new 
                    {
                        t.Subject,
                        t.Status,
                        t.CreatedAt
                    })
                    .ToListAsync();

                recentActivities = ticketData
                    .Select(t => new DashboardActivityDto
                    {
                        Message = $"Yêu cầu: {t.Subject}",
                        Status = t.Status switch
                        {
                            "Đã đóng" => "Hoàn thành",
                            "Đang xử lý" => "Đang xử lý",
                            "Chờ xử lý" => "Chờ xử lý",
                            _ => "Mới tạo"
                        },
                        Time = GetTimeAgo(t.CreatedAt)
                    })
                    .ToList();

                // Get upcoming maintenance schedules (next 10 schedules)
                var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
                var upcomingSchedules = await _context.AssetMaintenanceSchedules
                    .Include(s => s.Asset)
                    .Where(s => s.StartDate >= today && 
                               (s.Status == "SCHEDULED" || s.Status == "IN_PROGRESS"))
                    .OrderBy(s => s.StartDate)
                    .ThenBy(s => s.StartTime)
                    .Take(10)
                    .Select(s => new DashboardAlertDto
                    {
                        Title = s.Asset != null ? s.Asset.Name : "Tài sản",
                        Message = "", // Không cần message
                        Type = s.Status == "IN_PROGRESS" ? "warning" : "info",
                        Priority = "", // Không cần priority
                        ScheduledDate = s.StartDate,
                        ScheduledTime = s.StartTime,
                        EndDate = s.EndDate,
                        EndTime = s.EndTime
                    })
                    .ToListAsync();

                alerts = upcomingSchedules;

                return new DashboardStatisticsDto
                {
                    Stats = stats,
                    ChartData = chartData,
                    RecentActivities = recentActivities,
                    Alerts = alerts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting dashboard statistics");
                throw;
            }
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalDays >= 30)
            {
                var months = (int)(timeSpan.TotalDays / 30);
                return $"{months} tháng trước";
            }
            else if (timeSpan.TotalDays >= 1)
            {
                var days = (int)timeSpan.TotalDays;
                return $"{days} ngày trước";
            }
            else if (timeSpan.TotalHours >= 1)
            {
                var hours = (int)timeSpan.TotalHours;
                return $"{hours} giờ trước";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                var minutes = (int)timeSpan.TotalMinutes;
                return $"{minutes} phút trước";
            }
            else
            {
                return "Vừa xong";
            }
        }
    }
}

