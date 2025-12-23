import React from "react";
import { Modal, Input } from "antd";

export default function RestoreRequestModal({ open, reason, onOk, onCancel, onReasonChange }) {
    return (
        <Modal open={open} title="Yêu cầu hiển thị lại" onOk={onOk} onCancel={onCancel} okText="Gửi yêu cầu" cancelText="Hủy" destroyOnClose>
            <div className="space-y-2">
                <p>Gửi yêu cầu để ban quản lý phê duyệt và hiển thị lại tài liệu cho cư dân.</p>
                <Input.TextArea rows={3} placeholder="Lý do (không bắt buộc)" value={reason} onChange={(e) => onReasonChange(e.target.value)} />
            </div>
        </Modal>
    );
}





