import React, { useState, useEffect } from 'react';
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
  SearchOutlined,
  HistoryOutlined
} from '@ant-design/icons';
import { cardsApi } from '../../../features/building-management/cardsApi';
import CreateCard from './CreateCard';
import UpdateCard from './UpdateCard';
import HistoryCard from './HistoryCard';
import useNotification from '../../../hooks/useNotification';

const { Title, Text } = Typography;
const { Content } = Layout;
const { Option } = Select;

export default function Cards() {
  const { showNotification } = useNotification();
  
  const [cards, setCards] = useState([]);
  const [cardTypes, setCardTypes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [cardTypeFilter, setCardTypeFilter] = useState('all');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showUpdateModal, setShowUpdateModal] = useState(false);
  const [showHistoryModal, setShowHistoryModal] = useState(false);
  const [selectedCard, setSelectedCard] = useState(null);
  const [cardCapabilities, setCardCapabilities] = useState({});
  const [stats, setStats] = useState({
    total: 0,
    active: 0,
    inactive: 0,
    expired: 0,
    lost: 0
  });

  // State để track thẻ mới được thêm (để đảm bảo nó ở đầu danh sách)
  const [newlyAddedCardIds, setNewlyAddedCardIds] = useState(() => {
    try {
      const saved = localStorage.getItem('newlyAddedCardIds');
      return saved ? JSON.parse(saved) : [];
    } catch {
      return [];
    }
  });

  // Effect để lưu newlyAddedCardIds vào localStorage khi có thay đổi
  useEffect(() => {
    try {
      localStorage.setItem('newlyAddedCardIds', JSON.stringify(newlyAddedCardIds));
    } catch (error) {
    }
  }, [newlyAddedCardIds]);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      // Lấy dữ liệu thẻ từ API
      const cardsData = await cardsApi.getAll();
      setCards(cardsData);
      
      // Lấy card types từ API
      const cardTypesData = await cardsApi.getCardTypes();
      setCardTypes(cardTypesData);
      
      // Lấy capabilities cho từng thẻ
      const capabilitiesPromises = cardsData.map(async (card) => {
        const capabilities = await cardsApi.getCardCapabilities(card.cardId);
        return { cardId: card.cardId, capabilities };
      });
      
      const capabilitiesResults = await Promise.all(capabilitiesPromises);
      const capabilitiesMap = {};
      capabilitiesResults.forEach(({ cardId, capabilities }) => {
        capabilitiesMap[cardId] = capabilities;
      });
      setCardCapabilities(capabilitiesMap);
      
      // Tính toán stats từ dữ liệu thẻ
      const calculatedStats = {
        total: cardsData.length,
        active: cardsData.filter(c => c.status === 'ACTIVE' || c.status === 'Hoạt động').length,
        inactive: cardsData.filter(c => c.status === 'INACTIVE' || c.status === 'Không hoạt động').length,
        expired: cardsData.filter(c => c.status === 'EXPIRED' || c.status === 'Hết hạn').length,
        lost: cardsData.filter(c => c.status === 'LOST' || c.status === 'Mất thẻ').length
      };
      setStats(calculatedStats);
      
    } catch (err) {
      setError('Không thể tải dữ liệu');
    } finally {
      setLoading(false);
    }
  };

  const fetchCards = async () => {
    try {
      setLoading(true);
      const data = await cardsApi.getAll();
      setCards(data);
    } catch (err) {
      setError('Không thể tải danh sách thẻ');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status) => {
    const statusConfig = {
      'ACTIVE': { color: 'green', text: 'Hoạt động' },
      'INACTIVE': { color: 'default', text: 'Không hoạt động' },
      'EXPIRED': { color: 'orange', text: 'Hết hạn' },
      'LOST': { color: 'red', text: 'Mất thẻ' },
      'PENDING': { color: 'blue', text: 'Chờ duyệt' },
      'Hoạt động': { color: 'green', text: 'Hoạt động' },
      'Không hoạt động': { color: 'default', text: 'Không hoạt động' },
      'Hết hạn': { color: 'orange', text: 'Hết hạn' },
      'Mất thẻ': { color: 'red', text: 'Mất thẻ' },
      'Chờ duyệt': { color: 'blue', text: 'Chờ duyệt' }
    };
    
    const config = statusConfig[status] || { color: 'blue', text: status };
    return <Tag color={config.color}>{config.text}</Tag>;
  };


  const getCardCapabilitiesBadges = (cardId) => {
    const capabilities = cardCapabilities[cardId] || [];
    
    return capabilities.map((cap, index) => {
      // Tìm cardType từ cardTypes dựa trên cardTypeId
      const cardType = cardTypes.find(ct => ct.cardTypeId === cap.cardTypeId);
      const displayName = cardType?.name || 'Unknown';
      const description = cardType?.description || '';
      
      return (
        <Tag 
          key={index} 
          color="blue"
          style={{ 
            fontSize: '12px',
            marginBottom: '4px',
            display: 'block',
            textAlign: 'left'
          }}
          title={description}
        >
          {displayName}
        </Tag>
      );
    });
  };

  const formatDate = (dateString) => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('vi-VN');
  };

  const filteredCards = cards.filter(card => {
    // Search trong cardNumber và capabilities
    const capabilities = card.capabilities || [];
    const capabilityNames = capabilities.map(cap => {
      const cardType = cardTypes.find(ct => ct.cardTypeId === cap.cardTypeId);
      return cardType?.name || '';
    }).join(' ');
    
    const matchesSearch = card.cardNumber?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         capabilityNames.toLowerCase().includes(searchTerm.toLowerCase());
    
    const matchesStatus = statusFilter === 'all' || 
                         (statusFilter === 'Hoạt động' && (card.status === 'ACTIVE' || card.status === 'Hoạt động')) ||
                         (statusFilter === 'Không hoạt động' && (card.status === 'INACTIVE' || card.status === 'Không hoạt động')) ||
                         (statusFilter === 'Hết hạn' && (card.status === 'EXPIRED' || card.status === 'Hết hạn')) ||
                         (statusFilter === 'Mất thẻ' && (card.status === 'LOST' || card.status === 'Mất thẻ'));
    
    // Filter by card type - check if any capability matches
    const matchesCardType = cardTypeFilter === 'all' || 
                           capabilities.some(cap => {
                             const cardType = cardTypes.find(ct => ct.cardTypeId === cap.cardTypeId);
                             return cardType?.name === cardTypeFilter;
                           });
    
    return matchesSearch && matchesStatus && matchesCardType;
  }).sort((a, b) => {
    // Ưu tiên thẻ mới được thêm lên đầu
    const aIsNew = newlyAddedCardIds.includes(a.cardId);
    const bIsNew = newlyAddedCardIds.includes(b.cardId);
    
    if (aIsNew && bIsNew) {
      const aIndex = newlyAddedCardIds.indexOf(a.cardId);
      const bIndex = newlyAddedCardIds.indexOf(b.cardId);
      return bIndex - aIndex; // Mới nhất trước
    }
    
    if (aIsNew && !bIsNew) return -1;
    if (!aIsNew && bIsNew) return 1;
    
    // Sắp xếp theo thứ tự mới nhất cho thẻ cũ
    if (a.createdAt && b.createdAt) {
      return new Date(b.createdAt) - new Date(a.createdAt);
    }
    
    // Nếu không có createdAt, dùng issuedDate
    if (a.issuedDate && b.issuedDate) {
      return new Date(b.issuedDate) - new Date(a.issuedDate);
    }
    
    // Cuối cùng, sắp xếp theo cardNumber
    if (a.cardNumber && b.cardNumber) {
      return b.cardNumber.localeCompare(a.cardNumber);
    }
    
    return 0;
  });

  const handleShowCreateModal = () => {
    setShowCreateModal(true);
  };

  const handleHideCreateModal = () => {
    setShowCreateModal(false);
  };

  const handleCreateSuccess = async (newCardId) => {
    try {
      setLoading(true);
      const data = await cardsApi.getAll();
      
      // Tối ưu: Thêm thẻ mới vào đầu danh sách thay vì sort
      if (newCardId) {
        const newCard = data.find(card => card.cardId === newCardId);
        if (newCard) {
          // Thêm vào đầu danh sách
          setCards(prev => [newCard, ...prev.filter(c => c.cardId !== newCardId)]);
          // Thêm vào danh sách tracking
          setNewlyAddedCardIds(prev => [...prev, newCardId]);
        } else {
          setCards(data);
        }
      } else {
        setCards(data);
      }
      
      // Refresh capabilities
      const capabilitiesPromises = data.map(async (card) => {
        const capabilities = await cardsApi.getCardCapabilities(card.cardId);
        return { cardId: card.cardId, capabilities };
      });
      const capabilitiesResults = await Promise.all(capabilitiesPromises);
      const capabilitiesMap = {};
      capabilitiesResults.forEach(({ cardId, capabilities }) => {
        capabilitiesMap[cardId] = capabilities;
      });
      setCardCapabilities(capabilitiesMap);
      
      // Tính toán stats
      const calculatedStats = {
        total: data.length,
        active: data.filter(c => c.status === 'ACTIVE' || c.status === 'Hoạt động').length,
        inactive: data.filter(c => c.status === 'INACTIVE' || c.status === 'Không hoạt động').length,
        expired: data.filter(c => c.status === 'EXPIRED' || c.status === 'Hết hạn').length,
        lost: data.filter(c => c.status === 'LOST' || c.status === 'Mất thẻ').length
      };
      setStats(calculatedStats);
      
    } catch (err) {
      fetchData(); // Fallback to full refresh
    } finally {
      setLoading(false);
    }
  };

  const handleShowUpdateModal = (card) => {
    setSelectedCard(card);
    setShowUpdateModal(true);
  };

  const handleHideUpdateModal = () => {
    setShowUpdateModal(false);
    setSelectedCard(null);
  };

  const handleShowHistoryModal = (card) => {
    setSelectedCard(card);
    setShowHistoryModal(true);
  };

  const handleHideHistoryModal = () => {
    setShowHistoryModal(false);
    setSelectedCard(null);
  };

  const handleUpdateSuccess = async (updatedCardId) => {
    try {
      setLoading(true);
      const data = await cardsApi.getAll();
      
      // Cập nhật thẻ trong danh sách
      if (updatedCardId) {
        const updatedCard = data.find(card => card.cardId === updatedCardId);
        if (updatedCard) {
          setCards(prev => prev.map(card => 
            card.cardId === updatedCardId ? updatedCard : card
          ));
        } else {
          setCards(data);
        }
      } else {
        setCards(data);
      }
      
      // Refresh capabilities
      const capabilitiesPromises = data.map(async (card) => {
        const capabilities = await cardsApi.getCardCapabilities(card.cardId);
        return { cardId: card.cardId, capabilities };
      });
      const capabilitiesResults = await Promise.all(capabilitiesPromises);
      const capabilitiesMap = {};
      capabilitiesResults.forEach(({ cardId, capabilities }) => {
        capabilitiesMap[cardId] = capabilities;
      });
      setCardCapabilities(capabilitiesMap);
      
      // Tính toán stats
      const calculatedStats = {
        total: data.length,
        active: data.filter(c => c.status === 'ACTIVE' || c.status === 'Hoạt động').length,
        inactive: data.filter(c => c.status === 'INACTIVE' || c.status === 'Không hoạt động').length,
        expired: data.filter(c => c.status === 'EXPIRED' || c.status === 'Hết hạn').length,
        lost: data.filter(c => c.status === 'LOST' || c.status === 'Mất thẻ').length
      };
      setStats(calculatedStats);
      
    } catch (err) {
      fetchData(); // Fallback to full refresh
    } finally {
      setLoading(false);
    }
  };

  const handleShowToast = (message, type = 'success') => {
    showNotification(type, type === 'success' ? 'Thành công' : 'Lỗi', message);
  };

  const handleShowErrorToast = (message) => {
    showNotification('error', 'Lỗi', message);
  };

  const handleDeleteCard = async (card) => {
    try {
      setLoading(true);
      await cardsApi.softDelete(card.cardId);
      
      // Cập nhật danh sách cards (loại bỏ card đã xóa)
      setCards(prevCards => prevCards.filter(c => c.cardId !== card.cardId));
      
      // Xóa khỏi danh sách tracking
      setNewlyAddedCardIds(prev => prev.filter(id => id !== card.cardId));
      
      showNotification('success', 'Thành công', 'Đã xóa thẻ thành công');
      
      // Refresh để cập nhật stats
      fetchData();
    } catch (error) {
      const errorMessage = error.response?.data?.message || 'Có lỗi xảy ra khi xóa thẻ. Vui lòng kiểm tra Backend endpoint.';
      showNotification('error', 'Lỗi', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  // Sử dụng stats từ API thay vì tính toán local
  const uniqueCardTypes = cardTypes.map(type => type.name);

  // Render loading
  if (loading && cards.length === 0) {
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
          <Alert 
            message="Lỗi" 
            description={error} 
            type="error" 
            showIcon 
            action={
              <Button type="primary" onClick={fetchCards}>
                Thử lại
              </Button>
            }
          />
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
                  Quản lý thẻ
                </Title>
                <Text type="secondary">
                  Quản lý các thẻ ra vào của tòa nhà
                </Text>
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
                  placeholder="Tìm kiếm theo số thẻ, loại thẻ..."
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
                  <Option value="Hoạt động">Hoạt động</Option>
                  <Option value="Không hoạt động">Không hoạt động</Option>
                  <Option value="Hết hạn">Hết hạn</Option>
                  <Option value="Mất thẻ">Mất thẻ</Option>
                </Select>
              </Col>
              <Col xs={24} sm={12} md={5}>
                <Select 
                  value={cardTypeFilter} 
                  onChange={setCardTypeFilter}
                  style={{ width: '100%' }}
                  placeholder="Loại thẻ"
                >
                  <Option value="all">Tất cả loại thẻ</Option>
                  {uniqueCardTypes.map(type => (
                    <Option key={type} value={type}>{type}</Option>
                  ))}
                </Select>
              </Col>
              <Col xs={24} sm={12} md={8} style={{ display: 'flex', justifyContent: 'flex-end' }}>
                <Button 
                  type="primary"
                  onClick={handleShowCreateModal}
                  size="large"
                >
                  Thêm thẻ mới
                </Button>
              </Col>
            </Row>
          </Card>

          {/* Stats Cards */}
          <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
            <Col xs={12} sm={8} md={5}>
              <Card>
                <Statistic
                  title="Tổng thẻ"
                  value={stats.total}
                  valueStyle={{ color: '#3f8600' }}
                />
              </Card>
            </Col>
            <Col xs={12} sm={8} md={5}>
              <Card>
                <Statistic
                  title="Đang hoạt động"
                  value={stats.active}
                  prefix={<Badge status="success" />}
                  valueStyle={{ color: '#52c41a' }}
                />
              </Card>
            </Col>
            <Col xs={12} sm={8} md={5}>
              <Card>
                <Statistic
                  title="Không hoạt động"
                  value={stats.inactive}
                  prefix={<Badge status="default" />}
                  valueStyle={{ color: '#8c8c8c' }}
                />
              </Card>
            </Col>
            <Col xs={12} sm={8} md={5}>
              <Card>
                <Statistic
                  title="Hết hạn"
                  value={stats.expired}
                  prefix={<Badge status="warning" />}
                  valueStyle={{ color: '#faad14' }}
                />
              </Card>
            </Col>
            <Col xs={12} sm={8} md={4}>
              <Card>
                <Statistic
                  title="Mất thẻ"
                  value={stats.lost}
                  prefix={<Badge status="error" />}
                  valueStyle={{ color: '#f5222d' }}
                />
              </Card>
            </Col>
          </Row>

          {/* Cards Table */}
          <Card
            bodyStyle={{ padding: 0 }}
          >
            <Table
              columns={[
                {
                  title: 'Số thẻ',
                  dataIndex: 'cardNumber',
                  key: 'cardNumber',
                  render: (text) => (
                    <Text strong>{text}</Text>
                  ),
                },
                {
                  title: 'Chức năng thẻ',
                  key: 'capabilities',
                  render: (_, record) => (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
                      {getCardCapabilitiesBadges(record.cardId)}
                    </div>
                  ),
                },
                {
                  title: 'Số căn hộ',
                  dataIndex: 'issuedToApartmentNumber',
                  key: 'apartmentNumber',
                  render: (text) => (
                    <Text type="secondary">{text || 'N/A'}</Text>
                  ),
                },
                {
                  title: 'Tên chủ thẻ',
                  dataIndex: 'issuedToUserName',
                  key: 'ownerName',
                  render: (text) => (
                    <Text>{text || 'N/A'}</Text>
                  ),
                },
                {
                  title: 'Ngày cấp',
                  dataIndex: 'issuedDate',
                  key: 'issuedDate',
                  render: (date) => (
                    <Text type="secondary">{formatDate(date)}</Text>
                  ),
                },
                {
                  title: 'Ngày hết hạn',
                  dataIndex: 'expiredDate',
                  key: 'expiredDate',
                  render: (date) => (
                    <Text type="secondary">{formatDate(date)}</Text>
                  ),
                },
                {
                  title: 'Trạng thái',
                  dataIndex: 'status',
                  key: 'status',
                  render: (status) => getStatusBadge(status),
                },
                {
                  title: 'Thao tác',
                  key: 'action',
                  render: (_, record) => (
                    <Space>
                      <Tooltip title="Chỉnh sửa">
                        <Button
                          type="text"
                          icon={<EditOutlined />}
                          onClick={() => handleShowUpdateModal(record)}
                          size="small"
                        />
                      </Tooltip>
                      <Tooltip title="Xem lịch sử">
                        <Button
                          type="text"
                          icon={<HistoryOutlined />}
                          onClick={() => handleShowHistoryModal(record)}
                          size="small"
                        />
                      </Tooltip>
                      <Popconfirm
                        title="Xóa thẻ"
                        description="Bạn có chắc muốn xóa thẻ này không?"
                        onConfirm={() => handleDeleteCard(record)}
                        okText="Có"
                        cancelText="Không"
                      >
                        <Tooltip title="Xóa thẻ">
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
                }
              ]}
              dataSource={filteredCards}
              rowKey={(record) => record.cardId}
              loading={loading}
              pagination={{
                pageSize: 10,
                showSizeChanger: true,
                showQuickJumper: true,
                showTotal: (total, range) => 
                  `${range[0]}-${range[1]} của ${total} thẻ`,
              }}
              locale={{
                emptyText: (
                  <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description="Chưa có thẻ nào"
                  />
                )
              }}
              scroll={{ x: 800 }}
            />
          </Card>



          {/* Create Card Modal */}
          <CreateCard
            show={showCreateModal}
            onHide={handleHideCreateModal}
            onSuccess={handleCreateSuccess}
            onShowToast={handleShowToast}
            onShowErrorToast={handleShowErrorToast}
            existingCards={cards}
          />

          {/* Update Card Modal */}
          <UpdateCard
            show={showUpdateModal}
            onHide={handleHideUpdateModal}
            onSuccess={handleUpdateSuccess}
            onShowToast={handleShowToast}
            card={selectedCard}
            existingCards={cards}
          />

          {/* History Modal */}
          <HistoryCard
            show={showHistoryModal}
            onHide={handleHideHistoryModal}
            card={selectedCard}
          />
        </Content>
      </Layout>
    </App>
  );
}
