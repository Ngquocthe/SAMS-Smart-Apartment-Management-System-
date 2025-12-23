import React, { useState, useEffect, useMemo } from 'react';
import { 
  Layout, 
  Card, 
  Button, 
  Modal, 
  Form, 
  Input, 
  InputNumber,
  Table, 
  Space, 
  Typography, 
  Tag, 
  Row, 
  Col,
  Tooltip,
  Divider,
  Select,
  Popconfirm,
  Empty,
  Flex,
  List,
  App
} from 'antd';
import { 
  PlusOutlined, 
  EditOutlined, 
  DeleteOutlined, 
  CopyOutlined,
  TagOutlined,
  SearchOutlined
} from '@ant-design/icons';
import api from '../../lib/apiClient';
import floorApi from '../../features/building-management/floorApi';
import useNotification from '../../hooks/useNotification';

const { Title, Text } = Typography;
const { Content } = Layout;
const { Option } = Select;

export default function Apartments() {
  const [apartments, setApartments] = useState([]);
  const [floors, setFloors] = useState([]);
  const [, setFloorSummary] = useState([]);
  const [loading, setLoading] = useState(false);
  const [modalLoading, setModalLoading] = useState(false);
  const [selectedFloor, setSelectedFloor] = useState('');
  const [searchKeyword, setSearchKeyword] = useState('');
  // Modal states
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showReplicateModal, setShowReplicateModal] = useState(false);
  const [showRefactorModal, setShowRefactorModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingApartment, setEditingApartment] = useState(null);
  
  // Form instances
  const [createForm] = Form.useForm();
  const [replicateForm] = Form.useForm();
  const [refactorForm] = Form.useForm();
  const [editForm] = Form.useForm();
  
  // Create apartment form state
  const [apartmentsList, setApartmentsList] = useState([]);
  const [newApartment, setNewApartment] = useState({
    number: '',
    areaM2: '',
    bedrooms: '',
    type: '',
    image: ''
  });

  // Use custom notification hook (must be before useEffect)
  const { showMessage, showNotification } = useNotification();

  useEffect(() => {
    fetchApartments();
    fetchFloors();
    fetchFloorSummary();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchApartments = async (showSuccess = false) => {
    setLoading(true);
    try {
      const response = await api.get('/Apartment');
      const apartmentsData = Array.isArray(response.data) ? response.data : [];
      setApartments(apartmentsData);
      if (showSuccess) {
        showNotification('success', 'Thành công', 'Tải dữ liệu thành công');
      }
    } catch (error) {
      showNotification('error', 'Lỗi', 'Không thể tải danh sách căn hộ: ' + (error.response?.data?.message || error.message));
      setApartments([]);
    } finally {
      setLoading(false);
    }
  };

  const fetchFloors = async (showSuccess = false) => {
    try {
      const floorsData = await floorApi.getAll();
      setFloors(floorsData);
      if (showSuccess) {
        showNotification('success', 'Thành công', 'Tải dữ liệu thành công');
      }
    } catch (error) {
      showNotification('error', 'Lỗi', 'Không thể tải danh sách tầng: ' + (error.response?.data?.message || error.message));
      console.error(' Error fetching floors:', error);
    }
  };

  const fetchFloorSummary = async (showSuccess = false) => {
    try {
      const response = await api.get('/Apartment/summary');
      console.log('📊 Floor summary response:', response.data);
      
      const summaryData = Array.isArray(response.data) ? response.data : [];
      
      // Normalize data - API trả về apartmentCount
      const normalizedData = summaryData.map(floor => ({
        floorNumber: floor.floorNumber || floor.FloorNumber,
        floorName: floor.floorName || floor.FloorName,
        totalApartments: floor.apartmentCount || floor.ApartmentCount || 0,
        hasApartments: floor.hasApartments || floor.HasApartments || false
      }));
      
      console.log('📊 Normalized floor summary:', normalizedData);
      setFloorSummary(normalizedData);
      
      if (showSuccess) {
        showNotification('success', 'Thành công', 'Tải dữ liệu thành công');
      }
    } catch (error) {
      showNotification('error', 'Lỗi', 'Không thể tải tóm tắt tầng: ' + (error.response?.data?.message || error.message));
      console.error('❌ Error fetching floor summary:', error);
    }
  };

  const fetchApartmentsByFloor = async (floorNumber) => {
    setLoading(true);
    try {
      const response = await api.get(`/Apartment/floor/${floorNumber}`);
      const apartmentsData = Array.isArray(response.data) ? response.data : [];
      setApartments(apartmentsData);
      setSelectedFloor(floorNumber);
      showNotification('success', 'Thành công', 'Tải dữ liệu thành công');
    } catch (error) {
      showNotification('error', 'Lỗi', 'Không thể tải căn hộ của tầng: ' + (error.response?.data?.message || error.message));
      setApartments([]);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateApartments = async (values) => {
    if (apartmentsList.length === 0) {
      showMessage('warning', 'Vui lòng thêm ít nhất một căn hộ');
      return;
    }

    try {
      const response = await api.post('/Apartment/create-apartment', {
        BuildingCode: values.buildingCode,
        SourceFloorNumber: parseInt(values.sourceFloorNumber),
        Apartments: apartmentsList
      });

      if (response.data.success) {
        showNotification('success', 'Thành công', 'Tạo căn hộ thành công');
        setShowCreateModal(false);
        createForm.resetFields();
        setApartmentsList([]);
        fetchApartments();
        fetchFloorSummary();
      } else {
        showNotification('error', 'Lỗi', response.data.message || 'Không thể tạo căn hộ');
      }
    } catch (error) {
      showNotification('error', 'Lỗi', 'Lỗi khi tạo căn hộ: ' + (error.response?.data?.message || error.message));
    }
  };

  const handleReplicateApartments = async (values) => {
    try {
      const response = await api.post('/Apartment/replicate', {
        BuildingCode: values.buildingCode,
        SourceFloorNumber: parseInt(values.sourceFloorNumber),
        TargetFloorNumbers: values.targetFloorNumbers.map(f => parseInt(f))
      });

      if (response.data.success) {
        showNotification('success', 'Thành công', 'Nhân bản căn hộ thành công');
        setShowReplicateModal(false);
        replicateForm.resetFields();
        fetchApartments();
        fetchFloorSummary();
      } else {
        showNotification('error', 'Lỗi', response.data.message || 'Không thể nhân bản căn hộ');
      }
    } catch (error) {
      showNotification('error', 'Lỗi', 'Lỗi khi nhân bản căn hộ: ' + (error.response?.data?.message || error.message));
    }
  };

  const handleRefactorNames = async (values) => {
    try {
      const response = await api.put('/Apartment/refactor-names', {
        NewBuildingCode: values.newBuildingCode,
        FloorNumbers: values.floorNumbers.map(f => parseInt(f)),
        OldPrefix: values.oldPrefix
      });

      if (response.data.success) {
        showNotification('success', 'Thành công', `${response.data.message}. Đã cập nhật ${response.data.totalUpdated} căn hộ.`);
        setShowRefactorModal(false);
        refactorForm.resetFields();
        fetchApartments();
        fetchFloorSummary();
      } else {
        showNotification('error', 'Lỗi', response.data.message || 'Không thể refactor tên căn hộ');
      }
    } catch (error) {
      showNotification('error', 'Lỗi', 'Lỗi khi refactor tên căn hộ: ' + (error.response?.data?.message || error.message));
    }
  };

  const handleUpdateApartment = async (values) => {
    setModalLoading(true);
    try {
      await api.put(`/Apartment/${editingApartment.apartmentId}`, {
        Number: values.number,
        AreaM2: values.areaM2,
        Bedrooms: values.bedrooms,
        Type: values.type,
        Image: values.image,
        Status: values.status
      });
      showNotification('success', 'Thành công', 'Cập nhật căn hộ thành công');
      setShowEditModal(false);
      setEditingApartment(null);
      editForm.resetFields();
      fetchApartments();
      fetchFloorSummary();
    } catch (error) {
      showNotification('error', 'Lỗi', 'Không thể cập nhật căn hộ: ' + (error.response?.data?.message || error.message));
    } finally {
      setModalLoading(false);
    }
  };

  const openEditModal = (apartment) => {
    setEditingApartment(apartment);
    editForm.setFieldsValue({
      number: apartment.number,
      areaM2: apartment.areaM2,
      bedrooms: apartment.bedrooms,
      type: apartment.type,
      image: apartment.image,
      status: apartment.status
    });
    setShowEditModal(true);
  };

  const handleDeleteApartment = async (apartmentId) => {
    try {
      await api.delete(`/Apartment/${apartmentId}`);
      showNotification('success', 'Thành công', 'Xóa căn hộ thành công');
      fetchApartments();
      fetchFloorSummary();
    } catch (error) {
      showNotification('error', 'Lỗi', 'Không thể xóa căn hộ: ' + (error.response?.data?.message || error.message));
    }
  };

  const addApartmentToList = () => {
    if (!newApartment.number) {
      showMessage('warning', 'Vui lòng nhập số căn hộ');
      return;
    }
    if (!newApartment.areaM2 || newApartment.areaM2 < 10 || newApartment.areaM2 > 500) {
      showMessage('warning', 'Diện tích phải từ 10-500 m²');
      return;
    }
    if (!newApartment.bedrooms) {
      showMessage('warning', 'Vui lòng chọn số phòng ngủ');
      return;
    }
    if (!newApartment.type) {
      showMessage('warning', 'Vui lòng chọn loại căn hộ');
      return;
    }

    // Chuẩn hóa số căn hộ: 1-9 thành 01-09
    let normalizedNumber = newApartment.number.toString().trim();
    const numValue = parseInt(normalizedNumber);
    if (!isNaN(numValue) && numValue >= 1 && numValue <= 9) {
      normalizedNumber = numValue.toString().padStart(2, '0');
    }

    // Kiểm tra trùng mã căn hộ trong danh sách chờ
    const isDuplicate = apartmentsList.some(apt => apt.Number === normalizedNumber);
    if (isDuplicate) {
      showMessage('warning', `Căn hộ ${normalizedNumber} đã có trong danh sách chờ`);
      return;
    }

    const apartmentData = {
      Number: normalizedNumber,
      AreaM2: newApartment.areaM2 ? parseFloat(newApartment.areaM2) : null,
      Bedrooms: newApartment.bedrooms ? parseInt(newApartment.bedrooms) : null,
      Type: newApartment.type || null,
      Image: newApartment.image || null,
      Status: 'ACTIVE'
    };

    setApartmentsList([...apartmentsList, apartmentData]);
    setNewApartment({
      number: '',
      areaM2: '',
      bedrooms: '',
      type: '',
      image: ''
    });
  };

  const removeApartmentFromList = (index) => {
    setApartmentsList(apartmentsList.filter((_, i) => i !== index));
  };

  const filteredApartments = useMemo(() => {
    if (!apartments.length && !searchKeyword && !selectedFloor) {
      return [];
    }

    return apartments.filter(apt => {
      const aptFloorValue = apt?.floorNumber ?? apt?.floor?.floorNumber;
      const matchesFloor = selectedFloor === '' || String(aptFloorValue ?? '').trim() === String(selectedFloor).trim();
      const matchesKeyword = searchKeyword === '' ||
        apt.number.toLowerCase().includes(searchKeyword.toLowerCase()) ||
        (apt.ownerInfo?.fullName?.toLowerCase().includes(searchKeyword.toLowerCase())) ||
        (apt.ownerInfo?.phone?.includes(searchKeyword)) ||
        (apt.ownerInfo?.email?.toLowerCase().includes(searchKeyword.toLowerCase()));

      return matchesFloor && matchesKeyword;
    });
  }, [apartments, searchKeyword, selectedFloor]);

  const columns = [
    {
      title: 'Số căn hộ',
      dataIndex: 'number',
      key: 'number',
      render: (number) => (
        <Tag color="blue" style={{ fontSize: '14px', padding: '4px 8px' }}>
          {number}
        </Tag>
      )
    },
    {
      title: 'Tầng',
      dataIndex: 'floorNumber',
      key: 'floorNumber',
      sorter: (a, b) => a.floorNumber - b.floorNumber,
      render: (floorNumber) => (
        <Tag color="green">Tầng {floorNumber}</Tag>
      )
    },
    {
      title: 'Chủ căn hộ',
      dataIndex: 'ownerInfo',
      key: 'ownerInfo',
      render: (ownerInfo) => {
        if (!ownerInfo) {
          return <Text type="secondary">Chưa có chủ</Text>;
        }
        return (
          <div>
            <div style={{ fontWeight: 500, marginBottom: 4 }}>
              {ownerInfo.fullName}
            </div>
            <div style={{ fontSize: '12px', color: '#666' }}>
              {ownerInfo.phone}
            </div>
          </div>
        );
      }
    },
    {
      title: 'Cư dân',
      key: 'residentInfo',
      align: 'center',
      render: (_, record) => (
        <div style={{ textAlign: 'center' }}>
          <Text strong>{record.residentCount || 0}</Text>
          <br />
          <Text type="secondary" style={{ fontSize: '12px' }}>người</Text>
        </div>
      )
    },
    {
      title: 'Phương tiện',
      key: 'vehicleInfo',
      align: 'center',
      render: (_, record) => (
        <div style={{ textAlign: 'center' }}>
          <Text strong>{record.vehicleCount || 0}</Text>
          <br />
          <Text type="secondary" style={{ fontSize: '12px' }}>xe</Text>
        </div>
      )
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status) => {
        const getStatusDisplay = (status) => {
          switch (status) {
            case 'ACTIVE':
              return { text: 'Trống', color: 'success' };
            case 'RENTED':
              return { text: 'Đang cho thuê', color: 'processing' };
            case 'OWNED':
              return { text: 'Đã sở hữu', color: 'warning' };
            case 'MAINTENANCE':
              return { text: 'Bảo trì', color: 'error' };
            case 'INACTIVE':
              return { text: 'Không hoạt động', color: 'default' };
            default:
              return { text: status, color: 'default' };
          }
        };
        
        const { text, color } = getStatusDisplay(status);
        return <Tag color={color}>{text}</Tag>;
      }
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
              onClick={() => openEditModal(record)}
              size="small"
            />
          </Tooltip>
          <Popconfirm
            title="Xóa căn hộ"
            description="Bạn có chắc muốn xóa căn hộ này không?"
            onConfirm={() => handleDeleteApartment(record.apartmentId)}
            okText="Có"
            cancelText="Không"
          >
            <Tooltip title="Xóa">
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
    <App>
      <Layout style={{ minHeight: '100vh', background: '#f0f2f5' }}>
        <Content style={{ padding: '24px' }}>
        {/* Header */}
        <div style={{ marginBottom: 24 }}>
          <Flex justify="space-between" align="center" wrap="wrap" gap="middle">
            <div>
              <Title level={2} style={{ margin: 0, marginBottom: 8 }}>
                Quản lý căn hộ
              </Title>
              <Text type="secondary">
                Quản lý thông tin các căn hộ trong tòa nhà
              </Text>
            </div>
            <Space>
              <Button 
                type="default"
                onClick={() => setShowReplicateModal(true)}
                size="large"
              >
                Nhân bản căn hộ
              </Button>
              <Button 
                type="default"
                onClick={() => setShowRefactorModal(true)}
                size="large"
              >
                Refactor tên
              </Button>
              <Button 
                type="primary"
                onClick={() => setShowCreateModal(true)}
                size="large"
              >
                Tạo căn hộ
              </Button>
            </Space>
          </Flex>
        </div>

        {/* Search and Filter */}
        <Card 
          title={
            <Flex align="center" gap="small">
              <SearchOutlined />
              <span>Tìm kiếm và lọc căn hộ</span>
            </Flex>
          }
          style={{ marginBottom: 24 }}
        >
          <Row gutter={[16, 16]} align="middle">
            <Col xs={24} sm={12} md={8}>
              <Input
                placeholder="Tìm kiếm theo số căn hộ, tên chủ, SĐT, email..."
                prefix={<SearchOutlined />}
                value={searchKeyword}
                onChange={(e) => setSearchKeyword(e.target.value)}
                allowClear
                size="large"
              />
            </Col>
            <Col xs={24} sm={12} md={6}>
              <Select
                placeholder="Chọn tầng"
                value={selectedFloor || undefined}
                onChange={(value) => {
                  setSelectedFloor(value);
                  if (value) {
                    fetchApartmentsByFloor(value);
                  } else {
                    fetchApartments();
                  }
                }}
                allowClear
                showSearch
                size="large"
                style={{ width: '100%' }}
                optionFilterProp="children"
                filterOption={(input, option) =>
                  (option?.label ?? '').toLowerCase().includes(input.toLowerCase())
                }
                dropdownStyle={{ 
                  maxHeight: 256,
                }}
                getPopupContainer={() => document.body}
              >
                {floors.filter(floor => floor.floorType === 'RESIDENTIAL').map(floor => (
                  <Option key={floor.floorId} value={floor.floorNumber.toString()}>
                    Tầng {floor.floorNumber}{floor.name ? ` - ${floor.name}` : ''}
                  </Option>
                ))}
              </Select>
            </Col>
            <Col xs={24} sm={24} md={10}>
              <Button 
                onClick={async () => { 
                  setSelectedFloor(''); 
                  setSearchKeyword('');
                  await Promise.all([
                    fetchApartments(true), 
                    fetchFloors(),
                    fetchFloorSummary()
                  ]);
                }}
                loading={loading}
                size="large"
              >
                Làm mới
              </Button>
            </Col>
          </Row>
        </Card>

        {/* Apartments Table */}
        <Card
          title={
            <Flex align="center" gap="small">
              <span>Danh sách căn hộ {selectedFloor && `- Tầng ${selectedFloor}`}</span>
            </Flex>
          }
          bodyStyle={{ padding: 0 }}
        >
          <Table
            columns={columns}
            dataSource={filteredApartments}
            rowKey="apartmentId"
            loading={loading}
            pagination={{
              pageSize: 10,
              showSizeChanger: true,
              showQuickJumper: true,
              showTotal: (total, range) => 
                `${range[0]}-${range[1]} của ${total} căn hộ`,
            }}
            locale={{
              emptyText: (
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description={selectedFloor ? `Không có căn hộ nào trong tầng ${selectedFloor}` : "Chưa có căn hộ nào"}
                />
              )
            }}
            scroll={{ x: 1000 }}
          />
        </Card>

        {/* Create Apartments Modal */}
        <Modal
          title={
            <Flex align="center" gap="small">
              <span>Tạo căn hộ</span>
            </Flex>
          }
          open={showCreateModal}
          onCancel={() => {
            setShowCreateModal(false);
            createForm.resetFields();
            setApartmentsList([]);
            setNewApartment({
              number: '',
              areaM2: '',
              bedrooms: '',
              type: '',
              image: ''
            });
          }}
          footer={null}
          width={800}
        >
          <Form
            form={createForm}
            layout="vertical"
            onFinish={handleCreateApartments}
          >
            <Row gutter={16}>
              <Col span={12}>
                <Form.Item
                  label="Mã tòa nhà"
                  name="buildingCode"
                  rules={[{ required: true, message: 'Vui lòng nhập mã tòa nhà' }]}
                >
                  <Input placeholder="Nhập mã tòa nhà" />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  label="Tầng"
                  name="sourceFloorNumber"
                  rules={[{ required: true, message: 'Vui lòng chọn tầng' }]}
                >
                  <Select 
                    placeholder="Chọn tầng"
                    showSearch
                    optionFilterProp="children"
                    filterOption={(input, option) =>
                      (option?.children ?? '').toLowerCase().includes(input.toLowerCase())
                    }
                    dropdownStyle={{ maxHeight: 256 }}
                    getPopupContainer={() => document.body}
                  >
                    {floors.filter(floor => floor.floorType === 'RESIDENTIAL').map(floor => (
                      <Option key={floor.floorId} value={floor.floorNumber}>
                        Tầng {floor.floorNumber}{floor.name ? ` - ${floor.name}` : ''}
                      </Option>
                    ))}
                  </Select>
                </Form.Item>
              </Col>
            </Row>

            <Divider>Thêm căn hộ</Divider>

            <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
              <Col xs={24} sm={12} md={6}>
                <Text strong style={{ display: 'block', marginBottom: 4 }}>Số căn hộ *</Text>
                <Input
                  placeholder="Nhập số căn hộ"
                  value={newApartment.number}
                  onChange={(e) => setNewApartment({...newApartment, number: e.target.value})}
                />
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Text strong style={{ display: 'block', marginBottom: 4 }}>Diện tích (m²) *</Text>
                <InputNumber
                  placeholder="10-500"
                  value={newApartment.areaM2}
                  onChange={(value) => setNewApartment({...newApartment, areaM2: value})}
                  min={10}
                  max={500}
                  style={{ width: '100%' }}
                />
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Text strong style={{ display: 'block', marginBottom: 4 }}>Số phòng ngủ *</Text>
                <Select
                  placeholder="Chọn số phòng"
                  value={newApartment.bedrooms}
                  onChange={(value) => setNewApartment({...newApartment, bedrooms: value})}
                  style={{ width: '100%' }}
                >
                  <Option value={1}>1 phòng</Option>
                  <Option value={2}>2 phòng</Option>
                  <Option value={3}>3 phòng</Option>
                  <Option value={4}>4 phòng</Option>
                  <Option value={5}>5 phòng</Option>
                  <Option value={6}>6 phòng</Option>
                  <Option value={7}>7 phòng</Option>
                </Select>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Text strong style={{ display: 'block', marginBottom: 4 }}>Loại căn hộ *</Text>
                <Select
                  placeholder="Chọn loại"
                  value={newApartment.type}
                  onChange={(value) => setNewApartment({...newApartment, type: value})}
                  style={{ width: '100%' }}
                >
                  <Option value="Studio">Studio</Option>
                  <Option value="Standard">Thông thường</Option>
                  <Option value="Duplex">Duplex</Option>
                  <Option value="Penthouse">Penthouse</Option>
                  <Option value="Deluxe">Cao cấp</Option>
                </Select>
              </Col>
            </Row>

            <Row gutter={12}>
              <Col flex="auto"></Col>
              <Col>
                <Button 
                  type="primary" 
                  icon={<PlusOutlined />}
                  onClick={addApartmentToList}
                  disabled={!newApartment.number}
                >
                  Thêm
                </Button>
              </Col>
            </Row>

            {apartmentsList.length > 0 && (
              <List
                size="small"
                header={<Text strong>Danh sách căn hộ ({apartmentsList.length})</Text>}
                dataSource={apartmentsList}
                renderItem={(item, index) => (
                  <List.Item
                    actions={[
                      <Button
                        type="text"
                        danger
                        icon={<DeleteOutlined />}
                        onClick={() => removeApartmentFromList(index)}
                      />
                    ]}
                  >
                    <Space>
                      <Tag color="blue">{item.Number}</Tag>
                      <Text type="secondary">
                        {item.AreaM2}m² - {item.Bedrooms} phòng - {item.Type}
                      </Text>
                    </Space>
                  </List.Item>
                )}
                style={{ 
                  maxHeight: 200, 
                  overflowY: 'auto',
                  border: '1px solid #d9d9d9',
                  borderRadius: '6px',
                  padding: '8px'
                }}
              />
            )}

            <Form.Item style={{ marginBottom: 0, textAlign: 'right', marginTop: 16 }}>
              <Space>
                <Button onClick={() => setShowCreateModal(false)}>
                  Hủy
                </Button>
                <Button type="primary" htmlType="submit">
                  Tạo căn hộ
                </Button>
              </Space>
            </Form.Item>
          </Form>
        </Modal>

        {/* Replicate Apartments Modal */}
        <Modal
          title={
            <Flex align="center" gap="small">
              <CopyOutlined />
              <span>Nhân bản căn hộ</span>
            </Flex>
          }
          open={showReplicateModal}
          onCancel={() => {
            setShowReplicateModal(false);
            replicateForm.resetFields();
          }}
          footer={null}
          width={600}
        >
          <Form
            form={replicateForm}
            layout="vertical"
            onFinish={handleReplicateApartments}
          >
            <Row gutter={16}>
              <Col span={12}>
                <Form.Item
                  label="Mã tòa nhà"
                  name="buildingCode"
                  rules={[{ required: true, message: 'Vui lòng nhập mã tòa nhà' }]}
                >
                  <Input placeholder="Nhập mã tòa nhà" />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  label="Tầng gốc"
                  name="sourceFloorNumber"
                  rules={[{ required: true, message: 'Vui lòng chọn tầng gốc' }]}
                >
                  <Select 
                    placeholder="Chọn tầng gốc"
                    showSearch
                    optionFilterProp="children"
                    filterOption={(input, option) =>
                      (option?.children ?? '').toLowerCase().includes(input.toLowerCase())
                    }
                    dropdownStyle={{ maxHeight: 256 }}
                    getPopupContainer={() => document.body}
                  >
                    {floors.filter(floor => floor.floorType === 'RESIDENTIAL').map(floor => (
                      <Option key={floor.floorId} value={floor.floorNumber}>
                        Tầng {floor.floorNumber}{floor.name ? ` - ${floor.name}` : ''}
                      </Option>
                    ))}
                  </Select>
                </Form.Item>
              </Col>
            </Row>

            <Form.Item
              label="Tầng đích"
              name="targetFloorNumbers"
              rules={[{ required: true, message: 'Vui lòng chọn ít nhất một tầng đích' }]}
            >
              <Select
                mode="multiple"
                placeholder="Chọn các tầng đích"
                style={{ width: '100%' }}
                showSearch
                optionFilterProp="children"
                filterOption={(input, option) =>
                  (option?.children ?? '').toLowerCase().includes(input.toLowerCase())
                }
                dropdownStyle={{ maxHeight: 256 }}
                getPopupContainer={() => document.body}
                maxTagCount="responsive"
              >
                {floors.map(floor => (
                  <Option key={floor.floorId} value={floor.floorNumber}>
                    Tầng {floor.floorNumber}{floor.name ? ` - ${floor.name}` : ''}
                  </Option>
                ))}
              </Select>
            </Form.Item>

            <Form.Item style={{ marginBottom: 0, textAlign: 'right' }}>
              <Space>
                <Button onClick={() => setShowReplicateModal(false)}>
                  Hủy
                </Button>
                <Button type="primary" htmlType="submit">
                  Nhân bản
                </Button>
              </Space>
            </Form.Item>
          </Form>
        </Modal>

        {/* Refactor Names Modal */}
        <Modal
          title={
            <Flex align="center" gap="small">
              <TagOutlined />
              <span>Refactor tên căn hộ</span>
            </Flex>
          }
          open={showRefactorModal}
          onCancel={() => {
            setShowRefactorModal(false);
            refactorForm.resetFields();
          }}
          footer={null}
          width={600}
        >
          <Form
            form={refactorForm}
            layout="vertical"
            onFinish={handleRefactorNames}
          >
            <Form.Item
              label="Mã tòa nhà mới"
              name="newBuildingCode"
              rules={[{ required: true, message: 'Vui lòng nhập mã tòa nhà mới' }]}
            >
              <Input placeholder="Nhập mã tòa nhà mới" />
            </Form.Item>

            <Form.Item
              label="Tiền tố cũ"
              name="oldPrefix"
            >
              <Input placeholder="Ví dụ: A (để trống nếu không có)" />
            </Form.Item>

            <Form.Item
              label="Chọn tầng"
              name="floorNumbers"
              rules={[{ required: true, message: 'Vui lòng chọn ít nhất một tầng' }]}
            >
              <Select
                mode="multiple"
                placeholder="Chọn các tầng cần refactor"
                style={{ width: '100%' }}
                showSearch
                optionFilterProp="children"
                filterOption={(input, option) =>
                  (option?.children ?? '').toLowerCase().includes(input.toLowerCase())
                }
                dropdownStyle={{ maxHeight: 256 }}
                getPopupContainer={() => document.body}
                maxTagCount="responsive"
              >
                {floors.map(floor => (
                  <Option key={floor.floorId} value={floor.floorNumber}>
                    Tầng {floor.floorNumber}{floor.name ? ` - ${floor.name}` : ''}
                  </Option>
                ))}
              </Select>
            </Form.Item>

            <Form.Item style={{ marginBottom: 0, textAlign: 'right' }}>
              <Space>
                <Button onClick={() => setShowRefactorModal(false)}>
                  Hủy
                </Button>
                <Button type="primary" htmlType="submit">
                  Refactor
                </Button>
              </Space>
            </Form.Item>
          </Form>
        </Modal>

        {/* Edit Apartment Modal */}
        <Modal
          title={
            <Flex align="center" gap="small">
              <EditOutlined />
              <span>Chỉnh sửa căn hộ {editingApartment?.number}</span>
            </Flex>
          }
          open={showEditModal}
          onCancel={() => {
            setShowEditModal(false);
            setEditingApartment(null);
            editForm.resetFields();
          }}
          footer={null}
          width={600}
        >
          <Form
            form={editForm}
            layout="vertical"
            onFinish={handleUpdateApartment}
          >
            <Row gutter={16}>
              <Col span={12}>
                <Form.Item
                  label="Số căn hộ"
                  name="number"
                  rules={[{ required: true, message: 'Vui lòng nhập số căn hộ' }]}
                >
                  <Input placeholder="Nhập số căn hộ" />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  label="Diện tích (m²)"
                  name="areaM2"
                  rules={[
                    { required: true, message: 'Vui lòng nhập diện tích' },
                    { type: 'number', min: 10, max: 500, message: 'Diện tích phải từ 10-500 m²' }
                  ]}
                >
                  <InputNumber
                    placeholder="10-500"
                    style={{ width: '100%' }}
                    min={10}
                    max={500}
                  />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col span={12}>
                <Form.Item
                  label="Số phòng ngủ"
                  name="bedrooms"
                  rules={[{ required: true, message: 'Vui lòng chọn số phòng ngủ' }]}
                >
                  <Select
                    placeholder="Chọn số phòng"
                    style={{ width: '100%' }}
                  >
                    <Option value={1}>1 phòng</Option>
                    <Option value={2}>2 phòng</Option>
                    <Option value={3}>3 phòng</Option>
                    <Option value={4}>4 phòng</Option>
                    <Option value={5}>5 phòng</Option>
                    <Option value={6}>6 phòng</Option>
                    <Option value={7}>7 phòng</Option>
                  </Select>
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  label="Loại căn hộ"
                  name="type"
                  rules={[{ required: true, message: 'Vui lòng chọn loại căn hộ' }]}
                >
                  <Select
                    placeholder="Chọn loại căn hộ"
                    style={{ width: '100%' }}
                  >
                    <Option value="Studio">Studio</Option>
                    <Option value="Standard">Thông thường</Option>
                    <Option value="Duplex">Duplex</Option>
                    <Option value="Penthouse">Penthouse</Option>
                    <Option value="Deluxe">Cao cấp</Option>
                  </Select>
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col span={12}>
                <Form.Item
                  label="Trạng thái"
                  name="status"
                  rules={[{ required: true, message: 'Vui lòng chọn trạng thái' }]}
                >
                  <Select 
                    placeholder="Chọn trạng thái"
                    dropdownStyle={{ maxHeight: 256 }}
                    getPopupContainer={() => document.body}
                  >
                    <Option value="ACTIVE">Trống</Option>
                    <Option value="RENTED">Đang cho thuê</Option>
                    <Option value="OWNED">Đã sở hữu</Option>
                    <Option value="MAINTENANCE">Bảo trì</Option>
                    <Option value="INACTIVE">Không hoạt động</Option>
                  </Select>
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  label="Hình ảnh"
                  name="image"
                >
                  <Input placeholder="URL hình ảnh" />
                </Form.Item>
              </Col>
            </Row>

            <Form.Item style={{ marginBottom: 0, textAlign: 'right' }}>
              <Space>
                <Button onClick={() => {
                  setShowEditModal(false);
                  setEditingApartment(null);
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

        <style jsx>{`
          .ant-card-selected {
            border: 2px solid #1890ff !important;
            box-shadow: 0 0 0 2px rgba(24, 144, 255, 0.2) !important;
          }
        `}</style>
      </Content>
    </Layout>
    </App>
  );
}