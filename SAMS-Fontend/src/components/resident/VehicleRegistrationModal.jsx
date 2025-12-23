import React, { useState, useEffect } from 'react';
import {
  Modal,
  Form,
  Input,
  Select,
  Upload,
  Button,
  Space,
  message,
  Row,
  Col,
  Typography,
  Image
} from 'antd';
import {
  UploadOutlined,
  DeleteOutlined,
  EyeOutlined,
  CarOutlined
} from '@ant-design/icons';
import vehicleApi from '../../features/residents/vehicleApi';
import residentTicketsApi from '../../features/residents/residentTicketsApi';

const { TextArea } = Input;
const { Text } = Typography;
const { Option } = Select;

// File validation constants - chỉ cho phép ảnh
const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
const MAX_FILES = 3; // Tối đa 3 ảnh
const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/jpg', 'image/webp'];

const formatFileSize = (bytes) => {
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
};

const VehicleRegistrationModal = ({ open, onClose, onSuccess, apartmentId }) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [vehicleTypes, setVehicleTypes] = useState([]);
  const [loadingTypes, setLoadingTypes] = useState(false);
  const [fileList, setFileList] = useState([]);
  const [previewImage, setPreviewImage] = useState('');
  const [previewOpen, setPreviewOpen] = useState(false);

  useEffect(() => {
    if (open) {
      fetchVehicleTypes();
    }
  }, [open]);

  const fetchVehicleTypes = async () => {
    setLoadingTypes(true);
    try {
      const types = await vehicleApi.getVehicleTypes();
      setVehicleTypes(types);
    } catch (error) {
      message.error('Không thể tải danh sách loại xe');
    } finally {
      setLoadingTypes(false);
    }
  };

  const validateFile = (file) => {
    // Check file size
    if (file.size > MAX_FILE_SIZE) {
      message.error(`Ảnh "${file.name}" quá lớn. Dung lượng tối đa: ${formatFileSize(MAX_FILE_SIZE)}`);
      return Upload.LIST_IGNORE;
    }

    // Check file type
    if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
      message.error(`File "${file.name}" không phải là ảnh hợp lệ (JPG, PNG, WEBP)`);
      return Upload.LIST_IGNORE;
    }

    // Check max file count
    if (fileList.length >= MAX_FILES) {
      message.error(`Chỉ được chọn tối đa ${MAX_FILES} ảnh`);
      return Upload.LIST_IGNORE;
    }

    return false; // Không upload ngay, chỉ thêm vào danh sách
  };

  const handleChange = ({ fileList: newFileList }) => {
    // Chỉ giữ tối đa MAX_FILES ảnh
    setFileList(newFileList.slice(0, MAX_FILES));
  };

  const handleRemove = (file) => {
    setFileList(prev => prev.filter(f => f.uid !== file.uid));
  };

  const handlePreview = async (file) => {
    if (!file.url && !file.preview) {
      file.preview = await getBase64(file.originFileObj);
    }
    setPreviewImage(file.url || file.preview);
    setPreviewOpen(true);
  };

  const getBase64 = (file) =>
    new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => resolve(reader.result);
      reader.onerror = (error) => reject(error);
    });

  const handleSubmit = async (values) => {
    setLoading(true);
    try {
      // Upload các file ảnh trước, nhận về fileId
      const uploadedFileIds = [];
      
      if (fileList.length > 0) {
        for (const file of fileList) {
          try {
            const response = await residentTicketsApi.uploadFile(file.originFileObj);
            
            if (response && response.fileId) {
              uploadedFileIds.push(response.fileId);
            }
          } catch (uploadError) {
            console.error('Upload error:', uploadError);
            message.error(`Tải lên "${file.name}" thất bại`);
            throw uploadError; // Dừng nếu upload thất bại
          }
        }
      }

      // Tạo ticket với các fileId đã upload
      const data = {
        subject: values.subject,
        description: values.description,
        apartmentId: apartmentId || null,
        vehicleInfo: {
          vehicleTypeId: values.vehicleTypeId,
          licensePlate: values.licensePlate.trim().toUpperCase(),
          color: values.color?.trim() || null,
          brandModel: values.brandModel?.trim() || null,
        },
        attachmentFileIds: uploadedFileIds
      };

      await vehicleApi.createVehicleRegistration(data);
      message.success('Đăng ký xe thành công');
      handleClose();
      if (onSuccess) onSuccess();
    } catch (error) {
      const errorMsg = error.response?.data?.message || error.message || 'Đăng ký xe thất bại';
      message.error(errorMsg);
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    form.resetFields();
    setFileList([]);
    onClose();
  };

  const uploadProps = {
    beforeUpload: validateFile,
    onRemove: handleRemove,
    onPreview: handlePreview,
    fileList: fileList,
    onChange: handleChange,
    listType: 'picture-card',
    maxCount: MAX_FILES,
    accept: 'image/*',
    multiple: true, // Cho phép chọn nhiều ảnh cùng lúc
  };

  return (
    <>
      <Modal
        title={
          <Space>
            <CarOutlined />
            <span>Đăng ký gửi xe</span>
          </Space>
        }
        open={open}
        onCancel={handleClose}
        footer={null}
        width="100%"
        style={{ maxWidth: 700, top: 20 }}
        destroyOnClose
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
        >
          <Form.Item
            label="Tiêu đề"
            name="subject"
            rules={[
              { required: true, message: 'Vui lòng nhập tiêu đề' },
              { max: 255, message: 'Tiêu đề không được vượt quá 255 ký tự' }
            ]}
            initialValue="Đăng ký gửi xe"
          >
            <Input placeholder="Nhập tiêu đề yêu cầu" />
          </Form.Item>

          <Row gutter={[12, 12]}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Loại xe"
                name="vehicleTypeId"
                rules={[{ required: true, message: 'Vui lòng chọn loại xe' }]}
              >
                <Select
                  placeholder="Chọn loại xe"
                  loading={loadingTypes}
                  showSearch
                  optionFilterProp="children"
                >
                  {vehicleTypes.map(type => (
                    <Option key={type.vehicleTypeId} value={type.vehicleTypeId}>
                      {type.name} ({type.code})
                    </Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>

            <Col xs={24} sm={12}>
              <Form.Item
                label="Biển số xe"
                name="licensePlate"
                rules={[
                  { required: true, message: 'Vui lòng nhập biển số xe' },
                  { max: 64, message: 'Biển số không được vượt quá 64 ký tự' }
                ]}
              >
                <Input 
                  placeholder="VD: 29A-12345" 
                  style={{ textTransform: 'uppercase' }}
                />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={[12, 12]}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Màu sắc"
                name="color"
                rules={[
                  { max: 64, message: 'Màu sắc không được vượt quá 64 ký tự' }
                ]}
              >
                <Input placeholder="VD: Đỏ, Xanh, Trắng" />
              </Form.Item>
            </Col>

            <Col xs={24} sm={12}>
              <Form.Item
                label="Hãng/Model"
                name="brandModel"
                rules={[
                  { max: 128, message: 'Hãng/Model không được vượt quá 128 ký tự' }
                ]}
              >
                <Input placeholder="VD: Honda Wave Alpha" />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item
            label="Mô tả"
            name="description"
          >
            <TextArea 
              rows={3} 
              placeholder="Thông tin bổ sung về xe (tùy chọn)"
              maxLength={1000}
              showCount
            />
          </Form.Item>

          <Form.Item
            label={
              <Space>
                <span>Ảnh xe/Giấy tờ xe</span>
                <Text type="secondary" style={{ fontSize: 12 }}>
                  (Tối đa {MAX_FILES} ảnh, mỗi ảnh &lt; {formatFileSize(MAX_FILE_SIZE)})
                </Text>
              </Space>
            }
          >
            <Upload {...uploadProps}>
              {fileList.length < MAX_FILES && (
                <div>
                  <UploadOutlined />
                  <div style={{ marginTop: 8 }}>Tải ảnh</div>
                </div>
              )}
            </Upload>
            <Text type="secondary" style={{ fontSize: 12, display: 'block', marginTop: 8 }}>
              Hỗ trợ: JPG, PNG, WEBP
            </Text>
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, textAlign: 'right', marginTop: 24 }}>
            <Space>
              <Button onClick={handleClose}>
                Hủy
              </Button>
              <Button 
                type="primary" 
                htmlType="submit" 
                loading={loading}
                icon={<CarOutlined />}
              >
                Đăng ký
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      <Image
        preview={{
          visible: previewOpen,
          onVisibleChange: (visible) => setPreviewOpen(visible),
        }}
        src={previewImage}
        style={{ display: 'none' }}
      />
    </>
  );
};

export default VehicleRegistrationModal;
