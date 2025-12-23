import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Modal,
  Card,
  Row,
  Col,
  Typography,
  Badge,
  Button,
  Spin,
  Alert,
  Descriptions,
  Statistic,
  Space,
  Image,
  Result
} from 'antd';
import {
  QrcodeOutlined,
  ClockCircleOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  ExclamationCircleOutlined,
  ReloadOutlined,
  MobileOutlined,
  InfoCircleOutlined
} from '@ant-design/icons';
import { paymentApi } from '../features/payment/paymentApi';
import { amenityBookingApi } from '../features/amenity-booking/amenityBookingApi';
import { createReceiptFromPayment } from '../features/accountant/receiptApi';

const { Text } = Typography;

const QRPaymentModal = ({ open, onCancel, amenityData, onPaymentComplete, skipNavigation = false }) => {
  const navigate = useNavigate();
  const hasNavigatedRef = useRef(false);



  // States
  const [qrCode, setQrCode] = useState('');
  const [orderCode, setOrderCode] = useState('');
  const [deadline, setDeadline] = useState(Date.now() + 5 * 60 * 1000); // 5 phút từ bây giờ
  const [paymentStatus, setPaymentStatus] = useState('PENDING'); // PENDING, PAID, FAILED
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

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
    if (!amenityData) return;

    setLoading(true);
    setError('');

    try {
      const paymentRequest = {
        amount: amenityData.totalPrice,
        description: `Thanh toán đặt tiện ích ${amenityData.amenityName}`,
        expiredAt: 5, // 5 phút
        items: [{
          name: amenityData.amenityName,
          quantity: 1,
          price: amenityData.totalPrice
        }]
      };

      const result = await paymentApi.createPayment(paymentRequest);

      if (result.success) {
        setQrCode(result.qrCode);
        setOrderCode(result.orderCode);
        setDeadline(Date.now() + 5 * 60 * 1000); // Reset deadline

        // Bắt đầu kiểm tra trạng thái
        startStatusPolling(result.orderCode, amenityData.totalPrice);
      }
    } catch (err) {
      console.error('Create payment error:', err);
      setError(err.message || 'Không thể tạo mã QR thanh toán');
    } finally {
      setLoading(false);
    }
  };

  // Kiểm tra trạng thái thanh toán liên tục
  const startStatusPolling = (orderCode, amount) => {
    const interval = setInterval(async () => {
      // Kiểm tra nếu đã navigate rồi thì dừng ngay
      if (hasNavigatedRef.current) {
        clearAllIntervals();
        return;
      }

      try {
        const result = await paymentApi.checkPaymentStatus(orderCode, amount);

        // Check cả result.status và result.data?.status
        const paymentStatus = result.status || result.data?.status;

        console.log('Payment status check:', { orderCode, amount, status: paymentStatus, fullResult: result });

        if (result.success && paymentStatus) {
          // Cập nhật trạng thái ngay lập tức
          setPaymentStatus(paymentStatus);

          if (paymentStatus === 'PAID' || paymentStatus === 'SUCCESS') {
            console.log('Payment successful! Stopping polling and handling success');
            hasNavigatedRef.current = true; // Đánh dấu đã navigate
            clearInterval(interval);
            setStatusInterval(null);
            handlePaymentSuccess();
            return; // Dừng ngay sau khi navigate thành công
          } else if (paymentStatus === 'FAILED' || paymentStatus === 'CANCELLED') {
            console.log('Payment failed! Stopping polling and handling failure');
            hasNavigatedRef.current = true; // Đánh dấu đã navigate
            clearInterval(interval);
            setStatusInterval(null);
            handlePaymentFailed();
            return; // Dừng ngay sau khi navigate thất bại
          }
        } else {
          console.log('Payment status check: No status found', { success: result.success, status: paymentStatus });
        }
      } catch (err) {
        console.error('Check payment status error:', err);
      }
    }, 3000); // Kiểm tra mỗi 3 giây để tránh too many requests

    setStatusInterval(interval);
  };

  // Xử lý khi thanh toán thành công
  const handlePaymentSuccess = async () => {
    console.log('handlePaymentSuccess: Starting...', { bookingId: amenityData?.bookingId, orderCode });

    // Set payment status để hiển thị thông báo thành công ngay
    setPaymentStatus('PAID');

    // ========== TỰ ĐỘNG TẠO RECEIPT KHI THANH TOÁN INVOICE ==========
    if (amenityData?.invoiceId) {
      try {
        console.log('handlePaymentSuccess: Creating receipt for invoice...', amenityData.invoiceId);
        
        // Gọi API tạo Receipt từ payment
        const receiptPayload = {
          invoiceId: amenityData.invoiceId,
          amount: amenityData.totalPrice,
          paymentMethodCode: 'VIETQR',
          paymentDate: new Date().toISOString(),
          note: amenityData.description || 'Thanh toán VietQR thành công'
        };

        const receiptData = await createReceiptFromPayment(receiptPayload);
        console.log('handlePaymentSuccess: Receipt created successfully', receiptData);
        
        // Lưu thông tin receipt để hiển thị sau
        if (amenityData.description) {
          amenityData.receiptNo = receiptData.receiptNo;
          amenityData.receiptId = receiptData.receiptId;
        }
      } catch (error) {
        console.error('handlePaymentSuccess: Error creating receipt:', error);
        
        // Kiểm tra nếu receipt đã tồn tại (invoice đã có receipt rồi)
        const errorMsg = error?.response?.data?.error || error?.message || '';
        if (errorMsg.toLowerCase().includes('already has receipt') || 
            errorMsg.toLowerCase().includes('đã có biên lai')) {
          console.log('handlePaymentSuccess: Receipt already exists, continuing...');
          // Không hiển thị lỗi, vì invoice đã có receipt là OK (idempotency)
        } else {
          // Lỗi khác: Hiển thị warning nhưng vẫn tiếp tục flow
          console.warn('handlePaymentSuccess: Failed to create receipt but payment was successful');
          Modal.warning({
            title: 'Thanh toán thành công',
            content: 'Thanh toán đã hoàn tất nhưng có lỗi khi tạo biên lai. Biên lai sẽ được tạo tự động sau hoặc vui lòng liên hệ quản lý.',
          });
        }
      }
    }
    // ========== KẾT THÚC LOGIC TẠO RECEIPT ==========

    // Cập nhật trạng thái booking và thanh toán nếu có bookingId
    if (amenityData?.bookingId) {
      // Check if it's a temporary ID
      const isTemporaryId = amenityData.bookingId.toString().startsWith('temp_');

      if (!isTemporaryId) {
        try {
          console.log('handlePaymentSuccess: Confirming booking...', amenityData.bookingId);
          // First confirm the booking (changes status to Confirmed)
          await amenityBookingApi.confirm(amenityData.bookingId);

          // Đợi một chút để đảm bảo confirm đã được lưu vào DB
          await new Promise(resolve => setTimeout(resolve, 500));

          console.log('handlePaymentSuccess: Updating payment status...', amenityData.bookingId);
          // Then update payment status to Paid
          await amenityBookingApi.updatePaymentStatus(amenityData.bookingId, 'Paid');
          console.log('handlePaymentSuccess: Payment status updated successfully');
        } catch (error) {
          console.error('Error updating booking status:', error);
        }
      }
    }

    if (typeof window !== 'undefined') {
      window.dispatchEvent(new Event('amenity-notification-updated'));
    }

    // Đợi một chút để người dùng thấy thông báo thành công
    await new Promise(resolve => setTimeout(resolve, 1500));

    // Đóng modal trước khi navigate
    if (onCancel && typeof onCancel === 'function') {
      onCancel();
    }

    // Chỉ navigate nếu không skip (ví dụ: khi thanh toán từ màn hình lễ tân thì không navigate)
    if (!skipNavigation) {
      // Always navigate to payment success page
      console.log('handlePaymentSuccess: Navigating to success page...');
      navigate('/payment/success', {
        state: {
          paymentData: {
            data: { orderCode: orderCode },
            status: 'PAID'
          },
          bookingInfo: {
            amenityName: amenityData?.amenityName,
            bookingType: amenityData?.bookingType || 'Hourly',
            timeInfo: `${amenityData?.bookingDate} - ${amenityData?.timeSlot}`,
            amount: amenityData?.totalPrice,
            description: `Thanh toán đặt tiện ích ${amenityData?.amenityName}`,
            bookingId: amenityData?.bookingId
          },
          message: 'Thanh toán thành công!'
        }
      });
    }

    // Call callback if provided after navigation
    if (onPaymentComplete && typeof onPaymentComplete === 'function') {
      onPaymentComplete(true); // true indicates success
    }
  };

  // Xử lý khi thanh toán thất bại
  const handlePaymentFailed = async () => {
    // Cập nhật trạng thái thanh toán thất bại nếu có bookingId
    if (amenityData?.bookingId) {
      // Check if it's a temporary ID
      const isTemporaryId = amenityData.bookingId.toString().startsWith('temp_');

      if (!isTemporaryId) {
        try {
          // Use the dedicated cancel API endpoint with failure reason
          await amenityBookingApi.cancel(amenityData.bookingId, 'Payment failed');
        } catch (error) {
          console.error('Error cancelling booking on failure:', error);
        }
      }
    }

    // Call callback if provided
    if (onPaymentComplete && typeof onPaymentComplete === 'function') {
      onPaymentComplete(false); // false indicates failure
    }
  };

  // Xử lý khi hết thời gian
  const handleTimeout = async () => {
    hasNavigatedRef.current = true; // Đánh dấu đã navigate
    clearAllIntervals();

    // Cập nhật trạng thái booking thành Cancelled khi hết thời gian thanh toán
    if (amenityData?.bookingId) {
      // Check if it's a temporary ID
      const isTemporaryId = amenityData.bookingId.toString().startsWith('temp_');

      if (!isTemporaryId) {
        try {
          // Use the dedicated cancel API endpoint with timeout reason
          await amenityBookingApi.cancel(amenityData.bookingId, 'Payment timeout - exceeded time limit');
        } catch (error) {
          console.error('Error cancelling booking on timeout:', error);
        }
      }
    }

    // Show timeout message and close modal
    Modal.error({
      title: 'Đã hết thời gian thanh toán',
      content: 'Phiên thanh toán đã hết hạn. Vui lòng thực hiện lại giao dịch.',
      onOk: () => {
        if (onCancel) {
          onCancel();
        }
      }
    });

    // Call callback if provided
    if (onPaymentComplete && typeof onPaymentComplete === 'function') {
      onPaymentComplete(false); // false indicates failure due to timeout
    }
  };

  // Xử lý khi người dùng hủy
  const handleCancel = async () => {
    hasNavigatedRef.current = true; // Đánh dấu đã navigate
    clearAllIntervals();

    // Cập nhật trạng thái booking thành Cancelled khi user hủy thanh toán
    if (amenityData?.bookingId) {
      // Check if it's a temporary ID
      const isTemporaryId = amenityData.bookingId.toString().startsWith('temp_');

      if (!isTemporaryId) {
        try {
          // Use the dedicated cancel API endpoint
          await amenityBookingApi.cancel(amenityData.bookingId, 'User cancelled payment');
        } catch (error) {
          console.error('Error cancelling booking:', error);
        }
      }
    }

    // Show cancel message
    Modal.info({
      title: 'Đã hủy',
      content: 'Bạn đã hủy giao dịch thanh toán.',
      onOk: () => {
        // Call original onCancel after showing message
        if (onCancel && typeof onCancel === 'function') {
          onCancel();
        }
      }
    });

    // Call callback if provided
    if (onPaymentComplete && typeof onPaymentComplete === 'function') {
      onPaymentComplete(false); // false indicates cancellation
    }
  };  // Khởi tạo khi modal mở
  useEffect(() => {
    if (open && amenityData && !qrCode) {
      createPaymentQR();
    }
  }, [open, amenityData]); // eslint-disable-line react-hooks/exhaustive-deps

  // Cleanup khi đóng modal
  useEffect(() => {
    if (!open) {
      hasNavigatedRef.current = false; // Reset flag khi đóng modal
      clearAllIntervals();
      setQrCode('');
      setOrderCode('');
      setDeadline(Date.now() + 5 * 60 * 1000);
      setPaymentStatus('PENDING');
      setError('');
    }
  }, [open]); // eslint-disable-line react-hooks/exhaustive-deps

  // Cleanup khi component unmount
  useEffect(() => {
    return () => {
      clearAllIntervals();
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Render payment status badge
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
              {/* Thông tin đặt chỗ */}
              <Card
                title={
                  <Space>
                    <InfoCircleOutlined />
                    <span>Thông tin đặt chỗ</span>
                  </Space>
                }
                size="small"
              >
                <Descriptions column={1} size="small">
                  <Descriptions.Item label="Tiện ích">
                    <Text strong>{amenityData?.amenityName}</Text>
                  </Descriptions.Item>
                  <Descriptions.Item label="Ngày đặt">
                    {amenityData?.bookingDate}
                  </Descriptions.Item>
                  <Descriptions.Item label="Khung giờ">
                    {amenityData?.timeSlot}
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
                  value={amenityData?.totalPrice || 0}
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

export default QRPaymentModal;