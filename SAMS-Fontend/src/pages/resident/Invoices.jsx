import React, { useState, useEffect, useCallback } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import {
  Layout,
  Card,
  Row,
  Col,
  Button,
  Tag,
  Typography,
  Space,
  Badge,
  Table,
  Modal,
  Spin,
  Divider,
  Empty,
  Alert,
  Tabs,
} from "antd";
import {
  ClockCircleOutlined,
  CheckCircleOutlined,
  WarningOutlined,
} from "@ant-design/icons";
import useNotification from "../../hooks/useNotification";
import {
  getMyInvoices,
  categorizeInvoices,
  calculateTotals,
  formatInvoiceStatus,
  getInvoiceStatusColor,
  createInvoicePaymentQR,
  verifyInvoicePayment,
} from "../../features/resident/invoiceApi";
import InvoicePaymentModal from "../../components/InvoicePaymentModal";
import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";
import "dayjs/locale/vi";

dayjs.extend(relativeTime);
dayjs.locale("vi");

const { Content } = Layout;
const { Title, Text } = Typography;

export default function Invoices() {
  const navigate = useNavigate();
  const location = useLocation();
  const { showNotification } = useNotification();

  // States
  const [loading, setLoading] = useState(true);
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const [allInvoices, setAllInvoices] = useState([]);
  const [categorizedInvoices, setCategorizedInvoices] = useState({
    unpaid: [],
    paid: [],
    overdue: [],
  });
  const [selectedInvoice, setSelectedInvoice] = useState(null);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [showQRPayment, setShowQRPayment] = useState(false);
  const [paymentData, setPaymentData] = useState(null);
  const [activeTab, setActiveTab] = useState("unpaid");

  // Stats
  const [stats, setStats] = useState({
    totalInvoices: 0,
    unpaidCount: 0,
    paidCount: 0,
    overdueCount: 0,
    totalAmount: 0,
    totalDebt: 0,
  });

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (isDropdownOpen && !event.target.closest(".user-dropdown-container")) {
        setIsDropdownOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [isDropdownOpen]);

  const loadInvoicesData = useCallback(async () => {
    setLoading(true);
    try {
      const invoicesData = await getMyInvoices();
      setAllInvoices(invoicesData);

      const categorized = categorizeInvoices(invoicesData);
      setCategorizedInvoices(categorized);

      const totals = calculateTotals(invoicesData);
      setStats({
        totalInvoices: totals.totalInvoices,
        unpaidCount: categorized.unpaid.length,
        paidCount: categorized.paid.length,
        overdueCount: categorized.overdue.length,
        totalAmount: totals.totalAmount,
        totalDebt: totals.totalDebt,
      });
    } catch (error) {
      console.error("Error loading invoices:", error);
      showNotification("error", "Lỗi", "Không thể tải dữ liệu hóa đơn");
    } finally {
      setLoading(false);
    }
  }, [showNotification]);

  useEffect(() => {
    const initPage = async () => {
      try {
        loadInvoicesData();
      } catch (error) {
        console.error("Failed to initialize invoices page:", error);
        loadInvoicesData();
      }
    };
    initPage();
  }, [loadInvoicesData]);

  useEffect(() => {
    const searchParams = new URLSearchParams(location.search);
    const invoiceId = searchParams.get("invoiceId");
    if (!invoiceId) return;
    if (allInvoices.length === 0) return;

    const invoice = allInvoices.find((inv) => inv.invoiceId === invoiceId);
    if (!invoice) return;

    handleViewDetail(invoice);
    searchParams.delete("invoiceId");
    const newSearch = searchParams.toString();
    navigate(
      {
        pathname: location.pathname,
        search: newSearch ? `?${newSearch}` : "",
      },
      { replace: true }
    );
  }, [location.search, allInvoices, location.pathname, navigate]);

  const handleViewDetail = (invoice) => {
    setSelectedInvoice(invoice);
    setShowDetailModal(true);
  };

  const handlePayment = (invoice) => {
    setSelectedInvoice(invoice);
    setShowQRPayment(true);
  };

  const handlePaymentComplete = async (success) => {
    setShowQRPayment(false);

    if (success) {
      showNotification(
        "success",
        "Thành công",
        "Thanh toán hóa đơn thành công!"
      );

      // Reload data
      await loadInvoicesData();
    }

    setSelectedInvoice(null);
  };

  // Render invoice cards for each category
  const renderInvoiceCards = (invoices, showPaymentButton = false) => {
    if (!invoices || invoices.length === 0) {
      return (
        <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description="Không có hóa đơn nào"
        />
      );
    }

    return (
      <Row gutter={[12, 12]}>
        {invoices.map((invoice) => (
          <Col xs={24} sm={12} md={8} lg={6} xl={4} key={invoice.invoiceId}>
            <Card
              hoverable
              className="h-full"
              size="small"
              bodyStyle={{ padding: "12px" }}
              actions={[
                <Button
                  type="text"
                  size="small"
                  className="text-xs px-2"
                  onClick={() => handleViewDetail(invoice)}
                >
                  Chi tiết
                </Button>,
                showPaymentButton && (
                  <Button
                    type="primary"
                    size="small"
                    className="text-xs px-2"
                    onClick={() => handlePayment(invoice)}
                  >
                    Thanh toán
                  </Button>
                ),
              ].filter(Boolean)}
            >
              <Card.Meta
                title={
                  <Space direction="vertical" size={2} className="w-full">
                    <Text strong code className="text-xs">
                      {invoice.invoiceNo}
                    </Text>
                    <Tag
                      size="small"
                      color={getInvoiceStatusColor(invoice.status)}
                    >
                      {formatInvoiceStatus(invoice.status)}
                    </Tag>
                  </Space>
                }
                description={
                  <Space direction="vertical" size={4} className="w-full">
                    <div>
                      <Text type="secondary" className="text-xs">
                        Hạn: {dayjs(invoice.dueDate).format("DD/MM/YYYY")}
                      </Text>
                    </div>
                    <div className="text-center">
                      <Text strong className="text-sm text-blue-600">
                        {invoice.totalAmount.toLocaleString("vi-VN")} đ
                      </Text>
                    </div>
                  </Space>
                }
              />
            </Card>
          </Col>
        ))}
      </Row>
    );
  };

  if (loading) {
    return (
      <div className="text-center py-24">
        <Spin size="large" tip="Đang tải dữ liệu hóa đơn..." />
      </div>
    );
  }

  return (
    <div>
      {/* Main Content */}
      <div>
        <Content>
          <div className="mb-6">
            <Title level={2} className="mb-2">
              Hóa đơn của tôi
            </Title>

            <Text type="secondary">
              Quản lý và thanh toán các hóa đơn phí dịch vụ
            </Text>
          </div>

          {/* Alert for overdue invoices */}
          {stats.overdueCount > 0 && (
            <Alert
              message={`Bạn có ${stats.overdueCount} hóa đơn quá hạn thanh toán`}
              description="Vui lòng thanh toán sớm để tránh phí phạt chậm thanh toán"
              type="warning"
              showIcon
              closable
              className="mb-6"
            />
          )}

          {/* Refresh Button */}
          <div className="mb-6 text-right">
            <Button type="primary" onClick={loadInvoicesData} loading={loading}>
              Làm mới
            </Button>
          </div>

          {/* Invoices by Category */}
          <Card>
            <Tabs
              activeKey={activeTab}
              onChange={setActiveTab}
              type="card"
              size="default"
              items={[
                {
                  key: "unpaid",
                  label: (
                    <Space>
                      <ClockCircleOutlined />
                      <span>Chưa thanh toán</span>
                      <Badge
                        count={stats.unpaidCount}
                        showZero
                        color="orange"
                      />
                    </Space>
                  ),
                  children: (
                    <div className="py-4">
                      <div className="mb-4">
                        <Text type="secondary">
                          Các hóa đơn chưa được thanh toán. Vui lòng thanh toán
                          trước hạn để tránh phí phạt.
                        </Text>
                      </div>
                      {renderInvoiceCards(categorizedInvoices.unpaid, true)}
                    </div>
                  ),
                },
                {
                  key: "overdue",
                  label: (
                    <Space>
                      <WarningOutlined />
                      <span>Quá hạn</span>
                      <Badge count={stats.overdueCount} showZero color="red" />
                    </Space>
                  ),
                  children: (
                    <div className="py-4">
                      <Alert
                        message="Hóa đơn quá hạn thanh toán"
                        description="Các hóa đơn này đã quá hạn thanh toán. Vui lòng thanh toán ngay để tránh phí phạt chậm thanh toán."
                        type="warning"
                        showIcon
                        className="mb-4"
                      />
                      {renderInvoiceCards(categorizedInvoices.overdue, true)}
                    </div>
                  ),
                },
                {
                  key: "paid",
                  label: (
                    <Space>
                      <CheckCircleOutlined />
                      <span>Đã thanh toán</span>
                      <Badge count={stats.paidCount} showZero color="green" />
                    </Space>
                  ),
                  children: (
                    <div className="py-4">
                      <div className="mb-4">
                        <Text type="secondary">
                          Lịch sử các hóa đơn đã thanh toán thành công.
                        </Text>
                      </div>
                      {renderInvoiceCards(categorizedInvoices.paid, false)}
                    </div>
                  ),
                },
              ]}
            />
          </Card>
        </Content>
      </div>

      {/* Invoice Detail Modal */}
      <Modal
        title={`Chi tiết hóa đơn ${selectedInvoice?.invoiceNo}`}
        open={showDetailModal}
        onCancel={() => {
          setShowDetailModal(false);
          setSelectedInvoice(null);
        }}
        footer={[
          <Button key="close" onClick={() => setShowDetailModal(false)}>
            Đóng
          </Button>,
          selectedInvoice?.status !== "PAID" && (
            <Button
              key="pay"
              type="primary"
              onClick={() => {
                setShowDetailModal(false);
                handlePayment(selectedInvoice);
              }}
            >
              Thanh toán
            </Button>
          ),
        ].filter(Boolean)}
        width={650}
      >
        {selectedInvoice && (
          <div>
            {/* Invoice Header */}
            <Card size="small" className="mb-4">
              <Row gutter={[8, 8]}>
                <Col xs={24} sm={12}>
                  <div style={{ marginBottom: 8 }}>
                    <Text type="secondary" style={{ fontSize: "12px" }}>
                      Mã hóa đơn:
                    </Text>
                    <br />
                    <Text code strong style={{ fontSize: "13px" }}>
                      {selectedInvoice.invoiceNo}
                    </Text>
                  </div>
                  <div>
                    <Text type="secondary" style={{ fontSize: "12px" }}>
                      Ngày tạo:
                    </Text>
                    <br />
                    <Text style={{ fontSize: "13px" }}>
                      {selectedInvoice.createdAt &&
                        dayjs(selectedInvoice.createdAt).format(
                          "DD/MM/YYYY HH:mm"
                        )}
                    </Text>
                  </div>
                </Col>
                <Col xs={24} sm={12}>
                  <div style={{ marginBottom: 8 }}>
                    <Text type="secondary" style={{ fontSize: "12px" }}>
                      Ngày phát hành:
                    </Text>
                    <br />
                    <Text style={{ fontSize: "13px" }}>
                      {dayjs(selectedInvoice.issueDate).format("DD/MM/YYYY")}
                    </Text>
                  </div>
                  <div style={{ marginBottom: 8 }}>
                    <Text type="secondary" style={{ fontSize: "12px" }}>
                      Hạn thanh toán:
                    </Text>
                    <br />
                    <Text style={{ fontSize: "13px" }}>
                      {dayjs(selectedInvoice.dueDate).format("DD/MM/YYYY")}
                    </Text>
                  </div>
                  <div>
                    <Text type="secondary" style={{ fontSize: "12px" }}>
                      Trạng thái:
                    </Text>
                    <br />
                    <Tag
                      size="small"
                      color={getInvoiceStatusColor(selectedInvoice.status)}
                    >
                      {formatInvoiceStatus(selectedInvoice.status)}
                    </Tag>
                  </div>
                </Col>
              </Row>
            </Card>

            {/* Services Detail */}
            <Card title="Chi tiết dịch vụ" size="small" className="mb-4">
              <Table
                dataSource={selectedInvoice.details || []}
                columns={[
                  {
                    title: "Dịch vụ",
                    dataIndex: "serviceName",
                    key: "serviceName",
                    render: (text, record) => (
                      <div>
                        <Text strong>{text}</Text>
                        <br />
                        <Text type="secondary" className="text-xs">
                          Mã: {record.serviceCode}
                        </Text>
                        {record.description && (
                          <>
                            <br />
                            <Text type="secondary" className="text-[11px]">
                              {record.description}
                            </Text>
                          </>
                        )}
                      </div>
                    ),
                  },
                  {
                    title: "Số lượng",
                    dataIndex: "quantity",
                    key: "quantity",
                    render: (quantity, record) =>
                      `${quantity} ${record.serviceUnit}`,
                  },
                  {
                    title: "Đơn giá",
                    dataIndex: "unitPrice",
                    key: "unitPrice",
                    render: (price) => (
                      <Text>{price.toLocaleString("vi-VN")} đ</Text>
                    ),
                  },
                  {
                    title: "Thành tiền",
                    dataIndex: "amount",
                    key: "amount",
                    render: (amount) => (
                      <Text strong>{amount.toLocaleString("vi-VN")} đ</Text>
                    ),
                  },
                  {
                    title: "VAT",
                    dataIndex: "vatAmount",
                    key: "vatAmount",
                    render: (vatAmount, record) => (
                      <div>
                        <Text>{record.vatRate}%</Text>
                        <br />
                        <Text type="secondary" className="text-[11px]">
                          {vatAmount.toLocaleString("vi-VN")} đ
                        </Text>
                      </div>
                    ),
                  },
                ]}
                pagination={false}
                size="small"
              />
            </Card>

            {/* Total Summary */}
            <Card size="small">
              <Row justify="end">
                <Col xs={24} sm={12} md={10}>
                  <div
                    style={{
                      border: "1px solid #f0f0f0",
                      borderRadius: "6px",
                      padding: "12px",
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        justifyContent: "space-between",
                        marginBottom: "8px",
                      }}
                    >
                      <Text style={{ fontSize: "13px" }}>Tổng phụ:</Text>
                      <Text style={{ fontSize: "13px" }}>
                        {selectedInvoice.subtotalAmount.toLocaleString("vi-VN")}{" "}
                        đ
                      </Text>
                    </div>
                    <div
                      style={{
                        display: "flex",
                        justifyContent: "space-between",
                        marginBottom: "8px",
                      }}
                    >
                      <Text style={{ fontSize: "13px" }}>Thuế VAT:</Text>
                      <Text style={{ fontSize: "13px" }}>
                        {selectedInvoice.taxAmount.toLocaleString("vi-VN")} đ
                      </Text>
                    </div>
                    <Divider style={{ margin: "8px 0" }} />
                    <div
                      style={{
                        display: "flex",
                        justifyContent: "space-between",
                      }}
                    >
                      <Text strong style={{ fontSize: "14px" }}>
                        Tổng thanh toán:
                      </Text>
                      <Text
                        strong
                        style={{ fontSize: "16px", color: "#1890ff" }}
                      >
                        {selectedInvoice.totalAmount.toLocaleString("vi-VN")} đ
                      </Text>
                    </div>
                  </div>
                </Col>
              </Row>
            </Card>
          </div>
        )}
      </Modal>

      {/* Invoice Payment Modal */}
      <InvoicePaymentModal
        open={showQRPayment}
        onCancel={() => setShowQRPayment(false)}
        invoice={selectedInvoice}
        onPaymentComplete={handlePaymentComplete}
      />
    </div>
  );
}
