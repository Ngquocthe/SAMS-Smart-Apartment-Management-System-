import React from "react";
import { Card, Timeline, Avatar, Typography, Tag } from "antd";
import { ClockCircleOutlined, MessageOutlined, UserOutlined } from "@ant-design/icons";
import dayjs from "dayjs";

const { Text, Paragraph } = Typography;

export default function TicketComments({ comments = [] }) {
    return (
        <Card
            title={
                <div className="flex items-center space-x-2">
                    <ClockCircleOutlined className="text-orange-500" />
                    <span>Lịch sử bình luận ({comments.length})</span>
                </div>
            }
            className="shadow-sm"
        >
            {comments.length > 0 ? (
                <Timeline>
                    {comments.map((comment) => (
                        <Timeline.Item
                            key={comment.commentId}
                            dot={<Avatar size="small" icon={<UserOutlined />} />}
                        >
                            <Card size="small" className="ml-4">
                                <div className="flex justify-between items-start mb-2">
                                    <div className="flex items-center space-x-2">
                                        <Text strong>{comment.createdByUserName || "Không xác định"}</Text>
                                        {comment.isInternal && (
                                            <Tag color="orange" size="small">Nội bộ</Tag>
                                        )}
                                    </div>
                                    <Text type="secondary" className="text-xs">
                                        {dayjs(comment.commentTime).format("HH:mm:ss DD/MM/YYYY")}
                                    </Text>
                                </div>
                                <Paragraph className="!mb-0">{comment.content}</Paragraph>
                            </Card>
                        </Timeline.Item>
                    ))}
                </Timeline>
            ) : (
                <div className="text-center py-8">
                    <MessageOutlined className="text-4xl text-gray-300 mb-2" />
                    <Text type="secondary" italic>Chưa có bình luận nào</Text>
                </div>
            )}
        </Card>
    );
}

