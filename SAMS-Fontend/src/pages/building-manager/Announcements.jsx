import React, { useState, useEffect } from 'react';
import {
  Layout,
  Card,
  Button,
  Modal,
  Form,
  Input,
  Select,
  Table,
  Space,
  Typography,
  Tag,
  Row,
  Col,
  Tooltip,
  Popconfirm,
  Badge,
  Empty,
  Flex,
  Checkbox,
  DatePicker
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  SearchOutlined
} from '@ant-design/icons';
import { announcementApi, AnnouncementStatus, AnnouncementScope, AnnouncementType } from '../../features/building-management/announcementApi';
import useNotification from '../../hooks/useNotification';
import dayjs from 'dayjs';

const { Title, Text } = Typography;
const { Content } = Layout;
const { TextArea } = Input;

export default function Announcements() {
  const [announcements, setAnnouncements] = useState([]);
  const [loading, setLoading] = useState(false);
  const [modalLoading, setModalLoading] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingAnnouncement, setEditingAnnouncement] = useState(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('ACTIVE');
  const [scopeFilter, setScopeFilter] = useState('ALL');
  const [typeFilter, setTypeFilter] = useState('ALL');

  // Form instances
  const [createForm] = Form.useForm();
  const [editForm] = Form.useForm();

  // Use custom notification hook (must be before useEffect)
  const { showNotification } = useNotification();

  useEffect(() => {
    fetchAnnouncements();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchAnnouncements = async (pageNumber = 1, pageSize = 10) => {
    setLoading(true);
    const baseParams = {
      pageNumber,
      pageSize
    };

    const normalizeAnnouncements = (response) => {
      if (!response) return [];
      if (Array.isArray(response)) return response;
      if (Array.isArray(response.announcements)) return response.announcements;
      if (Array.isArray(response.items)) return response.items;
      if (Array.isArray(response.data)) return response.data;
      if (response.results && Array.isArray(response.results)) return response.results;
      return [];
    };

    const filterGeneralAnnouncements = (list) => {
      const excludedTypes = new Set([
        'MAINTENANCE_REMINDER',
        'MAINTENANCE_ASSIGNMENT',
        'MAINTENANCE_COMPLETED',
        'AMENITY_BOOKING_SUCCESS',
        'AMENITY_BOOKING_CONFLICT',
        'AMENITY_EXPIRATION_REMINDER',
        'AMENITY_EXPIRED',
        'AMENITY_MAINTENANCE_REMINDER',
        'ASSET_MAINTENANCE_NOTICE'
      ]);
      return list
        .filter(item => {
          const type = item.type || item.announcementType;
          return !excludedTypes.has(type);
        })
        .map(item => ({
          ...item,
          announcementId: item.announcementId || item.id,
          type: item.type || item.announcementType,
          visibilityScope: item.visibilityScope || item.scope,
          status: item.status || item.currentStatus
        }));
    };

    try {
      let response = null;

      try {
        response = await announcementApi.getAll({
          ...baseParams,
          excludeTypes: 'MAINTENANCE_REMINDER,MAINTENANCE_ASSIGNMENT,MAINTENANCE_COMPLETED,AMENITY_BOOKING_SUCCESS,AMENITY_BOOKING_CONFLICT,AMENITY_EXPIRATION_REMINDER,AMENITY_EXPIRED,AMENITY_MAINTENANCE_REMINDER,ASSET_MAINTENANCE_NOTICE'
        });
      } catch (errWithExclude) {
        response = await announcementApi.getAll(baseParams);
      }

      const rawAnnouncements = normalizeAnnouncements(response);
      const generalAnnouncements = filterGeneralAnnouncements(rawAnnouncements);
      setAnnouncements(generalAnnouncements);
    } catch (error) {
      showNotification(
        'error',
        'Lỗi',
        'Không thể tải danh sách thông báo: ' + (error.response?.data?.message || error.message)
      );
      setAnnouncements([]);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateAnnouncement = async (values) => {
    setModalLoading(true);
    try {
      // Đảm bảo tất cả field required có giá trị hợp lệ
      if (!values.title || !values.content) {
        showNotification('error', 'Lỗi', 'Tiêu đề và nội dung không được để trống');
        setModalLoading(false);
        return;
      }

      const createDto = {
        title: values.title.trim(),
        content: values.content.trim(),
        visibleFrom: dayjs(values.visibleFrom).format('YYYY-MM-DDTHH:mm:ss'),
        visibleTo: values.visibleTo ? dayjs(values.visibleTo).format('YYYY-MM-DDTHH:mm:ss') : null,
        visibilityScope: values.visibilityScope || AnnouncementScope.ALL,
        status: values.status || AnnouncementStatus.ACTIVE,
        isPinned: values.isPinned || false,
        type: values.type || AnnouncementType.ANNOUNCEMENT
      };
      
      await announcementApi.create(createDto);
      showNotification('success', 'Thành công', 'Tạo tin tức thành công');
      setShowCreateModal(false);
      createForm.resetFields();
      fetchAnnouncements();
    } catch (error) {
      showNotification('error', 'Lỗi', 'Lỗi khi tạo thông báo: ' + (error.response?.data?.message || error.message));
    } finally {
      setModalLoading(false);
    }
  };

  const handleUpdateAnnouncement = async (values) => {
    setModalLoading(true);
    try {
      // Đảm bảo tất cả field required có giá trị hợp lệ
      if (!values.title || !values.content) {
        showNotification('error', 'Lỗi', 'Tiêu đề và nội dung không được để trống');
        setModalLoading(false);
        return;
      }

      const updateDto = {
        title: values.title.trim(),
        content: values.content.trim(),
        visibleFrom: values.visibleFrom ? dayjs(values.visibleFrom).format('YYYY-MM-DDTHH:mm:ss') : dayjs().format('YYYY-MM-DDTHH:mm:ss'),
        visibleTo: values.visibleTo ? dayjs(values.visibleTo).format('YYYY-MM-DDTHH:mm:ss') : null,
        visibilityScope: values.visibilityScope || AnnouncementScope.ALL,
        status: values.status || AnnouncementStatus.ACTIVE,
        isPinned: values.isPinned || false,
        type: values.type || AnnouncementType.ANNOUNCEMENT
      };
      
      await announcementApi.update(editingAnnouncement.announcementId, updateDto);
      showNotification('success', 'Thành công', 'Cập nhật tin tức thành công');
      setShowEditModal(false);
      setEditingAnnouncement(null);
      editForm.resetFields();
      fetchAnnouncements();
    } catch (error) {
      let errorMessage = 'Lỗi khi cập nhật thông báo';
      if (error.response?.data?.errors) {
        const errors = error.response.data.errors;
        const errorMessages = [];
        
        if (errors.VisibleFrom) {
          errorMessages.push('Ngày hiển thị từ: ' + errors.VisibleFrom.join(', '));
        }
        if (errors.VisibleTo) {
          errorMessages.push('Ngày hiển thị đến: ' + errors.VisibleTo.join(', '));
        }
        if (errors.Title) {
          errorMessages.push('Tiêu đề: ' + errors.Title.join(', '));
        }
        if (errors.Content) {
          errorMessages.push('Nội dung: ' + errors.Content.join(', '));
        }
        
        if (errorMessages.length > 0) {
          errorMessage = errorMessages.join('; ');
        }
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      } else {
        errorMessage = error.message;
      }
      
      showNotification('error', 'Lỗi', errorMessage);
    } finally {
      setModalLoading(false);
    }
  };

  const handleDeleteAnnouncement = async (announcementId) => {
    try {
      await announcementApi.delete(announcementId);
      showNotification('success', 'Thành công', 'Ẩn tin tức thành công');
      fetchAnnouncements();
    } catch (error) {
      showNotification('error', 'Lỗi', 'Lỗi khi ẩn thông báo: ' + (error.response?.data?.message || error.message));
    }
  };

  const openEditModal = (announcement) => {
    setEditingAnnouncement(announcement);
    editForm.setFieldsValue({
      title: announcement.title,
      content: announcement.content,
      visibleFrom: dayjs(announcement.visibleFrom),
      visibleTo: announcement.visibleTo ? dayjs(announcement.visibleTo) : null,
      visibilityScope: announcement.visibilityScope || 'ALL',
      status: announcement.status || 'ACTIVE',
      type: announcement.type || AnnouncementType.ANNOUNCEMENT,
      isPinned: announcement.isPinned || false
    });
    setShowEditModal(true);
  };

  // Filter announcements based on search and filters
  const filteredAnnouncements = announcements.filter(announcement => {
    if (['ASSET_MAINTENANCE_NOTICE', 'AMENITY_MAINTENANCE_REMINDER', 'AMENITY_BOOKING_SUCCESS', 'AMENITY_BOOKING_CONFLICT', 'AMENITY_EXPIRATION_REMINDER', 'AMENITY_EXPIRED'].includes(announcement.type)) {
      return false;
    }

    const matchSearch = announcement.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
                       announcement.content.toLowerCase().includes(searchTerm.toLowerCase());
    const matchStatus = statusFilter === 'ALL' || announcement.status === statusFilter;
    const matchScope = scopeFilter === 'ALL' || announcement.visibilityScope === scopeFilter;
    const matchType = typeFilter === 'ALL' || announcement.type === typeFilter;

    return matchSearch && matchStatus && matchScope && matchType;
  });

  const getStatusColor = (status) => {
    switch (status) {
      case 'ACTIVE':
        return 'green';
      case 'INACTIVE':
        return 'red';
      case 'EXPIRED':
        return 'orange';
      case 'SCHEDULED':
        return 'blue';
      default:
        return 'default';
    }
  };

  const getStatusLabel = (status) => {
    switch (status) {
      case 'ACTIVE':
        return 'Hoạt động';
      case 'INACTIVE':
        return 'Không hoạt động';
      case 'EXPIRED':
        return 'Hết hạn';
      case 'SCHEDULED':
        return 'Đã set lịch';
      default:
        return status;
    }
  };

  const getScopeLabel = (scope) => {
    switch (scope) {
      case 'ALL':
        return 'Tất cả';
      case 'RESIDENTS':
        return 'Cư dân';
      case 'STAFF':
        return 'Nhân viên';
      case 'ADMIN':
        return 'Quản trị viên';
      default:
        return scope;
    }
  };

  const columns = [
    {
      title: 'Tiêu đề',
      dataIndex: 'title',
      key: 'title',
      width: 150,
      render: (text, record) => (
        <Tooltip title={record.content} color="white">
          <Text ellipsis strong style={{ maxWidth: '150px', display: 'block' }}>{text}</Text>
        </Tooltip>
      )
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (status) => (
        <Tag color={getStatusColor(status)}>
          {getStatusLabel(status)}
        </Tag>
      )
    },
    {
      title: 'Phạm vi',
      dataIndex: 'visibilityScope',
      key: 'visibilityScope',
      width: 80,
      render: (scope) => (
        <Tag color="blue" style={{ fontSize: '12px' }}>
          {getScopeLabel(scope)}
        </Tag>
      )
    },
    {
      title: 'Từ ngày',
      dataIndex: 'visibleFrom',
      key: 'visibleFrom',
      width: 130,
      render: (date) => dayjs(date).format('DD/MM HH:mm')
    },
    {
      title: 'Đến ngày',
      dataIndex: 'visibleTo',
      key: 'visibleTo',
      width: 130,
      render: (date) => date ? dayjs(date).format('DD/MM HH:mm') : <Text type="secondary">-</Text>
    },
    {
      title: 'Loại',
      dataIndex: 'type',
      key: 'type',
      width: 110,
      render: (type) => {
        const typeColors = {
          [AnnouncementType.ANNOUNCEMENT]: 'blue',
          [AnnouncementType.EVENT]: 'purple',
          'MAINTENANCE_REMINDER': 'orange',
          'MAINTENANCE_ASSIGNMENT': 'orange'
        };
        const typeLabels = {
          [AnnouncementType.ANNOUNCEMENT]: 'Thông báo',
          [AnnouncementType.EVENT]: 'Sự kiện',
          'MAINTENANCE_REMINDER': 'Nhắc nhở bảo trì',
          'MAINTENANCE_ASSIGNMENT': 'Gán nhiệm vụ bảo trì'
        };
        return (
          <Tag color={typeColors[type] || 'default'}>
            {typeLabels[type] || type}
          </Tag>
        );
      }
    },
    {
      title: 'Ghim',
      dataIndex: 'isPinned',
      key: 'isPinned',
      width: 60,
      align: 'center',
      render: (isPinned) => isPinned ? <Badge status="processing" /> : null
    },
    {
      title: 'Thao tác',
      key: 'action',
      width: 80,
      fixed: 'right',
      render: (_, record) => (
        <Space size="small">
          <Tooltip title="Chỉnh sửa">
            <Button
              type="text"
              icon={<EditOutlined />}
              onClick={() => openEditModal(record)}
              size="small"
            />
          </Tooltip>
          <Popconfirm
            title="Ẩn thông báo"
            description="Bạn có chắc muốn ẩn tin tức này không?"
            onConfirm={() => handleDeleteAnnouncement(record.announcementId)}
            okText="Có"
            cancelText="Không"
          >
            <Tooltip title="Ẩn">
              <Button
                type="text"
                danger
                icon={<DeleteOutlined />}
                size="small"
              />
            </Tooltip>
          </Popconfirm>
        </Space>
      )
    }
  ];

  return (
    <Layout style={{ minHeight: '100vh', background: '#f0f2f5' }}>
      <Content style={{ padding: '16px' }}>
          {/* Header */}
          <div style={{ marginBottom: 16 }}>
            <Flex justify="space-between" align="center" wrap="wrap" gap="small">
              <div>
                <Title level={3} style={{ margin: 0 }}>
                  Quản lý tin tức
                </Title>
              </div>
              <Space>
                <Button
                  onClick={() => fetchAnnouncements()}
                  loading={loading}
                  size="middle"
                >
                  Làm mới
                </Button>
                <Button
                  type="primary"
                  onClick={() => setShowCreateModal(true)}
                  size="middle"
                >
                  Tạo tin tức mới
                </Button>
              </Space>
            </Flex>
          </div>

          {/* Search & Filters */}
          <Card
            style={{ marginBottom: 16 }}
            bodyStyle={{ padding: '12px 16px' }}
          >
          <Row gutter={[12, 12]} align="middle">
              <Col xs={24} sm={12} md={6}>
                <Text strong style={{ display: 'block', marginBottom: 6 }}>Tìm kiếm</Text>
                <Input
                  placeholder="Tìm kiếm tiêu đề..."
                  prefix={<SearchOutlined />}
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  allowClear
                  size="small"
                />
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Text strong style={{ display: 'block', marginBottom: 6 }}>Trạng thái</Text>
                <Select
                  placeholder="Trạng thái"
                  value={statusFilter}
                  onChange={setStatusFilter}
                  style={{ width: '100%' }}
                  size="small"
                >
                  <Select.Option value="ALL">Tất cả</Select.Option>
                  <Select.Option value="ACTIVE">Hoạt động</Select.Option>
                  <Select.Option value="SCHEDULED">Đã set lịch</Select.Option>
                  <Select.Option value="INACTIVE">Không hoạt động</Select.Option>
                  <Select.Option value="EXPIRED">Hết hạn</Select.Option>
                </Select>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Text strong style={{ display: 'block', marginBottom: 6 }}>Phạm vi</Text>
                <Select
                  placeholder="Phạm vi"
                  value={scopeFilter}
                  onChange={setScopeFilter}
                  style={{ width: '100%' }}
                  size="small"
                >
                  <Select.Option value="ALL">Tất cả</Select.Option>
                  <Select.Option value="RESIDENTS">Cư dân</Select.Option>
                  <Select.Option value="STAFF">Nhân viên</Select.Option>
                  <Select.Option value="ADMIN">Quản trị viên</Select.Option>
                </Select>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Text strong style={{ display: 'block', marginBottom: 6 }}>Danh mục</Text>
                <Select
                  placeholder="Loại"
                  value={typeFilter}
                  onChange={setTypeFilter}
                  style={{ width: '100%' }}
                  size="small"
                >
                  <Select.Option value="ALL">Tất cả</Select.Option>
                  <Select.Option value={AnnouncementType.ANNOUNCEMENT}>Thông báo</Select.Option>
                  <Select.Option value={AnnouncementType.EVENT}>Sự kiện</Select.Option>
                </Select>
              </Col>
            </Row>
          </Card>

          {/* Announcements Table */}
          <Card
            title={
              <Flex align="center" gap="small">
                <span>Danh sách tin tức</span>
                <Badge count={filteredAnnouncements.length} showZero />
              </Flex>
            }
            bodyStyle={{ padding: 0 }}
          >
            <Table
              columns={columns}
              dataSource={filteredAnnouncements}
              rowKey={(record, index) => record.announcementId || `announcement-${index}`}
              loading={loading}
              size="small"
              pagination={{
                pageSize: 8,
                total: filteredAnnouncements.length,
                showSizeChanger: false,
                showQuickJumper: false,
                showTotal: (total, range) =>
                  `${range[0]}-${range[1]} của ${total}`,
              }}
              locale={{
                emptyText: (
                  <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description="Chưa có tin tức nào"
                  />
                )
              }}
              scroll={{ x: 'max-content' }}
            />
          </Card>

          {/* Create Announcement Modal */}
          <Modal
            title={
              <Flex align="center" gap="small">
                <PlusOutlined />
                <span>Tạo tin tức mới</span>
              </Flex>
            }
            open={showCreateModal}
            onCancel={() => {
              setShowCreateModal(false);
              createForm.resetFields();
            }}
            footer={null}
            width={700}
          >
            <Form
              form={createForm}
              layout="vertical"
              onFinish={handleCreateAnnouncement}
            >
              <Form.Item
                label="Tiêu đề"
                name="title"
                rules={[
                  { required: true, message: 'Tiêu đề là bắt buộc' },
                  { 
                    min: 5, 
                    message: 'Tiêu đề phải từ 5 đến 100 ký tự' 
                  },
                  { 
                    max: 100, 
                    message: 'Tiêu đề phải từ 5 đến 100 ký tự' 
                  },
                  {
                    whitespace: true,
                    message: 'Tiêu đề không được chỉ chứa khoảng trắng'
                  }
                ]}
                hasFeedback
              >
                <Input 
                  placeholder="Nhập tiêu đề tin tức (5-100 ký tự)" 
                  maxLength={100}
                  showCount
                />
              </Form.Item>

              <Form.Item
                label="Nội dung"
                name="content"
                rules={[
                  { required: true, message: 'Nội dung là bắt buộc' },
                  { 
                    min: 10, 
                    message: 'Nội dung phải từ 10 đến 5000 ký tự' 
                  },
                  { 
                    max: 5000, 
                    message: 'Nội dung phải từ 10 đến 5000 ký tự' 
                  },
                  {
                    whitespace: true,
                    message: 'Nội dung không được chỉ chứa khoảng trắng'
                  }
                ]}
                hasFeedback
              >
                <TextArea
                  placeholder="Nhập nội dung tin tức (10-5000 ký tự)"
                  rows={4}
                  maxLength={5000}
                  showCount
                />
              </Form.Item>

              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item
                    label="Hiển thị từ"
                    name="visibleFrom"
                    rules={[
                      { 
                        required: true, 
                        message: 'Ngày hiển thị từ là bắt buộc' 
                      },
                      {
                        validator: (_, value) => {
                          if (!value) return Promise.resolve();
                          const today = dayjs().startOf('day');
                          if (dayjs(value).isBefore(today)) {
                            return Promise.reject(new Error('Ngày hiển thị từ phải là hôm nay hoặc trong tương lai'));
                          }
                          return Promise.resolve();
                        }
                      }
                    ]}
                    hasFeedback
                  >
                    <DatePicker
                      showTime
                      format="DD/MM/YYYY HH:mm"
                      placeholder="Chọn ngày giờ"
                      style={{ width: '100%' }}
                      disabledDate={(current) => {
                        return current && current < dayjs().startOf('day');
                      }}
                    />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label="Hiển thị đến (tuỳ chọn)"
                    name="visibleTo"
                    dependencies={['visibleFrom']}
                    rules={[
                      {
                        validator: (_, value) => {
                          if (!value) return Promise.resolve();
                          
                          const today = dayjs().startOf('day');
                          if (dayjs(value).isBefore(today)) {
                            return Promise.reject(new Error('Ngày hiển thị đến phải là hôm nay hoặc trong tương lai'));
                          }
                          
                          const visibleFrom = createForm.getFieldValue('visibleFrom');
                          if (visibleFrom && dayjs(value).isBefore(dayjs(visibleFrom))) {
                            return Promise.reject(new Error('Ngày hiển thị đến phải bằng hoặc sau ngày hiển thị từ'));
                          }
                          
                          return Promise.resolve();
                        }
                      }
                    ]}
                    hasFeedback
                  >
                    <DatePicker
                      showTime
                      format="DD/MM/YYYY HH:mm"
                      placeholder="Chọn ngày giờ"
                      style={{ width: '100%' }}
                      disabledDate={(current) => {
                        const visibleFrom = createForm.getFieldValue('visibleFrom');
                        if (visibleFrom) {
                          return current && current < dayjs(visibleFrom).startOf('day');
                        }
                        return current && current < dayjs().startOf('day');
                      }}
                    />
                  </Form.Item>
                </Col>
              </Row>

              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item
                    label="Phạm vi hiển thị"
                    name="visibilityScope"
                    initialValue="ALL"
                  >
                    <Select>
                      <Select.Option value="ALL">Tất cả</Select.Option>
                      <Select.Option value="RESIDENTS">Cư dân</Select.Option>
                      <Select.Option value="STAFF">Nhân viên</Select.Option>
                      <Select.Option value="ADMIN">Quản trị viên</Select.Option>
                    </Select>
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label="Loại thông báo"
                    name="type"
                    initialValue={AnnouncementType.ANNOUNCEMENT}
                  >
                    <Select>
                      <Select.Option value={AnnouncementType.ANNOUNCEMENT}>Thông báo</Select.Option>
                      <Select.Option value={AnnouncementType.EVENT}>Sự kiện</Select.Option>
                    </Select>
                  </Form.Item>
                </Col>
              </Row>

              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item
                    label="Trạng thái"
                    name="status"
                    initialValue="ACTIVE"
                  >
                    <Select>
                      <Select.Option value="ACTIVE">Hoạt động</Select.Option>
                      <Select.Option value="SCHEDULED">Đã set lịch</Select.Option>
                      <Select.Option value="INACTIVE">Không hoạt động</Select.Option>
                      <Select.Option value="EXPIRED">Hết hạn</Select.Option>
                    </Select>
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    name="isPinned"
                    valuePropName="checked"
                    initialValue={false}
                  >
                    <Checkbox>Ghim thông báo</Checkbox>
                  </Form.Item>
                </Col>
              </Row>

              <Form.Item style={{ marginBottom: 0, textAlign: 'right' }}>
                <Space>
                  <Button onClick={() => setShowCreateModal(false)}>
                    Hủy
                  </Button>
                  <Button type="primary" htmlType="submit" loading={modalLoading}>
                    Tạo thông báo
                  </Button>
                </Space>
              </Form.Item>
            </Form>
          </Modal>

          {/* Edit Announcement Modal */}
          <Modal
            title={
              <Flex align="center" gap="small">
                <EditOutlined />
                <span>Chỉnh sửa thông báo</span>
              </Flex>
            }
            open={showEditModal}
            onCancel={() => {
              setShowEditModal(false);
              setEditingAnnouncement(null);
              editForm.resetFields();
            }}
            footer={null}
            width={700}
          >
            <Form
              form={editForm}
              layout="vertical"
              onFinish={handleUpdateAnnouncement}
            >
              <Form.Item
                label="Tiêu đề"
                name="title"
                rules={[
                  { required: true, message: 'Tiêu đề là bắt buộc' },
                  { 
                    min: 5, 
                    message: 'Tiêu đề phải từ 5 đến 100 ký tự' 
                  },
                  { 
                    max: 100, 
                    message: 'Tiêu đề phải từ 5 đến 100 ký tự' 
                  },
                  {
                    whitespace: true,
                    message: 'Tiêu đề không được chỉ chứa khoảng trắng'
                  }
                ]}
                hasFeedback
              >
                <Input 
                  placeholder="Nhập tiêu đề tin tức (5-100 ký tự)" 
                  maxLength={100}
                  showCount
                />
              </Form.Item>

              <Form.Item
                label="Nội dung"
                name="content"
                rules={[
                  { required: true, message: 'Nội dung là bắt buộc' },
                  { 
                    min: 10, 
                    message: 'Nội dung phải từ 10 đến 5000 ký tự' 
                  },
                  { 
                    max: 5000, 
                    message: 'Nội dung phải từ 10 đến 5000 ký tự' 
                  },
                  {
                    whitespace: true,
                    message: 'Nội dung không được chỉ chứa khoảng trắng'
                  }
                ]}
                hasFeedback
              >
                <TextArea
                  placeholder="Nhập nội dung tin tức (10-5000 ký tự)"
                  rows={4}
                  maxLength={5000}
                  showCount
                />
              </Form.Item>

              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item
                    label="Hiển thị từ"
                    name="visibleFrom"
                    rules={[
                      { 
                        required: true, 
                        message: 'Ngày hiển thị từ là bắt buộc' 
                      }
                    ]}
                    hasFeedback
                  >
                    <DatePicker
                      showTime
                      format="DD/MM/YYYY HH:mm"
                      placeholder="Chọn ngày giờ"
                      style={{ width: '100%' }}

                    />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label="Hiển thị đến (tuỳ chọn)"
                    name="visibleTo"
                    dependencies={['visibleFrom']}
                    rules={[
                      {
                        validator: (_, value) => {
                          if (!value) return Promise.resolve();
                          
                          const visibleFrom = editForm.getFieldValue('visibleFrom');
                          if (visibleFrom && dayjs(value).isBefore(dayjs(visibleFrom))) {
                            return Promise.reject(new Error('Ngày hiển thị đến phải sau ngày hiển thị từ'));
                          }
                          
                          return Promise.resolve();
                        }
                      }
                    ]}
                    hasFeedback
                  >
                    <DatePicker
                      showTime
                      format="DD/MM/YYYY HH:mm"
                      placeholder="Chọn ngày giờ"
                      style={{ width: '100%' }}
                      disabledDate={(current) => {
                        const visibleFrom = editForm.getFieldValue('visibleFrom');
                        if (visibleFrom) {
                          return current && current < dayjs(visibleFrom).startOf('day');
                        }
                        return false;
                      }}
                    />
                  </Form.Item>
                </Col>
              </Row>

              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item
                    label="Phạm vi hiển thị"
                    name="visibilityScope"
                  >
                    <Select>
                      <Select.Option value="ALL">Tất cả</Select.Option>
                      <Select.Option value="RESIDENTS">Cư dân</Select.Option>
                      <Select.Option value="STAFF">Nhân viên</Select.Option>
                      <Select.Option value="ADMIN">Quản trị viên</Select.Option>
                    </Select>
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label="Loại thông báo"
                    name="type"
                  >
                    <Select>
                      <Select.Option value={AnnouncementType.ANNOUNCEMENT}>Thông báo</Select.Option>
                      <Select.Option value={AnnouncementType.EVENT}>Sự kiện</Select.Option>
                    </Select>
                  </Form.Item>
                </Col>
              </Row>

              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item
                    label="Trạng thái"
                    name="status"
                  >
                    <Select>
                      <Select.Option value="ACTIVE">Hoạt động</Select.Option>
                      <Select.Option value="SCHEDULED">Đã set lịch</Select.Option>
                      <Select.Option value="INACTIVE">Không hoạt động</Select.Option>
                      <Select.Option value="EXPIRED">Hết hạn</Select.Option>
                    </Select>
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    name="isPinned"
                    valuePropName="checked"
                  >
                    <Checkbox>Ghim thông báo</Checkbox>
                  </Form.Item>
                </Col>
              </Row>

              <Form.Item style={{ marginBottom: 0, textAlign: 'right' }}>
                <Space>
                  <Button onClick={() => {
                    setShowEditModal(false);
                    setEditingAnnouncement(null);
                    editForm.resetFields();
                  }}>
                    Hủy
                  </Button>
                  <Button type="primary" htmlType="submit" loading={modalLoading}>
                    Cập nhật
                  </Button>
                </Space>
              </Form.Item>
            </Form>
          </Modal>
        </Content>
      </Layout>
  );
}
