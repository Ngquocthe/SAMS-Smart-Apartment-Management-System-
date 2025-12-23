import React from "react";
import { Modal, Form, Input, Select, Upload, Button, message } from "antd";
import { UploadOutlined } from "@ant-design/icons";
import { getCategoryOptions } from "../../features/documents/documentCategories";
import { getScopeOptions } from "../../features/documents/visibilityScopes";

// File validation constants for documents
const MAX_FILE_SIZE_DOCUMENT = 100 * 1024 * 1024; // 100MB
const ALLOWED_DOCUMENT_TYPES = ".jpg,.jpeg,.png,.gif,.bmp,.webp,.pdf,.doc,.docx,.xls,.xlsx,.txt,.csv";
const DANGEROUS_EXTENSIONS = ['.exe', '.bat', '.cmd', '.com', '.scr', '.vbs', '.js', '.jar', '.msi', '.dll', '.sh', '.ps1'];

const formatFileSize = (bytes) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
};

const formatFileSizeMb = (bytes) => `${(bytes / (1024 * 1024)).toFixed(1)}MB`;
const isFileTooLarge = (file) => !!file && file.size > MAX_FILE_SIZE_DOCUMENT;
const getOversizeMessage = (size) => `Dung lượng tối đa 100MB (file hiện ${formatFileSizeMb(size)})`;

const validateDocumentFile = (file) => {
    // Check file size
    if (file.size > MAX_FILE_SIZE_DOCUMENT) {
        message.error(`File "${file.name}" quá lớn. Dung lượng tối đa: ${formatFileSize(MAX_FILE_SIZE_DOCUMENT)}`);
        return Upload.LIST_IGNORE;
    }

    // Check file extension
    const fileName = file.name.toLowerCase();
    const extension = fileName.substring(fileName.lastIndexOf('.'));

    if (!extension || extension === fileName) {
        message.error(`File "${file.name}" không có phần mở rộng.`);
        return Upload.LIST_IGNORE;
    }

    // Check dangerous extensions
    if (DANGEROUS_EXTENSIONS.includes(extension)) {
        message.error(`File "${file.name}" không được phép upload vì lý do bảo mật.`);
        return Upload.LIST_IGNORE;
    }

    // Check allowed types
    const allowedExtensions = ALLOWED_DOCUMENT_TYPES.split(',');
    if (!allowedExtensions.some(ext => fileName.endsWith(ext.trim()))) {
        message.error(`File "${file.name}" không được hỗ trợ. Chỉ chấp nhận: ${ALLOWED_DOCUMENT_TYPES}`);
        return Upload.LIST_IGNORE;
    }

    return true;
};

export default function CreateDocumentModal({ open, form, onOk, onCancel, onBeforeUpload }) {
    const categoryOptions = getCategoryOptions();
    const scopeOptions = getScopeOptions();

    const handleOk = async () => {
        try {
            const values = await form.validateFields();
            if (!values.file?.file) {
                message.warning("Vui lòng chọn file cho phiên bản đầu tiên");
                return;
            }

            const selectedFile = values.file.file;

            // Validate file
            const isValid = validateDocumentFile(selectedFile);
            if (isValid === Upload.LIST_IGNORE) {
                return;
            }

            if (isFileTooLarge(selectedFile)) {
                message.error(getOversizeMessage(selectedFile.size));
                return;
            }

            onOk(values);
        } catch (error) {
            if (error?.errorFields) return;
            console.error("Validation error:", error);
        }
    };

    return (
        <Modal open={open} title="Tạo tài liệu" onOk={handleOk} onCancel={onCancel} okText="Tạo" cancelText="Hủy" destroyOnHidden>
            <Form form={form} layout="vertical">
                <Form.Item name="title" label="Tiêu đề" rules={[{ required: true, message: "Nhập tiêu đề" }]}>
                    <Input placeholder="Ví dụ: Quy trình tiếp khách" />
                </Form.Item>
                <Form.Item name="category" label="Phân loại" rules={[{ required: true, message: "Chọn phân loại" }]}>
                    <Select
                        placeholder="Chọn loại tài liệu"
                        options={categoryOptions}
                        allowClear
                        showSearch
                        filterOption={(input, option) => (option?.label ?? "").toLowerCase().includes(input.toLowerCase())}
                    />
                </Form.Item>
                <Form.Item name="visibilityScope" label="Phạm vi hiển thị" rules={[{ required: true, message: "Chọn phạm vi hiển thị" }]}>
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
                <Form.Item
                    name="file"
                    label="Tệp tin"
                    valuePropName="file"
                    rules={[{ required: true, message: "Chọn file" }]}
                    extra={`Hỗ trợ: JPG, PNG, GIF, BMP, WEBP, PDF, DOC, DOCX, XLS, XLSX, TXT, CSV (tối đa ${formatFileSize(MAX_FILE_SIZE_DOCUMENT)})`}
                >
                    <Upload
                        beforeUpload={(file) => {
                            const isValid = validateDocumentFile(file);
                            if (isValid === Upload.LIST_IGNORE) {
                                return Upload.LIST_IGNORE;
                            }
                            return onBeforeUpload ? onBeforeUpload(file) : false;
                        }}
                        maxCount={1}
                        accept={ALLOWED_DOCUMENT_TYPES}
                    >
                        <Button icon={<UploadOutlined />}>Chọn file</Button>
                    </Upload>
                </Form.Item>
            </Form>
        </Modal>
    );
}










