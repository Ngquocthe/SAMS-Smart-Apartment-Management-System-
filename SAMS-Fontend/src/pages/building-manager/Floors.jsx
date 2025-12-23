import React, { useState, useEffect } from "react";
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
  Popconfirm,
  Badge,
  Empty,
  Flex,
  Select,
  App,
} from "antd";
import {
  EditOutlined,
  DeleteOutlined,
  ReloadOutlined,
  SearchOutlined,
  BuildOutlined,
  HomeOutlined,
  PlusOutlined,
} from "@ant-design/icons";
import floorApi, { FloorType, getFloorTypeLabel, getFloorTypeColor } from "../../features/building-management/floorApi";
import useNotification from "../../hooks/useNotification";

const { Title, Text } = Typography;
const { Content } = Layout;

export default function Floors() {
  const [floors, setFloors] = useState([]);
  const [loading, setLoading] = useState(false);
  const [modalLoading, setModalLoading] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showCreateMultipleModal, setShowCreateMultipleModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingFloor, setEditingFloor] = useState(null);
  const [searchTerm, setSearchTerm] = useState("");

  // Form instances
  const [createSingleForm] = Form.useForm();
  const [createMultipleForm] = Form.useForm();
  const [editForm] = Form.useForm();

  // Exclude floors state
  const [excludeFloors, setExcludeFloors] = useState([]);
  const [excludeFloorInput, setExcludeFloorInput] = useState("");

  // Use custom notification hook (must be before useEffect)
  const { showMessage, showNotification } = useNotification();

  useEffect(() => {
    fetchFloors();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchFloors = async (showSuccess = false) => {
    setLoading(true);
    try {
      const floorsData = await floorApi.getAll();
      setFloors(floorsData);
      if (showSuccess) {
        showNotification(
          "success",
          "Thành công",
          "Tải dữ liệu thành công"
        );
      }
    } catch (error) {
      showNotification(
        "error",
        "Tải dữ liệu thất bại",
        error.response?.data?.message || error.message || "Không thể tải danh sách tầng"
      );
      setFloors([]);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateSingle = async (values) => {
    setModalLoading(true);
    try {
      await floorApi.createSingle({
        floorNumber: values.floorNumber,
        floorType: values.floorType,
        name: values.name || null,
      });

      showNotification(
        "success",
        "Thành công",
        "Tạo tầng thành công"
      );
      setShowCreateModal(false);
      createSingleForm.resetFields();
      fetchFloors();
    } catch (error) {
      showNotification(
        "error",
        "Tạo tầng thất bại",
        error.response?.data?.message || error.message || "Không thể tạo tầng"
      );
    } finally {
      setModalLoading(false);
    }
  };

  const handleCreateMultiple = async (values) => {
    setModalLoading(true);
    try {
      const result = await floorApi.createFloors({
        floorType: values.floorType,
        count: values.count,
        startFloor: values.startFloor || 1,
        excludeFloors: excludeFloors,
      });

      showNotification(
        "success",
        "Thành công",
        "Tạo tầng thành công"
      );
      setShowCreateMultipleModal(false);
      createMultipleForm.resetFields();
      setExcludeFloors([]);
      setExcludeFloorInput("");
      fetchFloors();
    } catch (error) {
      showNotification(
        "error",
        "Tạo tầng thất bại",
        error.response?.data?.message || error.message || "Không thể tạo các tầng"
      );
    } finally {
      setModalLoading(false);
    }
  };

  const handleUpdateName = async (values) => {
    setModalLoading(true);
    try {
      // Validate input
      if (!values.name || !values.name.trim()) {
        showNotification("error", "Dữ liệu không hợp lệ", "Tên tầng không được để trống");
        setModalLoading(false);
        return;
      }

      await floorApi.update(editingFloor.floorId, {
        name: values.name.trim(),
        floorType: values.floorType || editingFloor.floorType,
      });

      showNotification(
        "success",
        "Thành công",
        "Cập nhật tầng thành công"
      );
      setShowEditModal(false);
      setEditingFloor(null);
      editForm.resetFields();
      fetchFloors();
    } catch (error) {
      console.error("Update error:", error);
      console.error("Error response:", error.response?.data);
      showNotification(
        "error",
        "Cập nhật tầng thất bại",
        error.response?.data?.message || error.message || "Không thể cập nhật tầng"
      );
    } finally {
      setModalLoading(false);
    }
  };

  const openEditModal = (floor) => {
    setEditingFloor(floor);
    editForm.setFieldsValue({
      name: floor.name || "",
      floorType: floor.floorType,
    });
    setShowEditModal(true);
  };

  const handleDeleteFloor = async (floorId) => {
    try {
      await floorApi.delete(floorId);
      showNotification("success", "Thành công", "Xóa tầng thành công");
      fetchFloors();
    } catch (error) {
      showNotification(
        "error",
        "Xóa tầng thất bại",
        error.response?.data?.message || error.message || "Không thể xóa tầng"
      );
    }
  };

  const addExcludeFloor = () => {
    const floorNum = parseInt(excludeFloorInput);
    if (floorNum && !excludeFloors.includes(floorNum)) {
      setExcludeFloors([...excludeFloors, floorNum]);
      setExcludeFloorInput("");
    }
  };

  const removeExcludeFloor = (floorNum) => {
    setExcludeFloors(excludeFloors.filter((f) => f !== floorNum));
  };

  const filteredFloors = floors.filter(
    (floor) =>
      floor.floorNumber.toString().includes(searchTerm) ||
      (floor.name &&
        floor.name.toLowerCase().includes(searchTerm.toLowerCase()))
  );

  const columns = [
    {
      title: "Số tầng",
      dataIndex: "floorNumber",
      key: "floorNumber",
      sorter: (a, b) => a.floorNumber - b.floorNumber,
      render: (floorNumber) => (
        <Tag color="blue" style={{ fontSize: "14px", padding: "4px 8px" }}>
          Tầng {floorNumber}
        </Tag>
      ),
    },
    {
      title: "Tên tầng",
      dataIndex: "name",
      key: "name",
      render: (name) => (
        <Text>{name || <Text type="secondary">Chưa có tên</Text>}</Text>
      ),
    },
    {
      title: "Loại tầng",
      dataIndex: "floorType",
      key: "floorType",
      render: (floorType) => (
        <Tag color={getFloorTypeColor(floorType)}>
          {getFloorTypeLabel(floorType)}
        </Tag>
      ),
    },
    {
      title: "Số căn hộ",
      dataIndex: "apartmentCount",
      key: "apartmentCount",
      render: (count) => (
        <Badge count={count} showZero style={{ backgroundColor: "#52c41a" }} />
      ),
    },
    {
      title: "Thao tác",
      key: "action",
      render: (_, record) => (
        <Space>
          <Tooltip title="Chỉnh sửa tên">
            <Button
              type="text"
              icon={<EditOutlined />}
              onClick={() => openEditModal(record)}
              size="small"
            />
          </Tooltip>
          <Popconfirm
            title="Xóa tầng"
            description="Bạn có chắc muốn xóa tầng này không?"
            onConfirm={() => handleDeleteFloor(record.floorId)}
            okText="Có"
            cancelText="Không"
          >
            <Tooltip title="Xóa tầng">
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
  ];

  return (
    <App>
      <Layout style={{ minHeight: "100vh", background: "#f0f2f5" }}>
        <Content style={{ padding: "24px" }}>
          {/* Header */}
          <div style={{ marginBottom: 24 }}>
            <Flex
              justify="space-between"
              align="center"
              wrap="wrap"
              gap="middle"
            >
              <div>
                <Title level={2} style={{ margin: 0, marginBottom: 8 }}>
                  Quản lý tầng
                </Title>
              </div>
              <Space>
                <Button
                  type="default"
                  icon={<HomeOutlined />}
                  onClick={() => setShowCreateMultipleModal(true)}
                  size="large"
                >
                  Tạo nhiều tầng
                </Button>
                <Button
                  type="primary"
                  onClick={() => setShowCreateModal(true)}
                  size="large"
                >
                  Tạo tầng mới
                </Button>
              </Space>
            </Flex>
          </div>

          {/* Search & Actions */}
          <Card
            style={{ marginBottom: 24 }}
            bodyStyle={{ padding: "16px 24px" }}
          >
            <Row gutter={[16, 16]} align="middle">
              <Col xs={24} sm={12} md={8}>
                <Input
                  placeholder="Tìm kiếm theo số tầng hoặc tên..."
                  prefix={<SearchOutlined />}
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  allowClear
                />
              </Col>
              <Col xs={24} sm={12} md={16}>
                <Flex justify="end" gap="small" wrap="wrap">
                  <Button
                    icon={<ReloadOutlined />}
                    onClick={() => {
                      setSearchTerm("");
                      fetchFloors(true);
                    }}
                    loading={loading}
                  >
                    Làm mới
                  </Button>
                  <Badge count={filteredFloors.length} showZero>
                    <Button type="default">Tổng số tầng</Button>
                  </Badge>
                </Flex>
              </Col>
            </Row>
          </Card>

          {/* Floors Table */}
          <Card
            title={
              <Flex align="center" gap="small">
                <BuildOutlined />
                <span>Danh sách tầng</span>
              </Flex>
            }
            bodyStyle={{ padding: 0 }}
          >
            <Table
              columns={columns}
              dataSource={filteredFloors}
              rowKey="floorId"
              loading={loading}
              pagination={{
                pageSize: 10,
                showSizeChanger: true,
                showQuickJumper: true,
                showTotal: (total, range) =>
                  `${range[0]}-${range[1]} của ${total} tầng`,
              }}
              locale={{
                emptyText: (
                  <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description="Chưa có tầng nào"
                  />
                ),
              }}
              scroll={{ x: 800 }}
            />
          </Card>

          {/* Create Single Floor Modal */}
          <Modal
            title={
              <Flex align="center" gap="small">
                <span>Tạo tầng mới</span>
              </Flex>
            }
            open={showCreateModal}
            onCancel={() => {
              setShowCreateModal(false);
              createSingleForm.resetFields();
            }}
            footer={null}
            width={500}
          >
            <Form
              form={createSingleForm}
              layout="vertical"
              onFinish={handleCreateSingle}
            >
              <Form.Item
                label="Số tầng"
                name="floorNumber"
                rules={[
                  { required: true, message: "Vui lòng nhập số tầng" },
                  {
                    type: "number",
                    message: "Số tầng phải là số",
                  },
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      const floorType = getFieldValue('floorType');
                      if (value == null) return Promise.resolve();
                      
                      // Tầng hầm: -5 đến -1
                      if (floorType === FloorType.BASEMENT) {
                        if (value >= -5 && value <= -1) {
                          return Promise.resolve();
                        }
                        return Promise.reject(new Error('Tầng hầm phải từ -5 đến -1'));
                      }
                      
                      // Các tầng khác: 1 đến 80
                      if (value >= 1 && value <= 80) {
                        return Promise.resolve();
                      }
                      return Promise.reject(new Error('Số tầng phải từ 1 đến 80'));
                    },
                  }),
                ]}
                dependencies={['floorType']}
              >
                <InputNumber
                  placeholder="Nhập số tầng (âm cho tầng hầm)"
                  style={{ width: "100%" }}
                />
              </Form.Item>

              <Form.Item
                label="Loại tầng"
                name="floorType"
                rules={[
                  { required: true, message: "Vui lòng chọn loại tầng" },
                ]}
              >
                <Select placeholder="Chọn loại tầng">
                  <Select.Option value={FloorType.BASEMENT}>
                    {getFloorTypeLabel(FloorType.BASEMENT)}
                  </Select.Option>
                  <Select.Option value={FloorType.COMMERCIAL}>
                    {getFloorTypeLabel(FloorType.COMMERCIAL)}
                  </Select.Option>
                  <Select.Option value={FloorType.AMENITY}>
                    {getFloorTypeLabel(FloorType.AMENITY)}
                  </Select.Option>
                  <Select.Option value={FloorType.SERVICE}>
                    {getFloorTypeLabel(FloorType.SERVICE)}
                  </Select.Option>
                  <Select.Option value={FloorType.RESIDENTIAL}>
                    {getFloorTypeLabel(FloorType.RESIDENTIAL)}
                  </Select.Option>
                </Select>
              </Form.Item>

              <Form.Item
                label="Tên tầng (tùy chọn)"
                name="name"
              >
                <Input placeholder="Nhập tên tầng" />
              </Form.Item>

              <Form.Item style={{ marginBottom: 0, textAlign: "right" }}>
                <Space>
                  <Button onClick={() => setShowCreateModal(false)}>Hủy</Button>
                  <Button
                    type="primary"
                    htmlType="submit"
                    loading={modalLoading}
                  >
                    Tạo tầng
                  </Button>
                </Space>
              </Form.Item>
            </Form>
          </Modal>

          {/* Create Multiple Floors Modal */}
          <Modal
            title={
              <Flex align="center" gap="small">
                <HomeOutlined />
                <span>Tạo nhiều tầng</span>
              </Flex>
            }
            open={showCreateMultipleModal}
            onCancel={() => {
              setShowCreateMultipleModal(false);
              createMultipleForm.resetFields();
              setExcludeFloors([]);
              setExcludeFloorInput("");
            }}
            footer={null}
            width={600}
          >
            <Form
              form={createMultipleForm}
              layout="vertical"
              onFinish={handleCreateMultiple}
            >
              <Form.Item
                label="Loại tầng"
                name="floorType"
                rules={[
                  { required: true, message: "Vui lòng chọn loại tầng" },
                ]}
              >
                <Select placeholder="Chọn loại tầng">
                  <Select.Option value={FloorType.BASEMENT}>
                    {getFloorTypeLabel(FloorType.BASEMENT)}
                  </Select.Option>
                  <Select.Option value={FloorType.COMMERCIAL}>
                    {getFloorTypeLabel(FloorType.COMMERCIAL)}
                  </Select.Option>
                  <Select.Option value={FloorType.AMENITY}>
                    {getFloorTypeLabel(FloorType.AMENITY)}
                  </Select.Option>
                  <Select.Option value={FloorType.SERVICE}>
                    {getFloorTypeLabel(FloorType.SERVICE)}
                  </Select.Option>
                  <Select.Option value={FloorType.RESIDENTIAL}>
                    {getFloorTypeLabel(FloorType.RESIDENTIAL)}
                  </Select.Option>
                </Select>
              </Form.Item>

              <Form.Item
                label="Số lượng tầng"
                name="count"
                rules={[
                  { required: true, message: "Vui lòng nhập số lượng tầng" },
                  {
                    type: "number",
                    min: 1,
                    message: "Số lượng phải lớn hơn 0",
                  },
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      const floorType = getFieldValue('floorType');
                      if (value == null) return Promise.resolve();
                      
                      // Tầng hầm: max 5 tầng
                      if (floorType === FloorType.BASEMENT) {
                        if (value >= 1 && value <= 5) {
                          return Promise.resolve();
                        }
                        return Promise.reject(new Error('Tầng hầm tối đa 5 tầng'));
                      }
                      
                      // Các tầng khác: max 80 tầng
                      if (value >= 1 && value <= 80) {
                        return Promise.resolve();
                      }
                      return Promise.reject(new Error('Số lượng tầng tối đa 80'));
                    },
                  }),
                ]}
                dependencies={['floorType']}
              >
                <InputNumber
                  placeholder="Nhập số lượng tầng cần tạo"
                  style={{ width: "100%" }}
                  min={1}
                />
              </Form.Item>

              <Form.Item
                label="Tầng bắt đầu (tùy chọn)"
                name="startFloor"
                tooltip="Mặc định là 1 nếu không nhập"
                rules={[
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      if (value == null) return Promise.resolve();
                      
                      const floorType = getFieldValue('floorType');
                      // Tầng hầm không dùng startFloor
                      if (floorType === FloorType.BASEMENT) {
                        return Promise.resolve();
                      }
                      
                      // Các tầng khác: 1-80
                      if (value >= 1 && value <= 80) {
                        return Promise.resolve();
                      }
                      return Promise.reject(new Error('Tầng bắt đầu phải từ 1 đến 80'));
                    },
                  }),
                ]}
                dependencies={['floorType']}
              >
                <InputNumber
                  placeholder="Nhập tầng bắt đầu (mặc định: 1)"
                  style={{ width: "100%" }}
                  min={1}
                  max={80}
                />
              </Form.Item>

              <Form.Item label="Loại trừ các tầng">
                <Space.Compact style={{ width: "100%" }}>
                  <Input
                    placeholder="Nhập số tầng muốn loại trừ"
                    value={excludeFloorInput}
                    onChange={(e) => setExcludeFloorInput(e.target.value)}
                    onPressEnter={addExcludeFloor}
                  />
                  <Button
                    type="primary"
                    onClick={addExcludeFloor}
                    disabled={!excludeFloorInput}
                  >
                    Thêm
                  </Button>
                </Space.Compact>

                {excludeFloors.length > 0 && (
                  <div style={{ marginTop: 8 }}>
                    <Text type="secondary" style={{ fontSize: "12px" }}>
                      Các tầng được loại trừ:
                    </Text>
                    <div style={{ marginTop: 4 }}>
                      {excludeFloors.map((floor) => (
                        <Tag
                          key={floor}
                          closable
                          onClose={() => removeExcludeFloor(floor)}
                          style={{ marginBottom: 4 }}
                        >
                          Tầng {floor}
                        </Tag>
                      ))}
                    </div>
                  </div>
                )}
              </Form.Item>

              <Form.Item style={{ marginBottom: 0, textAlign: "right" }}>
                <Space>
                  <Button onClick={() => setShowCreateMultipleModal(false)}>
                    Hủy
                  </Button>
                  <Button
                    type="primary"
                    htmlType="submit"
                    loading={modalLoading}
                  >
                    Tạo các tầng
                  </Button>
                </Space>
              </Form.Item>
            </Form>
          </Modal>

          {/* Edit Floor Modal */}
          <Modal
            title={
              <Flex align="center" gap="small">
                <EditOutlined />
                <span>Chỉnh sửa tầng {editingFloor?.floorNumber}</span>
              </Flex>
            }
            open={showEditModal}
            onCancel={() => {
              setShowEditModal(false);
              setEditingFloor(null);
              editForm.resetFields();
            }}
            footer={null}
            width={500}
          >
            <Form form={editForm} layout="vertical" onFinish={handleUpdateName}>
              <Form.Item
                label="Tên tầng"
                name="name"
                rules={[{ required: true, message: "Vui lòng nhập tên tầng" }]}
              >
                <Input placeholder="Nhập tên tầng" autoFocus />
              </Form.Item>

              <Form.Item
                label="Loại tầng"
                name="floorType"
                rules={[
                  { required: true, message: "Vui lòng chọn loại tầng" },
                ]}
              >
                <Select placeholder="Chọn loại tầng">
                  <Select.Option value={FloorType.BASEMENT}>
                    {getFloorTypeLabel(FloorType.BASEMENT)}
                  </Select.Option>
                  <Select.Option value={FloorType.COMMERCIAL}>
                    {getFloorTypeLabel(FloorType.COMMERCIAL)}
                  </Select.Option>
                  <Select.Option value={FloorType.AMENITY}>
                    {getFloorTypeLabel(FloorType.AMENITY)}
                  </Select.Option>
                  <Select.Option value={FloorType.SERVICE}>
                    {getFloorTypeLabel(FloorType.SERVICE)}
                  </Select.Option>
                  <Select.Option value={FloorType.RESIDENTIAL}>
                    {getFloorTypeLabel(FloorType.RESIDENTIAL)}
                  </Select.Option>
                </Select>
              </Form.Item>

              <Form.Item style={{ marginBottom: 0, textAlign: "right" }}>
                <Space>
                  <Button
                    onClick={() => {
                      setShowEditModal(false);
                      setEditingFloor(null);
                      editForm.resetFields();
                    }}
                  >
                    Hủy
                  </Button>
                  <Button
                    type="primary"
                    htmlType="submit"
                    loading={modalLoading}
                  >
                    Cập nhật
                  </Button>
                </Space>
              </Form.Item>
            </Form>
          </Modal>
        </Content>
      </Layout>
    </App>
  );
}
