import React, { useState, useEffect } from 'react';
import {
  Modal,
  Card,
  Typography,
  Tag,
  Space,
  Divider,
  List,
  Avatar,
  Button,
  Input,
  Form,
  message,
  Timeline,
  Image,
  Descriptions,
  Row,
  Col,
  Spin
} from 'antd';
import {
  UserOutlined,
  FileTextOutlined,
  ClockCircleOutlined,
  MessageOutlined,
  SendOutlined
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import residentTicketsApi from '../../features/residents/residentTicketsApi';
import {
  formatInvoiceStatus,
  getInvoiceStatusColor,
} from '../../features/resident/invoiceApi';
import dayjs from 'dayjs';

const { Title, Text, Paragraph } = Typography;
const { TextArea } = Input;

const TicketDetailModal = ({ open, onClose, ticketId }) => {
  const [ticket, setTicket] = useState(null);
  const [comments, setComments] = useState([]);
  const [loading, setLoading] = useState(false);
  const [submittingComment, setSubmittingComment] = useState(false);
  const [commentForm] = Form.useForm();
  const [ticketInvoices, setTicketInvoices] = useState([]);

  // Fetch ticket details
  useEffect(() => {
    if (open && ticketId) {
      fetchTicketDetail();
      fetchComments();
      fetchTicketInvoices();
    }
  }, [open, ticketId]); // eslint-disable-line react-hooks/exhaustive-deps

  const fetchTicketDetail = async () => {
    setLoading(true);
    try {
      const result = await residentTicketsApi.getTicketById(ticketId);
      setTicket(result);
    } catch (error) {
      message.error('Không thể tải thông tin chi tiết: ' + error.message);
    } finally {
      setLoading(false);
    }
  };

  const fetchTicketInvoices = async () => {
    if (!ticketId) return;
    try {
      const invoices = await residentTicketsApi.getTicketInvoices(ticketId);
      setTicketInvoices(invoices || []);
    } catch (error) {
      console.error("Không thể tải hóa đơn liên quan:", error);
    }
  };

  const fetchComments = async () => {
    try {
      const result = await residentTicketsApi.getComments(ticketId);
      setComments(result || []);
    } catch (error) {
      console.error('Error fetching comments:', error);
    }
  };

  const handleAddComment = async (values) => {
    setSubmittingComment(true);
    try {
      await residentTicketsApi.addComment({
        ticketId,
        content: values.content
      });
      message.success('Đã thêm bình luận');
      commentForm.resetFields();
      await fetchComments();
    } catch (error) {
      message.error('Không thể thêm bình luận: ' + error.message);
    } finally {
      setSubmittingComment(false);
    }
  };

  const navigate = useNavigate();

  const getStatusColor = (status) => {
    const colors = {
      'Mới tạo': 'blue',
      'Đang xử lý': 'orange',
      'Chờ xử lý': 'gold',
      'Đã đóng': 'green'
    };
    return colors[status] || 'default';
  };

  const getStatusText = (status) => status || '—';

  const getPriorityColor = (priority) => {
    const colors = {
      'Thấp': 'green',
      'Bình thường': 'blue',
      'Cao': 'orange',
      'Khẩn cấp': 'red'
    };
    return colors[priority] || 'default';
  };

  const getPriorityText = (priority) => priority || '—';

  const getCategoryText = (category) => {
    if (category === 'VehicleRegistration') {
      return 'Đăng ký phương tiện';
    }
    return category || 'Khác';
  };

  const getVehicleStatusColor = (status) => {
    const colors = {
      'PENDING': 'gold',
      'ACTIVE': 'green',
      'REJECTED': 'red',
      'INACTIVE': 'default'
    };
    return colors[status] || 'default';
  };

  const getVehicleStatusText = (status) => {
    const texts = {
      'PENDING': 'Chờ duyệt',
      'ACTIVE': 'Đã duyệt',
      'REJECTED': 'Bị từ chối',
      'INACTIVE': 'Không hoạt động'
    };
    return texts[status] || status || '—';
  };

  if (loading) {
    return (
      <Modal
        title="Chi tiết yêu cầu"
        open={open}
        onCancel={onClose}
        footer={null}
        width={800}
      >
        <div style={{ textAlign: 'center', padding: '50px 0' }}>
          <Spin size="large" />
        </div>
      </Modal>
    );
  }

  if (!ticket) {
    return null;
  }

  return (
    <Modal
      title={
        <Space>
          <FileTextOutlined />
          <span>Chi tiết yêu cầu</span>
        </Space>
      }
      open={open}
      onCancel={onClose}
      footer={null}
      width={900}
      style={{ top: 20 }}
    >
      <div style={{ maxHeight: '70vh', overflowY: 'auto' }}>
        {/* Ticket Info */}
        <Card size="small" style={{ marginBottom: 16 }}>
          <Row gutter={16}>
            <Col span={12}>
              <Descriptions column={1} size="small">
                <Descriptions.Item label="Mã yêu cầu">
                  <Text code>{ticket.ticketId}</Text>
                </Descriptions.Item>
                <Descriptions.Item label="Loại">
                  <Tag color="cyan">{getCategoryText(ticket.category)}</Tag>
                </Descriptions.Item>
                <Descriptions.Item label="Trạng thái">
                  <Tag color={getStatusColor(ticket.status)}>
                    {getStatusText(ticket.status)}
                  </Tag>
                </Descriptions.Item>
              </Descriptions>
            </Col>
            <Col span={12}>
              <Descriptions column={1} size="small">
                <Descriptions.Item label="Mức độ ưu tiên">
                  <Tag color={getPriorityColor(ticket.priority)}>
                    {getPriorityText(ticket.priority)}
                  </Tag>
                </Descriptions.Item>
                <Descriptions.Item label="Ngày hoàn thành dự kiến">
                  {ticket.expectedCompletionAt
                    ? dayjs(ticket.expectedCompletionAt).format('DD/MM/YYYY HH:mm')
                    : '—'}
                </Descriptions.Item>
                <Descriptions.Item label="Căn hộ">
                  {ticket.apartmentNumber || 'Không xác định'}
                </Descriptions.Item>
                <Descriptions.Item label="Ngày tạo">
                  {dayjs(ticket.createdAt).format('DD/MM/YYYY HH:mm')}
                </Descriptions.Item>
              </Descriptions>
            </Col>
          </Row>
        </Card>

        {/* Title and Description */}
        <Card title="Thông tin chi tiết" style={{ marginBottom: 16 }}>
          <Title level={4}>{ticket.subject}</Title>
          <Paragraph>
            {ticket.description}
          </Paragraph>
        </Card>

        {/* Vehicle Information */}
        {ticket.vehicleInfo && (
          <Card title="Thông tin đăng ký xe" style={{ marginBottom: 16 }}>
            <Descriptions column={{ xs: 1, sm: 2 }} size="small">
              <Descriptions.Item label="Loại xe">
                <Text strong>{ticket.vehicleInfo.vehicleTypeName}</Text>
              </Descriptions.Item>
              <Descriptions.Item label="Biển số">
                <Text strong style={{ color: '#1890ff' }}>{ticket.vehicleInfo.licensePlate}</Text>
              </Descriptions.Item>
              <Descriptions.Item label="Màu sắc">
                {ticket.vehicleInfo.color || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Hãng/Model">
                {ticket.vehicleInfo.brandModel || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Trạng thái">
                <Tag color={getVehicleStatusColor(ticket.vehicleInfo.status)}>
                  {getVehicleStatusText(ticket.vehicleInfo.status)}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="Ngày đăng ký">
                {ticket.vehicleInfo.registeredAt 
                  ? dayjs(ticket.vehicleInfo.registeredAt).format('DD/MM/YYYY HH:mm')
                  : '—'
                }
              </Descriptions.Item>
            </Descriptions>
          </Card>
        )}

        {/* Attachments */}
        {ticket.attachments && ticket.attachments.length > 0 && (
          <Card title="Tệp đính kèm" size="small" style={{ marginBottom: 16 }}>
            <Row gutter={[16, 16]}>
              {ticket.attachments.map((attachment, index) => (
                <Col key={attachment.id || index} xs={24} sm={12} md={8}>
                  <div style={{
                    border: '1px solid #d9d9d9',
                    borderRadius: 8,
                    overflow: 'hidden',
                    backgroundColor: '#fafafa',
                    display: 'flex',
                    flexDirection: 'column'
                  }}>
                    <div style={{
                      display: 'flex',
                      justifyContent: 'center',
                      alignItems: 'center',
                      minHeight: 150,
                      padding: 8
                    }}>
                      <Image
                        src={attachment.file?.storagePath || attachment.url || attachment.cloudinaryUrl}
                        alt={attachment.file?.originalName || attachment.fileName || `Ảnh đính kèm ${index + 1}`}
                        style={{
                          maxWidth: '100%',
                          maxHeight: 300,
                          width: 'auto',
                          height: 'auto',
                          display: 'block'
                        }}
                        fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMIAAADDCAYAAADQvc6UAAABRWlDQ1BJQ0MgUHJvZmlsZQAAKJFjYGASSSwoyGFhYGDIzSspCnJ3UoiIjFJgf8LAwSDCIMogwMCcmFxc4BgQ4ANUwgCjUcG3awyMIPqyLsis7PPOq3QdDFcvjV3jOD1boQVTPQrgSkktTgbSf4A4LbmgqISBgTEFyFYuLykAsTuAbJEioKOA7DkgdjqEvQHEToKwj4DVhAQ5A9k3gGyB5IxEoBmML4BsnSQk8XQkNtReEOBxcfXxUQg1Mjc0dyHgXNJBSWpFCYh2zi+oLMpMzyhRcASGUqqCZ16yno6CkYGRAQMDKMwhqj/fAIcloxgHQqxAjIHBEugw5sUIsSQpBobtQPdLciLEVJYzMPBHMDBsayhILEqEO4DxG0txmrERhM29nYGBddr//5/DGRjYNRkY/l7////39v///y4Dmn+LgeHANwDrkl1AuO+pmgAAADhlWElmTU0AKgAAAAgAAYdpAAQAAAABAAAAGgAAAAAAAqACAAQAAAABAAAAwqADAAQAAAABAAAAwwAAAAD9b/HnAAAHlklEQVR4Ae3dP3Ik1RnG4W+FgYxN"
                        preview={{
                          mask: <div style={{ color: 'white' }}>Xem ảnh</div>
                        }}
                      />
                    </div>
                    {(attachment.file?.originalName || attachment.fileName || attachment.note) && (
                      <div style={{
                        padding: '8px 12px',
                        borderTop: '1px solid #d9d9d9',
                        backgroundColor: 'white'
                      }}>
                        <Text ellipsis style={{ fontSize: 12 }}>
                          {attachment.file?.originalName || attachment.fileName || attachment.note}
                        </Text>
                      </div>
                    )}
                  </div>
                </Col>
              ))}
            </Row>
          </Card>
        )}

        {/* Timeline */}
        <Card title="Lịch sử xử lý" size="small" style={{ marginBottom: 16 }}>
          <Timeline>
            <Timeline.Item
              dot={<ClockCircleOutlined />}
              color="blue"
            >
              <Text strong>Yêu cầu được tạo</Text>
              <br />
              <Text type="secondary">
                {dayjs(ticket.createdAt).format('DD/MM/YYYY HH:mm')}
              </Text>
            </Timeline.Item>

            {ticket.updatedAt && ticket.updatedAt !== ticket.createdAt && (
              <Timeline.Item color="orange">
                <Text strong>Yêu cầu được cập nhật</Text>
                <br />
                <Text type="secondary">
                  {dayjs(ticket.updatedAt).format('DD/MM/YYYY HH:mm')}
                </Text>
              </Timeline.Item>
            )}

            {ticket.closedAt && (
              <Timeline.Item color="green">
                <Text strong>Yêu cầu đã hoàn thành</Text>
                <br />
                <Text type="secondary">
                  {dayjs(ticket.closedAt).format('DD/MM/YYYY HH:mm')}
                </Text>
              </Timeline.Item>
            )}
          </Timeline>
        </Card>

        {ticketInvoices.length > 0 && (
          <Card title="Hóa đơn liên quan" size="small" style={{ marginBottom: 16 }}>
            <List
              dataSource={ticketInvoices}
              renderItem={(invoice) => (
                <List.Item
                  actions={[
                    <Button
                      key="invoice-detail"
                      type="link"
                      onClick={() =>
                        navigate(`/resident/invoices?invoiceId=${invoice.invoiceId}`, {
                          replace: true,
                        })
                      }
                    >
                      Xem chi tiết
                    </Button>,
                  ]}
                >
                  <List.Item.Meta
                    title={
                      <Space size={8}>
                        <Text strong>{invoice.invoiceNo}</Text>
                        <Tag color={getInvoiceStatusColor(invoice.status)}>
                          {formatInvoiceStatus(invoice.status)}
                        </Tag>
                      </Space>
                    }
                    description={
                      <Space direction="vertical" size={2}>
                        <Text type="secondary">
                          Hạn: {dayjs(invoice.dueDate).format("DD/MM/YYYY")}
                        </Text>
                        <Text strong>{invoice.totalAmount.toLocaleString("vi-VN")} đ</Text>
                      </Space>
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        )}

        {/* Comments */}
        <Card
          title={
            <Space>
              <MessageOutlined />
              <span>Bình luận ({comments.length})</span>
            </Space>
          }
          style={{ marginBottom: 16 }}
        >
          {/* Comments List */}
          {comments.length > 0 && (
            <List
              dataSource={comments}
              renderItem={(comment) => (
                <List.Item>
                  <List.Item.Meta
                    avatar={<Avatar icon={<UserOutlined />} />}
                    title={
                      <Space>
                        <Text strong>{comment.createdByUserName || 'Người dùng'}</Text>
                        <Text type="secondary" style={{ fontSize: '12px' }}>
                          {dayjs(comment.commentTime || comment.createdAt).format('DD/MM/YYYY HH:mm')}
                        </Text>
                      </Space>
                    }
                    description={comment.content}
                  />
                </List.Item>
              )}
              style={{ marginBottom: 16 }}
            />
          )}

          {/* Add Comment Form */}
          {ticket.status !== 'Đã đóng' && (
            <>
              <Divider />
              <Form
                form={commentForm}
                onFinish={handleAddComment}
                layout="vertical"
              >
                <Form.Item
                  name="content"
                  rules={[{ required: true, message: 'Vui lòng nhập nội dung bình luận' }]}
                >
                  <TextArea
                    rows={3}
                    placeholder="Nhập bình luận của bạn..."
                  />
                </Form.Item>
                <Form.Item style={{ marginBottom: 0 }}>
                  <Button
                    type="primary"
                    htmlType="submit"
                    icon={<SendOutlined />}
                    loading={submittingComment}
                  >
                    Gửi bình luận
                  </Button>
                </Form.Item>
              </Form>
            </>
          )}
        </Card>
      </div>
    </Modal>
  );
};

export default TicketDetailModal;