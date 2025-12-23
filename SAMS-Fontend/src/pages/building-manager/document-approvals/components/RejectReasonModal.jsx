import React from "react";
import { Modal, Input } from "antd";

export default function RejectReasonModal({ open, reason, onChange, onSubmit, onCancel }) {
    return (
        <Modal
            open={open}
            title="Lý do từ chối"
            onOk={onSubmit}
            onCancel={onCancel}
            okText="Từ chối"
            okButtonProps={{ danger: true }}
            cancelText="Hủy"
            destroyOnClose
        >
            <Input.TextArea
                rows={4}
                placeholder="Nhập lý do (không bắt buộc)"
                value={reason}
                onChange={(e) => onChange(e.target.value)}
            />
        </Modal>
    );
}





