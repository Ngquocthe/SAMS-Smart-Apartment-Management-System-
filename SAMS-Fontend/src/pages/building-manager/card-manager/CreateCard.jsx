import React, { useState, useEffect } from 'react';
import { Modal, Form, Button, Row, Col } from 'react-bootstrap';
import { cardsApi } from '../../../features/building-management/cardsApi';
import { apartmentsApi } from '../../../features/building-management/apartmentsApi';

export default function CreateCard({ show, onHide, onSuccess, onShowToast, onShowErrorToast, existingCards = [] }) {
  const [formData, setFormData] = useState({
    cardNumber: '',
    issuedDate: new Date().toISOString().split('T')[0],
    expiredDate: '',
    status: 'ACTIVE',
    capabilities: [],
    apartmentNumber: ''
  });

  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});
  const [availableCapabilities, setAvailableCapabilities] = useState([]);
  const [validatedApartmentId, setValidatedApartmentId] = useState(null);
  const [isCheckingApartment, setIsCheckingApartment] = useState(false);

  useEffect(() => {
    const loadInitialData = async () => {
      try {
        const cardTypesData = await cardsApi.getCardTypes();
        setAvailableCapabilities(cardTypesData);
      } catch (error) {
        // Silently fail - user will see empty capabilities list
      }
    };

    if (show) {
      loadInitialData();
    }
  }, [show]);

  useEffect(() => {
    const ENTRY_HOME_ID = 'f86850ac-4a7f-4244-b90f-75fe024c96cc';
    
    if (!validatedApartmentId && formData.capabilities.includes(ENTRY_HOME_ID)) {
      setFormData(prev => ({
        ...prev,
        capabilities: prev.capabilities.filter(id => id !== ENTRY_HOME_ID)
      }));
    }
  }, [validatedApartmentId, formData.capabilities]);

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

  const handleInputChange = (e) => {
    const { name, value, type, checked } = e.target;
    
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
    
    if (errors[name]) {
      setErrors(prev => ({
        ...prev,
        [name]: ''
      }));
    }

    if (name === 'cardNumber' && value.trim()) {
      const parts = value.split('-');
      
      if (parts.length !== 3) {
        setErrors(prev => ({
          ...prev,
          cardNumber: 'Số thẻ phải có 3 phần cách nhau bởi dấu gạch ngang: CARD-{Mã căn hộ}-{Số thứ tự}'
        }));
      } else {
        // Kiểm tra "CARD" phải là chữ hoa
        if (parts[0] !== 'CARD') {
          if (parts[0].toUpperCase() === 'CARD') {
            setErrors(prev => ({
              ...prev,
              cardNumber: 'Phần "CARD" phải viết hoa. Ví dụ: CARD-A1001-01'
            }));
          } else {
            setErrors(prev => ({
              ...prev,
              cardNumber: 'Số thẻ phải bắt đầu bằng "CARD" (viết hoa)'
            }));
          }
        } else if (!parts[1] || parts[1].trim() === '') {
          setErrors(prev => ({
            ...prev,
            cardNumber: 'Mã căn hộ không được để trống'
          }));
        } else {
          // Kiểm tra tên tòa phải là chữ hoa
          const firstChar = parts[1].charAt(0);
          if (!/^[A-Z]$/.test(firstChar)) {
            if (/^[a-z]$/.test(firstChar)) {
              setErrors(prev => ({
                ...prev,
                cardNumber: 'Tên tòa phải viết hoa. Ví dụ: CARD-A1001-01 (A là tên tòa, phải viết hoa)'
              }));
            } else {
              setErrors(prev => ({
                ...prev,
                cardNumber: 'Mã căn hộ phải bắt đầu bằng chữ cái viết hoa (tên tòa). Ví dụ: A1001'
              }));
            }
          } else if (!/^[A-Z][0-9]{4}$/.test(parts[1])) {
            setErrors(prev => ({
              ...prev,
              cardNumber: 'Mã căn hộ phải có định dạng: {Tên tòa}{4 số}. Ví dụ: A1001 (A là tên tòa viết hoa, 1001 là 4 số gồm số tầng và số căn hộ)'
            }));
          } else if (!/^\d{2,}$/.test(parts[2]) || parseInt(parts[2]) < 1) {
            setErrors(prev => ({
              ...prev,
              cardNumber: 'Số thứ tự phải là số nguyên dương có ít nhất 2 chữ số (01, 02, 10...)'
            }));
          } else {
            // Check uniqueness
            const isDuplicate = existingCards.some(card => 
              card.cardNumber && card.cardNumber.toLowerCase() === value.toLowerCase()
            );
            if (isDuplicate) {
              setErrors(prev => ({
                ...prev,
                cardNumber: 'Số thẻ này đã tồn tại trong hệ thống'
              }));
            } else {
              setErrors(prev => ({
                ...prev,
                cardNumber: ''
              }));
            }
          }
        }
      }
    }

    if (name === 'issuedDate' && value) {
      const issuedDate = new Date(value);
      const today = new Date();
      const oneYearAgo = new Date();
      oneYearAgo.setFullYear(today.getFullYear() - 1);
      
      // Cho phép ngày cấp trong quá khứ nhưng không quá 1 năm
      if (issuedDate < oneYearAgo) {
        setErrors(prev => ({
          ...prev,
          issuedDate: 'Ngày cấp không được quá 1 năm trong quá khứ'
        }));
      } else {
        setErrors(prev => ({
          ...prev,
          issuedDate: ''
        }));
      }
    }

    if (name === 'expiredDate' && value) {
      const expiredDate = new Date(value);
      const today = new Date();
      
      if (expiredDate <= today) {
        setErrors(prev => ({
          ...prev,
          expiredDate: 'Ngày hết hạn phải là trong tương lai'
        }));
        } else {
          if (formData.issuedDate) {
          const issuedDate = new Date(formData.issuedDate);
          const oneYearFromIssued = new Date(issuedDate);
          oneYearFromIssued.setFullYear(issuedDate.getFullYear() + 1);
          
          if (expiredDate < oneYearFromIssued) {
            setErrors(prev => ({
              ...prev,
              expiredDate: 'Thời gian hết hạn phải ít nhất 1 năm từ ngày cấp'
            }));
          } else {
            setErrors(prev => ({
              ...prev,
              expiredDate: ''
            }));
          }
        } else {
          setErrors(prev => ({
            ...prev,
            expiredDate: ''
          }));
        }
      }
    }
  };

  const handleCapabilityChange = (capabilityId, isChecked) => {
    const ENTRY_HOME_ID = 'f86850ac-4a7f-4244-b90f-75fe024c96cc';
    
    if (isChecked && capabilityId === ENTRY_HOME_ID && !validatedApartmentId) {
      setErrors(prev => ({
        ...prev,
        capabilities: 'Vui lòng điền số căn hộ hợp lệ trước khi chọn chức năng "Ra vào căn hộ"'
      }));
      return;
    }
    
    setFormData(prev => ({
      ...prev,
      capabilities: isChecked 
        ? [...prev.capabilities, capabilityId]
        : prev.capabilities.filter(id => id !== capabilityId)
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
    const newErrors = {};

    if (!formData.cardNumber.trim()) {
      newErrors.cardNumber = 'Số thẻ là bắt buộc';
    } else {
      const parts = formData.cardNumber.split('-');
      
      if (parts.length !== 3) {
        newErrors.cardNumber = 'Số thẻ phải có 3 phần cách nhau bởi dấu gạch ngang: CARD-{Mã căn hộ}-{Số thứ tự}';
      } else {
        // Kiểm tra "CARD" phải là chữ hoa
        if (parts[0] !== 'CARD') {
          if (parts[0].toUpperCase() === 'CARD') {
            newErrors.cardNumber = 'Phần "CARD" phải viết hoa. Ví dụ: CARD-A1001-01';
          } else {
            newErrors.cardNumber = 'Số thẻ phải bắt đầu bằng "CARD" (viết hoa)';
          }
        } else if (!parts[1] || parts[1].trim() === '') {
          newErrors.cardNumber = 'Mã căn hộ không được để trống';
        } else {
          // Kiểm tra tên tòa phải là chữ hoa
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
            // Check uniqueness
            const isDuplicate = existingCards.some(card => 
              card.cardNumber && card.cardNumber.toLowerCase() === formData.cardNumber.toLowerCase()
            );
            if (isDuplicate) {
              newErrors.cardNumber = 'Số thẻ này đã tồn tại trong hệ thống';
            }
          }
        }
      }
    }

    if (!formData.capabilities || formData.capabilities.length === 0) {
      newErrors.capabilities = 'Phải chọn ít nhất 1 chức năng cho thẻ';
    } else {
      const ENTRY_HOME_ID = 'f86850ac-4a7f-4244-b90f-75fe024c96cc';
      if (formData.capabilities.includes(ENTRY_HOME_ID) && !validatedApartmentId) {
        newErrors.capabilities = 'Chức năng "Ra vào căn hộ" yêu cầu phải có số căn hộ hợp lệ';
      }
    }

    if (!formData.status) {
      newErrors.status = 'Trạng thái là bắt buộc';
    }


    if (!formData.issuedDate) {
      newErrors.issuedDate = 'Ngày cấp là bắt buộc';
    } else {
      const issuedDate = new Date(formData.issuedDate);
      const today = new Date();
      const oneYearAgo = new Date();
      oneYearAgo.setFullYear(today.getFullYear() - 1);
      
      // Cho phép ngày cấp trong quá khứ nhưng không quá 1 năm
      if (issuedDate < oneYearAgo) {
        newErrors.issuedDate = 'Ngày cấp không được quá 1 năm trong quá khứ';
      }
    }

    if (!formData.expiredDate) {
      newErrors.expiredDate = 'Ngày hết hạn là bắt buộc';
    } else {
      const expiredDate = new Date(formData.expiredDate);
      const today = new Date();
      
      if (expiredDate <= today) {
        newErrors.expiredDate = 'Ngày hết hạn phải là trong tương lai';
      } else if (formData.issuedDate) {
        const issuedDate = new Date(formData.issuedDate);
        const oneYearFromIssued = new Date(issuedDate);
        oneYearFromIssued.setFullYear(issuedDate.getFullYear() + 1);
        
        if (expiredDate < oneYearFromIssued) {
          newErrors.expiredDate = 'Thời gian hết hạn phải ít nhất 1 năm từ ngày cấp';
        } else if (expiredDate <= issuedDate) {
          newErrors.expiredDate = 'Ngày hết hạn phải sau ngày cấp';
        }
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    setLoading(true);
    try {
      const createData = {
        cardNumber: formData.cardNumber,
        issuedDate: new Date(formData.issuedDate).toISOString(),
        expiredDate: new Date(formData.expiredDate).toISOString(),
        status: formData.status,
        cardTypeIds: formData.capabilities,
        issuedToUserId: null,
        issuedToApartmentId: validatedApartmentId,
        createdBy: 'buildingmanager'
      };

      const response = await cardsApi.createWithCapabilities(createData);
      
      onShowToast?.('Tạo thẻ thành công!');
      onSuccess?.(response.cardId || response.id);
      handleClose();
    } catch (error) {
      onShowErrorToast?.('Có lỗi xảy ra khi tạo thẻ');
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setFormData({
      cardNumber: '',
      issuedDate: new Date().toISOString().split('T')[0],
      expiredDate: '',
      status: 'ACTIVE',
      capabilities: [],
      apartmentNumber: ''
    });
    setErrors({});
    setValidatedApartmentId(null);
    onHide();
  };

  return (
    <Modal show={show} onHide={handleClose} size="lg" centered>
      <Modal.Header closeButton>
        <Modal.Title>
          Thêm thẻ mới
        </Modal.Title>
      </Modal.Header>
      
      <Form onSubmit={handleSubmit}>
        <Modal.Body>
          {/* Thông tin cơ bản */}
          <div className="mb-4">
            <h6 className="text-primary mb-3">
              Thông tin cơ bản
            </h6>
            
            <Row>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>
                    Số thẻ <span className="text-danger">*</span>
                  </Form.Label>
                  <Form.Control
                    type="text"
                    name="cardNumber"
                    value={formData.cardNumber}
                    onChange={handleInputChange}
                    placeholder="VD: CARD-A1001-01, CARD-A1001-02"
                    isInvalid={!!errors.cardNumber}
                  />
                  <Form.Control.Feedback type="invalid">
                    {errors.cardNumber}
                  </Form.Control.Feedback>
                </Form.Group>
              </Col>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>
                    Trạng thái <span className="text-danger">*</span>
                  </Form.Label>
                  <Form.Select
                    name="status"
                    value={formData.status}
                    onChange={handleInputChange}
                    isInvalid={!!errors.status}
                  >
                    <option value="ACTIVE">Hoạt động</option>
                    <option value="INACTIVE">Không hoạt động</option>
                    <option value="EXPIRED">Hết hạn</option>
                    <option value="LOST">Mất thẻ</option>
                  </Form.Select>
                  <Form.Control.Feedback type="invalid">
                    {errors.status}
                  </Form.Control.Feedback>
                </Form.Group>
              </Col>
            </Row>

            <Row>
              <Col md={12}>
                <Form.Group className="mb-3">
                  <Form.Label>
                    Số căn hộ <span className="text-muted">(Không bắt buộc)</span>
                  </Form.Label>
                  <div className="position-relative">
                    <Form.Control
                      type="text"
                      name="apartmentNumber"
                      value={formData.apartmentNumber}
                      onChange={handleInputChange}
                      placeholder="VD: A0101, A0205"
                      isInvalid={!!errors.apartmentNumber}
                      isValid={validatedApartmentId !== null && !errors.apartmentNumber}
                    />
                    {isCheckingApartment && (
                      <div 
                        className="position-absolute top-50 translate-middle-y end-0 me-2"
                        style={{ pointerEvents: 'none' }}
                      >
                        <span className="spinner-border spinner-border-sm text-primary" role="status">
                          <span className="visually-hidden">Đang kiểm tra...</span>
                        </span>
                      </div>
                    )}
                    <Form.Control.Feedback type="invalid">
                      {errors.apartmentNumber}
                    </Form.Control.Feedback>
                    <Form.Control.Feedback type="valid">
                      Căn hộ hợp lệ ✓
                    </Form.Control.Feedback>
                  </div>
                </Form.Group>
              </Col>
            </Row>

            <Row>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>
                    Ngày cấp <span className="text-danger">*</span>
                  </Form.Label>
                  <Form.Control
                    type="date"
                    name="issuedDate"
                    value={formData.issuedDate}
                    onChange={handleInputChange}
                    isInvalid={!!errors.issuedDate}
                  />
                  <Form.Control.Feedback type="invalid">
                    {errors.issuedDate}
                  </Form.Control.Feedback>
                </Form.Group>
              </Col>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>
                    Ngày hết hạn <span className="text-danger">*</span>
                  </Form.Label>
                  <Form.Control
                    type="date"
                    name="expiredDate"
                    value={formData.expiredDate}
                    onChange={handleInputChange}
                    isInvalid={!!errors.expiredDate}
                  />
                  <Form.Control.Feedback type="invalid">
                    {errors.expiredDate}
                  </Form.Control.Feedback>
                </Form.Group>
              </Col>
            </Row>
          </div>

          {/* Chức năng thẻ */}
          <div className="mb-4">
            <h6 className="text-primary mb-3 text-start">
              Chức năng thẻ <span className="text-danger">*</span>
            </h6>
            
            <div className="border rounded p-3 bg-light" style={{ maxHeight: '250px', overflowY: 'auto' }}>
              <div className="row">
                {availableCapabilities.map((capability) => {
                  const ENTRY_HOME_ID = 'f86850ac-4a7f-4244-b90f-75fe024c96cc';
                  const isEntryHome = capability.cardTypeId === ENTRY_HOME_ID;
                  const isDisabled = isEntryHome && !validatedApartmentId;
                  
                  return (
                    <div key={capability.cardTypeId} className="col-md-6 mb-2">
                      <Form.Check
                        type="checkbox"
                        id={`capability-${capability.cardTypeId}`}
                        label={
                          <div>
                            <strong className={isDisabled ? 'text-muted' : ''}>
                              {capability.name}
                              {isDisabled && <span className="text-danger ms-1">*</span>}
                            </strong>
                            <br />
                            <small className="text-muted">
                              {capability.description}
                              {isDisabled && (
                                <span className="text-danger d-block mt-1">
                                  <i className="fas fa-info-circle me-1"></i>
                                  Yêu cầu nhập số căn hộ hợp lệ
                                </span>
                              )}
                            </small>
                          </div>
                        }
                        checked={formData.capabilities.includes(capability.cardTypeId)}
                        onChange={(e) => handleCapabilityChange(capability.cardTypeId, e.target.checked)}
                        disabled={isDisabled}
                        className="mb-2"
                      />
                    </div>
                  );
                })}
              </div>
            </div>
            
            {errors.capabilities && (
              <div className="text-danger mt-2">
                <i className="fas fa-exclamation-triangle me-1"></i>
                {errors.capabilities}
              </div>
            )}
          </div>

        </Modal.Body>
        
        <Modal.Footer>
          <Button variant="secondary" onClick={handleClose} disabled={loading}>
            Hủy
          </Button>
          <Button variant="primary" type="submit" disabled={loading}>
            {loading ? (
              <>
                <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                Đang tạo...
              </>
            ) : (
              <>
                Tạo thẻ
              </>
            )}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}
