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
  Alert
} from 'antd';
import { 
  HomeOutlined, 
  FileTextOutlined, 
  CheckCircleOutlined 
} from '@ant-design/icons';
import { PaymentSuccessIcon } from '../../icons';
import ResidentLayout from '../../components/resident/ResidentLayout';

const { Text } = Typography;

const PaymentSuccess = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [paymentData, setPaymentData] = useState(null);

  useEffect(() => {
    // Lấy thông tin thanh toán từ state
    const stateData = location.state;
    let paymentInfo;
    
    if (stateData?.paymentData && stateData?.bookingInfo) {
      const bookingInfo = stateData.bookingInfo;
      
      if (bookingInfo.serviceType === 'Hóa đơn dịch vụ') {
        // Dữ liệu từ invoice payment
        paymentInfo = {
          transactionId: stateData.paymentData.data?.orderCode || 'TXN' + Date.now(),
          amount: bookingInfo.amount || '0',
          paymentMethod: bookingInfo.paymentMethod || 'VNPay',
          serviceType: 'Hóa đơn dịch vụ',
          invoiceId: bookingInfo.invoiceId,
          apartmentNumber: bookingInfo.apartmentNumber,
          paymentTime: new Date().toLocaleString('vi-VN'),
          description: bookingInfo.description || 'Thanh toán hóa đơn',
          status: 'SUCCESS',
          message: stateData.message || 'Thanh toán thành công!'
        };
      } else {
        // Dữ liệu từ amenity booking
        paymentInfo = {
          transactionId: stateData.paymentData.data?.orderCode || 'TXN' + Date.now(),
          amount: bookingInfo.amount || '0',
          paymentMethod: 'QR Banking',
          serviceType: 'Đăng ký tiện ích',
          amenityName: bookingInfo.amenityName,
          bookingType: bookingInfo.bookingType,
          timeInfo: bookingInfo.timeInfo,
          paymentTime: new Date().toLocaleString('vi-VN'),
          description: bookingInfo.description || 'Thanh toán đăng ký tiện ích',
          status: 'SUCCESS',
          message: stateData.message || 'Thanh toán thành công!'
        };
      }
    } else {
      // Dữ liệu từ URL params (fallback)
      const urlParams = new URLSearchParams(location.search);
      paymentInfo = {
        transactionId: urlParams.get('transactionId') || 'TXN' + Date.now(),
        amount: urlParams.get('amount') || '0',
        paymentMethod: urlParams.get('paymentMethod') || 'VNPay',
        serviceType: urlParams.get('serviceType') || 'Hóa đơn dịch vụ',
        apartmentNumber: urlParams.get('apartmentNumber') || 'N/A',
        paymentTime: new Date().toLocaleString('vi-VN'),
        description: urlParams.get('description') || 'Thanh toán hóa đơn',
        status: 'SUCCESS'
      };
    }

    setPaymentData(paymentInfo);
  }, [location]);

  const handleGoHome = () => {
    navigate('/resident/amenity-booking');
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

  const getPaymentMethodColor = (method) => {
    const colorMap = {
      'VNPay': 'blue',
      'MoMo': 'magenta',
      'ZaloPay': 'cyan',
      'Banking': 'green',
      'Cash': 'orange'
    };
    return colorMap[method] || 'default';
  };

  if (!paymentData) {
    return (
      <div style={{ padding: '50px', textAlign: 'center' }}>
        <Alert
          message="Đang tải thông tin thanh toán..."
          type="info"
          showIcon
        />
      </div>
    );
  }

  return (
    <ResidentLayout>
      <div style={{ maxWidth: '800px', margin: '0 auto', padding: '20px' }}>
        {/* Header Success */}
        <Result className="gap-1"
          status="success"
          title="Thanh toán thành công!"
          subTitle={`Giao dịch ${paymentData.transactionId} đã được xử lý thành công. Cảm ơn bạn đã sử dụng dịch vụ.`}
          icon={
            <div className="flex justify-center">
              <PaymentSuccessIcon size={72} style={{ color: "#52c41a" }} />
            </div>
          }
          style={{ marginBottom: 24 }}
        />

        {/* Thông tin chi tiết thanh toán */}
        <Card 
          title={
            <Space>
              <FileTextOutlined style={{ color: '#1890ff' }} />
              <span>Chi tiết thanh toán</span>
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
              <Text code copyable>{paymentData.transactionId}</Text>
            </Descriptions.Item>
            
            <Descriptions.Item label="Số tiền">
              <Text strong style={{ color: '#52c41a', fontSize: '16px' }}>
                {formatCurrency(paymentData.amount)}
              </Text>
            </Descriptions.Item>
            
            <Descriptions.Item label="Phương thức thanh toán">
              <Tag color={getPaymentMethodColor(paymentData.paymentMethod)}>
                {paymentData.paymentMethod}
              </Tag>
            </Descriptions.Item>
            
            <Descriptions.Item label="Loại dịch vụ">
              {paymentData.serviceType}
            </Descriptions.Item>
            
            {paymentData.amenityName && (
              <Descriptions.Item label="Tiện ích">
                <Text strong style={{ color: '#1890ff' }}>{paymentData.amenityName}</Text>
              </Descriptions.Item>
            )}
            
            {paymentData.timeInfo && (
              <Descriptions.Item label="Thời gian sử dụng" span={2}>
                <Text strong>{paymentData.timeInfo}</Text>
              </Descriptions.Item>
            )}
            
            {paymentData.apartmentNumber && (
              <Descriptions.Item label="Căn hộ">
                <Text strong>{paymentData.apartmentNumber}</Text>
              </Descriptions.Item>
            )}
            
            {paymentData.invoiceId && (
              <Descriptions.Item label="Mã hóa đơn">
                <Text code strong>{paymentData.invoiceId}</Text>
              </Descriptions.Item>
            )}
            
            <Descriptions.Item label="Thời gian thanh toán" span={2}>
              {paymentData.paymentTime}
            </Descriptions.Item>
            
            <Descriptions.Item label="Mô tả" span={2}>
              {paymentData.description}
            </Descriptions.Item>
            
            <Descriptions.Item label="Trạng thái" span={2}>
              <Tag color="success" icon={<CheckCircleOutlined />}>
                Thành công
              </Tag>
            </Descriptions.Item>
          </Descriptions>
        </Card>

        {/* Thông báo quan trọng */}
        <Alert
          message="Thông báo quan trọng"
          description={
            <div>
              {/* <p>• Vui lòng lưu lại mã giao dịch để tra cứu sau này</p> */}
              <p>• Biên lai thanh toán đã được gửi về email của bạn</p>
              <p>• Nếu có thắc mắc, vui lòng liên hệ bộ phận hỗ trợ với mã giao dịch</p>
            </div>
          }
          type="info"
          showIcon
          style={{ marginBottom: 24 }}
        />

        {/* Action buttons */}
        <Card>
          <Row gutter={[16, 16]} justify="center">
            <Col xs={24} sm={12} md={8}>
              <Button 
                type="primary" 
                icon={<HomeOutlined />}
                size="large"
                block
                onClick={handleGoHome}
              >
                Về trang chủ
              </Button>
            </Col>
            
            <Col xs={24} sm={12} md={8}>
              <Button 
                icon={<FileTextOutlined />}
                size="large"
                block
                onClick={handleViewInvoices}
              >
                {paymentData?.serviceType === 'Đăng ký tiện ích' ? 'Xem đặt chỗ' : 'Xem hóa đơn'}
              </Button>
            </Col>
          </Row>
        </Card>

        <Divider />

        {/* Footer info */}
        <div style={{ textAlign: 'center', color: '#666' }}>
          <Text type="secondary">
            Cảm ơn bạn đã sử dụng hệ thống thanh toán SAMS
          </Text>
          <br />
          <Text type="secondary" style={{ fontSize: '12px' }}>
            Mọi thắc mắc vui lòng liên hệ hotline: 1900-xxxx
          </Text>
        </div>
      </div>
    </ResidentLayout>
  );
};

export default PaymentSuccess;