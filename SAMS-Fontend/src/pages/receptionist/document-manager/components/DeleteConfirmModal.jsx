import React from "react";
import { Modal, Input } from "antd";

export default function DeleteConfirmModal({ open, reason, onOk, onCancel, onReasonChange, isPermanent = false }) {
    const description = isPermanent
        ? "Tài liệu đang ngừng hiển thị. Bạn có chắc muốn xóa vĩnh viễn? Thao tác không thể hoàn tác."
        : "Tài liệu sẽ được chuyển sang trạng thái Ngừng hiển thị.";

    return (
        <Modal
            open={open}
            title={isPermanent ? "Xác nhận xóa vĩnh viễn" : "Xác nhận xóa tài liệu"}
            onOk={onOk}
            onCancel={onCancel}
            okText={isPermanent ? "Xóa vĩnh viễn" : "Ngừng hiển thị"}
            cancelText="Hủy"
            okButtonProps={{ danger: true }}
            destroyOnClose
        >
            <div className="space-y-2">
                <p>{description}</p>
                <Input placeholder="Lý do (không bắt buộc)" value={reason} onChange={(e) => onReasonChange(e.target.value)} />
            </div>
        </Modal>
    );
}





