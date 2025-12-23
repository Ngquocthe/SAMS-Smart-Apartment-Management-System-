import React, { useState, useEffect } from 'react';
import { 
  Modal, 
  Form, 
  Input, 
  Select, 
  DatePicker, 
  Checkbox, 
  Button, 
  Row, 
  Col, 
  Space, 
  Alert, 
  Spin 
} from 'antd';
import dayjs from 'dayjs';
import { cardsApi } from '../../../features/building-management/cardsApi';
import { apartmentsApi } from '../../../features/building-management/apartmentsApi';

const { Option } = Select;

export default function UpdateCard({ show, onHide, onSuccess, card, onShowToast, existingCards = [] }) {
  const [form] = Form.useForm();
  const [formData, setFormData] = useState({
    cardNumber: '',
    issuedDate: null,
    expiredDate: null,
    status: 'ACTIVE',
    capabilities: [],
    apartmentNumber: ''
  });

  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});
  const [apiError, setApiError] = useState('');
  const [availableCapabilities, setAvailableCapabilities] = useState([]);
  const [validatedApartmentId, setValidatedApartmentId] = useState(null);
  const [isCheckingApartment, setIsCheckingApartment] = useState(false);

  useEffect(() => {
    const loadCardData = async () => {
      if (card) {
        try {
          const capabilities = await cardsApi.getCardCapabilities(card.cardId);
          const availableCapabilitiesData = await cardsApi.getCardTypes();
          setAvailableCapabilities(availableCapabilitiesData);
          
          const apartmentNumber = card.issuedToApartmentNumber || '';
          
          const cardData = {
            cardNumber: card.cardNumber || '',
            issuedDate: card.issuedDate ? dayjs(card.issuedDate) : null,
            expiredDate: card.expiredDate ? dayjs(card.expiredDate) : null,
            status: card.status || 'ACTIVE',
            capabilities: capabilities.map(cap => cap.cardTypeId),
            apartmentNumber: apartmentNumber
          };

          setFormData(cardData);
          form.setFieldsValue(cardData);
          
          if (card.issuedToApartmentId) {
            setValidatedApartmentId(card.issuedToApartmentId);
          } else {
            setValidatedApartmentId(null);
          }
          
          setErrors({});
          setApiError('');
        } catch (error) {
          setApiError('Không thể tải dữ liệu thẻ');
        }
      }
    };

    if (show && card) {
      loadCardData();
    } else if (!show) {
      form.resetFields();
      setFormData({
        cardNumber: '',
        issuedDate: null,
        expiredDate: null,
        status: 'ACTIVE',
        capabilities: [],
        apartmentNumber: ''
      });
      setErrors({});
      setApiError('');
      setValidatedApartmentId(null);
    }
  }, [card, show, form]);

  useEffect(() => {
    if (!formData.apartmentNumber || formData.apartmentNumber.trim() === '') {
      setValidatedApartmentId(null);
      setErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors.apartmentNumber;
        return newErrors;
      });
      return;
    }

    const checkApartment = async () => {
      setIsCheckingApartment(true);
      try {
        const apartment = await apartmentsApi.getByNumber(formData.apartmentNumber.trim());
        
        if (apartment && apartment.apartmentId) {
          setValidatedApartmentId(apartment.apartmentId);
          setErrors(prev => {
            const newErrors = { ...prev };
            delete newErrors.apartmentNumber;
            return newErrors;
          });
        } else {
          setValidatedApartmentId(null);
          setErrors(prev => ({
            ...prev,
            apartmentNumber: 'Không tìm thấy căn hộ này trong hệ thống'
          }));
        }
      } catch (error) {
        setValidatedApartmentId(null);
        
        let errorMessage = 'Không thể kiểm tra căn hộ';
        if (error.response?.status === 404) {
          errorMessage = `Căn hộ "${formData.apartmentNumber.trim()}" không tồn tại`;
        } else if (error.response?.data?.message) {
          errorMessage = error.response.data.message;
        } else if (typeof error.response?.data === 'string') {
          errorMessage = error.response.data;
        } else if (error.message) {
          errorMessage = error.message;
        }
        
        setErrors(prev => ({
          ...prev,
          apartmentNumber: errorMessage
        }));
      } finally {
        setIsCheckingApartment(false);
      }
    };

    const timeoutId = setTimeout(() => {
      checkApartment();
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [formData.apartmentNumber]);

  const handleChange = (name, value) => {
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));

    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: '' }));
    }

    validateField(name, value);
  };

  const validateField = (name, value) => {
    let error = '';
    const allCardNumbers = existingCards.map(c => c.cardNumber.toLowerCase()).filter(n => n !== card?.cardNumber.toLowerCase());

    switch (name) {
      case 'cardNumber':
        if (!value) {
          error = 'Số thẻ là bắt buộc';
        } else {
          const parts = value.split('-');
          if (parts.length !== 3) {
            error = 'Số thẻ phải có 3 phần cách nhau bởi dấu gạch ngang: CARD-{Mã căn hộ}-{Số thứ tự}';
          } else {
            if (parts[0] !== 'CARD') {
              if (parts[0].toUpperCase() === 'CARD') {
                error = 'Phần "CARD" phải viết hoa. Ví dụ: CARD-A1001-01';
              } else {
                error = 'Số thẻ phải bắt đầu bằng "CARD" (viết hoa)';
              }
            } else if (!parts[1] || parts[1].trim() === '') {
              error = 'Mã căn hộ không được để trống';
            } else {
              const firstChar = parts[1].charAt(0);
              if (!/^[A-Z]$/.test(firstChar)) {
                if (/^[a-z]$/.test(firstChar)) {
                  error = 'Tên tòa phải viết hoa. Ví dụ: CARD-A1001-01 (A là tên tòa, phải viết hoa)';
                } else {
                  error = 'Mã căn hộ phải bắt đầu bằng chữ cái viết hoa (tên tòa). Ví dụ: A1001';
                }
              } else if (!/^[A-Z][0-9]{4}$/.test(parts[1])) {
                error = 'Mã căn hộ phải có định dạng: {Tên tòa}{4 số}. Ví dụ: A1001 (A là tên tòa viết hoa, 1001 là 4 số gồm số tầng và số căn hộ)';
              } else if (!/^\d{2,}$/.test(parts[2]) || parseInt(parts[2]) < 1) {
                error = 'Số thứ tự phải là số nguyên dương có ít nhất 2 chữ số (01, 02, 10...)';
              } else if (allCardNumbers.includes(value.toLowerCase())) {
                error = 'Số thẻ đã tồn tại';
              }
            }
          }
        }
        break;
      case 'issuedDate':
        if (!value) {
          error = 'Ngày cấp là bắt buộc';
        } else {
          const issuedDate = dayjs(value);
          const today = dayjs();
          
          if (issuedDate.isAfter(today)) {
            error = 'Ngày cấp không được trong tương lai';
          }
        }
        break;
      case 'expiredDate':
        if (!value) {
          error = 'Ngày hết hạn là bắt buộc';
        } else {
          const expiredDate = dayjs(value);
          const today = dayjs();
          const issuedDate = formData.issuedDate ? dayjs(formData.issuedDate) : null;
          
          if (expiredDate.isBefore(today) || expiredDate.isSame(today)) {
            error = 'Ngày hết hạn phải trong tương lai';
          } else if (issuedDate && expiredDate.isBefore(issuedDate)) {
            error = 'Ngày hết hạn phải sau ngày cấp';
          } else if (issuedDate) {
            const oneYearFromIssued = issuedDate.add(1, 'year');
            if (expiredDate.isBefore(oneYearFromIssued)) {
              error = 'Ngày hết hạn phải ít nhất 1 năm sau ngày cấp';
            }
          }
        }
        break;
      case 'status':
        if (!value) {
          error = 'Trạng thái là bắt buộc';
        }
        break;
      default:
        break;
    }

    if (error) {
      setErrors(prev => ({ ...prev, [name]: error }));
    } else {
      setErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors[name];
        return newErrors;
      });
    }
  };

  const handleCapabilityChange = (values) => {
    setFormData(prev => ({
      ...prev,
      capabilities: values
    }));
    
    if (errors.capabilities) {
      setErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors.capabilities;
        return newErrors;
      });
    }
  };

  const validateForm = () => {
    let newErrors = {};
    
    if (!formData.cardNumber) {
      newErrors.cardNumber = 'Số thẻ là bắt buộc';
    } else {
      const parts = formData.cardNumber.split('-');
      if (parts.length !== 3) {
        newErrors.cardNumber = 'Số thẻ phải có 3 phần cách nhau bởi dấu gạch ngang: CARD-{Mã căn hộ}-{Số thứ tự}';
      } else {
        if (parts[0] !== 'CARD') {
          if (parts[0].toUpperCase() === 'CARD') {
            newErrors.cardNumber = 'Phần "CARD" phải viết hoa. Ví dụ: CARD-A1001-01';
          } else {
            newErrors.cardNumber = 'Số thẻ phải bắt đầu bằng "CARD" (viết hoa)';
          }
        } else if (!parts[1] || parts[1].trim() === '') {
          newErrors.cardNumber = 'Mã căn hộ không được để trống';
        } else {
          const firstChar = parts[1].charAt(0);
          if (!/^[A-Z]$/.test(firstChar)) {
            if (/^[a-z]$/.test(firstChar)) {
              newErrors.cardNumber = 'Tên tòa phải viết hoa. Ví dụ: CARD-A1001-01 (A là tên tòa, phải viết hoa)';
            } else {
              newErrors.cardNumber = 'Mã căn hộ phải bắt đầu bằng chữ cái viết hoa (tên tòa). Ví dụ: A1001';
            }
          } else if (!/^[A-Z][0-9]{4}$/.test(parts[1])) {
            newErrors.cardNumber = 'Mã căn hộ phải có định dạng: {Tên tòa}{4 số}. Ví dụ: A1001 (A là tên tòa viết hoa, 1001 là 4 số gồm số tầng và số căn hộ)';
          } else if (!/^\d{2,}$/.test(parts[2]) || parseInt(parts[2]) < 1) {
            newErrors.cardNumber = 'Số thứ tự phải là số nguyên dương có ít nhất 2 chữ số (01, 02, 10...)';
          } else {
            const allCardNumbers = existingCards.map(c => c.cardNumber.toLowerCase()).filter(n => n !== card?.cardNumber.toLowerCase());
            if (allCardNumbers.includes(formData.cardNumber.toLowerCase())) {
              newErrors.cardNumber = 'Số thẻ đã tồn tại';
            }
          }
        }
      }
    }

    if (!formData.capabilities || formData.capabilities.length === 0) {
      newErrors.capabilities = 'Chức năng thẻ là bắt buộc';
    } else {
      const ENTRY_HOME_ID = 'f86850ac-4a7f-4244-b90f-75fe024c96cc';
      if (formData.capabilities.includes(ENTRY_HOME_ID) && !validatedApartmentId) {
        newErrors.capabilities = 'Chức năng "Ra vào căn hộ" yêu cầu phải có số căn hộ hợp lệ';
      }
    }

    if (!formData.issuedDate) {
      newErrors.issuedDate = 'Ngày cấp là bắt buộc';
    } else {
      const issuedDate = dayjs(formData.issuedDate);
      const today = dayjs();
      
      if (issuedDate.isAfter(today)) {
        newErrors.issuedDate = 'Ngày cấp không được trong tương lai';
      }
    }

    if (!formData.expiredDate) {
      newErrors.expiredDate = 'Ngày hết hạn là bắt buộc';
    } else {
      const expiredDate = dayjs(formData.expiredDate);
      const today = dayjs();
      const issuedDate = formData.issuedDate ? dayjs(formData.issuedDate) : null;
      
      if (expiredDate.isBefore(today) || expiredDate.isSame(today)) {
        newErrors.expiredDate = 'Ngày hết hạn phải trong tương lai';
      } else if (issuedDate && expiredDate.isBefore(issuedDate)) {
        newErrors.expiredDate = 'Ngày hết hạn phải sau ngày cấp';
      } else if (issuedDate) {
        const oneYearFromIssued = issuedDate.add(1, 'year');
        if (expiredDate.isBefore(oneYearFromIssued)) {
          newErrors.expiredDate = 'Ngày hết hạn phải ít nhất 1 năm sau ngày cấp';
        }
      }
    }

    if (!formData.status) {
      newErrors.status = 'Trạng thái là bắt buộc';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validateForm()) return;

    setLoading(true);
    setApiError('');

    try {
      const submitData = {
        cardNumber: formData.cardNumber,
        issuedDate: formData.issuedDate ? formData.issuedDate.format('YYYY-MM-DD') + 'T00:00:00.000Z' : null,
        expiredDate: formData.expiredDate ? formData.expiredDate.format('YYYY-MM-DD') + 'T00:00:00.000Z' : null,
        status: formData.status,
        cardTypeIds: formData.capabilities,
        issuedToApartmentId: validatedApartmentId
      };

      await cardsApi.updateWithCapabilities(card.cardId, submitData);

      onHide();
      onShowToast?.('Đã cập nhật thẻ thành công');
      onSuccess?.(card.cardId);

    } catch (error) {
      let errorMessage = 'Có lỗi xảy ra khi cập nhật thẻ. Vui lòng thử lại!';
      
      if (error.response?.data) {
        const errorData = error.response.data;
        if (typeof errorData === 'string') {
          errorMessage = errorData;
        } else if (errorData.error) {
          errorMessage = errorData.error;
        } else if (errorData.message) {
          errorMessage = errorData.message;
        }
      }
      
      setApiError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleModalClose = () => {
    setErrors({});
    setApiError('');
    onHide();
  };

  return (
    <Modal
      title={
        <Space>
          <span>Cập nhật thông tin thẻ</span>
        </Space>
      }
      open={show}
      onCancel={handleModalClose}
      width={800}
      footer={null}
    >
      {apiError && (
        <Alert 
          message={apiError}
          type="error"
          style={{ marginBottom: 16 }}
          closable
          onClose={() => setApiError('')}
        />
      )}
      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        initialValues={formData}
      >
        {/* Thông tin cơ bản */}
        <div style={{ marginBottom: 24 }}>
          <h6 style={{ color: '#1890ff', marginBottom: 16, fontSize: '16px', fontWeight: 600 }}>
            Thông tin cơ bản
          </h6>
          
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                label="Số thẻ"
                name="cardNumber"
                rules={[
                  { required: true, message: 'Vui lòng nhập số thẻ' },
                  { max: 20, message: 'Số thẻ không được vượt quá 20 ký tự' }
                ]}
                validateStatus={errors.cardNumber ? 'error' : ''}
                help={errors.cardNumber}
              >
                <Input
                  placeholder="VD: CARD-A1001-01, CARD-A1001-02"
                  maxLength={20}
                  value={formData.cardNumber}
                  onChange={(e) => handleChange('cardNumber', e.target.value)}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                label="Trạng thái"
                name="status"
                rules={[{ required: true, message: 'Vui lòng chọn trạng thái' }]}
                validateStatus={errors.status ? 'error' : ''}
                help={errors.status}
              >
                <Select
                  placeholder="Chọn trạng thái"
                  onChange={(value) => handleChange('status', value)}
                >
                  <Option value="ACTIVE">Hoạt động</Option>
                  <Option value="INACTIVE">Không hoạt động</Option>
                  <Option value="EXPIRED">Hết hạn</Option>
                  <Option value="LOST">Mất thẻ</Option>
                </Select>
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col span={24}>
              <Form.Item
                label={
                  <span>
                    Số căn hộ <span style={{ color: '#999', fontWeight: 'normal' }}>(Không bắt buộc)</span>
                  </span>
                }
                name="apartmentNumber"
                validateStatus={errors.apartmentNumber ? 'error' : (validatedApartmentId !== null && !errors.apartmentNumber ? 'success' : '')}
                help={errors.apartmentNumber || (validatedApartmentId !== null && !errors.apartmentNumber ? 'Căn hộ hợp lệ ✓' : '')}
              >
                <Input
                  placeholder="VD: A0108, B0205"
                  suffix={isCheckingApartment ? <Spin size="small" /> : null}
                  onChange={(e) => handleChange('apartmentNumber', e.target.value)}
                />
              </Form.Item>
              <div style={{ marginTop: 8, fontSize: '12px', color: '#666' }}>
                <i className="fas fa-info-circle" style={{ marginRight: 4 }}></i>
                Nhập số căn hộ để gán thẻ cho căn hộ cụ thể. Cần nhập đúng trước khi chọn chức năng "Ra vào căn hộ". Một căn hộ có thể có nhiều thẻ.
              </div>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                label="Ngày cấp"
                name="issuedDate"
                rules={[{ required: true, message: 'Vui lòng chọn ngày cấp' }]}
                validateStatus={errors.issuedDate ? 'error' : ''}
                help={errors.issuedDate}
              >
                <DatePicker
                  style={{ width: '100%' }}
                  format="DD/MM/YYYY"
                  placeholder="Chọn ngày cấp"
                  onChange={(date) => handleChange('issuedDate', date)}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                label="Ngày hết hạn"
                name="expiredDate"
                rules={[{ required: true, message: 'Vui lòng chọn ngày hết hạn' }]}
                validateStatus={errors.expiredDate ? 'error' : ''}
                help={errors.expiredDate}
              >
                <DatePicker
                  style={{ width: '100%' }}
                  format="DD/MM/YYYY"
                  placeholder="Chọn ngày hết hạn"
                  onChange={(date) => handleChange('expiredDate', date)}
                />
              </Form.Item>
            </Col>
          </Row>
        </div>

        {/* Chức năng thẻ */}
        <div style={{ marginBottom: 24 }}>
          <h6 style={{ color: '#1890ff', marginBottom: 16, fontSize: '16px', fontWeight: 600 }}>
            Chức năng thẻ <span style={{ color: '#ff4d4f' }}>*</span>
          </h6>
          
          <Form.Item
            name="capabilities"
            rules={[{ required: true, message: 'Vui lòng chọn ít nhất một chức năng' }]}
            validateStatus={errors.capabilities ? 'error' : ''}
            help={errors.capabilities}
          >
            <Checkbox.Group
              style={{ width: '100%' }}
              onChange={handleCapabilityChange}
            >
              <div 
                style={{ 
                  border: '1px solid #d9d9d9', 
                  borderRadius: 6, 
                  padding: 16, 
                  backgroundColor: '#fafafa',
                  maxHeight: 250,
                  overflowY: 'auto'
                }}
              >
                <Row gutter={[16, 8]}>
                  {availableCapabilities.map((capability) => (
                    <Col span={12} key={capability.cardTypeId}>
                      <Checkbox value={capability.cardTypeId}>
                        <div>
                          <div style={{ fontWeight: 600 }}>{capability.name}</div>
                          <div style={{ fontSize: '12px', color: '#666' }}>{capability.description}</div>
                        </div>
                      </Checkbox>
                    </Col>
                  ))}
                </Row>
              </div>
            </Checkbox.Group>
          </Form.Item>
        </div>

        <Form.Item>
          <Space style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16 }}>
            <Button onClick={handleModalClose} disabled={loading}>
              Hủy
            </Button>
            <Button type="primary" htmlType="submit" loading={loading}>
              {loading ? 'Đang cập nhật...' : 'Cập nhật'}
            </Button>
          </Space>
        </Form.Item>
      </Form>
    </Modal>
  );
}