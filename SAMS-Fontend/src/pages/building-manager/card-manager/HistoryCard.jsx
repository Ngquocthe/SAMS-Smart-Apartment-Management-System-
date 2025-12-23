import React, { useState, useEffect, useCallback } from 'react';
import { Modal, Table, Tag, Spin, Alert, Row, Col, Card, Space, Typography, Empty } from 'antd';
import { ClockCircleOutlined } from '@ant-design/icons';
import { cardsApi } from '../../../features/building-management/cardsApi';

const { Title, Text } = Typography;

const HistoryCard = ({ show, onHide, card }) => {
  const [auditLogs, setAuditLogs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const fetchAuditLogs = useCallback(async () => {
    if (!card) return;
    
    setLoading(true);
    setError(null);
    
    try {
      const logs = await cardsApi.getCardAuditLogsByCardId(card.cardId);
      setAuditLogs(logs);
    } catch (err) {
      setError('Không thể tải lịch sử thay đổi');
    } finally {
      setLoading(false);
    }
  }, [card]);

  useEffect(() => {
    if (show && card) {
      fetchAuditLogs();
    }
  }, [show, card, fetchAuditLogs]);

  const getActionBadge = (eventCode, description) => {
    const actionMap = {
      'CARD_CREATED': { color: 'success', text: 'Tạo mới' },
      'CARD_UPDATED': { color: 'warning', text: 'Cập nhật' },
      'STATUS_CHANGE': { color: 'processing', text: 'Đổi trạng thái' },
      'CAPABILITY_CHANGE': { color: 'blue', text: 'Đổi chức năng' },
      'EXPIRY_CHANGE': { color: 'default', text: 'Đổi ngày hết hạn' },
      'ISSUED_DATE_CHANGE': { color: 'warning', text: 'Đổi ngày cấp' }
    };

    const descriptionMap = [
      { keywords: ['tạo mới thẻ', 'create new card'], color: 'success', text: 'Tạo mới thẻ' },
      { keywords: ['số thẻ', 'card number'], color: 'error', text: 'Đổi số thẻ' },
      { keywords: ['căn hộ', 'apartment'], color: 'orange', text: 'Đổi số căn hộ' },
      { keywords: ['chức năng thẻ', 'capability'], color: 'blue', text: 'Đổi chức năng' },
      { keywords: ['trạng thái', 'status'], color: 'processing', text: 'Đổi trạng thái' },
      { keywords: ['ngày hết hạn', 'expiry'], color: 'default', text: 'Đổi ngày hết hạn' },
      { keywords: ['ngày cấp', 'issued date', 'issued_date'], color: 'purple', text: 'Đổi ngày cấp' }
    ];

    // Check description first
    for (const { keywords, color, text } of descriptionMap) {
      if (keywords.some(keyword => description?.toLowerCase().includes(keyword))) {
        return <Tag color={color}>{text}</Tag>;
      }
    }

    // Fallback to eventCode
    const config = actionMap[eventCode] || { color: 'default', text: eventCode || 'N/A' };
    return <Tag color={config.color}>{config.text}</Tag>;
  };

  // Status mapping - shared across functions
  const STATUS_MAP = {
    'ACTIVE': 'Hoạt động',
    'INACTIVE': 'Không hoạt động',
    'EXPIRED': 'Hết hạn',
    'LOST': 'Mất thẻ',
    'BLOCKED': 'Bị khóa',
    'SUSPENDED': 'Tạm dừng',
    'PENDING': 'Chờ duyệt',
    'APPROVED': 'Đã duyệt',
    'REJECTED': 'Từ chối'
  };

  // Helper function to translate status values to Vietnamese
  const translateStatus = (status) => STATUS_MAP[status] || status;

  // Helper function to translate status values in description text
  const translateStatusInText = (text) => {
    if (!text) return text;
    
    let result = text;
    
    // Replace all status values in quotes
    Object.entries(STATUS_MAP).forEach(([english, vietnamese]) => {
      result = result.replace(new RegExp(`'${english}'`, 'g'), `'${vietnamese}'`);
    });
    
    // Replace "sang" with "->"
    result = result.replace(/ sang /g, ' -> ');
    
    return result;
  };

  const formatChangeDescription = (log) => {
    if (log.field_name && log.old_value && log.new_value) {
      const fieldMap = {
        'status': 'Trạng thái',
        'expired_date': 'Ngày hết hạn',
        'card_number': 'Số thẻ',
        'issued_date': 'Ngày cấp',
        'issued_to_user_id': 'Chủ sở hữu',
        'issued_to_apartment_id': 'Căn hộ'
      };
      
      const fieldName = fieldMap[log.field_name] || log.field_name;
      const isStatus = fieldName === 'Trạng thái';
      const isDate = fieldName === 'Ngày cấp' || fieldName === 'Ngày hết hạn';
      
      // Format dates nicely
      let oldValue = log.old_value;
      let newValue = log.new_value;
      
      if (isDate && oldValue) {
        try {
          const date = new Date(oldValue);
          oldValue = `${String(date.getDate()).padStart(2, '0')}/${String(date.getMonth() + 1).padStart(2, '0')}/${date.getFullYear()}`;
        } catch (e) {}
      }
      
      if (isDate && newValue) {
        try {
          const date = new Date(newValue);
          newValue = `${String(date.getDate()).padStart(2, '0')}/${String(date.getMonth() + 1).padStart(2, '0')}/${date.getFullYear()}`;
        } catch (e) {}
      }
      
      if (isStatus) {
        oldValue = translateStatus(oldValue);
        newValue = translateStatus(newValue);
      }
      
      return `${fieldName}: "${oldValue}" → "${newValue}"`;
    }
    
    // Process description
    let description = log.description || log.event_name || 'Thay đổi thông tin';
    ['. Lý do:', '. Reason:'].forEach(pattern => {
      if (description.includes(pattern)) {
        description = description.split(pattern)[0];
      }
    });
    
    return translateStatusInText(description);
  };

  const formatDateTime = (log) => {
    if (!log) return 'N/A';
    
    const timeToUse = log.eventTimeLocal || log.event_time_local || 
                      log.eventTimeUtc || log.event_time_utc || 
                      log.created_at;
    
    if (!timeToUse) return 'N/A';
    
    const date = new Date(timeToUse);
    date.setHours(date.getHours() - 7); // Adjust for Vietnam timezone
    
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const seconds = String(date.getSeconds()).padStart(2, '0');
    
    return `${day}/${month}/${year} ${hours}:${minutes}:${seconds}`;
  };

  // Ant Design Table columns configuration
  const columns = [
    {
      title: 'Thời gian',
      dataIndex: 'eventTime',
      key: 'eventTime',
      width: 150,
      render: (_, record) => (
        <Space direction="vertical" size={0}>
          <Text style={{ fontSize: '12px', color: '#666' }}>
            <ClockCircleOutlined style={{ marginRight: 4 }} />
            {formatDateTime(record)}
          </Text>
        </Space>
      )
    },
    {
      title: 'Hành động',
      dataIndex: 'action',
      key: 'action',
      width: 120,
      render: (_, record) => getActionBadge(record.event_code, record.description)
    },
    {
      title: 'Mô tả thay đổi',
      dataIndex: 'description',
      key: 'description',
      ellipsis: {
        showTitle: true,
      },
      render: (_, record) => (
        <Text 
          style={{ 
            whiteSpace: 'nowrap',
            display: 'block',
            fontSize: '13px',
            lineHeight: '1.5'
          }}
          title={formatChangeDescription(record)}
        >
          {formatChangeDescription(record)}
        </Text>
      )
    },
    {
      title: 'Người thực hiện',
      dataIndex: 'createdBy',
      key: 'createdBy',
      width: 150,
      render: (_, record) => (
        <Text style={{ fontSize: '12px' }}>
          {record.createdByName || record.created_by || 'building management'}
        </Text>
      )
    }
  ];

  const statsData = [
    { label: 'Tổng số thay đổi', value: auditLogs.length, color: '#1890ff' },
    { 
      label: 'Tạo mới', 
      value: auditLogs.filter(log => 
        log.event_code === 'CARD_CREATED' ||
        log.description?.toLowerCase().includes('tạo mới thẻ')
      ).length, 
      color: '#52c41a' 
    },
    { 
      label: 'Đổi căn hộ', 
      value: auditLogs.filter(log => 
        log.description?.toLowerCase().includes('căn hộ') ||
        log.description?.toLowerCase().includes('apartment')
      ).length, 
      color: '#fa8c16'
    },
    { 
      label: 'Đổi trạng thái', 
      value: auditLogs.filter(log => 
        log.event_code === 'STATUS_CHANGE' ||
        (log.description?.toLowerCase().includes('trạng thái') && 
         !log.description?.toLowerCase().includes('căn hộ'))
      ).length, 
      color: '#1890ff' 
    },
    { 
      label: 'Đổi chức năng', 
      value: auditLogs.filter(log => 
        log.event_code === 'CAPABILITY_CHANGE' ||
        log.description?.toLowerCase().includes('chức năng thẻ')
      ).length, 
      color: '#722ed1' 
    },
    { 
      label: 'Đổi ngày cấp', 
      value: auditLogs.filter(log => 
        log.event_code === 'ISSUED_DATE_CHANGE' ||
        (log.description?.toLowerCase().includes('ngày cấp') &&
         !log.description?.toLowerCase().includes('căn hộ'))
      ).length, 
      color: '#eb2f96' 
    },
    { 
      label: 'Đổi ngày hết hạn', 
      value: auditLogs.filter(log => 
        log.event_code === 'EXPIRY_CHANGE' ||
        (log.description?.toLowerCase().includes('ngày hết hạn') &&
         !log.description?.toLowerCase().includes('căn hộ'))
      ).length, 
      color: '#8c8c8c' 
    }
  ];

  return (
    <Modal
      title={
        <Space>
            <span>Lịch sử thay đổi - {card?.cardNumber}</span>
        </Space>
      }
      open={show}
      onCancel={onHide}
      width={1200}
      footer={null}
      style={{ top: 20 }}
    >
      {loading ? (
        <div style={{ textAlign: 'center', padding: '40px 0' }}>
          <Spin size="large" />
          <p style={{ marginTop: 16 }}>Đang tải lịch sử...</p>
        </div>
      ) : error ? (
        <Alert
          message="Lỗi"
          description={error}
          type="error"
          showIcon
          style={{ marginBottom: 16 }}
        />
      ) : auditLogs.length === 0 ? (
        <Empty 
          description="Chưa có lịch sử thay đổi nào cho thẻ này"
          image={Empty.PRESENTED_IMAGE_SIMPLE}
        />
      ) : (
        <div>
          {/* Thông tin thẻ */}
          <Card style={{ marginBottom: 16 }}>
            <div style={{ padding: '16px 0 0' }}>
              <Row gutter={[16, 8]}>
                <Col span={6}>
                  <Text><strong>Số thẻ:</strong> {card?.cardNumber}</Text>
                </Col>
                <Col span={6}>
                  <Text><strong>Trạng thái:</strong> {translateStatus(card?.status)}</Text>
                </Col>
                <Col span={6}>
                  <Text><strong>Ngày cấp:</strong> {card?.issuedDate ? new Date(card.issuedDate).toLocaleDateString('vi-VN') : 'N/A'}</Text>
                </Col>
                <Col span={6}>
                  <Text><strong>Ngày hết hạn:</strong> {card?.expiredDate ? new Date(card.expiredDate).toLocaleDateString('vi-VN') : 'N/A'}</Text>
                </Col>
              </Row>
            </div>
          </Card>

          {/* Bảng lịch sử */}
          <div style={{ marginBottom: 16 }}>
            <Table
              columns={columns}
              dataSource={auditLogs.map((log, index) => ({ ...log, key: index }))}
              pagination={{ 
                pageSize: 10,
                showSizeChanger: false,
                showQuickJumper: true,
                showTotal: (total) => `Tổng ${total} bản ghi`
              }}
              size="small"
              scroll={{ y: 300 }}
            />
          </div>

          {/* Thống kê */}
          <Card>
            <Card.Meta 
              title={
                <Title level={5} style={{ margin: 0 }}>
                  Thống kê
                </Title>
              }
            />
            <div style={{ padding: '16px 0 0' }}>
              <Row gutter={[16, 16]}>
                {statsData.map(({ label, value, color }, index) => (
                  <Col span={4} key={index}>
                    <div style={{ textAlign: 'center' }}>
                      <Title level={4} style={{ color, margin: 0 }}>{value}</Title>
                      <Text style={{ fontSize: '12px', color: '#666' }}>{label}</Text>
                    </div>
                  </Col>
                ))}
              </Row>
            </div>
          </Card>
        </div>
      )}
    </Modal>
  );
};

export default HistoryCard;
