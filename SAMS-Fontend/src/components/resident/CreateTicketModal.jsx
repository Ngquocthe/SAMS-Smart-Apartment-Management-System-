import React, { useState } from 'react';
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
  Card,
  Typography
} from 'antd';
import {
  UploadOutlined,
  FileProtectOutlined,
  ExclamationCircleOutlined,
  DeleteOutlined,
  EyeOutlined
} from '@ant-design/icons';
import residentTicketsApi from '../../features/residents/residentTicketsApi';

const { TextArea } = Input;
const { Text } = Typography;

// File validation constants for tickets
const MAX_FILE_SIZE_TICKET = 5 * 1024 * 1024; // 5MB
const MAX_FILES_COUNT = 5; // Tối đa 5 file
const ALLOWED_TICKET_TYPES = ['image/jpeg', 'image/png', 'image/jpg', 'image/webp', 'image/gif', 'image/bmp'];
const ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp', '.gif', '.bmp'];

const formatFileSize = (bytes) => {
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
};

const validateTicketFile = (file) => {
  // Check file size
  if (file.size > MAX_FILE_SIZE_TICKET) {
    message.error(`Ảnh "${file.name}" quá lớn. Dung lượng tối đa: ${formatFileSize(MAX_FILE_SIZE_TICKET)}`);
    return Upload.LIST_IGNORE;
  }

  // Check file type
  if (!ALLOWED_TICKET_TYPES.includes(file.type)) {
    message.error(`File "${file.name}" không phải là ảnh hợp lệ. Chỉ chấp nhận: JPG, PNG, WEBP, GIF, BMP`);
    return Upload.LIST_IGNORE;
  }

  // Check file extension
  const fileName = file.name.toLowerCase();
  const extension = fileName.substring(fileName.lastIndexOf('.'));
  
  if (!ALLOWED_EXTENSIONS.includes(extension)) {
    message.error(`File "${file.name}" không được hỗ trợ. Chỉ chấp nhận ảnh: JPG, PNG, WEBP, GIF, BMP`);
    return Upload.LIST_IGNORE;
  }

  return true;
};

