import React, { useState, useEffect, useMemo } from 'react';
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
  Badge,
  Empty,
  Flex,
  App,
  Select,
  Statistic,
  Spin,
  Alert
} from 'antd';
import { 
  EditOutlined, 
  DeleteOutlined, 
  ReloadOutlined,
  SearchOutlined,
  ToolOutlined
} from '@ant-design/icons';
import { useLanguage } from '../../../hooks/useLanguage';
import { assetsApi } from '../../../features/building-management/assetsApi';
import CreateAsset from './CreateAsset';
import UpdateAsset from './UpdateAsset';
import useNotification from '../../../hooks/useNotification';
import NotificationBell from '../../../components/NotificationBell';

const { Title, Text } = Typography;
const { Content } = Layout;
const { Option } = Select;

export default function Assets() {
  const { strings } = useLanguage();
  const { showNotification } = useNotification();
  
  const [assets, setAssets] = useState([]); 
  const [loading, setLoading] = useState(true); 
  const [error, setError] = useState('');
  const [categories, setCategories] = useState([]);
  const [searchTerm, setSearchTerm] = useState(''); 
  const [statusFilter, setStatusFilter] = useState('all'); 
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showUpdateModal, setShowUpdateModal] = useState(false);
  const [selectedAsset, setSelectedAsset] = useState(null);
  const [stats, setStats] = useState({
    totalAssets: 0,
    activeAssets: 0,
    inactiveAssets: 0,
    maintenanceAssets: 0
  });

  // State để track tài sản mới được thêm (để đảm bảo nó ở đầu danh sách)
  const [newlyAddedAssetIds, setNewlyAddedAssetIds] = useState(() => {
    try {
      const saved = localStorage.getItem('newlyAddedAssetIds');
      return saved ? JSON.parse(saved) : [];
    } catch {
      return [];
    }
  });



  // Effect để lưu newlyAddedAssetIds vào localStorage khi có thay đổi
  useEffect(() => {
    try {
      localStorage.setItem('newlyAddedAssetIds', JSON.stringify(newlyAddedAssetIds));
    } catch {}
  }, [newlyAddedAssetIds]);

  // Helper function: Loại bỏ dấu tiếng Việt để search
  const removeVietnameseTones = (str) => {
    if (!str) return '';
    return str
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/đ/g, 'd')
      .replace(/Đ/g, 'D')
      .toLowerCase();
  };

  // Refresh data function
  const refreshData = async () => {
    try {
      setLoading(true);
      
      const [assetsData, categoriesData] = await Promise.all([
        assetsApi.getAll(),
        assetsApi.getCategories().catch(() => [])
      ]);
      
      setAssets(assetsData);
      setCategories(categoriesData);
      
      // Tính thống kê (chỉ tính assets chưa bị xóa mềm)
      const activeAssetsOnly = assetsData.filter(a => a.isDelete !== true);
      try {
        const statsData = await assetsApi.getStatistics();
        setStats(statsData || {
          totalAssets: activeAssetsOnly.length,
          activeAssets: activeAssetsOnly.filter(a => a.status === 'ACTIVE').length,
          inactiveAssets: activeAssetsOnly.filter(a => a.status === 'INACTIVE').length,
          maintenanceAssets: activeAssetsOnly.filter(a => a.status === 'MAINTENANCE').length
        });
      } catch {
        // Fallback nếu API stats không hoạt động
        setStats({
          totalAssets: activeAssetsOnly.length,
          activeAssets: activeAssetsOnly.filter(a => a.status === 'ACTIVE').length,
          inactiveAssets: activeAssetsOnly.filter(a => a.status === 'INACTIVE').length,
          maintenanceAssets: activeAssetsOnly.filter(a => a.status === 'MAINTENANCE').length
        });
      }
      
      setError('');
    } catch (err) {
      setError('Có lỗi xảy ra khi tải dữ liệu.');
    } finally {
      setLoading(false);
    }
  };

  // Fetch data khi component mount
  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        
        const [assetsData, categoriesData] = await Promise.all([
          assetsApi.getAll(),
          assetsApi.getCategories().catch(() => [])
        ]);
        
        setAssets(assetsData);
        setCategories(categoriesData);
        
        // Tính thống kê (chỉ tính assets chưa bị xóa mềm)
        const activeAssetsOnly = assetsData.filter(a => a.isDelete !== true);
        try {
          const statsData = await assetsApi.getStatistics();
          setStats(statsData);
        } catch {
          setStats({
            totalAssets: activeAssetsOnly.length,
            activeAssets: activeAssetsOnly.filter(a => a.status === 'ACTIVE').length,
            inactiveAssets: activeAssetsOnly.filter(a => a.status === 'INACTIVE').length,
            maintenanceAssets: activeAssetsOnly.filter(a => a.status === 'MAINTENANCE').length
          });
        }
      } catch (err) {
        setError('Không thể tải danh sách tài sản. Vui lòng thử lại sau.'); 
      } finally {
        setLoading(false);
      }
    };

    fetchData(); 
  }, []); 

  // Normalize categories: BE trả về "name" -> FE cần "categoryName"
  const normalizedCategories = useMemo(() => {
    if (categories.length > 0) {
      return categories.map(cat => ({
        categoryId: cat.categoryId,
        categoryName: cat.name || cat.categoryName,
        code: cat.code
      }));
    }
    
    // Fallback: extract từ assets nếu không có API categories
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

  // Bỏ "Tiện ích chung cư" khỏi modal tạo/sửa (có trang quản lý riêng)
  const categoriesForModal = useMemo(() => {
    return normalizedCategories.filter(cat => 
      cat.code !== 'AMENITY' && !cat.categoryName?.includes('Tiện ích')
    );
  }, [normalizedCategories]);

  // Lấy danh sách tầng (locations) từ assets hiện có
  const availableLocations = useMemo(() => {
    const locations = new Set();
    assets.forEach(asset => {
      if (asset.location) {
        locations.add(asset.location);
      }
    });
    
    // Sắp xếp theo số thứ tự tầng (VD: Tầng 1, Tầng 2, ..., Tầng 10, Tầng 11)
    return Array.from(locations).sort((a, b) => {
      // Extract số từ chuỗi "Tầng X"
      const numA = parseInt(a.match(/\d+/)?.[0] || '0');
      const numB = parseInt(b.match(/\d+/)?.[0] || '0');
      return numA - numB;
    });
  }, [assets]);

  // Lọc và sort assets
  const filteredAssets = useMemo(() => {
    return assets.filter(asset => {
      // Chỉ hiển thị assets chưa bị xóa mềm
      if (asset.isDelete === true) return false;
      
      // Search không dấu - "sa" sẽ match với "sàn", "Sản", v.v.
      const assetName = asset.assetName || asset.name || '';
      const normalizedAssetName = removeVietnameseTones(assetName);
      const normalizedSearch = removeVietnameseTones(searchTerm);
      
      const matchesSearch = !searchTerm || normalizedAssetName.includes(normalizedSearch);
      const matchesStatus = statusFilter === 'all' || asset.status === statusFilter;
      const matchesCategory = categoryFilter === 'all' || 
        asset.categoryId === categoryFilter ||
        asset.assetCategory?.categoryId === categoryFilter;
      
      return matchesSearch && matchesStatus && matchesCategory;
    }).sort((a, b) => {
      // Tài sản mới được thêm luôn ở đầu danh sách
      const aIsNew = newlyAddedAssetIds.includes(a.assetId);
      const bIsNew = newlyAddedAssetIds.includes(b.assetId);
      
      if (aIsNew && !bIsNew) return -1;
      if (!aIsNew && bIsNew) return 1;
      
      // Nếu cả 2 đều mới hoặc đều cũ, sort theo ngày tạo
      if (a.createdDate && b.createdDate) {
        return new Date(b.createdDate) - new Date(a.createdDate);
      }
      return 0;
    });
  }, [assets, searchTerm, statusFilter, categoryFilter, newlyAddedAssetIds]);





  // Helper functions
  const getStatusBadge = (status) => {
    const config = {
      'ACTIVE': { color: 'green', label: 'Hoạt động' },
      'INACTIVE': { color: 'default', label: 'Không hoạt động' },
      'MAINTENANCE': { color: 'orange', label: 'Bảo trì' }
    };
    const statusConfig = config[status] || config.INACTIVE;
    return <Tag color={statusConfig.color}>{statusConfig.label}</Tag>;
  };

  const formatDate = (dateString) => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('vi-VN');
  };

  const getCategoryName = (asset) => {
    return asset.assetCategory?.name || 
           asset.assetCategory?.categoryName || 
           asset.categoryName ||
           normalizedCategories.find(c => c.categoryId === asset.categoryId)?.categoryName ||
           'N/A';
  };

  const handleCreateSuccess = async (newAssetId) => {
    try {
      setLoading(true);
      const [assetsData, statsData] = await Promise.all([
        assetsApi.getAll(),
        assetsApi.getStatistics().catch(() => null)
      ]);
      
      // Thêm tài sản mới vào đầu danh sách
      if (newAssetId) {
        const newAsset = assetsData.find(asset => asset.assetId === newAssetId);
        if (newAsset) {
          setAssets(prev => [newAsset, ...prev.filter(a => a.assetId !== newAssetId)]);
          // Thêm vào danh sách tracking
          setNewlyAddedAssetIds(prev => [...prev, newAssetId]);
        } else {
          setAssets(assetsData);
        }
      } else {
        setAssets(assetsData);
      }
      
      // Tính thống kê (chỉ tính assets chưa bị xóa mềm)
      const activeAssetsOnly = assetsData.filter(a => a.isDelete !== true);
      if (statsData) {
        setStats(statsData);
      } else {
        setStats({
          totalAssets: activeAssetsOnly.length,
          activeAssets: activeAssetsOnly.filter(a => a.status === 'ACTIVE').length,
          inactiveAssets: activeAssetsOnly.filter(a => a.status === 'INACTIVE').length,
          maintenanceAssets: activeAssetsOnly.filter(a => a.status === 'MAINTENANCE').length
        });
      }
    } catch (err) {
      setError('Có lỗi xảy ra khi làm mới dữ liệu.');
    } finally {
      setLoading(false);
    }
  };

  const handleEdit = (asset) => {
    setSelectedAsset(asset);
    setShowUpdateModal(true);
  };

  const handleUpdateSuccess = async () => {
    try {
      setLoading(true);
      
      // Reset filters về "all" để người dùng thấy được tài sản vừa cập nhật
      setStatusFilter('all');
      setCategoryFilter('all');
      setSearchTerm('');
      
      const [assetsData, statsData] = await Promise.all([
        assetsApi.getAll(),
        assetsApi.getStatistics().catch(() => null)
      ]);
      
      setAssets(assetsData);
      
      // Tính thống kê (chỉ tính assets chưa bị xóa mềm)
      const activeAssetsOnly = assetsData.filter(a => a.isDelete !== true);
      if (statsData) {
        setStats(statsData);
      } else {
        setStats({
          totalAssets: activeAssetsOnly.length,
          activeAssets: activeAssetsOnly.filter(a => a.status === 'ACTIVE').length,
          inactiveAssets: activeAssetsOnly.filter(a => a.status === 'INACTIVE').length,
          maintenanceAssets: activeAssetsOnly.filter(a => a.status === 'MAINTENANCE').length
        });
      }
    } catch (err) {
      setError('Có lỗi xảy ra khi làm mới dữ liệu.');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteClick = async (asset) => {
    try {
      setLoading(true);
      
      await assetsApi.delete(asset.assetId);
      
      setAssets(prevAssets => prevAssets.filter(a => a.assetId !== asset.assetId));
      
      showNotification('success', 'Thành công', 'Đã xóa tài sản thành công');
    } catch (error) {
      const errorMessage = error.response?.data?.message || 'Có lỗi xảy ra khi xóa tài sản. Vui lòng thử lại!';
      showNotification('error', 'Lỗi', errorMessage);
    } finally {
      setLoading(false);
    }
  };



  // Render loading
  if (loading && assets.length === 0) {
    return (
      <Layout style={{ minHeight: '100vh', background: '#f0f2f5' }}>
        <Content style={{ padding: '24px', display: 'flex', justifyContent: 'center', alignItems: 'center', height: '400px' }}>
          <Spin size="large" />
        </Content>
      </Layout>
    );
  }

  // Render error
  if (error) {
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
                  Quản lý tài sản
                </Title>
                <Text type="secondary">
                  {strings?.assetsDescription || 'Quản lý và theo dõi tất cả tài sản chung cư'}
                </Text>
              </div>
              <Space>
                <NotificationBell onlyMaintenance={true} />
                <Button 
                  type="primary"
                  onClick={() => setShowCreateModal(true)}
                  size="large"
                >
                  Thêm tài sản mới
                </Button>
              </Space>
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
                  value={statusFilter} 
                  onChange={setStatusFilter}
                  style={{ width: '100%' }}
                  placeholder="Trạng thái"
                >
                  <Option value="all">Tất cả trạng thái</Option>
                  <Option value="ACTIVE">Hoạt động</Option>
                  <Option value="MAINTENANCE">Bảo trì</Option>
                  <Option value="INACTIVE">Không hoạt động</Option>
                </Select>
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
                    <Option key={cat.categoryId} value={cat.categoryId}>{cat.categoryName}</Option>
                  ))}
                </Select>
              </Col>
              <Col xs={24} sm={12} md={8}>
                <Flex justify="end" gap="small" wrap="wrap">
                  <Button 
                    icon={<ReloadOutlined />} 
                    onClick={() => {
                      setSearchTerm('');
                      refreshData();
                    }}
                    loading={loading}
                  >
                    Làm mới
                  </Button>
                </Flex>
              </Col>
            </Row>
          </Card>

          {/* Stats Cards */}
          <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
            <Col xs={24} sm={12} md={6}>
              <Card>
                <Statistic
                  title="Tổng tài sản"
                  value={stats.totalAssets}
                  prefix={<ToolOutlined />}
                  valueStyle={{ color: '#3f8600' }}
                />
              </Card>
            </Col>
            <Col xs={24} sm={12} md={6}>
              <Card>
                <Statistic
                  title="Đang hoạt động"
                  value={stats.activeAssets}
                  prefix={<Badge status="success" />}
                  valueStyle={{ color: '#52c41a' }}
                />
              </Card>
            </Col>
            <Col xs={24} sm={12} md={6}>
              <Card>
                <Statistic
                  title="Không hoạt động"
                  value={stats.inactiveAssets}
                  prefix={<Badge status="default" />}
                  valueStyle={{ color: '#8c8c8c' }}
                />
              </Card>
            </Col>
            <Col xs={24} sm={12} md={6}>
              <Card>
                <Statistic
                  title="Bảo trì"
                  value={stats.maintenanceAssets}
                  prefix={<Badge status="warning" />}
                  valueStyle={{ color: '#faad14' }}
                />
              </Card>
            </Col>
          </Row>

          {/* Assets Table */}
          <Card
            title={
              <Flex align="center" gap="small">
                <ToolOutlined />
                <span>Danh sách tài sản</span>
              </Flex>
            }
            bodyStyle={{ padding: 0 }}
          >
            <Table
              columns={[
                {
                  title: 'Tên tài sản',
                  dataIndex: 'assetName',
                  key: 'assetName',
                  render: (text, record) => (
                    <Text strong>{record.assetName || record.name}</Text>
                  ),
                },
                {
                  title: 'Danh mục',
                  dataIndex: 'categoryName',
                  key: 'categoryName',
                  render: (text, record) => (
                    <Text type="secondary">{getCategoryName(record)}</Text>
                  ),
                },
                {
                  title: 'Ngày mua',
                  dataIndex: 'purchaseDate',
                  key: 'purchaseDate',
                  render: (date) => (
                    <Text type="secondary">{formatDate(date)}</Text>
                  ),
                },
                {
                  title: 'Hết hạn BH',
                  dataIndex: 'warrantyExpire',
                  key: 'warrantyExpire',
                  render: (date, record) => (
                    <Text type="secondary">{formatDate(date || record.warranty_expire)}</Text>
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
                  render: (_, record) => (
                    <Space>
                      <Tooltip title="Chỉnh sửa">
                        <Button
                          type="text"
                          icon={<EditOutlined />}
                          onClick={() => handleEdit(record)}
                          size="small"
                        />
                      </Tooltip>
                      <Popconfirm
                        title="Xóa tài sản"
                        description="Bạn có chắc muốn xóa tài sản này không?"
                        onConfirm={() => handleDeleteClick(record)}
                        okText="Có"
                        cancelText="Không"
                      >
                        <Tooltip title="Xóa tài sản">
                          <Button
                            type="text"
                            danger
                            icon={<DeleteOutlined />}
                            size="small"
                          />
                        </Tooltip>
                      </Popconfirm>
                    </Space>
                  ),
                },
              ]}
              dataSource={filteredAssets}
              rowKey="assetId"
              loading={loading}
              pagination={{
                pageSize: 10,
                showSizeChanger: true,
                showQuickJumper: true,
                showTotal: (total, range) => 
                  `${range[0]}-${range[1]} của ${total} tài sản`,
              }}
              locale={{
                emptyText: (
                  <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description="Chưa có tài sản nào"
                  />
                )
              }}
              scroll={{ x: 800 }}
            />
          </Card>

          {/* Create Asset Modal */}
          <CreateAsset
            show={showCreateModal}
            onHide={() => setShowCreateModal(false)}
            onSuccess={handleCreateSuccess}
            categories={categoriesForModal}
            existingAssets={assets}
            availableLocations={availableLocations}
            onShowToast={(message, type = 'success') => {
              showNotification(type, type === 'success' ? 'Thành công' : 'Lỗi', message);
            }}
          />

          {/* Update Asset Modal */}
          <UpdateAsset
            show={showUpdateModal}
            onHide={() => setShowUpdateModal(false)}
            onSuccess={handleUpdateSuccess}
            categories={categoriesForModal}
            allCategories={normalizedCategories}
            asset={selectedAsset}
            existingAssets={assets}
            availableLocations={availableLocations}
            onShowToast={(message, type = 'success') => {
              showNotification(type, type === 'success' ? 'Thành công' : 'Lỗi', message);
            }}
          />
        </Content>
      </Layout>
    </App>
  );
}
