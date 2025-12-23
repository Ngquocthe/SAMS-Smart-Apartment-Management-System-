import React, { useState, useEffect } from "react";
import { Modal, Form, Select, Input, message, Spin, Tag, Space, Descriptions } from "antd";
import { CarOutlined, InfoCircleOutlined } from "@ant-design/icons";
import { getMyVehicles, cancelVehicleRegistration, getVehicleStatusText, getVehicleStatusColor } from "../../features/vehicle/vehicleApi";

const { Option } = Select;
const { TextArea } = Input;

const VehicleCancellationModal = ({ visible, onClose, onSuccess }) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [vehicles, setVehicles] = useState([]);
  const [selectedVehicle, setSelectedVehicle] = useState(null);
  const [loadingVehicles, setLoadingVehicles] = useState(false);

  useEffect(() => {
    if (visible) {
      fetchVehicles();
      form.resetFields();
      setSelectedVehicle(null);
    }
  }, [visible, form]);

  const fetchVehicles = async () => {
    try {
      setLoadingVehicles(true);
      const data = await getMyVehicles();
      // Chỉ hiển thị xe đang ACTIVE
      const activeVehicles = data.filter(v => v.status === "ACTIVE");
      setVehicles(activeVehicles);
      
      if (activeVehicles.length === 0) {
        message.warning("Bạn không có xe nào đang hoạt động để hủy đăng ký");
      }
    } catch (error) {
      console.error("Error fetching vehicles:", error);
      message.error(error.errorMessage || "Không thể tải danh sách xe");
      setVehicles([]);
    } finally {
      setLoadingVehicles(false);
    }
  };

  const handleVehicleChange = (vehicleId) => {
    const vehicle = vehicles.find(v => v.vehicleId === vehicleId);
    setSelectedVehicle(vehicle);
    
    // Auto-fill subject
    if (vehicle) {
      form.setFieldsValue({
        subject: `Hủy đăng ký xe ${vehicle.licensePlate}`,
      });
    }
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      setLoading(true);

      const requestData = {
        vehicleId: values.vehicleId,
        subject: values.subject,
        description: values.description || "",
        attachmentFileIds: [], // Không cần file đính kèm
      };

      await cancelVehicleRegistration(requestData);
      
      message.success("Tạo yêu cầu hủy đăng ký xe thành công!");
      form.resetFields();
      setSelectedVehicle(null);
      onSuccess?.();
      onClose();
    } catch (error) {
      console.error("Error canceling vehicle:", error);
      if (error.errorFields) {
        // Validation errors
        return;
      }
      message.error(error.errorMessage || "Không thể tạo yêu cầu hủy đăng ký");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      title={
        <Space>
          <CarOutlined style={{ color: "#ff4d4f" }} />
          <span>Hủy đăng ký xe</span>
        </Space>
      }
      open={visible}
      onCancel={onClose}
      onOk={handleSubmit}
      confirmLoading={loading}
      width={600}
      okText="Gửi yêu cầu"
      cancelText="Hủy"
      okButtonProps={{ danger: true }}
    >
      <Spin spinning={loadingVehicles}>
        <Form
          form={form}
          layout="vertical"
          style={{ marginTop: 20 }}
        >
          <Form.Item
            name="vehicleId"
            label="Chọn xe cần hủy đăng ký"
            rules={[
              { required: true, message: "Vui lòng chọn xe" }
            ]}
          >
            <Select
              placeholder="Chọn xe..."
              onChange={handleVehicleChange}
              showSearch
              optionFilterProp="children"
              size="large"
              disabled={vehicles.length === 0}
            >
              {vehicles.map(vehicle => (
                <Option key={vehicle.vehicleId} value={vehicle.vehicleId}>
                  <Space>
                    <strong>{vehicle.licensePlate}</strong>
                    <span>-</span>
                    <span>{vehicle.brandModel}</span>
                    <Tag color={getVehicleStatusColor(vehicle.status)}>
                      {getVehicleStatusText(vehicle.status)}
                    </Tag>
                  </Space>
                </Option>
              ))}
            </Select>
          </Form.Item>

          {selectedVehicle && (
            <div style={{ marginBottom: 16 }}>
              <Descriptions
                bordered
                size="small"
                column={2}
                title={
                  <Space>
                    <InfoCircleOutlined style={{ color: "#1890ff" }} />
                    <span>Thông tin xe</span>
                  </Space>
                }
              >
                <Descriptions.Item label="Biển số" span={2}>
                  <strong>{selectedVehicle.licensePlate}</strong>
                </Descriptions.Item>
                <Descriptions.Item label="Loại xe">
                  {selectedVehicle.vehicleTypeName}
                </Descriptions.Item>
                <Descriptions.Item label="Màu sắc">
                  {selectedVehicle.color}
                </Descriptions.Item>
                <Descriptions.Item label="Hiệu xe" span={2}>
                  {selectedVehicle.brandModel}
                </Descriptions.Item>
                <Descriptions.Item label="Căn hộ">
                  {selectedVehicle.apartmentNumber}
                </Descriptions.Item>
                <Descriptions.Item label="Thẻ gửi xe">
                  {selectedVehicle.parkingCardNumber || "Chưa có"}
                </Descriptions.Item>
              </Descriptions>
            </div>
          )}

          <Form.Item
            name="subject"
            label="Tiêu đề"
            rules={[
              { required: true, message: "Vui lòng nhập tiêu đề" },
              { max: 200, message: "Tiêu đề tối đa 200 ký tự" }
            ]}
          >
            <Input 
              placeholder="Nhập tiêu đề yêu cầu hủy..." 
              size="large"
            />
          </Form.Item>

          <Form.Item
            name="description"
            label="Lý do hủy đăng ký"
            rules={[
              { required: true, message: "Vui lòng nhập lý do" },
              { min: 10, message: "Lý do tối thiểu 10 ký tự" },
              { max: 1000, message: "Lý do tối đa 1000 ký tự" }
            ]}
          >
            <TextArea 
              rows={4} 
              placeholder="Nhập lý do hủy đăng ký xe (Ví dụ: Đã bán xe, chuyển nhà, ...)"
              showCount
              maxLength={1000}
            />
          </Form.Item>
        </Form>
      </Spin>
    </Modal>
  );
};

export default VehicleCancellationModal;
