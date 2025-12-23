import React, { useState, useEffect } from "react";
import {
  Card,
  Avatar,
  Row,
  Col,
  Statistic,
  Button,
  List,
  Tag,
  Typography,
  Space,
  Modal,
  Form,
  Input,
  Select,
  DatePicker,
  Divider,
  Descriptions,
} from "antd";
import {
  FileTextOutlined,
  DollarOutlined,
  PlusOutlined,
  ExclamationCircleOutlined,
  TeamOutlined,
  PhoneOutlined,
  FileProtectOutlined,
  UserOutlined,
  CarOutlined,
} from "@ant-design/icons";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";
import CreateTicketModal from "../../components/resident/CreateTicketModal";
import TicketDetailModal from "../../components/resident/TicketDetailModal";
import VehicleRegistrationModal from "../../components/resident/VehicleRegistrationModal";
import VehicleCancellationModal from "../../components/resident/VehicleCancellationModal";
import useNotification from "../../hooks/useNotification";
import {
  getMyInvoices,
  categorizeInvoices,
  calculateTotals,
} from "../../features/resident/invoiceApi";
import residentTicketsApi from "../../features/residents/residentTicketsApi";
import amenityBookingApi from "../../features/amenity-booking/amenityBookingApi";
import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";
import "dayjs/locale/vi";

dayjs.extend(relativeTime);
dayjs.locale("vi");

const { Text } = Typography;
const { TextArea } = Input;

