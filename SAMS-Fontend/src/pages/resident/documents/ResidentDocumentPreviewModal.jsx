import React, { useState, useEffect } from "react";
import { Modal, Button, Tooltip } from "antd";
import { EyeOutlined, ZoomInOutlined, ZoomOutOutlined } from "@ant-design/icons";
import documentsApi from "../../../features/documents/documentsApi";

export default function ResidentDocumentPreviewModal({
    open,
    record,
    onClose,
    onDownload,
    onNext,
    onPrev,
    currentIndex,
    totalCount,
}) {
    const [zoom, setZoom] = useState(1);
    const isImage = record && String(record.mimeType || "").toLowerCase().startsWith("image/");
    const isPdf = record && String(record.mimeType || "").toLowerCase() === "application/pdf";
    const viewUrl = record?.fileId ? documentsApi.buildViewUrl(record.fileId) : undefined;
    const downloadUrl = record?.fileId ? documentsApi.buildDownloadUrl(record.fileId) : undefined;

    // Reset zoom when record changes
    useEffect(() => {
        setZoom(1);
    }, [record?.documentId]);

    // Keyboard shortcuts
    useEffect(() => {
        if (!open) return;

        const handleKeyDown = (e) => {
            if (e.key === "Escape") {
                onClose();
            } else if (e.key === "ArrowLeft" && onPrev) {
                onPrev();
            } else if (e.key === "ArrowRight" && onNext) {
                onNext();
            } else if (e.key === "+" || e.key === "=") {
                e.preventDefault();
                setZoom((z) => Math.min(z + 0.2, 3));
            } else if (e.key === "-") {
                e.preventDefault();
                setZoom((z) => Math.max(z - 0.2, 0.5));
            } else if (e.key === "0") {
                e.preventDefault();
                setZoom(1);
            }
        };

        window.addEventListener("keydown", handleKeyDown);
        return () => window.removeEventListener("keydown", handleKeyDown);
    }, [open, onClose, onNext, onPrev]);

    if (!record) return null;

    const renderContent = () => {
        if (isImage && viewUrl) {
            // Background pattern for transparent images (checkerboard)
            const checkerboardPattern = {
                backgroundImage: `
                    linear-gradient(45deg, #f0f0f0 25%, transparent 25%),
                    linear-gradient(-45deg, #f0f0f0 25%, transparent 25%),
                    linear-gradient(45deg, transparent 75%, #f0f0f0 75%),
                    linear-gradient(-45deg, transparent 75%, #f0f0f0 75%)
                `,
                backgroundSize: "20px 20px",
                backgroundPosition: "0 0, 0 10px, 10px -10px, -10px 0px",
            };

            return (
                <div
                    className="flex items-center justify-center"
                    style={{
                        width: "100%",
                        height: "calc(85vh - 140px)",
                        minHeight: "400px",
                        backgroundColor: "#f5f5f5",
                        position: "relative",
                        overflow: "auto",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        padding: "20px",
                        ...checkerboardPattern,
                    }}
                >
                    <div
                        style={{
                            position: "relative",
                            display: "inline-block",
                            maxWidth: "100%",
                            maxHeight: "100%",
                            boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
                            borderRadius: "4px",
                            overflow: "hidden",
                            backgroundColor: "#fff",
                        }}
                    >
                        <img
                            src={viewUrl}
                            alt={record.originalFileName || record.title}
                            style={{
                                maxWidth: "100%",
                                maxHeight: "calc(85vh - 180px)",
                                width: "auto",
                                height: "auto",
                                objectFit: "contain",
                                transform: `scale(${zoom})`,
                                transition: "transform 0.2s ease-out",
                                display: "block",
                                margin: "0 auto",
                            }}
                        />
                    </div>
                    {/* Zoom controls for images */}
                    {isImage && (
                        <div
                            style={{
                                position: "absolute",
                                bottom: 20,
                                right: 20,
                                display: "flex",
                                gap: 8,
                                backgroundColor: "rgba(0,0,0,0.75)",
                                padding: "8px 12px",
                                borderRadius: 8,
                                backdropFilter: "blur(4px)",
                                boxShadow: "0 2px 8px rgba(0,0,0,0.3)",
                            }}
                        >
                            <Tooltip title="Phóng to (+)">
                                <Button
                                    type="text"
                                    icon={<ZoomInOutlined />}
                                    onClick={() => setZoom((z) => Math.min(z + 0.2, 3))}
                                    style={{ color: "#fff" }}
                                />
                            </Tooltip>
                            <span style={{ color: "#fff", padding: "0 8px", display: "flex", alignItems: "center", minWidth: "50px", justifyContent: "center", fontSize: "13px" }}>
                                {Math.round(zoom * 100)}%
                            </span>
                            <Tooltip title="Thu nhỏ (-)">
                                <Button
                                    type="text"
                                    icon={<ZoomOutOutlined />}
                                    onClick={() => setZoom((z) => Math.max(z - 0.2, 0.5))}
                                    style={{ color: "#fff" }}
                                />
                            </Tooltip>
                            <Tooltip title="Đặt lại (0)">
                                <Button
                                    type="text"
                                    onClick={() => setZoom(1)}
                                    style={{ color: "#fff", fontSize: 12, padding: "0 8px" }}
                                >
                                    Reset
                                </Button>
                            </Tooltip>
                        </div>
                    )}
                </div>
            );
        }

        if (isPdf && viewUrl) {
            return (
                <div className="w-full" style={{ height: "75vh", maxHeight: "calc(85vh - 60px)", backgroundColor: "#525252" }}>
                    <iframe src={viewUrl} width="100%" height="100%" style={{ border: "none" }} title={record.title} />
                </div>
            );
        }

        if (downloadUrl) {
            return (
                <div className="text-center py-8" style={{ minHeight: "75vh", maxHeight: "calc(85vh - 60px)", display: "flex", flexDirection: "column", justifyContent: "center" }}>
                    <div className="mb-4">
                        <div style={{ fontSize: 48, marginBottom: 16 }}>📄</div>
                        <p className="text-gray-600 mb-4">File này không thể xem trước. Bạn có thể mở hoặc tải xuống.</p>
                    </div>
                    <div className="flex gap-3 justify-center">
                        <Button type="primary" icon={<EyeOutlined />} onClick={() => window.open(downloadUrl, "_blank")}>
                            Mở file
                        </Button>
                    </div>
                </div>
            );
        }

        return (
            <div className="text-center text-gray-500 py-8" style={{ minHeight: "75vh", maxHeight: "calc(85vh - 60px)", display: "flex", flexDirection: "column", justifyContent: "center" }}>
                <div style={{ fontSize: 48, marginBottom: 16 }}>📄</div>
                <p>Không có file để hiển thị</p>
            </div>
        );
    };

    return (
        <Modal
            open={open}
            title={
                <div style={{ display: "flex", alignItems: "center", padding: "8px 0" }}>
                    <span style={{ fontWeight: 600 }}>{record.title}</span>
                </div>
            }
            footer={null}
            onCancel={onClose}
            width="60vw"
            style={{ top: 100, bottom: 40, paddingBottom: 0, margin: "0 auto" }}
            bodyStyle={{ padding: 0, overflow: "hidden", marginTop: 0 }}
            destroyOnClose
            closable={false}
            maskClosable={true}
            centered={false}
        >
            {renderContent()}
        </Modal>
    );
}



