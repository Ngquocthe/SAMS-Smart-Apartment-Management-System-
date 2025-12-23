import React from "react";
import { Card, Tag, Button, Tooltip } from "antd";
import { DownloadOutlined, EyeOutlined, FileImageOutlined, FilePdfOutlined, FileOutlined } from "@ant-design/icons";
import { getCategoryLabel } from "../../../features/documents/documentCategories";
import documentsApi from "../../../features/documents/documentsApi";

export default function ResidentDocumentCard({ doc, onPreview, onDownload }) {
    const category = getCategoryLabel(doc.category);
    const isImage = String(doc.mimeType || "").toLowerCase().startsWith("image/");
    const isPdf = String(doc.mimeType || "").toLowerCase() === "application/pdf";
    const thumbnailUrl = isImage && doc.fileId ? documentsApi.buildViewUrl(doc.fileId) : null;

    const getFileIcon = () => {
        if (isImage) return <FileImageOutlined style={{ fontSize: 24, color: "#52c41a" }} />;
        if (isPdf) return <FilePdfOutlined style={{ fontSize: 24, color: "#ff4d4f" }} />;
        return <FileOutlined style={{ fontSize: 24, color: "#1890ff" }} />;
    };

    const handleCardClick = (e) => {
        // Không mở nếu click vào button
        if (e.target.closest("button")) return;
        onPreview(doc);
    };

    const handleDownloadClick = (e) => {
        e.stopPropagation();
        onDownload(doc.fileId);
    };

    return (
        <Card
            hoverable
            onClick={handleCardClick}
            className="document-card-resident"
            style={{
                height: "100%",
                cursor: "pointer",
                transition: "all 0.3s",
                borderRadius: 12,
                overflow: "hidden",
            }}
            bodyStyle={{ padding: 0 }}
        >
            {/* Thumbnail/Preview Area */}
            <div
                style={{
                    width: "100%",
                    height: 200,
                    backgroundColor: "#f5f5f5",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    overflow: "hidden",
                    position: "relative",
                }}
            >
                {thumbnailUrl ? (
                    <img
                        src={thumbnailUrl}
                        alt={doc.title}
                        style={{
                            width: "100%",
                            height: "100%",
                            objectFit: "cover",
                            transition: "transform 0.3s",
                        }}
                        onMouseEnter={(e) => {
                            e.currentTarget.style.transform = "scale(1.05)";
                        }}
                        onMouseLeave={(e) => {
                            e.currentTarget.style.transform = "scale(1)";
                        }}
                    />
                ) : (
                    <div style={{ textAlign: "center", color: "#999" }}>
                        {getFileIcon()}
                        <div style={{ marginTop: 8, fontSize: 12 }}>{doc.mimeType?.split("/")[1]?.toUpperCase() || "FILE"}</div>
                    </div>
                )}
                <div
                    style={{
                        position: "absolute",
                        top: 8,
                        right: 8,
                    }}
                >
                    <Tag color="blue">{category}</Tag>
                </div>
            </div>

            {/* Content Area */}
            <div style={{ padding: 16 }}>
                <h3
                    style={{
                        margin: 0,
                        marginBottom: 12,
                        fontSize: 16,
                        fontWeight: 600,
                        color: "#262626",
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        display: "-webkit-box",
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: "vertical",
                        minHeight: 48,
                    }}
                >
                    {doc.title}
                </h3>

                <div style={{ fontSize: 12, color: "#8c8c8c", marginBottom: 12 }}>
                    <div>Cập nhật: {doc.changedAt ? new Date(doc.changedAt).toLocaleDateString("vi-VN") : "—"}</div>
                </div>

                {/* Action Buttons */}
                <div
                    style={{
                        display: "flex",
                        gap: 8,
                        borderTop: "1px solid #f0f0f0",
                        paddingTop: 12,
                    }}
                >
                    <Button
                        type="primary"
                        icon={<EyeOutlined />}
                        onClick={(e) => {
                            e.stopPropagation();
                            onPreview(doc);
                        }}
                        block
                        style={{ flex: 1 }}
                    >
                        Xem
                    </Button>
                    <Tooltip title="Tải xuống">
                        <Button
                            icon={<DownloadOutlined />}
                            onClick={handleDownloadClick}
                            style={{ flexShrink: 0 }}
                        />
                    </Tooltip>
                </div>
            </div>
        </Card>
    );
}



