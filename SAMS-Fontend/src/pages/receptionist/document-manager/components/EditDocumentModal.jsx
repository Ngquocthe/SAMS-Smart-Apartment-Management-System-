import React from "react";
import { Modal, Form, Input, Select } from "antd";
import { getCategoryOptions } from "../../../../features/documents/documentCategories";
import { getScopeOptions } from "../../../../features/documents/visibilityScopes";

export default function EditDocumentModal({ open, form, onOk, onCancel }) {
    const categoryOptions = getCategoryOptions();
    const scopeOptions = getScopeOptions();

    const handleOk = async () => {
        try {
            const values = await form.validateFields();
            onOk(values);
        } catch (error) {
            if (error?.errorFields) return;
            console.error("Validation error:", error);
        }
    };

    return (
        <Modal open={open} title="Chỉnh sửa thông tin" onOk={handleOk} onCancel={onCancel} okText="Lưu" cancelText="Hủy" destroyOnClose>
            <Form form={form} layout="vertical">
                <Form.Item name="title" label="Tiêu đề" rules={[{ required: true, message: "Nhập tiêu đề" }]}>
                    <Input />
                </Form.Item>
                <Form.Item name="category" label="Phân loại">
                    <Select
                        placeholder="Chọn loại tài liệu"
                        options={categoryOptions}
                        showSearch
                        filterOption={(input, option) => (option?.label ?? "").toLowerCase().includes(input.toLowerCase())}
                    />
                </Form.Item>
                <Form.Item name="visibilityScope" label="Phạm vi hiển thị">
                    <Select
                        placeholder="Chọn phạm vi hiển thị"
                        options={scopeOptions}
                        allowClear
                        showSearch
                        filterOption={(input, option) => (option?.label ?? "").toLowerCase().includes(input.toLowerCase())}
                        dropdownStyle={{ maxHeight: 240, overflowY: "auto" }}
                        listHeight={240}
                        virtual={false}
                        getPopupContainer={(triggerNode) => triggerNode.parentNode}
                    />
                </Form.Item>
            </Form>
        </Modal>
    );
}


