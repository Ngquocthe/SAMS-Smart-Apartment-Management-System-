import React from "react";
import { Card, Row, Col, Avatar, Typography, Tag } from "antd";
import { MessageOutlined, UserOutlined, CalendarOutlined } from "@ant-design/icons";
import dayjs from "dayjs";
import utc from "dayjs/plugin/utc";
import timezone from "dayjs/plugin/timezone";

dayjs.extend(utc);
dayjs.extend(timezone);

const { Title, Text } = Typography;

const statusColors = {
    "Mới tạo": "blue",
    "Đang xử lý": "gold",
    "Chờ xử lý": "orange",
    "Đã đóng": "red",
    "Đã hủy": "gray"
};

export default function TicketHeader({ ticket }) {
    return (
        <Card className="shadow-lg border-0">
            <Row gutter={[24, 16]} align="middle">
                <Col xs={24} lg={16}>
                    <div className="flex items-start space-x-4">
                        <Avatar
                            size={64}
                            icon={<MessageOutlined />}
                            className="bg-blue-500"
                        />
                        <div className="flex-1">
                            <Title level={2} className="!mb-2 !text-gray-800">
                                {ticket.subject}
                            </Title>
                            <div className="flex flex-wrap gap-2 mb-3">
                                <Tag
                                    color={statusColors[ticket.status] || "default"}
                                    className="px-3 py-1 text-sm font-medium"
                                >
                                    {ticket.status || "—"}
                                </Tag>
                                <Tag color="purple" className="px-3 py-1 text-sm">
                                    {ticket.priority || "—"}
                                </Tag>
                                <Tag color="cyan" className="px-3 py-1 text-sm">
                                    {ticket.category || "—"}
                                </Tag>
                            </div>
                            <div className="flex items-center space-x-6 text-gray-600">
                                <div className="flex items-center space-x-2">
                                    <UserOutlined />
                                    <Text>{ticket.createdByUserName || "Không xác định"}</Text>
                                </div>
                                <div className="flex items-center space-x-2">
                                    <CalendarOutlined />
                                    <Text>{dayjs(ticket.createdAt).utc().tz("Asia/Ho_Chi_Minh").format("HH:mm:ss DD/MM/YYYY")}</Text>
                                </div>
                            </div>
                        </div>
                    </div>
                </Col>
                <Col xs={24} lg={8}>
                    <Card size="small" className="bg-gradient-to-r from-blue-50 to-indigo-50 border-0">
                        <div className="space-y-2">
                            <div className="flex justify-between">
                                <Text strong>Mã ticket:</Text>
                                <Text code className="text-xs">{ticket?.ticketId ? `${ticket.ticketId.slice(0, 8)}...` : "—"}</Text>
                            </div>
                            <div className="flex justify-between">
                                <Text strong>Phạm vi:</Text>
                                <Text>{ticket.scope || "Tòa nhà"}</Text>
                            </div>
                            {(ticket.apartmentId || ticket.apartmentNumber) && (
                                <div className="flex justify-between">
                                    <Text strong>Căn hộ:</Text>
                                    <Text>{ticket.apartmentNumber || (ticket.apartmentId ? ticket.apartmentId.slice(0, 8) + '...' : '-')}</Text>
                                </div>
                            )}
                            {ticket.updatedAt && (
                                <div className="flex justify-between">
                                    <Text strong>Cập nhật:</Text>
                                    <Text className="text-xs">
                                        {dayjs(ticket.updatedAt).utc().tz("Asia/Ho_Chi_Minh").format("HH:mm:ss DD/MM/YYYY")}
                                    </Text>
                                </div>
                            )}
                        </div>
                    </Card>
                </Col>
            </Row>
        </Card>
    );
}


