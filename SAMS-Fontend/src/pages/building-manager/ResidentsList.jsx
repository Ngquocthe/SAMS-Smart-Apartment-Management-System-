import { useState } from "react";
import { Modal, Descriptions, Tag, Space, Avatar, List } from "antd";
import {
  UserOutlined,
  PhoneOutlined,
  MailOutlined,
  HomeOutlined,
} from "@ant-design/icons";
import ResidentDirectory from "../../components/resident/ResidentDirectory";
import dayjs from "dayjs";

export default function ResidentsList() {
  const [detailModalOpen, setDetailModalOpen] = useState(false);
  const [selectedResident, setSelectedResident] = useState(null);

  const handleViewDetail = (record) => {
    setSelectedResident(record);
    setDetailModalOpen(true);
  };

  const handleCloseModal = () => {
    setDetailModalOpen(false);
    setSelectedResident(null);
  };

  const genderMap = {
    MALE: "Nam",
    FEMALE: "Nữ",
    OTHER: "Khác",
  };

  const statusMap = {
    ACTIVE: { color: "green", text: "Hoạt động" },
    INACTIVE: { color: "default", text: "Không hoạt động" },
    PENDING: { color: "orange", text: "Chờ duyệt" },
  };

  return (
    <div style={{ padding: "24px" }}>
      <ResidentDirectory onViewDetail={handleViewDetail} />

      {/* Detail Modal */}
      <Modal
        title={
          <Space>
            <UserOutlined />
            <span>Chi tiết cư dân</span>
          </Space>
        }
        open={detailModalOpen}
        onCancel={handleCloseModal}
        footer={null}
        width={800}
        destroyOnHidden
      >
        {selectedResident && (
          <Descriptions bordered column={2}>
            <Descriptions.Item label="Họ tên" span={2}>
              <Space>
                <Avatar icon={<UserOutlined />} />
                <span style={{ fontWeight: 600, fontSize: 16 }}>
                  {selectedResident.fullName || "—"}
                </span>
              </Space>
            </Descriptions.Item>

            <Descriptions.Item label="Số điện thoại">
              <Space>
                <PhoneOutlined />
                <span>{selectedResident.phone || "—"}</span>
              </Space>
            </Descriptions.Item>

            <Descriptions.Item label="Email">
              <Space>
                <MailOutlined />
                <span>{selectedResident.email || "—"}</span>
              </Space>
            </Descriptions.Item>

            <Descriptions.Item label="CMND/CCCD">
              {selectedResident.idNumber || "—"}
            </Descriptions.Item>

            <Descriptions.Item label="Ngày sinh">
              {selectedResident.dob
                ? dayjs(selectedResident.dob).format("DD/MM/YYYY")
                : "—"}
            </Descriptions.Item>

            <Descriptions.Item label="Giới tính">
              {selectedResident.gender
                ? genderMap[selectedResident.gender] || selectedResident.gender
                : "—"}
            </Descriptions.Item>

            <Descriptions.Item label="Địa chỉ" span={2}>
              {selectedResident.address || "—"}
            </Descriptions.Item>

            <Descriptions.Item label="Trạng thái">
              {selectedResident.status ? (
                <Tag
                  color={
                    statusMap[selectedResident.status]?.color || "default"
                  }
                >
                  {statusMap[selectedResident.status]?.text ||
                    selectedResident.status}
                </Tag>
              ) : (
                "—"
              )}
            </Descriptions.Item>

            <Descriptions.Item label="Đã đăng ký khuôn mặt">
              {selectedResident.hasFaceRegistered ? (
                <Tag color="green">Đã đăng ký</Tag>
              ) : (
                <Tag color="default">Chưa đăng ký</Tag>
              )}
            </Descriptions.Item>

            <Descriptions.Item label="Ngày tạo">
              {selectedResident.createdAt
                ? dayjs(selectedResident.createdAt).format(
                    "DD/MM/YYYY HH:mm"
                  )
                : "—"}
            </Descriptions.Item>

            <Descriptions.Item label="Ngày cập nhật">
              {selectedResident.updatedAt
                ? dayjs(selectedResident.updatedAt).format(
                    "DD/MM/YYYY HH:mm"
                  )
                : "—"}
            </Descriptions.Item>

            <Descriptions.Item label="Căn hộ" span={2}>
              {selectedResident.apartments &&
              selectedResident.apartments.length > 0 ? (
                <List
                  size="small"
                  dataSource={selectedResident.apartments}
                  renderItem={(apt) => (
                    <List.Item>
                      <Space>
                        <HomeOutlined />
                        <span style={{ fontWeight: 500 }}>
                          {apt.apartmentNumber || "—"}
                        </span>
                        {apt.isPrimary && (
                          <Tag color="blue">Chủ hộ</Tag>
                        )}
                        <Tag>{apt.relationType || "—"}</Tag>
                        {apt.startDate && (
                          <span style={{ color: "#999", fontSize: 12 }}>
                            Từ: {dayjs(apt.startDate).format("DD/MM/YYYY")}
                          </span>
                        )}
                        {apt.endDate && (
                          <span style={{ color: "#999", fontSize: 12 }}>
                            Đến: {dayjs(apt.endDate).format("DD/MM/YYYY")}
                          </span>
                        )}
                      </Space>
                    </List.Item>
                  )}
                />
              ) : (
                "—"
              )}
            </Descriptions.Item>
          </Descriptions>
        )}
      </Modal>
    </div>
  );
}

