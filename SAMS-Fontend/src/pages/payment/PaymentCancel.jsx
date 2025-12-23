import React, { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { 
  Result, 
  Button, 
  Card, 
  Descriptions, 
  Typography, 
  Space, 
  Divider,
  Row,
  Col,
  Tag,
  Alert,
  Steps
} from 'antd';
import { 
  HomeOutlined, 
  ReloadOutlined, 
  CustomerServiceOutlined,
  PhoneOutlined,
  MailOutlined,
  QuestionCircleOutlined,
  CloseCircleOutlined
} from '@ant-design/icons';
import { PaymentFailedIcon } from '../../icons';
import ResidentLayout from '../../components/resident/ResidentLayout';

const { Text, Paragraph } = Typography;
const { Step } = Steps;

const PaymentCancel = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [paymentData, setPaymentData] = useState(null);

  useEffect(() => {
    // Lấy thông tin thanh toán từ state (từ QR Payment Modal)
    const stateData = location.state;
    let paymentInfo;
    
    if (stateData?.bookingInfo) {
      // Dữ liệu từ amenity booking
      paymentInfo = {
        transactionId: 'CANCELLED_' + Date.now(),
        amount: stateData.bookingInfo.amount || '0',
        paymentMethod: 'QR Banking',
        serviceType: 'Đăng ký tiện ích',
        amenityName: stateData.bookingInfo.amenityName,
        bookingType: stateData.bookingInfo.bookingType,
        timeInfo: stateData.bookingInfo.timeInfo,
        reason: stateData.reason || 'Thanh toán bị hủy',
        message: stateData.message || 'Thanh toán không thành công',
        cancelTime: new Date().toLocaleString('vi-VN'),
        status: 'CANCELLED'
      };
    } else {
      // Dữ liệu từ URL params (fallback)
      const urlParams = new URLSearchParams(location.search);
      paymentInfo = {
        transactionId: urlParams.get('transactionId') || 'TXN' + Date.now(),
        amount: urlParams.get('amount') || '0',
        paymentMethod: urlParams.get('paymentMethod') || 'VNPay',
        serviceType: urlParams.get('serviceType') || 'Hóa đơn dịch vụ',
        apartmentNumber: urlParams.get('apartmentNumber') || 'N/A',
        failureReason: urlParams.get('reason') || 'Người dùng hủy giao dịch',
        attemptTime: new Date().toLocaleString('vi-VN'),
        errorCode: urlParams.get('errorCode') || 'USER_CANCEL',
        status: 'FAILED'
      };
    }

    setPaymentData(paymentInfo);
  }, [location]);

  const handleGoHome = () => {
    navigate('/');
  };

  const handleRetryPayment = () => {
    // Logic thử lại thanh toán
    if (paymentData?.serviceType === 'Đăng ký tiện ích') {
      // Quay về trang đăng ký tiện ích
      navigate('/resident/amenity-booking');
    } else {
      // Logic thử lại thanh toán hóa đơn
      const retryData = {
        amount: paymentData.amount,
        serviceType: paymentData.serviceType,
        apartmentNumber: paymentData.apartmentNumber
      };
      navigate('/payment', { state: { retryData } });
    }
  };

  const handleContactSupport = () => {
    // Logic liên hệ hỗ trợ - có thể mở chat hoặc form liên hệ
    navigate('/support', { state: { issue: 'payment_failed', transactionId: paymentData?.transactionId } });
  };

  const handleViewInvoices = () => {
    if (paymentData?.serviceType === 'Đăng ký tiện ích') {
      navigate('/resident/my-bookings');
    } else {
      navigate('/resident/invoices');
    }
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND'
    }).format(amount);
  };

  const getErrorDescription = (errorCode, reason) => {
    const errorMap = {
      'USER_CANCEL': 'Bạn đã hủy giao dịch thanh toán',
      'INSUFFICIENT_FUNDS': 'Tài khoản không đủ số dư để thực hiện giao dịch',
      'CARD_EXPIRED': 'Thẻ thanh toán đã hết hạn',
      'INVALID_CARD': 'Thông tin thẻ không hợp lệ',
      'NETWORK_ERROR': 'Lỗi kết nối mạng, vui lòng thử lại',
      'TIMEOUT': 'Giao dịch bị hết thời gian chờ',
      'SYSTEM_ERROR': 'Lỗi hệ thống, vui lòng thử lại sau',
      'DECLINED': 'Giao dịch bị từ chối bởi ngân hàng'
    };
    
    return errorMap[errorCode] || reason || 'Giao dịch không thành công';
  };

  const getSolutionSteps = (errorCode) => {
    const solutionMap = {
      'USER_CANCEL': [
        { title: 'Thử lại thanh toán', description: 'Nhấn nút "Thử lại" để tiến hành thanh toán lại' },
        { title: 'Chọn phương thức khác', description: 'Có thể chọn phương thức thanh toán khác phù hợp' }
      ],
      'INSUFFICIENT_FUNDS': [
        { title: 'Kiểm tra số dư', description: 'Đảm bảo tài khoản có đủ số dư' },
        { title: 'Nạp thêm tiền', description: 'Nạp thêm tiền vào tài khoản thanh toán' },
        { title: 'Chọn phương thức khác', description: 'Sử dụng thẻ/tài khoản khác có đủ số dư' }
      ],
      'NETWORK_ERROR': [
        { title: 'Kiểm tra kết nối', description: 'Đảm bảo thiết bị có kết nối internet ổn định' },
        { title: 'Thử lại sau', description: 'Đợi một vài phút rồi thực hiện lại giao dịch' },
        { title: 'Liên hệ hỗ trợ', description: 'Nếu vấn đề vẫn tiếp diễn, hãy liên hệ bộ phận hỗ trợ' }
      ]
    };

    return solutionMap[errorCode] || [
      { title: 'Thử lại thanh toán', description: 'Thực hiện lại giao dịch sau vài phút' },
      { title: 'Liên hệ hỗ trợ', description: 'Liên hệ bộ phận hỗ trợ để được trợ giúp' }
    ];
  };

  if (!paymentData) {
    return (
      <ResidentLayout>
        <div style={{ padding: '50px', textAlign: 'center' }}>
          <Alert
            message="Đang tải thông tin..."
            type="info"
            showIcon
          />
        </div>
      </ResidentLayout>
    );
  }

  const solutionSteps = getSolutionSteps(paymentData.errorCode);

  return (
    <ResidentLayout>
      <div style={{ maxWidth: '800px', margin: '0 auto', padding: '20px' }}>
        {/* Header Failed */}
        <Result
          status="error"
          title="Thanh toán không thành công"
          subTitle={getErrorDescription(paymentData.errorCode, paymentData.failureReason)}
          icon={<PaymentFailedIcon size={72} style={{ color: '#ff4d4f' }} />}
        />

        {/* Thông tin chi tiết giao dịch */}
        <Card 
          title={
            <Space>
              <QuestionCircleOutlined style={{ color: '#faad14' }} />
              <span>Chi tiết giao dịch</span>
            </Space>
          }
          style={{ marginBottom: 24 }}
        >
          <Descriptions 
            column={{ xs: 1, sm: 2, md: 2 }}
            bordered
            size="middle"
          >
            <Descriptions.Item label="Mã giao dịch" span={2}>
              <Text code>{paymentData.transactionId}</Text>
            </Descriptions.Item>
            
            <Descriptions.Item label="Số tiền">
              <Text strong style={{ fontSize: '16px' }}>
                {formatCurrency(paymentData.amount)}
              </Text>
            </Descriptions.Item>
            
            <Descriptions.Item label="Phương thức thanh toán">
              <Tag color="default">{paymentData.paymentMethod}</Tag>
            </Descriptions.Item>
            
            <Descriptions.Item label="Loại dịch vụ">
              {paymentData.serviceType}
            </Descriptions.Item>
            
            <Descriptions.Item label="Căn hộ">
              <Text strong>{paymentData.apartmentNumber}</Text>
            </Descriptions.Item>
            
            <Descriptions.Item label="Thời gian thử" span={2}>
              {paymentData.attemptTime}
            </Descriptions.Item>
            
            <Descriptions.Item label="Lý do thất bại" span={2}>
              <Text type="danger">{getErrorDescription(paymentData.errorCode, paymentData.failureReason)}</Text>
            </Descriptions.Item>
            
            <Descriptions.Item label="Trạng thái" span={2}>
              <Tag color="error" icon={<CloseCircleOutlined />}>
                Thất bại
              </Tag>
            </Descriptions.Item>
          </Descriptions>
        </Card>

        {/* Hướng dẫn giải quyết */}
        <Card
          title={
            <Space>
              <CustomerServiceOutlined style={{ color: '#1890ff' }} />
              <span>Hướng dẫn giải quyết</span>
            </Space>
          }
          style={{ marginBottom: 24 }}
        >
          <Steps
            direction="vertical"
            size="small"
            current={-1}
          >
            {solutionSteps.map((step, index) => (
              <Step
                key={index}
                title={step.title}
                description={step.description}
                icon={<QuestionCircleOutlined />}
              />
            ))}
          </Steps>
        </Card>

        {/* Thông báo hỗ trợ */}
        <Alert
          message="Cần hỗ trợ?"
          description={
            <div>
              <p>• Nếu bạn gặp khó khăn, đội ngũ hỗ trợ luôn sẵn sàng giúp đỡ</p>
              <p>• Hãy cung cấp mã giao dịch khi liên hệ để được hỗ trợ nhanh chóng</p>
              <Space wrap style={{ marginTop: 8 }}>
                <Tag icon={<PhoneOutlined />} color="blue">Hotline: 1900-xxxx</Tag>
                <Tag icon={<MailOutlined />} color="green">Email: support@sams.com</Tag>
              </Space>
            </div>
          }
          type="warning"
          showIcon
          style={{ marginBottom: 24 }}
        />

        {/* Action buttons */}
        <Card>
          <Row gutter={[16, 16]} justify="center">
            <Col xs={24} sm={12} md={6}>
              <Button 
                type="primary" 
                danger
                icon={<ReloadOutlined />}
                size="large"
                block
                onClick={handleRetryPayment}
              >
                Thử lại
              </Button>
            </Col>
            
            <Col xs={24} sm={12} md={6}>
              <Button 
                type="primary"
                icon={<CustomerServiceOutlined />}
                size="large"
                block
                onClick={handleContactSupport}
              >
                Hỗ trợ
              </Button>
            </Col>
            
            <Col xs={24} sm={12} md={6}>
              <Button 
                icon={<HomeOutlined />}
                size="large"
                block
                onClick={handleGoHome}
              >
                Về trang chủ
              </Button>
            </Col>
            
            <Col xs={24} sm={12} md={6}>
              <Button 
                icon={<QuestionCircleOutlined />}
                size="large"
                block
                onClick={handleViewInvoices}
              >
                Xem hóa đơn
              </Button>
            </Col>
          </Row>
        </Card>

        <Divider />

        {/* Footer info */}
        <div style={{ textAlign: 'center', color: '#666' }}>
          <Paragraph type="secondary">
            Xin lỗi vì sự bất tiện này. Chúng tôi luôn cố gắng cải thiện trải nghiệm thanh toán.
          </Paragraph>
          <Text type="secondary" style={{ fontSize: '12px' }}>
            Mọi thắc mắc vui lòng liên hệ hotline: 1900-xxxx
          </Text>
        </div>
      </div>
    </ResidentLayout>
  );
};

export default PaymentCancel;