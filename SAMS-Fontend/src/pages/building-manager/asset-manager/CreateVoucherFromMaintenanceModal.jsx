import React, { useEffect, useState } from 'react';
import { Modal, Form, Input, Select } from 'antd';
import { ExclamationCircleOutlined } from '@ant-design/icons';

const { TextArea } = Input;

export default function CreateVoucherFromMaintenanceModal({
  open,
  onCancel,
  onSubmit,
  form,
  serviceTypes = [],
  serviceTypesLoading = false,
  maintenanceServiceTypeId = null,
  submitting = false,
  error = null
}) {
  const [amountError, setAmountError] = useState('');

  // Format price with thousand separators
  const formatPrice = (value) => {
    if (!value) return '';
    const numericValue = value.toString().replace(/[^\d]/g, '');
    return numericValue.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  };

  // Parse formatted price to number
  const parseFormattedPrice = (value) => {
    if (!value) return '';
    return value.replace(/,/g, '');
  };

  useEffect(() => {
    if (open && maintenanceServiceTypeId && form) {
      // Tự động set serviceTypeId khi mở modal
      form.setFieldsValue({
        serviceTypeId: maintenanceServiceTypeId
      });
    }
  }, [open, maintenanceServiceTypeId, form]);

  useEffect(() => {
    if (!open) {
      setAmountError('');
    }
  }, [open]);

  const handleOk = async () => {
    try {
      const values = await form.validateFields();
      // Convert formatted amount to number
      const amountValue = parseInt(parseFormattedPrice(values.amount));
      await onSubmit({ ...values, amount: amountValue });
    } catch (error) {
      // Validation errors sẽ được Ant Design tự xử lý
      if (!error.errorFields) {
        console.error('Validation error:', error);
      }
    }
  };

  const handleAmountChange = (e) => {
    const value = e.target.value;
    const formatted = formatPrice(value);
    const numericValue = parseInt(parseFormattedPrice(formatted));

    // Update form value
    form.setFieldsValue({ amount: formatted });

    // Real-time validation
    if (formatted && !isNaN(numericValue)) {
      if (numericValue < 10000) {
        setAmountError('Số tiền tối thiểu là 10.000 VNĐ');
      } else if (numericValue > 100000000) {
        setAmountError('Số tiền tối đa là 100.000.000 VNĐ');
      } else {
        setAmountError('');
      }
    } else if (formatted) {
      setAmountError('Vui lòng nhập số hợp lệ');
    } else {
      setAmountError('');
    }
  };

  return (
    <Modal
      open={open}
      onCancel={onCancel}
      onOk={handleOk}
      title="Tạo phiếu chi từ lịch sử bảo trì"
      okText="Xác nhận"
      cancelText="Hủy"
      okButtonProps={{ loading: submitting, disabled: submitting }}
      cancelButtonProps={{ disabled: submitting }}
      width={600}
    >
      <Form
        form={form}
        layout="vertical"
        onFinish={handleOk}
      >
        <Form.Item
          name="serviceTypeId"
          label="Loại dịch vụ"
          rules={[{ required: true, message: 'Loại dịch vụ là bắt buộc' }]}
        >
          <Select
            placeholder="Chọn loại dịch vụ"
            options={serviceTypes.map(st => ({
              label: st.name || st.serviceTypeName,
              value: st.id || st.serviceTypeId
            }))}
            loading={serviceTypesLoading}
            disabled={!!maintenanceServiceTypeId}
            showSearch
            filterOption={(input, option) =>
              (option?.label || '').toLowerCase().includes((input || '').toLowerCase())
            }
          />
        </Form.Item>

        <Form.Item
          name="amount"
          label="Số tiền"
          rules={[
            { required: true, message: 'Vui lòng nhập số tiền' },
            {
              validator: (_, value) => {
                if (!value) return Promise.reject('Vui lòng nhập số tiền');
                const numericValue = parseInt(parseFormattedPrice(value));
                if (isNaN(numericValue)) return Promise.reject('Vui lòng nhập số hợp lệ');
                if (numericValue < 10000) return Promise.reject('Số tiền phải từ 10.000 VNĐ trở lên');
                if (numericValue > 100000000) return Promise.reject('Số tiền không được vượt quá 100.000.000 VNĐ');
                return Promise.resolve();
              }
            }
          ]}
          validateStatus={amountError ? 'error' : ''}
          help={amountError}
        >
          <Input
            placeholder="Nhập số tiền (10.000 - 100.000.000 VNĐ)"
            onChange={handleAmountChange}
          />
        </Form.Item>

        <Form.Item
          name="companyInfo"
          label="Bên sửa chữa"
          rules={[
            { required: true, message: 'Bên sửa chữa là bắt buộc' },
            { min: 3, message: 'Bên sửa chữa phải có ít nhất 3 ký tự' },
            { max: 500, message: 'Bên sửa chữa không được vượt quá 500 ký tự' },
            { 
              pattern: /^[a-zA-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ0-9\s]+$/, 
              message: 'Bên sửa chữa không được chứa ký tự đặc biệt' 
            }
          ]}
        >
          <Input
            placeholder="Nhập tên bên sửa chữa"
            maxLength={500}
            showCount
          />
        </Form.Item>

        <Form.Item
          name="note"
          label="Ghi chú"
          rules={[
            { min: 3, message: 'Ghi chú phải có ít nhất 3 ký tự (nếu nhập)' },
            { max: 1000, message: 'Ghi chú không được vượt quá 1000 ký tự' }
          ]}
        >
          <TextArea
            rows={4}
            placeholder="Nhập ghi chú (tùy chọn, tối đa 1000 ký tự)"
            maxLength={1000}
            showCount
          />
        </Form.Item>

        {error && (
          <div style={{ 
            marginTop: 16, 
            padding: '12px 16px', 
            background: '#fff2f0', 
            border: '1px solid #ffccc7', 
            borderRadius: 4,
            color: '#ff4d4f'
          }}>
            <ExclamationCircleOutlined style={{ marginRight: 8 }} />
            {error}
          </div>
        )}
      </Form>
    </Modal>
  );
}

