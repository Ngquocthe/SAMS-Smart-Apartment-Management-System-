import React, { useState } from 'react';
import { Modal, Form, Button, Row, Col } from 'react-bootstrap';
import { assetsApi } from '../../../features/building-management/assetsApi';

export default function CreateAsset({ show, onHide, onSuccess, categories = [], onShowToast, existingAssets = [], availableLocations = [] }) {
  const [formData, setFormData] = useState({
    code: '',
    assetName: '',
    categoryId: categories.length > 0 ? categories[0]?.categoryId : '',
    status: 'ACTIVE',
    purchaseDate: '',
    warrantyExpire: '',
    location: '',
    maintenanceFrequency: ''
  });

  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});

  // Helper function để validate text field (tên tài sản, vị trí)
  const validateTextField = (value, fieldName, checkDuplicate = false) => {
    const trimmed = value.trim();
    const fieldLabels = {
      assetName: 'Tên tài sản',
      location: 'Vị trí'
    };
    const label = fieldLabels[fieldName] || fieldName;

    if (trimmed.length < 3) {
      return `${label} phải có ít nhất 3 ký tự`;
    }
    if (trimmed.length > 50) {
      return `${label} không được vượt quá 50 ký tự`;
    }
    
    const firstCharPattern = /^[A-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]/;
    if (!firstCharPattern.test(trimmed.charAt(0))) {
      return `Chữ cái đầu của ${label.toLowerCase()} phải viết hoa`;
    }
    
    // Cho phép dấu phẩy trong vị trí
    const allowedCharsPattern = fieldName === 'location' 
      ? /^[a-zA-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ0-9\s,]+$/
      : /^[a-zA-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ0-9\s]+$/;
    
    if (!allowedCharsPattern.test(trimmed)) {
      return fieldName === 'location' 
        ? 'Chỉ được chứa chữ cái, số, khoảng trắng và dấu phẩy'
        : 'Chỉ được chứa chữ cái, số và khoảng trắng';
    }

    // Check duplicate nếu cần
    if (checkDuplicate && fieldName === 'assetName') {
      const existingName = existingAssets.find(asset => 
        (asset.assetName || asset.name)?.toLowerCase() === trimmed.toLowerCase()
      );
      if (existingName) {
        return `${label} này đã tồn tại trong hệ thống`;
      }
    }

    return '';
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    
    // Tự động chuyển sang chữ hoa cho trường code
    const processedValue = name === 'code' ? value.toUpperCase() : value;
    
    setFormData(prev => ({
      ...prev,
      [name]: processedValue
    }));

    // Clear error khi đang nhập
    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: '' }));
    }

    // Real-time validation
    if (name === 'code' && processedValue.trim()) {
      const codePattern = /^[A-Z]{3}_\d{3}$/;
      if (!codePattern.test(processedValue)) {
        setErrors(prev => ({
          ...prev,
          code: 'Mã phải có dạng ABC_123 (VD: FAN_001, AIR_999)'
        }));
      } else {
        // Check uniqueness
        const existingCode = existingAssets.find(asset => 
          asset.code?.toLowerCase() === processedValue.trim().toLowerCase()
        );
        if (existingCode) {
          setErrors(prev => ({
            ...prev,
            code: 'Mã tài sản này đã tồn tại trong hệ thống'
          }));
        } else {
          setErrors(prev => ({
            ...prev,
            code: ''
          }));
        }
      }
    }

    // Validate tên tài sản và vị trí
    if ((name === 'assetName' || name === 'location') && value.trim()) {
      const errorMessage = validateTextField(value, name, name === 'assetName');
      setErrors(prev => ({ ...prev, [name]: errorMessage }));
    }

    // Validate ngày bảo hành
    if (name === 'warrantyExpire' && value && formData.purchaseDate) {
      const purchaseDate = new Date(formData.purchaseDate);
      const warrantyDate = new Date(value);
      
      if (warrantyDate <= purchaseDate) {
        setErrors(prev => ({
          ...prev,
          warrantyExpire: 'Ngày hết hạn BH phải sau ngày mua'
        }));
      } else {
        setErrors(prev => ({
          ...prev,
          warrantyExpire: ''
        }));
      }
    }

    if (name === 'purchaseDate' && value && formData.warrantyExpire) {
      const purchaseDate = new Date(value);
      const warrantyDate = new Date(formData.warrantyExpire);
      
      if (warrantyDate <= purchaseDate) {
        setErrors(prev => ({
          ...prev,
          warrantyExpire: 'Ngày hết hạn BH phải sau ngày mua'
        }));
      } else {
        setErrors(prev => ({
          ...prev,
          warrantyExpire: ''
        }));
      }
    }
  };

  const validateForm = () => {
    const newErrors = {};

    // Validate mã tài sản: đúng 3 chữ HOA + "_" + đúng 3 số
    if (!formData.code.trim()) {
      newErrors.code = 'Mã tài sản là bắt buộc';
    } else {
      const codePattern = /^[A-Z]{3}_\d{3}$/;
      if (!codePattern.test(formData.code)) {
        newErrors.code = 'Mã phải có dạng ABC_123 (VD: FAN_001, AIR_999)';
      } else {
        // Check uniqueness
        const existingCode = existingAssets.find(asset => 
          asset.code?.toLowerCase() === formData.code.trim().toLowerCase()
        );
        if (existingCode) {
          newErrors.code = 'Mã tài sản này đã tồn tại';
        }
      }
    }

    // Validate tên tài sản
    if (!formData.assetName.trim()) {
      newErrors.assetName = 'Tên tài sản là bắt buộc';
    } else {
      const errorMsg = validateTextField(formData.assetName, 'assetName', true);
      if (errorMsg) newErrors.assetName = errorMsg;
    }

    // Validate danh mục
    if (!formData.categoryId) {
      newErrors.categoryId = 'Vui lòng chọn danh mục';
    }

    // Validate ngày mua
    if (!formData.purchaseDate) {
      newErrors.purchaseDate = 'Ngày mua là bắt buộc';
    } else {
      const purchaseDate = new Date(formData.purchaseDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      
      if (purchaseDate > today) {
        newErrors.purchaseDate = 'Ngày mua không được ở tương lai';
      }
    }

    // Validate ngày hết hạn bảo hành
    if (formData.warrantyExpire) {
      const purchaseDate = new Date(formData.purchaseDate);
      const warrantyDate = new Date(formData.warrantyExpire);
      
      if (warrantyDate <= purchaseDate) {
        newErrors.warrantyExpire = 'Ngày hết hạn BH phải sau ngày mua';
      }
    }

    // Validate vị trí
    if (!formData.location || !formData.location.trim()) {
      newErrors.location = 'Vị trí là bắt buộc';
    } else {
      const errorMsg = validateTextField(formData.location, 'location', false);
      if (errorMsg) newErrors.location = errorMsg;
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateForm()) return;

    setLoading(true);

    try {
      const submitData = {
        categoryId: formData.categoryId,
        code: formData.code.trim(),
        name: formData.assetName.trim(),
        status: formData.status,
        location: formData.location.trim(),
        purchaseDate: formData.purchaseDate || null,
        warrantyExpire: formData.warrantyExpire || null
      };

      // Chỉ thêm maintenanceFrequency nếu có giá trị
      if (formData.maintenanceFrequency && formData.maintenanceFrequency.trim() !== '') {
        submitData.maintenanceFrequency = parseInt(formData.maintenanceFrequency);
      }

      const response = await assetsApi.create(submitData);

      resetForm();
      onHide();
      onShowToast?.('Đã tạo tài sản mới thành công');
      onSuccess?.(response?.assetId || response?.id);

    } catch (error) {
      let errorMessage = 'Có lỗi xảy ra khi tạo tài sản. Vui lòng thử lại!';
      
      if (error.response?.data) {
        const errorData = error.response.data;
        
        if (errorData.errors) {
          const validationErrors = Object.entries(errorData.errors)
            .map(([field, messages]) => `${field}: ${messages.join(', ')}`)
            .join('\n');
          errorMessage = `Lỗi validation:\n${validationErrors}`;
        } else if (errorData.error) {
          errorMessage = `Lỗi: ${errorData.error}`;
        } else if (errorData.message) {
          errorMessage = `Lỗi: ${errorData.message}`;
        }
      }
      
      alert(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setFormData({
      code: '',
      assetName: '',
      categoryId: categories.length > 0 ? categories[0]?.categoryId : '',
      status: 'ACTIVE',
      purchaseDate: '',
      warrantyExpire: '',
      location: '',
      maintenanceFrequency: ''
    });
    setErrors({});
  };

  const handleClose = () => {
    resetForm();
    onHide();
  };

  return (
    <Modal 
      show={show} 
      onHide={handleClose}
      size="lg"
      centered
      backdrop="static"
    >
      <Modal.Header closeButton>
        <Modal.Title>
          <div className="d-flex align-items-center gap-2">
            <span>Thêm tài sản mới</span>
          </div>
        </Modal.Title>
      </Modal.Header>
      
      <Modal.Body>
        <p className="text-muted mb-4">
          Thêm tài sản mới vào hệ thống quản lý
        </p>

        <Form onSubmit={handleSubmit}>
          <Row>
            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Mã tài sản <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="text"
                  name="code"
                  value={formData.code}
                  onChange={handleChange}
                  placeholder="VD: FAN_001, AIR_999"
                  isInvalid={!!errors.code}
                  maxLength={7}
                  autoFocus
                />
                <Form.Control.Feedback type="invalid">
                  {errors.code}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Tên tài sản <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="text"
                  name="assetName"
                  value={formData.assetName}
                  onChange={handleChange}
                  placeholder="VD: Máy lạnh"
                  isInvalid={!!errors.assetName}
                  maxLength={50}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.assetName}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>

          <Row>
            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Danh mục <span className="text-danger">*</span>
                </Form.Label>
                <Form.Select
                  name="categoryId"
                  value={formData.categoryId}
                  onChange={handleChange}
                  isInvalid={!!errors.categoryId}
                >
                  <option value="">Chọn danh mục</option>
                  {categories.map(category => (
                    <option key={category.categoryId} value={category.categoryId}>
                      {category.categoryName}
                    </option>
                  ))}
                </Form.Select>
                <Form.Control.Feedback type="invalid">
                  {errors.categoryId}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Vị trí <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="text"
                  name="location"
                  value={formData.location}
                  onChange={handleChange}
                  placeholder="VD: Tầng 1, Sảnh tầng trệt"
                  isInvalid={!!errors.location}
                  maxLength={50}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.location}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>

          <Row>
            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Ngày mua <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="date"
                  name="purchaseDate"
                  value={formData.purchaseDate}
                  onChange={handleChange}
                  isInvalid={!!errors.purchaseDate}
                  max={new Date().toISOString().split('T')[0]}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.purchaseDate}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Ngày hết hạn bảo hành
                </Form.Label>
                <Form.Control
                  type="date"
                  name="warrantyExpire"
                  value={formData.warrantyExpire}
                  onChange={handleChange}
                  isInvalid={!!errors.warrantyExpire}
                  min={formData.purchaseDate || undefined}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.warrantyExpire}
                </Form.Control.Feedback>
                <Form.Text className="text-muted">
                  (Tùy chọn) Để trống nếu không có bảo hành
                </Form.Text>
              </Form.Group>
            </Col>
          </Row>

          <Row>
            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Trạng thái <span className="text-danger">*</span>
                </Form.Label>
                <Form.Select
                  name="status"
                  value={formData.status}
                  onChange={handleChange}
                >
                  <option value="ACTIVE">Hoạt động</option>
                  <option value="INACTIVE">Không hoạt động</option>
                </Form.Select>
              </Form.Group>
            </Col>
          </Row>
        </Form>
      </Modal.Body>

      <Modal.Footer>
        <Button 
          variant="secondary" 
          onClick={handleClose}
          disabled={loading}
        >
          Hủy
        </Button>
        <Button 
          variant="primary" 
          onClick={handleSubmit}
          disabled={loading}
        >
          {loading ? (
            <>
              <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
              Đang tạo...
            </>
          ) : (
            <>
              Thêm mới
            </>
          )}
        </Button>
      </Modal.Footer>
    </Modal>
  );
}

