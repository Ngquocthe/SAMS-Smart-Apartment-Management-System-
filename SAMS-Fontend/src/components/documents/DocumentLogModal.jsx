import React from "react";
import { Modal, List, Tag, Empty } from "antd";

const actionColors = {
    CREATE: "green",
    NEW_VERSION: "blue",
    CHANGE_STATUS: "orange",
    UPDATE_METADATA: "purple",
    SOFT_DELETE: "red",
    RESTORE: "cyan",
    REQUEST_APPROVAL: "gold",
    REQUEST_RESTORE: "lime",
    REQUEST_DELETE: "red",
};

const actionLabels = {
    CREATE: "Tạo mới",
    NEW_VERSION: "Phiên bản mới",
    CHANGE_STATUS: "Thay đổi trạng thái",
    UPDATE_METADATA: "Cập nhật thông tin",
    SOFT_DELETE: "Xóa",
    RESTORE: "Khôi phục",
    REQUEST_APPROVAL: "Yêu cầu duyệt",
    REQUEST_RESTORE: "Yêu cầu hiển thị lại",
    REQUEST_DELETE: "Yêu cầu xóa",
};

const getActionLabel = (action) => {
    return actionLabels[action] || action;
};

export default function DocumentLogModal({ open, loading, logs, documentTitle, onClose }) {
    return (
        <Modal
            open={open}
            title={`Lịch sử thao tác: ${documentTitle || ""}`}
            onCancel={onClose}
            footer={null}
            width={720}
            destroyOnClose
        >
            {logs.length === 0 && !loading ? (
                <Empty description="Chưa có log nào" />
            ) : (
                <List
                    loading={loading}
                    itemLayout="vertical"
                    dataSource={logs}
                    renderItem={(item) => (
                        <List.Item key={item.actionLogId}>
                            <List.Item.Meta
                                title={
                                    <div className="flex items-center gap-2">
                                        <Tag color={actionColors[item.action] || "default"}>{getActionLabel(item.action)}</Tag>
                                        <span className="text-xs text-gray-500">
                                            {item.actionAt ? new Date(item.actionAt).toLocaleString("vi-VN") : ""}
                                        </span>
                                    </div>
                                }
                                description={
                                    <div className="text-sm text-gray-600">
                                        <div><strong>Người thực hiện:</strong> {item.actorName || item.actorId || "—"}</div>
                                        {item.detail && (
                                            <div className="mt-1 whitespace-pre-wrap">
                                                <strong>Chi tiết:</strong> {item.detail}
                                            </div>
                                        )}
                                    </div>
                                }
                            />
                        </List.Item>
                    )}
                />
            )}
        </Modal>
    );
}

