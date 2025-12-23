import React, { useCallback, useEffect, useState } from "react";
import {
  Button,
  Input,
  Select,
  Table,
  Tag,
  Space,
  message,
  DatePicker,
  Card,
  Typography,
  Row,
  Col,
  Modal,
  Form,
} from "antd";
import {
  ReloadOutlined,
  FileTextOutlined,
  EyeOutlined,
  ExclamationCircleOutlined,
  FileProtectOutlined,
  CarOutlined,
} from "@ant-design/icons";
import dayjs from "dayjs";
import residentTicketsApi from "../../features/residents/residentTicketsApi";
import CreateTicketModal from "../../components/resident/CreateTicketModal";
import TicketDetailModal from "../../components/resident/TicketDetailModal";
import VehicleRegistrationModal from "../../components/resident/VehicleRegistrationModal";
import VehicleCancellationModal from "../../components/resident/VehicleCancellationModal";

const { Title, Text } = Typography;


const defaultQuery = {
  keyword: "",
  status: undefined,
  fromDate: undefined,
  toDate: undefined,
  page: 1,
  pageSize: 10,
};

export default function MyTickets() {
  const [query, setQuery] = useState(defaultQuery);
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState({ total: 0, items: [] });
  const [showCreateMaintenanceModal, setShowCreateMaintenanceModal] = useState(false);
  const [showCreateComplaintModal, setShowCreateComplaintModal] = useState(false);
  const [showOtherRequestModal, setShowOtherRequestModal] = useState(false);
  const [selectedRequestType, setSelectedRequestType] = useState(null);
  const [showVehicleRegistrationModal, setShowVehicleRegistrationModal] = useState(false);
  const [showVehicleCancellationModal, setShowVehicleCancellationModal] = useState(false);
  const [showOtherTicketModal, setShowOtherTicketModal] = useState(false);
  const [selectedTicket, setSelectedTicket] = useState(null);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [stats, setStats] = useState({
    total: 0,
    completed: 0,
    inProgress: 0,
    pending: 0,
    closed: 0,
  });

  const categoryColors = {
    'Bảo trì': 'blue',
    'Tiếp tân': 'cyan',
    'An ninh': 'red',
    'Hóa đơn': 'purple',
    'Khiếu nại': 'orange',
    'Vệ sinh': 'green',
    'Bãi đỗ xe': 'gold',
    'CNTT': 'magenta',
    'Tiện ích': 'geekblue',
    'Khác': 'default'
  };

  const fetchStats = useCallback(async () => {
    try {
      const result = await residentTicketsApi.getStatistics();
      setStats({
        total: result.total || 0,
        completed: result.completed || 0,
        inProgress: result.inProgress || 0,
        pending: result.pending || 0,
        closed: result.closed || 0,
      });
    } catch (e) {
      console.error("Không thể lấy thống kê ticket:", e);
    }
  }, []);

  const getCategoryText = (category) => {
    if (category === 'VehicleRegistration') {
      return 'Đăng ký phương tiện';
    }
    return category || 'Khác';
  };
  const getCategoryColor = (category) => categoryColors[category] || 'default';

  const handleViewDetail = (ticket) => {
    setSelectedTicket(ticket);
    setShowDetailModal(true);
  };

  const handleCreateSuccess = () => {
    fetchList();
    fetchStats();
  };

  const columns = [
    {
      title: "Tiêu đề",
      dataIndex: "subject",
      key: "subject",
      render: (value, record) => (
        <Space direction="vertical" size={0}>
          <Text strong>{value}</Text>
          <Space size={4}>
            <Tag color={getCategoryColor(record.category)} size="small">
              {getCategoryText(record.category)}
            </Tag>
            {record.apartmentNumber && (
              <Text type="secondary" style={{ fontSize: 12 }}>
                Căn hộ: {record.apartmentNumber}
              </Text>
            )}
          </Space>
        </Space>
      ),
    },

    {
      title: "Trạng thái",
      dataIndex: "status",
      key: "status",
      width: 130,
      render: (v) => {
        const statusColors = {
          "Mới tạo": "blue",
          "Đã tiếp nhận": "cyan",
          "Đang xử lý": "gold",
          "Hoàn thành": "green",
          "Đã đóng": "gray",
        };
        return (
          <Tag color={statusColors[v] || "default"}>
            {v || "—"}
          </Tag>
        );
      },
    },
    {
      title: "Ngày tạo",
      dataIndex: "createdAt",
      key: "createdAt",
      width: 130,
      render: (date) => new Date(date).toLocaleDateString("vi-VN"),
    },
    {
      title: "Ngày hoàn thành dự kiến",
      dataIndex: "expectedCompletionAt",
      key: "expectedCompletionAt",
      width: 180,
      render: (date) =>
        date ? dayjs(date).format("DD/MM/YYYY HH:mm") : "—",
    },
    {
      title: "Thao tác",
      key: "actions",
      width: 100,
      render: (_, record) => (
        <Button
          type="link"
          icon={<EyeOutlined />}
          onClick={() => handleViewDetail(record)}
        >
          Chi tiết
        </Button>
      ),
    },
  ];

  const fetchList = useCallback(async () => {
    setLoading(true);
    try {
      const result = await residentTicketsApi.getMyTickets({
        status: query.status || undefined,
        category: query.category || undefined,
        priority: query.priority || undefined,
        fromDate: query.fromDate || undefined,
        toDate: query.toDate || undefined,
        page: query.page,
        pageSize: query.pageSize,
      });
      setData({ total: result.total || 0, items: result.items || [] });
    } catch (e) {
      message.error("Không tải được danh sách yêu cầu");
    } finally {
      setLoading(false);
    }
  }, [
    query.status,
    query.category,
    query.priority,
    query.fromDate,
    query.toDate,
    query.page,
    query.pageSize,
  ]);

  useEffect(() => {
    fetchList();
    fetchStats();
  }, []); // eslint-disable-line

  useEffect(() => {
    fetchList();
  }, [query.page, query.pageSize]); // eslint-disable-line

  const handleSearch = () => {
    setQuery((q) => ({ ...q, page: 1 }));
    fetchList();
  };

  return (
    <div>
      {/* Page Title */}
      <div style={{ marginBottom: 24 }}>
        <Title level={3} style={{ margin: 0, color: "#1890ff" }}>
          <FileTextOutlined style={{ marginRight: 8 }} />
          Yêu cầu của tôi
        </Title>
      </div>




      {/* Search and Filters */}
      <Card style={{ marginBottom: 24, borderRadius: 12 }}>
        <Row gutter={[12, 12]} align="middle">
          <Col xs={24} sm={12} md={5}>
            <Input
              placeholder="Tìm kiếm theo tiêu đề"
              value={query.keyword}
              onChange={(e) =>
                setQuery((q) => ({ ...q, keyword: e.target.value }))
              }
              allowClear
              size="middle"
            />
          </Col>
          <Col xs={12} sm={8} md={4}>
            <Select
              placeholder="Trạng thái"
              value={query.status}
              onChange={(v) => setQuery((q) => ({ ...q, status: v }))}
              style={{ width: "100%" }}
              allowClear
              size="middle"
              options={[
                { label: "Mới tạo", value: "Mới tạo" },
                { label: "Đang xử lý", value: "Đang xử lý" },
                { label: "Chờ xử lý", value: "Chờ xử lý" },
                { label: "Đã đóng", value: "Đã đóng" },
              ]}
            />
          </Col>
          <Col xs={12} sm={8} md={4}>
            <DatePicker
              allowClear
              placeholder="Ngày tạo"
              style={{ width: "100%" }}
              size="middle"
              onChange={(d) => {
                if (d) {
                  const startOfDay = d.startOf("day").toDate().toISOString();
                  const endOfDay = d.endOf("day").toDate().toISOString();
                  setQuery((q) => ({
                    ...q,
                    fromDate: startOfDay,
                    toDate: endOfDay,
                  }));
                } else {
                  setQuery((q) => ({
                    ...q,
                    fromDate: undefined,
                    toDate: undefined,
                  }));
                }
              }}
            />
          </Col>
          <Col xs={24} sm={16} md={11}>
            <Space wrap size="small">
              <Button
                type="primary"
                icon={<ReloadOutlined />}
                onClick={handleSearch}
                size="middle"
              >
                Lọc
              </Button>
              <Button
                icon={<ReloadOutlined />}
                onClick={() => {
                  setQuery(defaultQuery);
                  fetchList();
                  fetchStats();
                }}
                size="middle"
              >
                Làm mới
              </Button>
              <Button
                icon={<FileProtectOutlined />}
                onClick={() => setShowCreateMaintenanceModal(true)}
                size="middle"
              >
                Bảo trì
              </Button>
              <Button
                icon={<ExclamationCircleOutlined />}
                onClick={() => setShowCreateComplaintModal(true)}
                size="middle"
              >
                Khiếu nại
              </Button>
              <Button
                icon={<FileTextOutlined />}
                onClick={() => setShowOtherRequestModal(true)}
                size="middle"
              >
                Khác
              </Button>
            </Space>
          </Col>
        </Row>
      </Card>

      {/* Tickets Table */}
      <Card style={{ borderRadius: 12 }}>
        <Table
          rowKey={(r) => r.ticketId}
          columns={columns}
          dataSource={data.items}
          loading={loading}
          pagination={{
            current: query.page,
            pageSize: query.pageSize,
            total: data.total,
            onChange: (p, ps) =>
              setQuery((q) => ({ ...q, page: p, pageSize: ps })),
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (total, range) =>
              `${range[0]}-${range[1]} của ${total} yêu cầu`,
          }}
          scroll={{ x: 800 }}
        />
      </Card>

      {/* Create Maintenance Modal */}
      <CreateTicketModal
        open={showCreateMaintenanceModal}
        onClose={() => setShowCreateMaintenanceModal(false)}
        onSuccess={handleCreateSuccess}
        type="bảo trì"
      />

      {/* Create Complaint Modal */}
      <CreateTicketModal
        open={showCreateComplaintModal}
        onClose={() => setShowCreateComplaintModal(false)}
        onSuccess={handleCreateSuccess}
        type="khiếu nại"
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
                if (value === 'vehicle-registration') {
                  setShowVehicleRegistrationModal(true);
                } else if (value === 'vehicle-cancellation') {
                  setShowVehicleCancellationModal(true);
                }
                
                // Reset sau khi chọn
                setTimeout(() => setSelectedRequestType(null), 100);
              }}
            >
              <Select.Option value="vehicle-registration">
                <Space>
                  <CarOutlined style={{ color: '#52c41a' }} />
                  <span>Đăng ký gửi xe</span>
                </Space>
              </Select.Option>
              <Select.Option value="vehicle-cancellation">
                <Space>
                  <CarOutlined style={{ color: '#ff4d4f' }} />
                  <span>Hủy đăng ký xe</span>
                </Space>
              </Select.Option>
              {/* Có thể thêm các loại yêu cầu khác ở đây */}
            </Select>
          </Form.Item>
        </Form>
      </Modal>

      {/* Create Other Ticket Modal */}
      <CreateTicketModal
        open={showOtherTicketModal}
        onClose={() => setShowOtherTicketModal(false)}
        onSuccess={handleCreateSuccess}
        type="yêu cầu khác"
      />

      {/* Vehicle Registration Modal */}
      <VehicleRegistrationModal
        open={showVehicleRegistrationModal}
        onClose={() => setShowVehicleRegistrationModal(false)}
        onSuccess={handleCreateSuccess}
      />

      {/* Vehicle Cancellation Modal */}
      <VehicleCancellationModal
        visible={showVehicleCancellationModal}
        onClose={() => setShowVehicleCancellationModal(false)}
        onSuccess={handleCreateSuccess}
      />

      {/* Ticket Detail Modal */}
      <TicketDetailModal
        open={showDetailModal}
        onClose={() => {
          setShowDetailModal(false);
          setSelectedTicket(null);
        }}
        ticketId={selectedTicket?.ticketId}
      />
    </div>
  );
}
