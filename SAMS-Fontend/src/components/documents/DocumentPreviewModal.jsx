import React, { useMemo } from "react";
import { Modal, Button } from "antd";
import { EyeOutlined, DownloadOutlined } from "@ant-design/icons";
import documentsApi from "../../features/documents/documentsApi";

export default function DocumentPreviewModal({ open, record, onClose, onDownload }) {
    const previewContent = useMemo(() => {
        if (!record) return null;
        const isImage = String(record.mimeType || "").toLowerCase().startsWith("image/");
        const isPdf = String(record.mimeType || "").toLowerCase() === "application/pdf";
        const viewUrl = record.fileId ? documentsApi.buildViewUrl(record.fileId) : undefined;
        const downloadUrl = record.fileId ? documentsApi.buildDownloadUrl(record.fileId) : undefined;

        const wrapperStyle = {
            background: "linear-gradient(145deg,#f4f6fb,#fff)",
            borderRadius: 16,
            padding: 24,
            minHeight: "50vh",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            border: "1px solid #e5e7eb",
        };

        if (isImage && viewUrl) {
            return (
                <div style={wrapperStyle}>
                    <div
                        style={{
                            background: "#fff",
                            borderRadius: 16,
                            padding: 12,
                            width: "100%",
                            maxWidth: "100%",
                            maxHeight: "70vh",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            boxShadow: "0 12px 40px rgba(15,23,42,0.12)",
                        }}
                    >
                        <img
                            src={viewUrl}
                            alt={record.originalFileName || record.title}
                            style={{
                                width: "100%",
                                height: "100%",
                                maxHeight: "65vh",
                                objectFit: "contain",
                                borderRadius: 12,
                                background: "#fff",
                            }}
                        />
                    </div>
                </div>
            );
        }

        if (isPdf && viewUrl) {
            return (
                <div>
                    <div className="mb-3 flex flex-wrap gap-2 justify-between items-center">
                        <h4 className="font-semibold m-0">Trình xem PDF</h4>
                        <div className="flex gap-2">
                            <Button type="primary" icon={<EyeOutlined />} onClick={() => window.open(viewUrl, "_blank")} size="small">
                                Mở tab mới
                            </Button>
                            <Button icon={<DownloadOutlined />} onClick={() => onDownload?.(record.fileId)} size="small">
                                Tải xuống
                            </Button>
                        </div>
                    </div>
                    <div
                        style={{
                            height: "70vh",
                            borderRadius: 16,
                            overflow: "hidden",
                            boxShadow: "0 12px 32px rgba(15,23,42,0.12)",
                            border: "1px solid #e5e7eb",
                        }}
                    >
                        <iframe src={viewUrl} width="100%" height="100%" style={{ border: "none" }} title={record.originalFileName || "Tài liệu PDF"} />
                    </div>
                </div>
            );
        }

        if (downloadUrl) {
            return (
                <div className="text-center py-8">
                    <div className="mb-4">
                        <div style={{ fontSize: "48px", marginBottom: "16px" }}>📄</div>
                        <p className="text-gray-600 mb-4">File này không thể xem trước. Bạn có thể mở hoặc tải xuống.</p>
                    </div>
                    <div className="flex gap-3 justify-center">
                        <Button type="primary" icon={<EyeOutlined />} onClick={() => window.open(downloadUrl, "_blank")}>
                            Mở file
                        </Button>
                        <Button icon={<DownloadOutlined />} onClick={() => onDownload?.(record.fileId)}>
                            Tải xuống
                        </Button>
                    </div>
                </div>
            );
        }

        return (
            <div className="text-center text-gray-500 py-8">
                <div style={{ fontSize: "48px", marginBottom: "16px" }}>📄</div>
                <p>Không có file để hiển thị</p>
            </div>
        );
    }, [record, onDownload]);

    return (
        <Modal
            open={open}
            title={record?.title || "Xem tài liệu"}
            footer={null}
            onCancel={onClose}
            width={960}
            destroyOnClose
            bodyStyle={{ background: "#f8fafc", padding: "16px 24px" }}
        >
            {record && previewContent}
        </Modal>
    );
}





