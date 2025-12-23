import React from "react";
import { Modal, Button, Tag } from "antd";
import { EyeOutlined, DownloadOutlined } from "@ant-design/icons";

export default function DocumentVersionsModal({ open, record, loading, versions, onClose, onPreviewVersion, onDownloadVersion }) {
    return (
        <Modal open={open} title={`Phiên bản của tài liệu: ${record?.title || ""}`} onCancel={onClose} footer={null} width={800} destroyOnClose>
            <div style={{ maxHeight: "500px", overflowY: "auto" }}>
                {loading ? (
                    <div
                        style={{
                            textAlign: "center",
                            padding: "40px 20px",
                            display: "flex",
                            flexDirection: "column",
                            alignItems: "center",
                            gap: "16px",
                        }}
                    >
                        <div className="ant-spin ant-spin-lg">
                            <span className="ant-spin-dot ant-spin-dot-spin">
                                <i className="ant-spin-dot-item"></i>
                                <i className="ant-spin-dot-item"></i>
                                <i className="ant-spin-dot-item"></i>
                                <i className="ant-spin-dot-item"></i>
                            </span>
                        </div>
                        <div style={{ color: "#666", fontSize: "14px" }}>Đang tải danh sách phiên bản...</div>
                    </div>
                ) : versions.length === 0 ? (
                    <div
                        style={{
                            textAlign: "center",
                            padding: "40px 20px",
                            color: "#999",
                            display: "flex",
                            flexDirection: "column",
                            alignItems: "center",
                            gap: "8px",
                        }}
                    >
                        <div style={{ fontSize: "48px" }}>📄</div>
                        <div>Không có phiên bản nào</div>
                    </div>
                ) : (
                    <div className="space-y-3">
                        {versions.map((version, index) => (
                            <div key={version.id || version.versionNo || index} className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 transition-colors">
                                <div className="flex justify-between items-start">
                                    <div className="flex-1">
                                        <div className="flex items-center gap-2 mb-2">
                                            <Tag color="blue">Phiên bản {version.versionNo || index + 1}</Tag>
                                            {version.isLatest && <Tag color="green">Mới nhất</Tag>}
                                        </div>
                                        <div className="text-sm text-gray-600 mb-1">
                                            <strong>Tên file:</strong> {version.originalFileName || version.fileName || record?.originalFileName || "Không có tên"}
                                        </div>
                                        <div className="text-sm text-gray-600 mb-1">
                                            <strong>Ngày tạo:</strong> {version.changedAt ? new Date(version.changedAt).toLocaleString("vi-VN") : "Không xác định"}
                                        </div>
                                        {version.note && (
                                            <div className="text-sm text-gray-600">
                                                <strong>Ghi chú:</strong> {version.note}
                                            </div>
                                        )}
                                    </div>
                                    <div className="flex gap-2 ml-4">
                                        {version.fileId && (
                                            <Button type="link" icon={<EyeOutlined />} onClick={() => onPreviewVersion(version)}>
                                                Xem
                                            </Button>
                                        )}
                                        {version.fileId && (
                                            <Button type="link" icon={<DownloadOutlined />} onClick={() => onDownloadVersion(version.fileId)}>
                                                Tải
                                            </Button>
                                        )}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </Modal>
    );
}