export default function ResidentDashboard() {
  const { showNotification } = useNotification();

  // States
  const [loading, setLoading] = useState(true);
  const [stats, setStats] = useState({
    pendingBills: 0,
    totalDebt: 0,
    activeTickets: 0,
    upcomingBookings: 0,
    amenitiesInUse: 0,
  });
  const [recentTickets, setRecentTickets] = useState([]);
  const [billingData, setBillingData] = useState([]);

  const [contract, setContract] = useState(null);
  const [householdMembers, setHouseholdMembers] = useState([]);
  const [showTicketModal, setShowTicketModal] = useState(false);
  const [showOtherRequestModal, setShowOtherRequestModal] = useState(false);
  const [selectedRequestType, setSelectedRequestType] = useState(null);
  const [showVehicleRegistrationModal, setShowVehicleRegistrationModal] =
    useState(false);
  const [showVehicleCancellationModal, setShowVehicleCancellationModal] =
    useState(false);
  const [showAmenityModal, setShowAmenityModal] = useState(false);
  const [showCreateMaintenanceModal, setShowCreateMaintenanceModal] =
    useState(false);
  const [showCreateComplaintModal, setShowCreateComplaintModal] =
    useState(false);
  const [selectedTicket, setSelectedTicket] = useState(null);
  const [showTicketDetailModal, setShowTicketDetailModal] = useState(false);
  const [ticketForm] = Form.useForm();
  const [amenityForm] = Form.useForm();

  useEffect(() => {
    const initDashboard = async () => {
      try {
        // Load dashboard data immediately with mock data
        loadDashboardData();
      } catch (error) {
        console.error("Failed to initialize dashboard:", error);
        // Still try to load dashboard with mock data
        loadDashboardData();
      }
    };

    initDashboard();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Chỉ chạy 1 lần khi mount

  const loadDashboardData = async () => {
    setLoading(true);
    try {
      // Load real invoice data
      let invoiceStats = {
        pendingBills: 0,
        totalDebt: 0,
      };

      try {
        const invoicesData = await getMyInvoices();
        const categorized = categorizeInvoices(invoicesData);
        const totals = calculateTotals(invoicesData);

        invoiceStats = {
          pendingBills: categorized.unpaid.length,
          totalDebt: totals.totalDebt,
        };

        // Generate billing chart data based on real invoices
        const last6Months = [];
        for (let i = 5; i >= 0; i--) {
          const monthDate = dayjs().subtract(i, "month");
          const monthInvoices = invoicesData.filter((inv) =>
            dayjs(inv.issueDate).isSame(monthDate, "month")
          );
          const monthAmount = monthInvoices.reduce(
            (sum, inv) => sum + inv.totalAmount,
            0
          );
          const monthPaid = monthInvoices
            .filter((inv) => inv.status === "PAID")
            .reduce((sum, inv) => sum + inv.totalAmount, 0);

          last6Months.push({
            month: monthDate.format("MMM"),
            amount: monthAmount,
            paid: monthPaid,
          });
        }
        setBillingData(last6Months);
      } catch (error) {
        console.error("Error loading invoice data:", error);
        // Fallback to mock data for invoices
        invoiceStats = {
          pendingBills: 2,
          totalDebt: 5500000,
        };
      }

      // Load amenity booking data (count confirmed bookings)
      const bookingStats = {
        upcomingBookings: 0,
        amenitiesInUse: 0,
      };

      try {
        const bookings = await amenityBookingApi.getMyBookings();
        bookingStats.amenitiesInUse = bookings.filter((booking) => {
          const normalized = (booking?.status || "").toLowerCase();
          return normalized === "confirmed";
        }).length;
      } catch (error) {
        console.error("Error fetching amenity bookings:", error);
      }

      setStats({
        ...invoiceStats,
        ...bookingStats,
        activeTickets: 0, // Will be updated when tickets are fetched
      });

      // Fetch recent tickets
      try {
        const ticketResponse = await residentTicketsApi.getMyTickets({
          pageSize: 3, // Chỉ lấy 3 tickets gần đây nhất
        });

        if (ticketResponse && ticketResponse.items) {
          setRecentTickets(ticketResponse.items);

          // Update active tickets count
          const activeCount = ticketResponse.items.filter(
            (ticket) => ticket.status !== "Đã đóng"
          ).length;

          setStats((prevStats) => ({
            ...prevStats,
            activeTickets: activeCount,
          }));
        }
      } catch (error) {
        console.error("Error fetching tickets:", error);
        setRecentTickets([]);
      }

      setContract({
        contractNumber: "HD-2023-001234",
        startDate: "2023-01-01",
        endDate: "2025-12-31",
        monthlyRent: 15000000,
        deposit: 45000000,
        status: "Còn hiệu lực",
      });

      setHouseholdMembers([
        {
          id: 1,
          name: "Nguyễn Văn A",
          relationship: "Chủ hộ",
          phone: "0901234567",
          idCard: "001234567890",
        },
        {
          id: 2,
          name: "Trần Thị B",
          relationship: "Vợ/Chồng",
          phone: "0907654321",
          idCard: "001234567891",
        },
      ]);
    } catch (error) {
      showNotification("error", "Lỗi", "Không thể tải dữ liệu dashboard");
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTicket = async (values) => {
    try {
      // Mock create ticket
      showNotification("success", "Thành công", "Đã tạo yêu cầu mới");
      setShowTicketModal(false);
      ticketForm.resetFields();
      loadDashboardData();
    } catch (error) {
      showNotification("error", "Lỗi", "Không thể tạo yêu cầu");
    }
  };

  const handleBookAmenity = async (values) => {
    try {
      // Mock book amenity
      showNotification("success", "Thành công", "Đã đặt lịch tiện ích");
      setShowAmenityModal(false);
      amenityForm.resetFields();
      loadDashboardData();
    } catch (error) {
      showNotification("error", "Lỗi", "Không thể đặt lịch");
    }
  };

  return (
    <div>
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card
            hoverable
            style={{
              background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
              border: "none",
            }}
          >
            <Statistic
              title={
                <Text style={{ color: "rgba(255,255,255,0.85)" }}>
                  Hóa đơn chưa thanh toán
                </Text>
              }
              value={stats.pendingBills}
              valueStyle={{ color: "#fff" }}
              suffix={stats.pendingBills === 1 ? "hóa đơn" : "hóa đơn"}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card
            hoverable
            style={{
              background: "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)",
              border: "none",
            }}
          >
            <Statistic
              title={
                <Text style={{ color: "rgba(255,255,255,0.85)" }}>
                  Tổng công nợ
                </Text>
              }
              value={stats.totalDebt}
              formatter={(value) => `${value.toLocaleString("vi-VN")}`}
              valueStyle={{ color: "#fff" }}
              suffix="đ"
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card
            hoverable
            style={{
              background: "linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)",
              border: "none",
            }}
          >
            <Statistic
              title={
                <Text style={{ color: "rgba(255,255,255,0.85)" }}>
                  Yêu cầu đang xử lý
                </Text>
              }
              value={stats.activeTickets}
              valueStyle={{ color: "#fff" }}
              suffix="yêu cầu"
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card
            hoverable
            style={{
              background:
                "linear-gradient(135deg, #09d528ff 0%, #00fe61ff 100%)",
              border: "none",
            }}
          >
            <Statistic
              title={
                <Text style={{ color: "rgba(255,255,255,0.85)" }}>
                  Tiện ích đang sử dụng
                </Text>
              }
              value={stats.amenitiesInUse}
              valueStyle={{ color: "#fff" }}
              suffix="tiện ích"
            />
          </Card>
        </Col>
      </Row>

      <Row gutter={[24, 24]}>
        {/* Left Column */}
        <Col xs={24} lg={16}>
          {/* Quick Create Request */}
          <Card
            title={
              <Space>
                <PlusOutlined />
                <span>Tạo yêu cầu nhanh</span>
              </Space>
            }
            style={{ marginBottom: 24 }}
          >
            <Row gutter={[12, 12]}>
              <Col xs={24} sm={12} md={8}>
                <Button
                  block
                  size="large"
                  icon={<FileProtectOutlined />}
                  onClick={() => setShowCreateMaintenanceModal(true)}
                  style={{
                    backgroundColor: "#f0f9ff",
                    borderColor: "#1890ff",
                    color: "#1890ff",
                  }}
                >
                  Yêu cầu bảo trì
                </Button>
              </Col>
              <Col xs={24} sm={12} md={8}>
                <Button
                  block
                  size="large"
                  icon={<ExclamationCircleOutlined />}
                  onClick={() => setShowCreateComplaintModal(true)}
                  style={{
                    backgroundColor: "#fff2e8",
                    borderColor: "#fa8c16",
                    color: "#fa8c16",
                  }}
                >
                  Tạo khiếu nại
                </Button>
              </Col>
              <Col xs={24} sm={12} md={8}>
                <Button
                  block
                  size="large"
                  icon={<FileTextOutlined />}
                  onClick={() => setShowOtherRequestModal(true)}
                >
                  Yêu cầu khác
                </Button>
              </Col>
            </Row>
          </Card>

          {/* Billing Chart */}
          <Card
            title={
              <Space>
                <DollarOutlined />
                <span>Biểu đồ công nợ 6 tháng</span>
              </Space>
            }
            style={{ marginBottom: 24 }}
          >
            <div style={{ height: 300 }}>
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={billingData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="month" />
                  <YAxis
                    tickFormatter={(value) =>
                      `${(value / 1000000).toFixed(1)}tr`
                    }
                  />
                  <Tooltip
                    formatter={(value) => [
                      `${value.toLocaleString("vi-VN")} đ`,
                      "Số tiền",
                    ]}
                  />
                  <Legend />
                  <Bar
                    dataKey="amount"
                    name="Số tiền"
                    fill="#52c41a"
                    shape={(props) => {
                      const { x, y, width, height, payload } = props;
                      const unpaidMonths = ["T9", "T10"];
                      const color = unpaidMonths.includes(payload.month)
                        ? "#ff4d4f"
                        : "#52c41a";
                      return (
                        <rect
                          x={x}
                          y={y}
                          width={width}
                          height={height}
                          fill={color}
                        />
                      );
                    }}
                  />
                </BarChart>
              </ResponsiveContainer>
            </div>
            <Divider />
            <Row gutter={[12, 12]}>
              <Col xs={24} sm={12}>
                <Statistic
                  title="Tổng chi phí 6 tháng"
                  value={billingData.reduce(
                    (sum, item) => sum + item.amount,
                    0
                  )}
                  formatter={(value) => `${value.toLocaleString("vi-VN")} đ`}
                  valueStyle={{ color: "#1890ff" }}
                />
              </Col>
              <Col xs={24} sm={12}>
                <Statistic
                  title="Công nợ hiện tại"
                  value={stats.totalDebt}
                  formatter={(value) => `${value.toLocaleString("vi-VN")} đ`}
                  valueStyle={{ color: "#ff4d4f" }}
                />
              </Col>
            </Row>
          </Card>
        </Col>

        {/* Right Column */}
        <Col xs={24} lg={8}>
          {/* Contract Info */}
          <Card
            title={
              <Space>
                <FileProtectOutlined />
                <span>Thông tin hợp đồng</span>
              </Space>
            }
            style={{ marginBottom: 24 }}
          >
            {contract && (
              <Descriptions column={1} size="small">
                <Descriptions.Item label="Số HĐ">
                  {contract.contractNumber}
                </Descriptions.Item>
                <Descriptions.Item label="Thời hạn">
                  {dayjs(contract.startDate).format("DD/MM/YYYY")} -{" "}
                  {dayjs(contract.endDate).format("DD/MM/YYYY")}
                </Descriptions.Item>
                <Descriptions.Item label="Tiền thuê">
                  <Text strong style={{ color: "#1890ff" }}>
                    {contract.monthlyRent.toLocaleString("vi-VN")} đ/tháng
                  </Text>
                </Descriptions.Item>
                <Descriptions.Item label="Tiền đặt cọc">
                  {contract.deposit.toLocaleString("vi-VN")} đ
                </Descriptions.Item>
                <Descriptions.Item label="Trạng thái">
                  <Tag color="green">{contract.status}</Tag>
                </Descriptions.Item>
              </Descriptions>
            )}
            <Divider />
            <Text type="secondary" style={{ fontSize: 12 }}>
              Hợp đồng sẽ hết hạn sau{" "}
              {dayjs(contract?.endDate).diff(dayjs(), "day")} ngày
            </Text>
          </Card>

          {/* Household Members */}
          <Card
            title={
              <Space>
                <TeamOutlined />
                <span>Thông tin gia cảnh</span>
              </Space>
            }
          >
            <List
              dataSource={householdMembers}
              renderItem={(member) => (
                <List.Item>
                  <List.Item.Meta
                    avatar={<Avatar icon={<UserOutlined />} />}
                    title={
                      <Space>
                        <Text strong>{member.name}</Text>
                        <Tag color="blue" style={{ fontSize: 11 }}>
                          {member.relationship}
                        </Tag>
                      </Space>
                    }
                    description={
                      <Space direction="vertical" size="small">
                        <Text type="secondary" style={{ fontSize: 12 }}>
                          <PhoneOutlined /> {member.phone}
                        </Text>
                        <Text type="secondary" style={{ fontSize: 12 }}>
                          CCCD: {member.idCard}
                        </Text>
                      </Space>
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        </Col>
      </Row>

      {/* Create Ticket Modal */}
      <Modal
        title="Tạo yêu cầu mới"
        open={showTicketModal}
        onCancel={() => {
          setShowTicketModal(false);
          ticketForm.resetFields();
        }}
        footer={null}
        width={600}
      >
        <Form form={ticketForm} layout="vertical" onFinish={handleCreateTicket}>
          <Form.Item
            name="type"
            label="Loại yêu cầu"
            rules={[{ required: true, message: "Vui lòng chọn loại yêu cầu" }]}
          >
            <Select placeholder="Chọn loại yêu cầu">
              <Select.Option value="Bảo trì">Bảo trì</Select.Option>
              <Select.Option value="Khiếu nại">Khiếu nại</Select.Option>
              <Select.Option value="Yêu cầu khác">Yêu cầu khác</Select.Option>
            </Select>
          </Form.Item>
          <Form.Item
            name="priority"
            label="Mức độ ưu tiên"
            rules={[
              { required: true, message: "Vui lòng chọn mức độ ưu tiên" },
            ]}
          >
            <Select placeholder="Chọn mức độ ưu tiên">
              <Select.Option value="Cao">Cao</Select.Option>
              <Select.Option value="Trung bình">Trung bình</Select.Option>
              <Select.Option value="Thấp">Thấp</Select.Option>
            </Select>
          </Form.Item>
          <Form.Item
            name="title"
            label="Tiêu đề"
            rules={[{ required: true, message: "Vui lòng nhập tiêu đề" }]}
          >
            <Input placeholder="Nhập tiêu đề yêu cầu" />
          </Form.Item>
          <Form.Item
            name="description"
            label="Mô tả chi tiết"
            rules={[{ required: true, message: "Vui lòng nhập mô tả" }]}
          >
            <TextArea rows={4} placeholder="Mô tả chi tiết vấn đề của bạn" />
          </Form.Item>
          <Form.Item style={{ marginBottom: 0, textAlign: "right" }}>
            <Space>
              <Button
                onClick={() => {
                  setShowTicketModal(false);
                  ticketForm.resetFields();
                }}
              >
                Hủy
              </Button>
              <Button type="primary" htmlType="submit">
                Tạo yêu cầu
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Book Amenity Modal */}
      <Modal
        title="Đặt lịch tiện ích"
        open={showAmenityModal}
        onCancel={() => {
          setShowAmenityModal(false);
          amenityForm.resetFields();
        }}
        footer={null}
        width={600}
      >
        <Form form={amenityForm} layout="vertical" onFinish={handleBookAmenity}>
          <Form.Item
            name="amenity"
            label="Tiện ích"
            rules={[{ required: true, message: "Vui lòng chọn tiện ích" }]}
          >
            <Select placeholder="Chọn tiện ích">
              <Select.Option value="pool">Hồ bơi</Select.Option>
              <Select.Option value="gym">Phòng gym</Select.Option>
              <Select.Option value="party">Phòng tiệc</Select.Option>
              <Select.Option value="bbq">Khu BBQ</Select.Option>
            </Select>
          </Form.Item>
          <Form.Item
            name="date"
            label="Ngày đặt"
            rules={[{ required: true, message: "Vui lòng chọn ngày" }]}
          >
            <DatePicker
              style={{ width: "100%" }}
              format="DD/MM/YYYY"
              disabledDate={(current) =>
                current && current < dayjs().startOf("day")
              }
            />
          </Form.Item>
          <Row gutter={[12, 12]}>
            <Col xs={24} sm={12}>
              <Form.Item
                name="startTime"
                label="Giờ bắt đầu"
                rules={[{ required: true, message: "Vui lòng chọn giờ" }]}
              >
                <Select placeholder="Chọn giờ">
                  {Array.from({ length: 14 }, (_, i) => i + 6).map((hour) => (
                    <Select.Option key={hour} value={`${hour}:00`}>
                      {`${hour}:00`}
                    </Select.Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="duration"
                label="Thời lượng"
                rules={[
                  { required: true, message: "Vui lòng chọn thời lượng" },
                ]}
              >
                <Select placeholder="Chọn thời lượng">
                  <Select.Option value="1">1 giờ</Select.Option>
                  <Select.Option value="2">2 giờ</Select.Option>
                  <Select.Option value="3">3 giờ</Select.Option>
                </Select>
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="note" label="Ghi chú">
            <TextArea rows={3} placeholder="Ghi chú thêm (không bắt buộc)" />
          </Form.Item>
          <Form.Item style={{ marginBottom: 0, textAlign: "right" }}>
            <Space>
              <Button
                onClick={() => {
                  setShowAmenityModal(false);
                  amenityForm.resetFields();
                }}
              >
                Hủy
              </Button>
              <Button type="primary" htmlType="submit">
                Đặt lịch
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Create Maintenance Modal */}
      <CreateTicketModal
        open={showCreateMaintenanceModal}
        onClose={() => setShowCreateMaintenanceModal(false)}
        onSuccess={() => {
          setShowCreateMaintenanceModal(false);
          // Refresh tickets if needed
        }}
        type="bảo trì"
      />

      {/* Create Complaint Modal */}
      <CreateTicketModal
        open={showCreateComplaintModal}
        onClose={() => setShowCreateComplaintModal(false)}
        onSuccess={() => {
          setShowCreateComplaintModal(false);
          // Refresh tickets if needed
        }}
        type="khiếu nại"
      />

      {/* Ticket Detail Modal */}
      <TicketDetailModal
        open={showTicketDetailModal}
        onClose={() => setShowTicketDetailModal(false)}
        ticketId={selectedTicket?.ticketId}
      />

      {/* Other Request Selection Modal */}
      <Modal
        title="Chọn loại yêu cầu"
        open={showOtherRequestModal}
        onCancel={() => {
          setShowOtherRequestModal(false);
          setSelectedRequestType(null);
        }}
        footer={null}
        width={500}
      >
        <Form layout="vertical">
          <Form.Item label="Loại yêu cầu">
            <Select
              placeholder="Chọn loại yêu cầu"
              size="large"
              value={selectedRequestType}
              onChange={(value) => {
                setSelectedRequestType(value);
                setShowOtherRequestModal(false);

                // Mở modal tương ứng
                if (value === "vehicle-registration") {
                  setShowVehicleRegistrationModal(true);
                } else if (value === "vehicle-cancellation") {
                  setShowVehicleCancellationModal(true);
                }

                // Reset sau khi chọn
                setTimeout(() => setSelectedRequestType(null), 100);
              }}
            >
              <Select.Option value="vehicle-registration">
                <Space>
                  <CarOutlined style={{ color: "#52c41a" }} />
                  <span>Đăng ký gửi xe</span>
                </Space>
              </Select.Option>
              <Select.Option value="vehicle-cancellation">
                <Space>
                  <CarOutlined style={{ color: "#ff4d4f" }} />
                  <span>Hủy đăng ký xe</span>
                </Space>
              </Select.Option>
              {/* Có thể thêm các loại yêu cầu khác ở đây */}
            </Select>
          </Form.Item>
        </Form>
      </Modal>

      {/* Vehicle Registration Modal */}
      <VehicleRegistrationModal
        open={showVehicleRegistrationModal}
        onClose={() => setShowVehicleRegistrationModal(false)}
        onSuccess={() => {
          setShowVehicleRegistrationModal(false);
          loadDashboardData(); // Refresh để cập nhật danh sách tickets
        }}
        apartmentId={contract?.apartmentId}
      />

      {/* Vehicle Cancellation Modal */}
      <VehicleCancellationModal
        visible={showVehicleCancellationModal}
        onClose={() => setShowVehicleCancellationModal(false)}
        onSuccess={() => {
          setShowVehicleCancellationModal(false);
          loadDashboardData(); // Refresh để cập nhật danh sách tickets
        }}
      />
    </div>
  );
}
