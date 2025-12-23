import React, { useState, useEffect, useMemo, useRef } from 'react';
import { 
  Layout, 
  Card, 
  Button, 
  Input,
  Table, 
  Space, 
  Typography, 
  Tag, 
  Row, 
  Col,
  Tooltip,
  Popconfirm,
  Empty,
  Flex,
  App,
  Select,
  DatePicker,
  Spin,
  Alert
} from 'antd';
import { 
  EditOutlined, 
  DeleteOutlined, 
  ReloadOutlined,
  SearchOutlined,
  CalendarOutlined
} from '@ant-design/icons';
import { assetsApi } from '../../../features/building-management/assetsApi';
import CreateMaintenanceSchedule from './CreateMaintenanceSchedule';
import UpdateMaintenanceSchedule from './UpdateMaintenanceSchedule';
import useNotification from '../../../hooks/useNotification';
import dayjs from 'dayjs';

const { Title, Text } = Typography;
const { Content } = Layout;
const { Option } = Select;
const { RangePicker } = DatePicker;

export default function MaintenanceScheduleListPage() {
  const { showNotification } = useNotification();
  
  const [schedules, setSchedules] = useState([]);
  const [assets, setAssets] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [dateRange, setDateRange] = useState(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showUpdateModal, setShowUpdateModal] = useState(false);
  const [selectedSchedule, setSelectedSchedule] = useState(null);
  
  const intervalRef = useRef(null);

  useEffect(() => {
    fetchData();
    
    // Refresh định kỳ mỗi 15 phút để nhận trạng thái mới từ backend
    intervalRef.current = setInterval(() => {
      fetchData();
    }, 15 * 60 * 1000); // 15 phút

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, []);

  // Refresh khi user quay lại màn hình (tab được focus)
  useEffect(() => {
    const handleVisibilityChange = () => {
      if (!document.hidden) {
        // User quay lại màn hình, refresh dữ liệu
        fetchData();
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [schedulesData, assetsData, categoriesData] = await Promise.all([
        assetsApi.getMaintenanceSchedules(),
        assetsApi.getAll(),
        assetsApi.getCategories().catch(() => [])
      ]);
      
      setSchedules(Array.isArray(schedulesData) ? schedulesData : []);
      setAssets(Array.isArray(assetsData) ? assetsData : []);
      setCategories(Array.isArray(categoriesData) ? categoriesData : []);
      setError('');
    } catch (err) {
      setError('Không thể tải danh sách lịch bảo trì. Vui lòng thử lại sau.');
      console.error('Error fetching data:', err);
    } finally {
      setLoading(false);
    }
  };

  const normalizedCategories = useMemo(() => {
    if (categories.length > 0) {
      return categories.map(cat => ({
        categoryId: cat.categoryId,
        categoryName: cat.name || cat.categoryName,
        code: cat.code
      }));
    }
    
    const categoryMap = new Map();
    assets.forEach(asset => {
      const catId = asset.categoryId || asset.assetCategory?.categoryId;
      const catName = asset.assetCategory?.name || asset.categoryName;
      if (catId && catName) {
        categoryMap.set(catId, { categoryId: catId, categoryName: catName });
      }
    });
    return Array.from(categoryMap.values());
  }, [assets, categories]);

  const filteredSchedules = useMemo(() => {
    return schedules.filter(schedule => {
      if (schedule.status === 'DONE') {
        return false;
      }
      
      let matchesSearch = true;
      if (searchTerm) {
        let assetName = '';
        if (schedule.asset?.assetName) {
          assetName = schedule.asset.assetName;
        } else {
          const asset = assets.find(a => 
            a.assetId === schedule.assetId || 
            String(a.assetId) === String(schedule.assetId)
          );
          assetName = asset?.assetName || asset?.name || '';
        }
        matchesSearch = assetName.toLowerCase().includes(searchTerm.toLowerCase());
      }
      
      let matchesCategory = true;
      if (categoryFilter !== 'all') {
        const scheduleAsset = assets.find(a => 
          a.assetId === schedule.assetId || 
          String(a.assetId) === String(schedule.assetId)
        );
        matchesCategory = scheduleAsset && (
          scheduleAsset.categoryId === categoryFilter ||
          scheduleAsset.assetCategory?.categoryId === categoryFilter
        );
      }
      
      const matchesStatus = statusFilter === 'all' || schedule.status === statusFilter;
      
      let matchesDateRange = true;
      if (dateRange && dateRange[0] && dateRange[1]) {
        const rangeStartDate = dayjs(dateRange[0]).startOf('day');
        const rangeEndDate = dayjs(dateRange[1]).endOf('day');
        const scheduleStartDate = schedule.startDate ? dayjs(schedule.startDate).startOf('day') : null;
        const scheduleEndDate = schedule.endDate ? dayjs(schedule.endDate).startOf('day') : null;
        
        if (scheduleStartDate && scheduleEndDate) {
          matchesDateRange = (scheduleStartDate.isBefore(rangeEndDate) || scheduleStartDate.isSame(rangeEndDate)) && 
                            (scheduleEndDate.isAfter(rangeStartDate) || scheduleEndDate.isSame(rangeStartDate));
        } else if (scheduleStartDate) {
          matchesDateRange = (scheduleStartDate.isAfter(rangeStartDate) || scheduleStartDate.isSame(rangeStartDate)) && 
                            (scheduleStartDate.isBefore(rangeEndDate) || scheduleStartDate.isSame(rangeEndDate));
        } else if (scheduleEndDate) {
          matchesDateRange = (scheduleEndDate.isAfter(rangeStartDate) || scheduleEndDate.isSame(rangeStartDate)) && 
                            (scheduleEndDate.isBefore(rangeEndDate) || scheduleEndDate.isSame(rangeEndDate));
        } else {
          matchesDateRange = false;
        }
      }
      
      return matchesSearch && matchesCategory && matchesStatus && matchesDateRange;
    });
  }, [schedules, searchTerm, categoryFilter, statusFilter, dateRange, assets]);

  const getStatusBadge = (status) => {
    const config = {
      'SCHEDULED': { color: 'blue', label: 'Đã lên lịch' },
      'IN_PROGRESS': { color: 'orange', label: 'Đang thực hiện' },
      'DONE': { color: 'green', label: 'Hoàn thành' },
      'CANCELLED': { color: 'red', label: 'Đã hủy' }
    };
    const statusConfig = config[status] || config.SCHEDULED;
    return <Tag color={statusConfig.color}>{statusConfig.label}</Tag>;
  };

  const formatDateWithTime = (dateString, timeString) => {
    if (!dateString) return 'N/A';
    const date = dayjs(dateString);
    
    if (timeString) {
      const time = dayjs(timeString, 'HH:mm:ss');
      if (time.isValid()) {
        return `${time.format('HH:mm')}, ${date.format('DD/MM/YYYY')}`;
      }
    }
    
    return date.format('DD/MM/YYYY');
  };

  const formatRecurrence = (recurrenceType, interval) => {
    if (!recurrenceType) return 'Không lặp lại';
    const recurrenceMap = {
      'DAILY': 'Hàng ngày',
      'WEEKLY': 'Hàng tuần',
      'MONTHLY': 'Hàng tháng',
      'YEARLY': 'Hàng năm'
    };
    const recurrenceText = recurrenceMap[recurrenceType] || recurrenceType;
    const intervalValue = interval || 1;
    
    if (intervalValue > 1) {
      const intervalText = {
        'DAILY': 'ngày',
        'WEEKLY': 'tuần',
        'MONTHLY': 'tháng',
        'YEARLY': 'năm'
      };
      return `Mỗi ${intervalValue} ${intervalText[recurrenceType] || recurrenceType.toLowerCase()}`;
    }
    
    return recurrenceText;
  };

  const getAssetName = (schedule) => {
    if (schedule.asset?.assetName) return schedule.asset.assetName;
    const asset = assets.find(a => a.assetId === schedule.assetId);
    return asset?.assetName || asset?.name || 'N/A';
  };

  // Kiểm tra xem tài sản có đang trong quá trình bảo trì không
  const isAssetUnderMaintenance = (schedule) => {
    if (!schedule || !schedule.status) {
      return false;
    }
    
    // Nếu status là IN_PROGRESS (Đang thực hiện) thì đang bảo trì
    const status = String(schedule.status).trim().toUpperCase();
    return status === 'IN_PROGRESS';
  };

  const handleEdit = (schedule) => {
    setSelectedSchedule(schedule);
    setShowUpdateModal(true);
  };

  const handleDelete = async (schedule) => {
    try {
      setLoading(true);
      await assetsApi.deleteMaintenanceSchedule(schedule.id || schedule.scheduleId);
      showNotification('success', 'Thành công', 'Đã xóa lịch bảo trì thành công');
      fetchData();
    } catch (error) {
      const errorMessage = error.response?.data?.message || 'Có lỗi xảy ra khi xóa lịch bảo trì. Vui lòng thử lại!';
      showNotification('error', 'Lỗi', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateSuccess = () => {
    setShowCreateModal(false);
    fetchData();
  };

  const handleUpdateSuccess = () => {
    setShowUpdateModal(false);
    setSelectedSchedule(null);
    fetchData();
  };



  if (loading && schedules.length === 0) {
    return (
      <Layout style={{ minHeight: '100vh', background: '#f0f2f5' }}>
        <Content style={{ padding: '24px', display: 'flex', justifyContent: 'center', alignItems: 'center', height: '400px' }}>
          <Spin size="large" />
        </Content>
      </Layout>
    );
  }

  if (error && schedules.length === 0) {
    return (
      <Layout style={{ minHeight: '100vh', background: '#f0f2f5' }}>
        <Content style={{ padding: '24px' }}>
          <Alert message="Lỗi" description={error} type="error" showIcon />
        </Content>
      </Layout>
    );
  }

  return (
    <App>
      <Layout style={{ minHeight: '100vh', background: '#f0f2f5' }}>
        <Content style={{ padding: '24px' }}>
          <div style={{ marginBottom: 24 }}>
            <Flex justify="space-between" align="center" wrap="wrap" gap="middle">
              <div>
                <Title level={2} style={{ margin: 0, marginBottom: 8 }}>
                  Quản lý lịch bảo trì
                </Title>
              </div>
              <Space>
                <Button 
                  type="primary"
                  onClick={() => setShowCreateModal(true)}
                  size="large"
                >
                  Tạo lịch bảo trì mới
                </Button>
              </Space>
            </Flex>
          </div>

          <Card 
            style={{ marginBottom: 24 }}
            bodyStyle={{ padding: '16px 24px' }}
          >
            <Row gutter={[16, 16]} align="middle">
              <Col xs={24} sm={12} md={6}>
                <Input
                  placeholder="Tìm theo tên tài sản..."
                  prefix={<SearchOutlined />}
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  allowClear
                />
              </Col>
              <Col xs={24} sm={12} md={5}>
                <Select 
                  value={categoryFilter} 
                  onChange={setCategoryFilter}
                  style={{ width: '100%' }}
                  placeholder="Danh mục"
                >
                  <Option value="all">Tất cả danh mục</Option>
                  {normalizedCategories.map(cat => (
                    <Option key={cat.categoryId} value={cat.categoryId}>
                      {cat.categoryName}
                    </Option>
                  ))}
                </Select>
              </Col>
              <Col xs={24} sm={12} md={5}>
                <Select 
                  value={statusFilter} 
                  onChange={setStatusFilter}
                  style={{ width: '100%' }}
                  placeholder="Trạng thái"
                >
                  <Option value="all">Tất cả trạng thái</Option>
                  <Option value="SCHEDULED">Đã lên lịch</Option>
                  <Option value="IN_PROGRESS">Đang thực hiện</Option>
                </Select>
              </Col>
              <Col xs={24} sm={12} md={8}>
                <RangePicker
                  style={{ width: '100%' }}
                  placeholder={['Từ ngày', 'Đến ngày']}
                  format="DD/MM/YYYY"
                  value={dateRange}
                  onChange={setDateRange}
                />
              </Col>
              <Col xs={24} sm={12} md={24}>
                <Flex justify="end" gap="small" wrap="wrap">
                  <Button 
                    icon={<ReloadOutlined />} 
                    onClick={() => {
                      setSearchTerm('');
                      setCategoryFilter('all');
                      setStatusFilter('all');
                      setDateRange(null);
                      fetchData();
                    }}
                    loading={loading}
                  >
                    Làm mới
                  </Button>
                </Flex>
              </Col>
            </Row>
          </Card>

          <Card
            title={
              <Flex align="center" gap="small">
                <CalendarOutlined />
                <span>Danh sách lịch bảo trì ({filteredSchedules.length})</span>
              </Flex>
            }
            bodyStyle={{ padding: 0 }}
          >
            <Table
              columns={[
                {
                  title: 'Tài sản',
                  dataIndex: 'assetName',
                  key: 'assetName',
                  render: (text, record) => (
                    <Text strong>{getAssetName(record)}</Text>
                  ),
                },
                {
                  title: 'Ngày bắt đầu',
                  dataIndex: 'startDate',
                  key: 'startDate',
                  render: (date, record) => (
                    <Text type="secondary">
                      {formatDateWithTime(date, record.startTime)}
                    </Text>
                  ),
                },
                {
                  title: 'Ngày kết thúc',
                  dataIndex: 'endDate',
                  key: 'endDate',
                  render: (date, record) => (
                    <Text type="secondary">
                      {formatDateWithTime(date, record.endTime)}
                    </Text>
                  ),
                },
                {
                  title: 'Lặp lại',
                  key: 'recurrence',
                  render: (_, record) => (
                    <Text type="secondary">
                      {formatRecurrence(record.recurrenceType || record.recurrence, record.recurrenceInterval)}
                    </Text>
                  ),
                },
                {
                  title: 'Trạng thái',
                  dataIndex: 'status',
                  key: 'status',
                  render: (status) => getStatusBadge(status),
                },
                {
                  title: 'Hành động',
                  key: 'action',
                  render: (_, record) => {
                    const isUnderMaintenance = isAssetUnderMaintenance(record);
                    return (
                      <Space>
                        <Tooltip title="Chỉnh sửa">
                          <Button
                            type="text"
                            icon={<EditOutlined />}
                            onClick={() => handleEdit(record)}
                            size="small"
                          />
                        </Tooltip>
                        {!isUnderMaintenance && (
                          <Popconfirm
                            title="Xóa lịch bảo trì"
                            description="Bạn có chắc muốn xóa lịch bảo trì này không?"
                            onConfirm={() => handleDelete(record)}
                            okText="Có"
                            cancelText="Không"
                          >
                            <Tooltip title="Xóa lịch bảo trì">
                              <Button
                                type="text"
                                danger
                                icon={<DeleteOutlined />}
                                size="small"
                              />
                            </Tooltip>
                          </Popconfirm>
                        )}
                      </Space>
                    );
                  },
                },
              ]}
              dataSource={filteredSchedules}
              rowKey={(record) => record.id || record.scheduleId}
              loading={loading}
              pagination={{
                pageSize: 10,
                showSizeChanger: true,
                showQuickJumper: true,
                showTotal: (total, range) => 
                  `${range[0]}-${range[1]} của ${total} lịch bảo trì`,
              }}
              locale={{
                emptyText: (
                  <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description="Chưa có lịch bảo trì nào"
                  />
                )
              }}
              scroll={{ x: 800 }}
            />
          </Card>

          <CreateMaintenanceSchedule
            show={showCreateModal}
            onHide={() => setShowCreateModal(false)}
            onSuccess={handleCreateSuccess}
            assets={assets.filter(a => !a.isDelete)}
          />

          <UpdateMaintenanceSchedule
            show={showUpdateModal}
            onHide={() => {
              setShowUpdateModal(false);
              setSelectedSchedule(null);
            }}
            onSuccess={handleUpdateSuccess}
            schedule={selectedSchedule}
            assets={assets.filter(a => !a.isDelete)}
          />
        </Content>
      </Layout>
    </App>
  );
}

