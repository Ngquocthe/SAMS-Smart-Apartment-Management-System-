import React, { useState, useEffect } from "react";
import { Modal, Form, Select, InputNumber, Input } from "antd";
import { getCurrentPrice } from "../../features/accountant/servicepricesApi";

export default function CreateInvoiceModal({ open, onCancel, onOk, form, serviceTypes = [], serviceTypesLoading = false, disabled = false }) {
    const [loadingPrice, setLoadingPrice] = useState(false);
    const [hasFixedPrice, setHasFixedPrice] = useState(false);

    useEffect(() => {
        if (open) {
            form.setFieldsValue({ quantity: 1 });
        }
    }, [open, form]);

    const handleServiceTypeChange = async (serviceTypeId) => {
        if (!serviceTypeId) {
            form.setFieldsValue({ unitPrice: undefined });
            setHasFixedPrice(false);
            return;
        }

        // Kiểm tra xem service type có giá trong service_prices không
        setLoadingPrice(true);
        try {
            const response = await getCurrentPrice(serviceTypeId);
            if (response?.unitPrice != null) {
                // Nếu có giá trong service_prices, tự động điền và disable trường input
                form.setFieldsValue({ unitPrice: response.unitPrice });
                setHasFixedPrice(true);
            } else {
                // Nếu không có giá, cho phép nhập tự do
                form.setFieldsValue({ unitPrice: undefined });
                setHasFixedPrice(false);
            }
        } catch (error) {
            // Nếu không tìm thấy giá hoặc lỗi, cho phép nhập tự do
            form.setFieldsValue({ unitPrice: undefined });
            setHasFixedPrice(false);
            console.log('Không tìm thấy giá cho service type:', serviceTypeId);
        } finally {
            setLoadingPrice(false);
        }
    };

    // Reset state khi modal đóng
    const handleCancel = () => {
        setHasFixedPrice(false);
        setLoadingPrice(false);
        onCancel();
    };

    return (
        <Modal open={open} onCancel={handleCancel} onOk={onOk} title="Tạo hóa đơn"
            okButtonProps={{ disabled }}
        >
            <Form layout="vertical" form={form}>
                <Form.Item name="serviceTypeId" label="Loại dịch vụ" rules={[{ required: true, message: 'Chọn loại dịch vụ' }]}>
                    <Select
                        placeholder="Chọn loại dịch vụ"
                        options={serviceTypes}
                        loading={serviceTypesLoading}
                        showSearch
                        filterOption={(input, option) => (option?.label || '').toLowerCase().includes((input || '').toLowerCase())}
                        onChange={handleServiceTypeChange}
                    />
                </Form.Item>
                <Form.Item
                    name="unitPrice"
                    label="Đơn giá"
                    rules={[
                        { required: true, message: 'Nhập đơn giá' },
                        {
                            validator: (_, value) => {
                                if (value == null) return Promise.resolve();
                                if (Number(value) <= 0) {
                                    return Promise.reject(new Error('Đơn giá không được âm'));
                                }
                                return Promise.resolve();
                            }
                        }
                    ]}
                    extra={hasFixedPrice ? "Loại dịch vụ này đã có giá cố định trong hệ thống, không thể chỉnh sửa" : null}
                >
                    <InputNumber
                        min={0}
                        placeholder="0"
                        style={{ width: '100%' }}
                        loading={loadingPrice}
                        disabled={loadingPrice || hasFixedPrice}
                    />
                </Form.Item>
                <Form.Item
                    name="quantity"
                    label="Số lượng"
                    rules={[
                        { required: true, message: 'Nhập số lượng' },
                        {
                            validator: (_, value) => {
                                if (value == null) return Promise.resolve();
                                if (Number(value) <= 0) {
                                    return Promise.reject(new Error('Số lượng phải lớn hơn 0'));
                                }
                                return Promise.resolve();
                            }
                        }
                    ]}
                >
                    <InputNumber
                        min={1}
                        placeholder="1"
                        style={{ width: '100%' }}
                    />
                </Form.Item>
                <Form.Item name="note" label="Ghi chú">
                    <Input.TextArea rows={3} placeholder="Ghi chú" />
                </Form.Item>
            </Form>
        </Modal>
    );
}