const CreateTicketModal = ({
  open,
  onClose,
  onSuccess,
  type = 'bảo trì' // 'bảo trì' or 'khiếu nại'
}) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [uploadedFiles, setUploadedFiles] = useState([]);
  const [uploading, setUploading] = useState(false);

  const isComplaint = type === 'khiếu nại';
  const title = isComplaint ? 'Tạo phiếu khiếu nại' : 'Tạo phiếu bảo trì';
  const icon = isComplaint ? <ExclamationCircleOutlined /> : <FileProtectOutlined />;

  const handleFileUpload = async (file) => {
    // Kiểm tra số lượng file đã upload
    if (uploadedFiles.length >= MAX_FILES_COUNT) {
      message.warning(`Chỉ được upload tối đa ${MAX_FILES_COUNT} ảnh. Vui lòng xóa ảnh cũ trước khi thêm ảnh mới.`);
      return Upload.LIST_IGNORE;
    }

    // Validate file before upload
    const isValid = validateTicketFile(file);
    if (isValid === Upload.LIST_IGNORE) {
      return Upload.LIST_IGNORE;
    }

    setUploading(true);
    try {
      const result = await residentTicketsApi.uploadFile(file);
      const newFile = {
        id: result.fileId,
        name: file.name,
        size: file.size,
        url: result.url
      };
      setUploadedFiles(prev => [...prev, newFile]);
      message.success('Tải file thành công');
      return false; // Prevent default upload
    } catch (error) {
      message.error('Tải file thất bại: ' + (error.response?.data?.message || error.message));
      return Upload.LIST_IGNORE;
    } finally {
      setUploading(false);
    }
  };

  const removeFile = (fileId) => {
    setUploadedFiles(prev => prev.filter(f => f.id !== fileId));
  };

  const handleSubmit = async (values) => {
    setLoading(true);
    try {
      const dto = {
        subject: values.subject,
        description: values.description,
        apartmentId: values.apartmentId || null,
        attachmentFileIds: uploadedFiles.map(f => f.id)
      };

      let result;
      if (isComplaint) {
        result = await residentTicketsApi.createComplaintTicket(dto);
      } else {
        result = await residentTicketsApi.createMaintenanceTicket(dto);
      }

      message.success(`Tạo ${isComplaint ? 'khiếu nại' : 'phiếu bảo trì'} thành công`);
      form.resetFields();
      setUploadedFiles([]);
      onSuccess?.(result);
      onClose();
    } catch (error) {
      message.error(`Tạo ${isComplaint ? 'khiếu nại' : 'phiếu bảo trì'} thất bại: ` +
        (error.response?.data?.message || error.message));
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    form.resetFields();
    setUploadedFiles([]);
    onClose();
  };

  return (
    <Modal
      title={
        <Space>
          {icon}
          <span>{title}</span>
        </Space>
      }
      open={open}
      onCancel={handleCancel}
      footer={null}
      width={700}
      destroyOnClose
    >
      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
      >
        <Row gutter={16}>
          <Col span={24}>
            <Form.Item
              name="subject"
              label="Tiêu đề"
              rules={[
                { required: true, message: 'Vui lòng nhập tiêu đề' },
                { max: 255, message: 'Tiêu đề không được vượt quá 255 ký tự' }
              ]}
            >
              <Input
                placeholder={`Nhập tiêu đề ${isComplaint ? 'khiếu nại' : 'bảo trì'}`}
                size="large"
              />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item
          name="apartmentId"
          label="Căn hộ (tùy chọn)"
        >
          <Select
            placeholder="Chọn căn hộ hoặc để trống"
            size="large"
            allowClear
          >
            {/* TODO: Load apartments from API */}
          </Select>
        </Form.Item>

        <Form.Item
          name="description"
          label="Mô tả chi tiết"
          rules={[{ required: true, message: 'Vui lòng nhập mô tả chi tiết' }]}
        >
          <TextArea
            rows={6}
            placeholder={`Mô tả chi tiết về ${isComplaint ? 'vấn đề khiếu nại' : 'sự cố cần bảo trì'}`}
          />
        </Form.Item>

        {/* File Upload */}
        <Form.Item label="Đính kèm hình ảnh">
          <Upload
            beforeUpload={handleFileUpload}
            showUploadList={false}
            accept="image/jpeg,image/png,image/jpg,image/webp,image/gif,image/bmp"
            multiple
          >
            <Button
              icon={<UploadOutlined />}
              loading={uploading}
              disabled={uploading || uploadedFiles.length >= MAX_FILES_COUNT}
            >
              {uploading ? 'Đang tải...' : 'Chọn ảnh'}
            </Button>
          </Upload>
          <Text type="secondary" style={{ marginLeft: 8, fontSize: '12px' }}>
            Chỉ chấp nhận ảnh: JPG, PNG, WEBP, GIF, BMP (tối đa {formatFileSize(MAX_FILE_SIZE_TICKET)} mỗi ảnh, tối đa {MAX_FILES_COUNT} ảnh)
          </Text>
        </Form.Item>

        {/* Uploaded Files List */}
        {uploadedFiles.length > 0 && (
          <Card size="small" title="Ảnh đã tải lên" style={{ marginBottom: 16 }}>
            <Space direction="vertical" style={{ width: '100%' }}>
              {uploadedFiles.map(file => (
                <div key={file.id} style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  padding: '8px 0'
                }}>
                  <Space>
                    <Text strong>{file.name}</Text>
                    <Text type="secondary">
                      ({(file.size / 1024 / 1024).toFixed(2)} MB)
                    </Text>
                  </Space>
                  <Space>
                    {file.url && (
                      <Button
                        type="link"
                        icon={<EyeOutlined />}
                        onClick={() => window.open(file.url, '_blank')}
                      >
                        Xem
                      </Button>
                    )}
                    <Button
                      type="link"
                      danger
                      icon={<DeleteOutlined />}
                      onClick={() => removeFile(file.id)}
                    >
                      Xóa
                    </Button>
                  </Space>
                </div>
              ))}
            </Space>
          </Card>
        )}

        <Form.Item style={{ marginBottom: 0, textAlign: 'right' }}>
          <Space>
            <Button onClick={handleCancel}>
              Hủy
            </Button>
            <Button
              type="primary"
              htmlType="submit"
              loading={loading}
              disabled={loading || uploading}
            >
              Tạo {isComplaint ? 'khiếu nại' : 'phiếu bảo trì'}
            </Button>
          </Space>
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default CreateTicketModal;