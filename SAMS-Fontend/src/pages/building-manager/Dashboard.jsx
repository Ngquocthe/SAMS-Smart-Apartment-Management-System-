import { useState, useEffect } from "react";
import { useLanguage } from "../../hooks/useLanguage";
import "../../styles/Dashboard.css";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from "recharts";
import { dashboardApi } from "../../features/building-management/dashboardApi";

export default function Dashboard() {
  const { strings } = useLanguage();
  const [loading, setLoading] = useState(true);
  const [data, setData] = useState({
    stats: {
      totalBuildings: 0,
      totalApartments: 0,
      totalResidents: 0,
      occupancyRate: 0,
      maintenanceRequests: 0,
      pendingTickets: 0,
      securityAlerts: 0,
      announcementCount: 0,
      monthlyRevenue: 0,
      amenityRevenue: 0,
      serviceRevenue: 0,
      totalRevenue: 0,
      totalExpense: 0,
    },
    chartData: {
      revenue: [],
      occupancy: [],
    },
    recentActivities: [],
    alerts: [],
  });
  const [error, setError] = useState("");

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        setLoading(true);
        // Gọi cả 2 API song song
        const [dashboardData, financialData] = await Promise.all([
          dashboardApi.getStatistics(),
          dashboardApi.getFinancialData('month')
        ]);

        if (mounted) {
          const amenityRevenue = dashboardData.stats?.amenityRevenue || 0;
          const serviceRevenue = financialData?.totalRevenue || 0;
          const totalRevenue = amenityRevenue + serviceRevenue;

          // Lấy dữ liệu chart revenue từ building management
          // Backend đã tính tổng cả Invoice Revenue và Amenity Revenue
          const revenueChartData = (dashboardData.chartData?.revenue || []).map((r) => ({
            month: r.month || r.Month,
            amount: r.amount || r.Amount,
          }));

          // Transform data to match component structure
          setData({
            stats: {
              totalBuildings: dashboardData.stats?.totalBuildings || 0,
              totalApartments: dashboardData.stats?.totalApartments || 0,
              totalResidents: dashboardData.stats?.totalResidents || 0,
              occupancyRate: dashboardData.stats?.occupancyRate || 0,
              maintenanceRequests:
                dashboardData.stats?.maintenanceRequests || 0,
              pendingTickets: dashboardData.stats?.pendingTickets || 0,
              securityAlerts: dashboardData.stats?.securityAlerts || 0,
              announcementCount: dashboardData.stats?.announcementCount || 0,
              monthlyRevenue: dashboardData.stats?.monthlyRevenue || 0,
              amenityRevenue: amenityRevenue,
              serviceRevenue: serviceRevenue,
              totalRevenue: totalRevenue,
              totalExpense: financialData?.totalExpense || 0,
            },
            chartData: {
              revenue: revenueChartData,
              occupancy: (dashboardData.chartData?.occupancy || []).map(
                (o) => ({
                  building: o.building || o.Building,
                  rate: o.rate || o.Rate,
                })
              ),
            },
            recentActivities: dashboardData.recentActivities || [],
            alerts: dashboardData.alerts || [],
          });
          setLoading(false);
        }
      } catch (err) {
        console.error("Error loading dashboard data:", err);
        if (mounted) {
          setError(
            err.response?.data?.error ||
            err.message ||
            "Không thể tải dữ liệu dashboard"
          );
          setLoading(false);
        }
      }
    })();
    return () => {
      mounted = false;
    };
  }, []);

  if (loading) {
    return (
      <div
        className="flex justify-center items-center"
        style={{ height: "50vh" }}
      >
        <div
          className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"
          role="status"
        >
          <span className="sr-only">Loading...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div
        className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded"
        role="alert"
      >
        Error: {error}
      </div>
    );
  }

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-800 mb-2">
          {strings.dashboard} - Quản lý tòa nhà
        </h1>
      </div>

      {/* Statistics Cards - Thông tin chung */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <div className="bg-green-500 text-white p-6 rounded-lg shadow-lg">
          <div>
            <h6 className="text-sm font-medium opacity-90">
              Tổng căn hộ
            </h6>
            <h3 className="text-3xl font-bold">
              {data.stats.totalApartments}
            </h3>
          </div>
        </div>
        <div className="bg-purple-500 text-white p-6 rounded-lg shadow-lg">
          <div>
            <h6 className="text-sm font-medium opacity-90">
              Tổng số cư dân
            </h6>
            <h3 className="text-3xl font-bold">
              {data.stats.totalResidents}
            </h3>
          </div>
        </div>
        <div className="bg-blue-500 text-white p-6 rounded-lg shadow-lg">
          <div>
            <h6 className="text-sm font-medium opacity-90">
              Tin tức / Sự kiện
            </h6>
            <h3 className="text-3xl font-bold">
              {data.stats.announcementCount}
            </h3>
          </div>
        </div>
        <div className="bg-red-500 text-white p-6 rounded-lg shadow-lg">
          <div>
            <h6 className="text-sm font-medium opacity-90">
              Ticket chờ xử lý
            </h6>
            <h3 className="text-3xl font-bold">
              {data.stats.pendingTickets}
            </h3>
          </div>
        </div>
      </div>

      {/* Financial Stats - Thông tin tài chính */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div className="bg-blue-600 text-white p-6 rounded-lg shadow-lg">
          <div>
            <h6 className="text-sm font-medium opacity-90">
              Doanh thu tiện ích
            </h6>
            <h3 className="text-2xl font-bold">
              {data.stats.amenityRevenue.toLocaleString('vi-VN')} VNĐ
            </h3>
          </div>
        </div>
        <div className="bg-green-600 text-white p-6 rounded-lg shadow-lg">
          <div>
            <h6 className="text-sm font-medium opacity-90">
              Doanh thu dịch vụ
            </h6>
            <h3 className="text-2xl font-bold">
              {data.stats.serviceRevenue.toLocaleString('vi-VN')} VNĐ
            </h3>
          </div>
        </div>
        <div className="bg-rose-600 text-white p-6 rounded-lg shadow-lg">
          <div>
            <h6 className="text-sm font-medium opacity-90">
              Tổng chi phí
            </h6>
            <h3 className="text-2xl font-bold">
              {data.stats.totalExpense.toLocaleString('vi-VN')} VNĐ
            </h3>
          </div>
        </div>
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        <div className="bg-white p-6 rounded-lg shadow-lg">
          <h5 className="text-lg font-semibold mb-4">
            Doanh thu theo tháng
          </h5>
          <div className="h-80">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data.chartData.revenue}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="month" />
                <YAxis
                  tickFormatter={(value) => `${(value / 1000000).toFixed(0)}M`}
                />
                <Tooltip
                  formatter={(value) => [
                    `${(value / 1000000).toFixed(1)}M VNĐ`,
                    "Doanh thu",
                  ]}
                  labelFormatter={(label) => `Tháng ${label}`}
                />
                <Bar dataKey="amount" fill="#667eea" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
        <div className="bg-white p-6 rounded-lg shadow-lg">
          <h5 className="text-lg font-semibold mb-4">
            {strings.occupancyByBuilding}
          </h5>
          <div className="h-80">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={data.chartData.occupancy}
                  cx="50%"
                  cy="50%"
                  labelLine={true}
                  label={({ building, rate }) => `${building}: ${rate}%`}
                  outerRadius={110}
                  innerRadius={35}
                  fill="#8884d8"
                  dataKey="rate"
                  paddingAngle={2}
                  nameKey="building"
                >
                  {data.chartData.occupancy.map((entry, index) => (
                    <Cell
                      key={`cell-${index}`}
                      fill={
                        [
                          "#667eea",
                          "#f093fb",
                          "#4facfe",
                          "#43e97b",
                          "#fa709a",
                          "#fee140",
                          "#30cfd0",
                          "#a8edea",
                          "#fed6e3",
                          "#ffecd2",
                        ][index % 10]
                      }
                    />
                  ))}
                </Pie>
                <Tooltip
                  formatter={(value) => [`${value}%`, "Tỷ lệ lấp đầy"]}
                  contentStyle={{ fontSize: "14px" }}
                  labelFormatter={(label, payload) => {
                    if (
                      payload &&
                      payload.length > 0 &&
                      payload[0]?.payload?.building
                    ) {
                      return payload[0].payload.building;
                    }
                    return label || "Tầng";
                  }}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>

      {/* Recent Activities and Alerts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white p-6 rounded-lg shadow-lg">
          <h5 className="text-lg font-semibold mb-4">
            {strings.recentActivities}
          </h5>
          <div className="space-y-3">
            {data.recentActivities.map((activity, index) => (
              <div
                key={index}
                className="flex justify-between items-start p-3 bg-gray-50 rounded-lg"
              >
                <div className="flex-1">
                  <div className="font-semibold text-gray-800">
                    {activity.message}
                  </div>
                  <small className="text-gray-500">
                    Trạng thái: {activity.status}
                  </small>
                </div>
                <small className="text-gray-400 ml-2">{activity.time}</small>
              </div>
            ))}
          </div>
        </div>
        <div className="bg-white p-6 rounded-lg shadow-lg">
          <h5 className="text-lg font-semibold mb-4">{strings.alerts}</h5>
          <div className="space-y-3">
            {data.alerts.map((alert, index) => {
              // Format date and time
              const formatDate = (dateStr) => {
                if (!dateStr) return '';
                const date = new Date(dateStr);
                return date.toLocaleDateString('vi-VN', {
                  day: '2-digit',
                  month: '2-digit',
                  year: 'numeric'
                });
              };

              const formatTime = (timeStr) => {
                if (!timeStr) return '';
                // timeStr format: "HH:mm:ss"
                return timeStr.substring(0, 5); // Get HH:mm
              };

              return (
                <div
                  key={index}
                  className={`p-3 rounded-lg ${alert.type === "warning"
                    ? "bg-yellow-50 border-l-4 border-yellow-400"
                    : alert.type === "error"
                      ? "bg-red-50 border-l-4 border-red-400"
                      : "bg-blue-50 border-l-4 border-blue-400"
                    }`}
                >
                  <div className="flex justify-between items-start">
                    <div className="flex-1">
                      <div className="font-semibold text-gray-800 mb-2">
                        {alert.title}
                      </div>
                      {/* Start Date & Time */}
                      {alert.scheduledDate && (
                        <div className="text-sm text-gray-700">
                          <span className="font-medium">Bắt đầu:</span>{' '}
                          {alert.scheduledTime && `${formatTime(alert.scheduledTime)} - `}
                          {formatDate(alert.scheduledDate)}
                        </div>
                      )}
                      {/* End Date & Time */}
                      {(alert.endDate || alert.EndDate) && (
                        <div className="text-sm text-gray-700">
                          <span className="font-medium">Kết thúc:</span>{' '}
                          {(alert.endTime || alert.EndTime) && `${formatTime(alert.endTime || alert.EndTime)} - `}
                          {formatDate(alert.endDate || alert.EndDate)}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}
