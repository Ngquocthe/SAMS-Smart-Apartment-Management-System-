import React from "react";
import { Card, Row, Col, Image, Typography } from "antd";
import { FileImageOutlined } from "@ant-design/icons";

const { Text } = Typography;

export default function TicketAttachments({ attachments }) {
    if (!attachments || attachments.length === 0) {
        return null;
    }

    return (
        <Card
            title={
                <div className="flex items-center space-x-2">
                    <FileImageOutlined className="text-green-500" />
                    <span>Ảnh đính kèm ({attachments.length})</span>
                </div>
            }
            className="shadow-sm"
        >
            <Row gutter={[16, 16]}>
                {attachments.map((attachment) => {
                    const file = attachment.file || {};
                    const imgSrc = file.url || file.storagePath || file.StoragePath || "";
                    const fileName = file.fileName || file.originalName || file.OriginalName || "attachment";
                    return (
                        <Col xs={12} sm={8} md={6} key={attachment.attachmentId}>
                            <Card
                                size="small"
                                className="hover:shadow-md transition-shadow cursor-pointer"
                                cover={
                                    <Image
                                        src={imgSrc}
                                        alt={fileName}
                                        className="h-32 object-cover"
                                        fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMIAAADDCAYAAADQvc6UAAABRWlDQ1BJQ0MgUHJvZmlsZQAAKJFjYGASSSwoyGFhYGDIzSspCnJ3UoiIjFJgf8LAwSDCIMogwMCcmFxc4BgQ4ANUwgCjUcG3awyMIPqyLsis7PPOq3QdDFcvjV3jOD1boQVTPQrgSkktTgbSf4A4LbmgqISBgTEFyFYuLykAsTuAbJEioKOA7DkgdjqEvQHEToKwj4DVhAQ5A9k3gGyB5IxEoBmML4BsnSQk8XQkNtReEOBxcfXxUQg1Mjc0dyHgXNJBSWpFCYh2zi+oLMpMzyhRcASGUqqCZ16yno6CkYGRAQMDKMwhqj/fAIcloxgHQqxAjIHBEugw5sUIsSQpBobtQPdLciLEVJYzMPBHMDBsayhILEqEO4DxG0txmrERhM29nYGBddr//5/DGRjYNRkY/l7////39v///y4Dmn+LgeHANwDrkl1AuO+pmgAAADhlWElmTU0AKgAAAAgAAYdpAAQAAAABAAAAGgAAAAAAAqACAAQAAAABAAAAwqADAAQAAAABAAAAwwAAAAD9b/HnAAAHlklEQVR4Ae3dP3Ik1RnG4W+FgYxN"
                                    />
                                }
                            >
                                <Card.Meta
                                    title={
                                        <Text className="text-xs" ellipsis>
                                            {fileName}
                                        </Text>
                                    }
                                />
                            </Card>
                        </Col>
                    );
                })}
            </Row>
        </Card>
    );
}


