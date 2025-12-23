import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Modal,
  Card,
  Row,
  Col,
  Typography,
  Button,
  Spin,
  Alert,
  Descriptions,
  Space,
  Result,
  Statistic,
  Badge,
  Image
} from 'antd';
import {
  QrcodeOutlined,
  ClockCircleOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  ReloadOutlined,
  InfoCircleOutlined,
  ExclamationCircleOutlined,
  MobileOutlined
} from '@ant-design/icons';
import { createInvoicePaymentQR, verifyInvoicePayment } from '../features/resident/invoiceApi';
import dayjs from 'dayjs';

const { Text } = Typography;

const InvoicePaymentModal = ({ open, onCancel, invoice, onPaymentComplete }) => {
  const navigate = useNavigate();
  const hasVerifiedRef = useRef(false);

  // States
  const [qrCode, setQrCode] = useState('');
  const [orderCode, setOrderCode] = useState('');
  const [deadline, setDeadline] = useState(Date.now() + 5 * 60 * 1000);
  const [paymentStatus, setPaymentStatus] = useState('PENDING'); // PENDING, PAID, FAILED
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [verifying, setVerifying] = useState(false);
  const [receiptInfo, setReceiptInfo] = useState(null);

  // Intervals
  const [statusInterval, setStatusInterval] = useState(null);

  // Format giá tiền
  const formatPrice = (price) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND'
    }).format(price);
  };

  // Xóa tất cả intervals
  const clearAllIntervals = () => {
    if (statusInterval) {
      clearInterval(statusInterval);
      setStatusInterval(null);
    }
  };

  // Tạo mã QR thanh toán
  const createPaymentQR = async () => {
    if (!invoice) return;

    console.log('Invoice data:', invoice); // Debug: xem cấu trúc invoice

    setLoading(true);
    setError('');
    hasVerifiedRef.current = false;

    try {
      const result = await createInvoicePaymentQR(invoice.invoiceId);

      if (result.success) {
        setQrCode(result.qrCode);
        setOrderCode(result.orderCode);
        setDeadline(Date.now() + 5 * 60 * 1000); // Reset deadline

        // Bắt đầu kiểm tra trạng thái
        startStatusPolling(invoice.invoiceId, result.orderCode);
      } else {
        setError(result.message || 'Không thể tạo mã QR thanh toán');
      }
    } catch (err) {
      console.error('Create payment QR error:', err);
      setError(err.errorMessage || err.message || 'Không thể tạo mã QR thanh toán');
    } finally {
      setLoading(false);
    }
  };

  // Kiểm tra trạng thái thanh toán liên tục
  const startStatusPolling = (invoiceId, orderCode) => {
    const interval = setInterval(async () => {
      if (hasVerifiedRef.current) {
        clearAllIntervals();
        return;
      }

      try {
        setVerifying(true);
        const result = await verifyInvoicePayment(invoiceId, orderCode);

        if (result.success && result.isPaid) {
          hasVerifiedRef.current = true;
          clearAllIntervals();
          setPaymentStatus('PAID');
          setReceiptInfo(result.receipt);
          handlePaymentSuccess(result);
        }
      } catch (err) {
        console.error('Verify payment error:', err);
      } finally {
        setVerifying(false);
      }
    }, 3000); // Kiểm tra mỗi 3 giây

    setStatusInterval(interval);
  };

  // Xử lý khi thanh toán thành công
  const handlePaymentSuccess = async (result) => {
    console.log('handlePaymentSuccess: Starting...', { invoiceId: invoice?.invoiceId, orderCode });

    setPaymentStatus('PAID');

    // Đợi một chút để người dùng thấy thông báo thành công
    await new Promise(resolve => setTimeout(resolve, 1500));

    // Đóng modal
    if (onCancel) {
      onCancel();
    }

    // Callback để reload data
    if (onPaymentComplete) {
      onPaymentComplete(true);
    }

    // Navigate đến trang success
    navigate('/payment/success', {
      state: {
        paymentData: {
          data: { orderCode: orderCode },
          status: 'PAID',
        },
        bookingInfo: {
          serviceType: 'Hóa đơn dịch vụ',
          invoiceId: invoice.invoiceId,
          invoiceNo: result.invoice?.invoiceNo || invoice.invoiceNo,
          amount: result.invoice?.totalAmount || invoice.totalAmount,
          apartmentNumber: invoice.apartmentNumber,
          paymentMethod: 'QR Banking',
          description: `Thanh toán hóa đơn ${invoice.invoiceNo}`,
          receiptNo: result.receipt?.receiptNo,
          transactionReference: result.transaction?.transactionReference,
        },
        message: 'Thanh toán hóa đơn thành công!',
      },
    });
  };

  // Xử lý khi thanh toán thất bại
  const handlePaymentFailed = () => {
    setPaymentStatus('FAILED');
    if (onPaymentComplete) {
      onPaymentComplete(false);
    }
  };

  // Xử lý khi hết thời gian
  const handleTimeout = () => {
    hasVerifiedRef.current = true;
    clearAllIntervals();
    setPaymentStatus('FAILED');

    Modal.error({
      title: 'Đã hết thời gian thanh toán',
      content: 'Phiên thanh toán đã hết hạn. Vui lòng thực hiện lại giao dịch.',
      onOk: () => {
        if (onCancel) {
          onCancel();
        }
      }
    });

    if (onPaymentComplete) {
      onPaymentComplete(false);
    }
  };

  // Xử lý khi người dùng hủy
  const handleCancel = () => {
    hasVerifiedRef.current = true;
    clearAllIntervals();

    Modal.info({
      title: 'Đã hủy',
      content: 'Bạn đã hủy giao dịch thanh toán.',
      onOk: () => {
        if (onCancel) {
          onCancel();
        }
      }
    });

    if (onPaymentComplete) {
      onPaymentComplete(false);
    }
  };

  // Khởi tạo khi modal mở
  useEffect(() => {
    if (open && invoice && !qrCode) {
      createPaymentQR();
    }
  }, [open, invoice]); // eslint-disable-line react-hooks/exhaustive-deps

  // Cleanup khi đóng modal
  useEffect(() => {
    if (!open) {
      hasVerifiedRef.current = false;
      clearAllIntervals();
      setQrCode('');
      setOrderCode('');
      setDeadline(Date.now() + 5 * 60 * 1000);
      setPaymentStatus('PENDING');
      setError('');
      setReceiptInfo(null);
    }
  }, [open]); // eslint-disable-line react-hooks/exhaustive-deps

  // Cleanup khi component unmount
  useEffect(() => {
    return () => {
      clearAllIntervals();
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Render status badge
  const renderStatusBadge = () => {
    const statusConfig = {
      PAID: { status: 'success', text: 'Đã thanh toán', icon: <CheckCircleOutlined /> },
      FAILED: { status: 'error', text: 'Thất bại', icon: <CloseCircleOutlined /> },
      PENDING: { status: 'processing', text: 'Đang chờ thanh toán', icon: <ClockCircleOutlined /> }
    };

    const config = statusConfig[paymentStatus];
    return (
      <Badge
        status={config.status}
        text={
          <Space>
            {config.icon}
            {config.text}
          </Space>
        }
      />
    );
  };

  // Modal footer
  const modalFooter = (
    <Space>
      <Button
        icon={<CloseCircleOutlined />}
        onClick={handleCancel}
      >
        Hủy thanh toán
      </Button>
      {qrCode && (
        <Button
          type="primary"
          icon={<ReloadOutlined />}
          onClick={createPaymentQR}
          loading={loading}
        >
          Tạo lại QR
        </Button>
      )}
    </Space>
  );

  if (!invoice) return null;

  return (
    <Modal
      title={
        <Space>
          <QrcodeOutlined />
          <span>Thanh toán QR Code</span>
        </Space>
      }
      open={open}
      onCancel={handleCancel}
      footer={modalFooter}
      width={800}
      centered
      destroyOnClose
    >
      {loading ? (
        <div style={{ textAlign: 'center', padding: '60px 0' }}>
          <Spin size="large" />
          <div style={{ marginTop: 16 }}>
            <Text type="secondary">Đang tạo mã QR thanh toán...</Text>
          </div>
        </div>
      ) : error ? (
        <Result
          status="error"
          title="Có lỗi xảy ra"
          subTitle={error}
          extra={
            <Button type="primary" icon={<ReloadOutlined />} onClick={createPaymentQR}>
              Thử lại
            </Button>
          }
        />
      ) : (
        <Row gutter={[24, 24]}>
          {/* Cột bên trái - QR Code */}
          <Col xs={24} md={12}>
            <Card
              title={
                <Space>
                  <QrcodeOutlined />
                  <span>Mã QR thanh toán</span>
                </Space>
              }
              style={{ height: '100%' }}
            >
              <div style={{ textAlign: 'center' }}>
                {qrCode ? (
                  <div style={{ marginBottom: 16 }}>
                    <Image
                      src={qrCode}
                      alt="QR Code"
                      width={250}
                      height={250}
                      style={{ border: '1px solid #d9d9d9', borderRadius: 8 }}
                    />
                  </div>
                ) : (
                  <div
                    style={{
                      width: 250,
                      height: 250,
                      backgroundColor: '#f5f5f5',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      margin: '0 auto 16px',
                      borderRadius: 8,
                      border: '1px solid #d9d9d9'
                    }}
                  >
                    <Text type="secondary">Đang tạo QR...</Text>
                  </div>
                )}

                {/* Countdown Timer */}
                <Card size="small" style={{ marginBottom: 16 }}>
                  <Statistic.Countdown
                    title={
                      <Space>
                        <ClockCircleOutlined />
                        <span>Thời gian còn lại</span>
                      </Space>
                    }
                    value={deadline}
                    onFinish={handleTimeout}
                    format="mm:ss"
                    valueStyle={{ color: deadline - Date.now() <= 60000 ? '#cf1322' : '#1890ff' }}
                  />
                </Card>

                {/* Payment Status */}
                <div style={{ marginBottom: 16 }}>
                  <Text strong>Trạng thái: </Text>
                  {renderStatusBadge()}
                </div>
              </div>
            </Card>
          </Col>

          {/* Cột bên phải - Thông tin thanh toán */}
          <Col xs={24} md={12}>
            <Space direction="vertical" style={{ width: '100%' }} size="middle">
              {/* Thông tin hóa đơn */}
              <Card
                title={
                  <Space>
                    <InfoCircleOutlined />
                    <span>Thông tin hóa đơn</span>
                  </Space>
                }
                size="small"
              >
                <Descriptions column={1} size="small">
                  <Descriptions.Item label="Mã hóa đơn">
                    <Text strong code>{invoice.invoiceNo}</Text>
                  </Descriptions.Item>
                  <Descriptions.Item label="Hạn thanh toán">
                    {dayjs(invoice.dueDate).format('DD/MM/YYYY')}
                  </Descriptions.Item>
                  {orderCode && (
                    <Descriptions.Item label="Mã đơn hàng">
                      <Text code>{orderCode}</Text>
                    </Descriptions.Item>
                  )}
                </Descriptions>
              </Card>

              {/* Tổng tiền */}
              <Card size="small">
                <Statistic
                  title="Tổng tiền thanh toán"
                  value={invoice.totalAmount || 0}
                  formatter={(value) => formatPrice(value)}
                  valueStyle={{ color: '#1890ff', fontSize: 24 }}
                />
              </Card>

              {/* Hướng dẫn */}
              <Card
                title={
                  <Space>
                    <MobileOutlined />
                    <span>Hướng dẫn thanh toán</span>
                  </Space>
                }
                size="small"
              >
                <ol style={{ margin: 0, paddingLeft: 20 }}>
                  <li>Mở ứng dụng ngân hàng trên điện thoại</li>
                  <li>Chọn chức năng quét mã QR</li>
                  <li>Quét mã QR hiển thị bên trái</li>
                  <li>Xác nhận thông tin và hoàn tất thanh toán</li>
                </ol>
              </Card>

              {/* Thông báo quan trọng */}
              <Alert
                message="Lưu ý quan trọng"
                description="Vui lòng hoàn tất thanh toán trong thời gian quy định. Sau khi thanh toán thành công, bạn sẽ được chuyển đến trang xác nhận."
                type="info"
                showIcon
                icon={<ExclamationCircleOutlined />}
              />
            </Space>
          </Col>
        </Row>
      )}
    </Modal>
  );
};

export default InvoicePaymentModal;
