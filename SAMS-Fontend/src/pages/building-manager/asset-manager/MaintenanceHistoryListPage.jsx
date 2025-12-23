import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { 
  Layout, 
  Card, 
  Button, 
  Input,
  Table, 
  Typography, 
  Row, 
  Col,
  Empty,
  Flex,
  App,
  Select,
  Spin,
  Alert,
  Tag,
  Form,
  DatePicker
} from 'antd';
import { 
  ReloadOutlined,
  SearchOutlined,
  HistoryOutlined,
  CheckCircleOutlined
} from '@ant-design/icons';
import { assetsApi } from '../../../features/building-management/assetsApi';
import { userApi } from '../../../features/user/userApi';
import { checkVoucherByHistory, createVoucherFromMaintenance } from '../../../features/accountant/voucherApi';
import { listServiceType } from '../../../features/accountant/servicetypesApi';
import CreateVoucherFromMaintenanceModal from './CreateVoucherFromMaintenanceModal';
import useNotification from '../../../hooks/useNotification';
import dayjs from 'dayjs';

const { Title, Text } = Typography;
const { Content } = Layout;
const { Option } = Select;
const { RangePicker } = DatePicker;

export default function MaintenanceHistoryListPage() {
  const notification = useNotification();
  const [form] = Form.useForm();
  
  const [history, setHistory] = useState([]);
  const [assets, setAssets] = useState([]);
  const [schedules, setSchedules] = useState([]);
  const [categories, setCategories] = useState([]);
  const [userMap, setUserMap] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [dateRange, setDateRange] = useState(null);
  
  // Voucher states
  const [voucherStatusMap, setVoucherStatusMap] = useState({}); // { historyId: { hasVoucher, voucherId, voucherNumber } }
  const [checkingVouchers, setCheckingVouchers] = useState(false);
  const [showCreateVoucherModal, setShowCreateVoucherModal] = useState(false);
  const [selectedHistoryForVoucher, setSelectedHistoryForVoucher] = useState(null);
  const [submittingVoucher, setSubmittingVoucher] = useState(false);
  const [voucherError, setVoucherError] = useState(null);
  const [serviceTypes, setServiceTypes] = useState([]);
  const [serviceTypesLoading, setServiceTypesLoading] = useState(false);
  const [maintenanceServiceTypeId, setMaintenanceServiceTypeId] = useState(null);

  // Fetch service types và tìm ID của "Dịch vụ bảo trì"
  useEffect(() => {
    const fetchServiceTypes = async () => {
      try {
        setServiceTypesLoading(true);
        const data = await listServiceType();
        const serviceTypesList = Array.isArray(data) ? data : (data?.items || []);
        setServiceTypes(serviceTypesList);
        
        // Tìm ID của "Dịch vụ bảo trì"
        const maintenanceService = serviceTypesList.find(
          st => (st.name || st.serviceTypeName || '').toLowerCase().includes('bảo trì') ||
                (st.name || st.serviceTypeName || '').toLowerCase().includes('maintenance')
        );
        if (maintenanceService) {
          setMaintenanceServiceTypeId(maintenanceService.id || maintenanceService.serviceTypeId);
        }
      } catch (err) {
        console.error('Error fetching service types:', err);
      } finally {
        setServiceTypesLoading(false);
      }
    };
    
    fetchServiceTypes();
  }, []);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError('');
      const [historyData, assetsData, schedulesData, categoriesData] = await Promise.all([
        assetsApi.getMaintenanceHistory().catch(err => {
          console.error('Error fetching maintenance history:', err);
          return [];
        }),
        assetsApi.getAll().catch(() => []),
        assetsApi.getMaintenanceSchedules().catch(() => []),
        assetsApi.getCategories().catch(() => [])
      ]);
      
      if (historyData && historyData.length > 0) {
        const userIds = new Set();
        historyData.forEach(record => {
          // Lấy userId từ schedule nếu có
          const scheduleCreatedBy = record.schedule?.createdBy || 
                                   record.maintenanceSchedule?.createdBy;
          if (scheduleCreatedBy && !record.createdByUserName) {
            userIds.add(scheduleCreatedBy);
          }
        });
        
        if (userIds.size > 0) {
          const userPromises = Array.from(userIds).map(async (userId) => {
            try {
              const user = await userApi.getUserById(userId);
              return { userId, user };
            } catch (err) {
              console.error(`Error fetching user ${userId}:`, err);
              return { userId, user: null };
            }
          });
          
          const userResults = await Promise.all(userPromises);
          const newUserMap = {};
          userResults.forEach(({ userId, user }) => {
            if (user) {
              // Ưu tiên fullName, sau đó là firstName + lastName, cuối cùng là userName
              newUserMap[userId] = user.fullName || 
                                  (user.firstName && user.lastName 
                                    ? `${user.firstName} ${user.lastName}`.trim()
                                    : user.userName || '');
            }
          });
          setUserMap(newUserMap);
        }
      }
      
      // Loại bỏ duplicate records dựa trên historyId
      const uniqueHistoryData = Array.isArray(historyData) 
        ? historyData.filter((record, index, self) => {
            const historyId = record.id || record.historyId;
            if (!historyId) return false; // Bỏ qua records không có ID
            // Chỉ giữ lại record đầu tiên nếu có duplicate
            return index === self.findIndex(r => (r.id || r.historyId) === historyId);
          })
        : [];
      
      setHistory(uniqueHistoryData);
      setAssets(Array.isArray(assetsData) ? assetsData : []);
      setSchedules(Array.isArray(schedulesData) ? schedulesData : []);
      setCategories(Array.isArray(categoriesData) ? categoriesData : []);
      
      if (!Array.isArray(historyData)) {
        setError('Không thể tải danh sách lịch sử bảo trì. Vui lòng thử lại sau.');
      } else if (uniqueHistoryData.length > 0) {
        // Check voucher status cho mỗi history (sử dụng dữ liệu đã loại bỏ duplicate)
        checkVoucherStatuses(uniqueHistoryData);
      }
    } catch (err) {
      setError('Không thể tải danh sách lịch sử bảo trì. Vui lòng thử lại sau.');
      console.error('Error fetching data:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  // Fetch data
  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Check voucher status cho danh sách history
  const checkVoucherStatuses = async (historyList) => {
    try {
      setCheckingVouchers(true);
      const statusPromises = historyList.map(async (record) => {
        const historyId = record.id || record.historyId;
        if (!historyId) return null;
        
        try {
          const result = await checkVoucherByHistory(historyId);
          const voucher = result?.voucher;
          return {
            historyId,
            hasVoucher: result?.hasVoucher || false,
            voucherId: result?.voucherId || null,
            voucherNumber: voucher?.voucherNumber || result?.voucherNumber || null,
            voucherAmount: voucher?.totalAmount || voucher?.amount || null
          };
        } catch (err) {
          console.error(`Error checking voucher for history ${historyId}:`, err);
          return {
            historyId,
            hasVoucher: false,
            voucherId: null,
            voucherNumber: null,
            voucherAmount: null
          };
        }
      });
      
      const statuses = await Promise.all(statusPromises);
      const newStatusMap = {};
      statuses.forEach(status => {
        if (status) {
          newStatusMap[status.historyId] = status;
        }
      });
      setVoucherStatusMap(newStatusMap);
    } catch (err) {
      console.error('Error checking voucher statuses:', err);
    } finally {
      setCheckingVouchers(false);
    }
  };

  // Normalize categories
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

  // Helper function to get asset name
  const getAssetName = useCallback((record) => {
    if (record.asset?.assetName) return record.asset.assetName;
    const asset = assets.find(a => a.assetId === record.assetId);
    return asset?.assetName || asset?.name || 'N/A';
  }, [assets]);

  // Filter history
  const filteredHistory = useMemo(() => {
    return history.filter(record => {
      // Search filter - sử dụng getAssetName để tìm đúng tên tài sản
      const assetName = getAssetName(record);
      const matchesSearch = !searchTerm || 
        assetName.toLowerCase().includes(searchTerm.toLowerCase());
      
      // Category filter
      let matchesCategory = true;
      if (categoryFilter !== 'all') {
        const recordAsset = assets.find(a => 
          a.assetId === record.assetId || 
          String(a.assetId) === String(record.assetId)
        );
        matchesCategory = recordAsset && (
          recordAsset.categoryId === categoryFilter ||
          recordAsset.assetCategory?.categoryId === categoryFilter
        );
      }
      
      // Date range filter
      let matchesDateRange = true;
      if (dateRange && dateRange[0] && dateRange[1]) {
        const rangeStartDate = dayjs(dateRange[0]).startOf('day');
        const rangeEndDate = dayjs(dateRange[1]).endOf('day');
        
        // Lấy ngày từ record (có thể là actionDate, maintenanceDate, startDate, endDate)
        const recordDate = record.actionDate || record.maintenanceDate || record.startDate || record.endDate;
        if (recordDate) {
          const recordDateObj = dayjs(recordDate).startOf('day');
          matchesDateRange = (recordDateObj.isAfter(rangeStartDate) || recordDateObj.isSame(rangeStartDate)) && 
                            (recordDateObj.isBefore(rangeEndDate) || recordDateObj.isSame(rangeEndDate));
        } else {
          matchesDateRange = false;
        }
      }
      
      return matchesSearch && matchesCategory && matchesDateRange;
    });
  }, [history, searchTerm, categoryFilter, dateRange, assets, getAssetName]);

  // Helper functions
  const formatDateWithTime = (dateString, timeString) => {
    if (!dateString) return 'N/A';
    const date = dayjs(dateString);
    
    // Nếu dateString đã chứa thời gian (ISO format hoặc có HH:mm:ss)
    if (dateString.includes('T') || dateString.includes(' ')) {
      const dateTime = dayjs(dateString);
      if (dateTime.isValid() && dateTime.format('HH:mm') !== '00:00') {
        return `${dateTime.format('HH:mm')}, ${dateTime.format('DD/MM/YYYY')}`;
      }
    }
    
    // Nếu có timeString riêng
    if (timeString) {
      const time = dayjs(timeString, ['HH:mm:ss', 'HH:mm', 'HH:mm:ss.SSS']);
      if (time.isValid()) {
        return `${time.format('HH:mm')}, ${date.format('DD/MM/YYYY')}`;
      }
    }
    
    return date.format('DD/MM/YYYY');
  };

  const formatCurrency = (amount) => {
    if (!amount && amount !== 0) return 'N/A';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND'
    }).format(amount);
  };


  const cleanNote = (text = '') => {
    const autoPrefix = 'lịch bảo trì đã được hoàn thành tự động.';
    const normalized = text.trim();
    if (normalized.toLowerCase().startsWith(autoPrefix)) {
      return normalized.slice(autoPrefix.length).trim().replace(/^mô tả:\s*/i, '').trim();
    }
    return normalized;
  };

  // Handle create voucher
  const handleCreateVoucher = (record) => {
    setSelectedHistoryForVoucher(record);
    setVoucherError(null);
    form.resetFields();
    setShowCreateVoucherModal(true);
  };

  const handleVoucherSubmit = async (values) => {
    if (!selectedHistoryForVoucher) return;
    
    const historyId = selectedHistoryForVoucher.id || selectedHistoryForVoucher.historyId;
    if (!historyId) {
      setVoucherError('Không tìm thấy ID lịch sử bảo trì');
      return;
    }

    if (!maintenanceServiceTypeId) {
      setVoucherError('Không tìm thấy loại dịch vụ bảo trì');
      return;
    }

    try {
      setSubmittingVoucher(true);
      setVoucherError(null);
      
      // Đảm bảo amount là number
      const amount = Number(values.amount);
      if (isNaN(amount) || amount <= 0) {
        setVoucherError('Số tiền không hợp lệ');
        setSubmittingVoucher(false);
        return;
      }
      
      // Tạo payload với PascalCase để match với CreateVoucherFromMaintenanceRequest DTO
      // Backend có PropertyNameCaseInsensitive = true nhưng tốt nhất là match chính xác
      const payload = {
        HistoryId: historyId,
        Amount: amount,
        Note: values.note || null,
        ServiceTypeId: maintenanceServiceTypeId,
        CompanyInfo: values.companyInfo || null
      };

      const result = await createVoucherFromMaintenance(payload);
      
      // Kiểm tra success: nếu có voucherId hoặc voucherNumber thì coi như thành công
      // Backend trả về: { voucherId, voucherNumber } hoặc { success: true, message, ... }
      const isSuccess = result?.success === true || 
                       (result?.voucherId || result?.voucherNumber);
      
      if (isSuccess) {
        // Đóng modal
        setShowCreateVoucherModal(false);
        form.resetFields();
        setSelectedHistoryForVoucher(null);
        
        // Hiển thị toast/notification với message từ backend hoặc message mặc định
        const message = result?.message || 
                       (result?.voucherNumber ? `Tạo phiếu chi thành công. Voucher Number: ${result.voucherNumber}` : 'Tạo phiếu chi thành công');
        notification.showNotification(
          'success',
          'Thành công',
          message,
          5
        );
        
        // Refresh voucher status cho history này để disable nút và cập nhật chi phí
        try {
          const voucherStatus = await checkVoucherByHistory(historyId);
          const voucher = voucherStatus?.voucher;
          setVoucherStatusMap(prev => ({
            ...prev,
            [historyId]: {
              hasVoucher: voucherStatus?.hasVoucher || true,
              voucherId: voucherStatus?.voucherId || result?.voucherId,
              voucherNumber: voucher?.voucherNumber || voucherStatus?.voucherNumber || result?.voucherNumber,
              voucherAmount: voucher?.totalAmount || voucher?.amount || amount
            }
          }));
        } catch (err) {
          console.error('Error refreshing voucher status:', err);
          // Nếu không refresh được, vẫn cập nhật UI với thông tin từ result
          if (result?.voucherId || result?.voucherNumber) {
            setVoucherStatusMap(prev => ({
              ...prev,
              [historyId]: {
                hasVoucher: true,
                voucherId: result.voucherId,
                voucherNumber: result?.voucherNumber || result?.voucherNo,
                voucherAmount: amount // Dùng amount từ form
              }
            }));
          }
        }
        
        // Không redirect đến màn hình chi tiết voucher (theo yêu cầu)
      } else {
        // Nếu không có voucherId/voucherNumber và success !== true, xử lý như error
        const errorMsg = result?.message || result?.error || 'Không thể tạo phiếu chi. Vui lòng thử lại sau.';
        setVoucherError(errorMsg);
      }
    } catch (err) {
      console.error('Error creating voucher from maintenance:', err);
      console.error('Error response:', err?.response?.data);
      console.error('Error status:', err?.response?.status);
      
      const errorMessage = err?.response?.data?.message || 
                          err?.response?.data?.error || 
                          err?.message || 
                          'Không thể tạo phiếu chi. Vui lòng thử lại sau.';
      
      // Kiểm tra nếu là duplicate voucher
      if (errorMessage.toLowerCase().includes('duplicate') || 
          errorMessage.toLowerCase().includes('đã tồn tại') ||
          errorMessage.toLowerCase().includes('đã có phiếu chi')) {
        setVoucherError('Phiếu chi đã tồn tại cho lịch sử bảo trì này');
        // Refresh voucher status để disable nút và cập nhật chi phí
        try {
          const voucherStatus = await checkVoucherByHistory(historyId);
          const voucher = voucherStatus?.voucher;
          setVoucherStatusMap(prev => ({
            ...prev,
            [historyId]: {
              hasVoucher: true,
              voucherId: voucherStatus?.voucherId,
              voucherNumber: voucher?.voucherNumber || voucherStatus?.voucherNumber,
              voucherAmount: voucher?.totalAmount || voucher?.amount
            }
          }));
        } catch (refreshErr) {
          console.error('Error refreshing voucher status:', refreshErr);
        }
      } else {
        setVoucherError(errorMessage);
      }
    } finally {
      setSubmittingVoucher(false);
    }
  };

  const handleVoucherModalCancel = () => {
    setShowCreateVoucherModal(false);
    setSelectedHistoryForVoucher(null);
    setVoucherError(null);
    form.resetFields();
  };


  // Render loading
  if (loading && history.length === 0) {
    return (
      <Layout style={{ minHeight: '100vh', background: '#f0f2f5' }}>
        <Content style={{ padding: '24px', display: 'flex', justifyContent: 'center', alignItems: 'center', height: '400px' }}>
          <Spin size="large" />
        </Content>
      </Layout>
    );
  }

  // Render error
  if (error && history.length === 0) {
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
          {/* Header */}
          <div style={{ marginBottom: 24 }}>
            <Flex justify="space-between" align="center" wrap="wrap" gap="middle">
              <div>
                <Title level={2} style={{ margin: 0, marginBottom: 8 }}>
                  Lịch sử bảo trì
                </Title>
              </div>
            </Flex>
          </div>

          {/* Search & Filters */}
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
              <Col xs={24} sm={12} md={8}>
                <RangePicker
                  style={{ width: '100%' }}
                  placeholder={['Từ ngày', 'Đến ngày']}
                  format="DD/MM/YYYY"
                  value={dateRange}
                  onChange={setDateRange}
                />
              </Col>
              <Col xs={24} sm={12} md={5}>
                <Button 
                  icon={<ReloadOutlined />} 
                  onClick={() => {
                    setSearchTerm('');
                    setCategoryFilter('all');
                    setDateRange(null);
                    fetchData();
                  }}
                  loading={loading}
                  style={{ width: '100%' }}
                >
                  Làm mới
                </Button>
              </Col>
            </Row>
          </Card>

          {/* History Table */}
          <Card
            title={
              <Flex align="center" gap="small">
                <HistoryOutlined />
                <span>Danh sách lịch sử bảo trì ({filteredHistory.length})</span>
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
                  render: (date, record) => {
                    // Ưu tiên lấy actual_start_date (thời gian thực tế)
                    let actualStart = record.actualStartDate || record.actual_start_date;
                    
                    if (actualStart) {
                      // Nếu có actual start date (DATETIME), hiển thị với giờ
                      return (
                        <Text type="secondary">
                          {dayjs(actualStart).format('HH:mm, DD/MM/YYYY')}
                        </Text>
                      );
                    }
                    
                    // Fallback: lấy scheduled start date
                    let startDate = record.scheduledStartDate || record.scheduled_start_date || record.startDate || record.start_date;
                    let startTime = record.startTime || record.start_time;
                    
                    // Thử lấy từ schedule object nested trong record
                    const schedule = record.schedule || record.maintenanceSchedule;
                    if (schedule) {
                      if (!startDate) {
                        startDate = schedule.scheduledStartDate || schedule.scheduled_start_date || schedule.startDate || schedule.start_date;
                      }
                      if (!startTime) {
                        startTime = schedule.startTime || schedule.start_time;
                      }
                    }
                    
                    // Nếu không có trong record, thử lấy từ schedules array
                    if ((!startDate || !startTime) && record.scheduleId) {
                      const relatedSchedule = schedules.find(s => 
                        (s.id || s.scheduleId) === record.scheduleId ||
                        String(s.id || s.scheduleId) === String(record.scheduleId)
                      );
                      if (relatedSchedule) {
                        if (!startDate) {
                          startDate = relatedSchedule.scheduledStartDate || relatedSchedule.scheduled_start_date || relatedSchedule.startDate || relatedSchedule.start_date;
                        }
                        if (!startTime) {
                          startTime = relatedSchedule.startTime || relatedSchedule.start_time;
                        }
                      }
                    }
                    
                    return (
                      <Text type="secondary">
                        {formatDateWithTime(startDate, startTime)}
                      </Text>
                    );
                  },
                },
                {
                  title: 'Ngày kết thúc',
                  dataIndex: 'endDate',
                  key: 'endDate',
                  render: (date, record) => {
                    // Ưu tiên lấy actual_end_date (thời gian thực tế)
                    let actualEnd = record.actualEndDate || record.actual_end_date;
                    
                    if (actualEnd) {
                      // Nếu có actual end date (DATETIME), hiển thị với giờ
                      return (
                        <Text type="secondary">
                          {dayjs(actualEnd).format('HH:mm, DD/MM/YYYY')}
                        </Text>
                      );
                    }
                    
                    // Fallback: lấy scheduled end date hoặc action date
                    let endDate = record.scheduledEndDate || record.scheduled_end_date || record.endDate || record.end_date || record.actionDate || record.action_date;
                    let endTime = record.endTime || record.end_time;
                    
                    // Thử lấy từ schedule object nested trong record
                    const schedule = record.schedule || record.maintenanceSchedule;
                    if (schedule) {
                      if (!endDate || endDate === record.actionDate || endDate === record.action_date) {
                        endDate = schedule.scheduledEndDate || schedule.scheduled_end_date || schedule.endDate || schedule.end_date || endDate;
                      }
                      if (!endTime) {
                        endTime = schedule.endTime || schedule.end_time;
                      }
                    }
                    
                    // Nếu không có trong record, thử lấy từ schedules array
                    if ((!endDate || !endTime) && record.scheduleId) {
                      const relatedSchedule = schedules.find(s => 
                        (s.id || s.scheduleId) === record.scheduleId ||
                        String(s.id || s.scheduleId) === String(record.scheduleId)
                      );
                      if (relatedSchedule) {
                        if (!endDate || endDate === record.actionDate || endDate === record.action_date) {
                          endDate = relatedSchedule.scheduledEndDate || relatedSchedule.scheduled_end_date || relatedSchedule.endDate || relatedSchedule.end_date || endDate;
                        }
                        if (!endTime) {
                          endTime = relatedSchedule.endTime || relatedSchedule.end_time;
                        }
                      }
                    }
                    
                    return (
                      <Text type="secondary">
                        {formatDateWithTime(endDate, endTime)}
                      </Text>
                    );
                  },
                },
                {
                  title: 'Người thực hiện',
                  dataIndex: 'createdByUserName',
                  key: 'createdByUserName',
                  render: (createdByUserName, record) => {
                    // Ưu tiên dùng createdByUserName từ DTO (đã được map sẵn)
                    if (createdByUserName) {
                      return <Text type="secondary">{createdByUserName}</Text>;
                    }
                    
                    // Fallback: Lấy từ schedule nếu có
                    const scheduleUserName = record.schedule?.createdByUserName || 
                                            record.maintenanceSchedule?.createdByUserName;
                    if (scheduleUserName) {
                      return <Text type="secondary">{scheduleUserName}</Text>;
                    }
                    
                    // Fallback cuối cùng: Lấy từ userMap nếu có userId
                    const scheduleCreatedBy = record.schedule?.createdBy || 
                                            record.maintenanceSchedule?.createdBy;
                    if (scheduleCreatedBy && userMap[scheduleCreatedBy]) {
                      return <Text type="secondary">{userMap[scheduleCreatedBy]}</Text>;
                    }
                    
                    return <Text type="secondary">N/A</Text>;
                  },
                },
                {
                  title: 'Chi phí',
                  dataIndex: 'costAmount',
                  key: 'costAmount',
                  render: (costAmount, record) => {
                    const historyId = record.id || record.historyId;
                    const voucherStatus = historyId ? voucherStatusMap[historyId] : null;
                    
                    // Ưu tiên lấy số tiền từ voucher nếu đã có voucher
                    let cost = null;
                    if (voucherStatus?.hasVoucher && voucherStatus?.voucherAmount) {
                      cost = voucherStatus.voucherAmount;
                    } else {
                      // Nếu chưa có voucher, lấy từ record
                      if (record.costAmount !== undefined && record.costAmount !== null) {
                        cost = record.costAmount;
                      } else if (record.cost_amount !== undefined && record.cost_amount !== null) {
                        cost = record.cost_amount;
                      } else if (record.cost !== undefined && record.cost !== null) {
                        cost = record.cost;
                      }
                    }
                    
                    if (cost === null || cost === undefined) {
                      return <Text type="secondary">Chưa có</Text>;
                    }
                    
                    const costNumber = typeof cost === 'string' ? parseFloat(cost) : cost;
                    
                    if (isNaN(costNumber)) {
                      return <Text type="secondary">Chưa có</Text>;
                    }
                    
                    return (
                      <Text strong style={{ color: '#52c41a' }}>
                        {formatCurrency(costNumber)}
                      </Text>
                    );
                  },
                },
                {
                  title: 'Ghi chú',
                  dataIndex: 'notes',
                  key: 'notes',
                  width: 150,
                  render: (notes, record) => {
                    const note =
                      notes ||
                      record.note ||
                      record.description ||
                      record.maintenanceNotes ||
                      record.maintenanceNote;
                    const cleanedNote = cleanNote(note || '');
                    
                    if (!cleanedNote) {
                      return <Text type="secondary">N/A</Text>;
                    }
                    
                    return (
                      <Text
                        style={{ display: 'block', maxWidth: 140 }}
                        ellipsis={{ tooltip: cleanedNote }}
                      >
                        {cleanedNote}
                      </Text>
                    );
                  },
                },
                {
                  title: 'Phiếu chi',
                  key: 'voucher',
                  width: 200,
                  render: (_, record) => {
                    const historyId = record.id || record.historyId;
                    const voucherStatus = historyId ? voucherStatusMap[historyId] : null;
                    const hasVoucher = voucherStatus?.hasVoucher || false;
                    
                    if (checkingVouchers) {
                      return <Spin size="small" />;
                    }
                    
                    if (hasVoucher) {
                      return (
                        <Tag color="green" icon={<CheckCircleOutlined />}>
                          Đã có phiếu chi
                        </Tag>
                      );
                    }
                    
                    return (
                      <Button
                        type="primary"
                        onClick={() => handleCreateVoucher(record)}
                        size="small"
                        loading={checkingVouchers}
                        style={{ fontSize: '12px', padding: '0 8px', height: '24px' }}
                      >
                        Tạo phiếu chi
                      </Button>
                    );
                  },
                },
              ]}
              dataSource={filteredHistory}
              rowKey={(record, index) => {
                const historyId = record.id || record.historyId;
                // Tạo unique key bằng cách kết hợp historyId với index
                if (historyId) {
                  return `history-${historyId}`;
                }
                // Fallback: sử dụng index nếu không có ID (không nên xảy ra)
                return `history-fallback-${index}`;
              }}
              loading={loading}
              pagination={{
                pageSize: 10,
                showSizeChanger: true,
                showQuickJumper: true,
                showTotal: (total, range) => 
                  `${range[0]}-${range[1]} của ${total} bản ghi`,
              }}
              locale={{
                emptyText: (
                  <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description="Chưa có lịch sử bảo trì nào"
                  />
                )
              }}
              scroll={{ x: 1200 }}
            />
          </Card>

          {/* Create Voucher Modal */}
          <CreateVoucherFromMaintenanceModal
            open={showCreateVoucherModal}
            onCancel={handleVoucherModalCancel}
            onSubmit={handleVoucherSubmit}
            form={form}
            serviceTypes={serviceTypes}
            serviceTypesLoading={serviceTypesLoading}
            maintenanceServiceTypeId={maintenanceServiceTypeId}
            submitting={submittingVoucher}
            error={voucherError}
          />
        </Content>
      </Layout>
    </App>
  );
}

