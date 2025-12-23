import React, { useState, useEffect } from "react";
import { Modal, Form, Input, Select, InputNumber } from "antd";
import { getCurrentPrice } from "../../features/accountant/servicepricesApi";

export default function CreateVoucherModal({ open, onCancel, onOk, form, serviceTypes = [], serviceTypesLoading = false, disabled = false }) {
    const [loadingPrice, setLoadingPrice] = useState(false);
    const [hasFixedPrice, setHasFixedPrice] = useState(false);

    useEffect(() => {
        if (open) {
            setHasFixedPrice(false);
        }
    }, [open, form]);

    const handleServiceTypeChange = async (serviceTypeId) => {
        if (!serviceTypeId) {
            form.setFieldsValue({ amount: undefined });
            setHasFixedPrice(false);
            return;
        }

        setLoadingPrice(true);
        try {
            const response = await getCurrentPrice(serviceTypeId);
            if (response?.unitPrice != null) {
                const price = response.unitPrice;
                form.setFieldsValue({ amount: price });
                setHasFixedPrice(true);
            } else {
                form.setFieldsValue({ amount: undefined });
                setHasFixedPrice(false);
            }
        } catch (error) {
            form.setFieldsValue({ amount: undefined });
            setHasFixedPrice(false);
            console.log("Không tìm thấy giá cho service type:", serviceTypeId);
        } finally {
            setLoadingPrice(false);
        }
    };

    return (
        <Modal open={open} onCancel={onCancel} onOk={onOk} title="Tạo phiếu chi"
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
                    name="amount"
                    label="Số tiền"
                    rules={[
                        { required: true, message: 'Nhập số tiền' },
                        {
                            validator: (_, value) => {
                                if (value == null) return Promise.resolve();
                                if (Number(value) <= 0) {
                                    return Promise.reject(new Error('Số tiền phải lớn hơn 0'));
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
                        disabled={hasFixedPrice}
                        loading={loadingPrice}
                    />
                </Form.Item>
                <Form.Item name="note" label="Ghi chú">
                    <Input.TextArea rows={3} placeholder="Ghi chú" />
                </Form.Item>
            </Form>
        </Modal>
    );
}


