import React, { useState, useEffect } from 'react';
import { Modal, Form, Button, Row, Col, InputGroup } from 'react-bootstrap';
import { amenitiesApi } from '../../../features/building-management/amenitiesApi';
import { amenityPackageApi } from '../../../features/amenity-booking/amenityPackageApi';

export default function CreateAmenity({ show, onHide, onSuccess, categories = [], onShowToast, existingAmenities = [] }) {
  const [formData, setFormData] = useState({
    code: '',
    name: '',
    location: '',
    has_monthly_package: false,
    requires_face_verification: false,
    fee_type: 'Paid',
    status: 'ACTIVE',
    category_name: categories.length > 0 ? categories[0] : ''
  });

  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});
  const [packagePriceErrors, setPackagePriceErrors] = useState({}); // Lỗi cho từng gói tháng
  const [dayPackagePriceErrors, setDayPackagePriceErrors] = useState({});
  
  // State quản lý package tháng - có thể chọn nhiều gói
  const [selectedMonthCounts, setSelectedMonthCounts] = useState(['1']); // Mặc định chọn 1 tháng
  // State quản lý giá cho từng gói tháng: { '1': '100000', '3': '250000', ... }
  const [packagePrices, setPackagePrices] = useState({ '1': '' });
  
  // State quản lý package ngày
  const [selectedDayCounts, setSelectedDayCounts] = useState([]);
  const [dayPackagePrices, setDayPackagePrices] = useState({});
  
  // Cập nhật category_name khi categories thay đổi
  useEffect(() => {
    if (categories.length > 0 && !formData.category_name) {
      setFormData(prev => ({
        ...prev,
        category_name: categories[0]
      }));
    }
  }, [categories, formData.category_name]);

  const formatPrice = (value) => {
    // Remove all non-digit characters
    const numbers = value.replace(/\D/g, '');
    // Format với dấu chấm ngăn cách hàng nghìn
    if (numbers) {
      return numbers.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    }
    return numbers;
  };

  const parseFormattedPrice = (formattedValue) => {
    if (!formattedValue || formattedValue === '') {
      return '';
    }
    return formattedValue.replace(/\./g, '');
  };

  // Hàm validate giá theo thứ tự gói tháng
  const validatePackagePrices = (prices, selectedMonths) => {
    const errors = {};
    const sortedMonths = [...selectedMonths].map(m => parseInt(m)).sort((a, b) => a - b);

    for (let i = 0; i < sortedMonths.length; i++) {
      const month = sortedMonths[i].toString();
      const priceStr = prices[month];

      if (!priceStr || priceStr.trim() === '') {
        continue; // Bỏ qua nếu chưa nhập giá
      }

      const price = parseInt(parseFormattedPrice(priceStr));

      if (isNaN(price) || price <= 0) {
        errors[month] = 'Giá phải lớn hơn 0';
        continue;
      }

      // Kiểm tra với các gói tháng nhỏ hơn (CHỈ KHI CẢ HAI ĐỀU ĐÃ NHẬP GIÁ)
      for (let j = 0; j < i; j++) {
        const prevMonth = sortedMonths[j].toString();
        const prevPriceStr = prices[prevMonth];

        // CHỈ validate khi gói trước đó đã nhập giá
        if (prevPriceStr && prevPriceStr.trim() !== '') {
          const prevPrice = parseInt(parseFormattedPrice(prevPriceStr));
          if (!isNaN(prevPrice) && prevPrice > 0 && price <= prevPrice) {
            errors[month] = `Giá gói ${month} tháng phải cao hơn gói ${prevMonth} tháng`;
            break;
          }
        }
      }

      // Kiểm tra với các gói tháng lớn hơn (CHỈ KHI CẢ HAI ĐỀU ĐÃ NHẬP GIÁ)
      for (let j = i + 1; j < sortedMonths.length; j++) {
        const nextMonth = sortedMonths[j].toString();
        const nextPriceStr = prices[nextMonth];

        // CHỈ validate khi gói sau đó đã nhập giá
        if (nextPriceStr && nextPriceStr.trim() !== '') {
          const nextPrice = parseInt(parseFormattedPrice(nextPriceStr));
          if (!isNaN(nextPrice) && nextPrice > 0 && price >= nextPrice) {
            errors[month] = `Giá gói ${month} tháng phải thấp hơn gói ${nextMonth} tháng`;
            break;
          }
        }
      }
    }

    return errors;
  };

  const validateDayPackagePrices = (dayPrices, selectedDays, monthPrices, selectedMonths) => {
    const errors = {};

    if (selectedDays.length === 0 || selectedMonths.length === 0) {
      return errors;
    }

    let minMonthPrice = Infinity;
    for (const month of selectedMonths) {
      const price = monthPrices[month];
      if (price && price.trim() !== '') {
        const priceValue = parseInt(parseFormattedPrice(price));
        if (!isNaN(priceValue) && priceValue > 0 && priceValue < minMonthPrice) {
          minMonthPrice = priceValue;
        }
      }
    }

    for (const day of selectedDays) {
      const priceStr = dayPrices[day];
      if (!priceStr || priceStr.trim() === '') { continue; }

      const priceValue = parseInt(parseFormattedPrice(priceStr));
      if (isNaN(priceValue) || priceValue <= 0) {
        errors[day] = 'Giá phải lớn hơn 0';
        continue;
      }

      if (minMonthPrice !== Infinity && priceValue >= minMonthPrice) {
        errors[day] = `Giá gói ${day} ngày phải thấp hơn gói tháng thấp nhất`;
      }
    }

    return errors;
  };

  // Hàm capitalize chữ cái đầu
  const capitalizeFirstLetter = (str) => {
    if (!str) return '';
    return str.charAt(0).toUpperCase() + str.slice(1);
  };

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;

    // Xử lý đặc biệt cho location và name - auto capitalize
    if (name === 'location' || name === 'name') {
      let processedValue = value;

      // Auto capitalize chữ cái đầu
      if (processedValue.length > 0) {
        processedValue = capitalizeFirstLetter(processedValue);
      }

      // Cập nhật giá trị với chữ cái đầu viết hoa
      setFormData(prev => ({
        ...prev,
        [name]: processedValue
      }));

      // Real-time validation
      const newErrors = { ...errors };
      
      if (name === 'name') {
        if (!processedValue.trim()) {
          newErrors.name = 'Tên tiện ích là bắt buộc';
        } else if (processedValue.length < 3) {
          newErrors.name = 'Tên tiện ích phải có ít nhất 3 ký tự';
        } else if (processedValue.length > 50) {
          newErrors.name = 'Tên tiện ích không được vượt quá 50 ký tự';
        } else if (!/^[a-zA-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ0-9\s]+$/.test(processedValue)) {
          newErrors.name = 'Tên tiện ích không được chứa ký tự đặc biệt';
        } else {
          delete newErrors.name;
        }
      }
      
      if (name === 'location') {
        if (processedValue.trim().length > 0 && processedValue.trim().length < 3) {
          newErrors.location = 'Vị trí phải có ít nhất 3 ký tự';
        } else if (processedValue.length > 50) {
          newErrors.location = 'Vị trí không được vượt quá 50 ký tự';
        } else if (processedValue.trim() && !/^[a-zA-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ0-9\s,]+$/.test(processedValue)) {
          newErrors.location = 'Vị trí không được chứa ký tự đặc biệt (cho phép dấu phẩy)';
        } else {
          delete newErrors.location;
        }
      }
      
      setErrors(newErrors);
      return;
    }

    // Xử lý các trường khác
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));

    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: '' }));
    }

    // Real-time validation for code
    if (name === 'code' && value.trim()) {
      const codePattern = /^[A-Z]{3}_\d{3}$/;
      if (!codePattern.test(value)) {
        setErrors(prev => ({
          ...prev,
          code: 'Mã phải có dạng ABC_123 (VD: GYM_001, POL_999)'
        }));
      } else {
        // Check uniqueness
        const existingCode = existingAmenities.find(amenity =>
          amenity.code && amenity.code.toLowerCase() === value.toLowerCase()
        );
        if (existingCode) {
          setErrors(prev => ({
            ...prev,
            code: 'Mã tiện ích này đã tồn tại trong hệ thống'
          }));
        } else {
          setErrors(prev => ({
            ...prev,
            code: ''
          }));
        }
      }
    }

  };

  const validateForm = () => {
    const newErrors = {};

    // Validate mã tiện ích: 3 chữ HOA + "_" + đúng 3 số
    // Pattern: ABC_123 (VD: GYM_001, POL_999)
    if (!formData.code.trim()) {
      newErrors.code = 'Mã tiện ích là bắt buộc';
    } else {
      const codePattern = /^[A-Z]{3}_\d{3}$/;
      if (!codePattern.test(formData.code)) {
        newErrors.code = 'Mã phải có dạng ABC_123 (VD: GYM_001, POL_999)';
      } else {
        // Kiểm tra mã tiện ích đã tồn tại
        const existingCode = existingAmenities.find(amenity =>
          amenity.code && amenity.code.toLowerCase() === formData.code.toLowerCase()
        );
        if (existingCode) {
          newErrors.code = 'Mã tiện ích này đã tồn tại';
        }
      }
    }

    // Validate tên tiện ích: Chữ đầu viết hoa, không ký tự đặc biệt, tối đa 50 ký tự
    if (!formData.name.trim()) {
      newErrors.name = 'Tên tiện ích là bắt buộc';
    } else {
      if (formData.name.length < 3) {
        newErrors.name = 'Tên tiện ích phải có ít nhất 3 ký tự';
      } else if (formData.name.length > 50) {
        newErrors.name = 'Không được vượt quá 50 ký tự';
      } else if (!/^[a-zA-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ0-9\s]+$/.test(formData.name)) {
        newErrors.name = 'Chỉ được chứa chữ cái, số và khoảng trắng';
      } else {
        // Kiểm tra tên không được trùng
        const existingName = existingAmenities.find(amenity =>
          amenity.name && amenity.name.toLowerCase() === formData.name.toLowerCase()
        );
        if (existingName) {
          newErrors.name = 'Tên tiện ích này đã tồn tại';
        }
      }
    }

    // Validate vị trí (tầng)
    if (!formData.location) {
      newErrors.location = 'Vui lòng nhập vị trí';
    } else if (formData.location.trim().length > 50) {
      newErrors.location = 'Vị trí không được quá 50 ký tự';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateForm()) return;

    // Validate giá cho các gói đã chọn
    if (formData.fee_type === 'Paid' && selectedMonthCounts.length > 0) {
      const priceErrors = validatePackagePrices(packagePrices, selectedMonthCounts);
      if (Object.keys(priceErrors).length > 0) {
        setPackagePriceErrors(priceErrors);
        alert('Vui lòng kiểm tra lại giá của các gói tháng. Gói tháng nhiều hơn phải có giá cao hơn gói tháng ít hơn.');
        return;
      }
      
      for (const monthCount of selectedMonthCounts) {
        const price = packagePrices[monthCount];
        if (!price || price.trim() === '') {
          alert(`Vui lòng nhập giá cho gói ${monthCount} tháng`);
          return;
        }
        const priceValue = parseInt(parseFormattedPrice(price));
        if (isNaN(priceValue) || priceValue < 10000) {
          alert(`Giá cho gói ${monthCount} tháng phải từ 10.000 VNĐ trở lên`);
          return;
        }
        if (priceValue > 10000000) {
          alert(`Giá cho gói ${monthCount} tháng không được vượt quá 10.000.000 VNĐ`);
          return;
        }
      }
    }

    if (formData.fee_type === 'Paid' && selectedDayCounts.length > 0) {
      const dayErrors = validateDayPackagePrices(dayPackagePrices, selectedDayCounts, packagePrices, selectedMonthCounts);
      if (Object.keys(dayErrors).length > 0) {
        setDayPackagePriceErrors(dayErrors);
        alert('Vui lòng kiểm tra lại giá của các gói ngày.');
        return;
      }

      for (const dayCount of selectedDayCounts) {
        const price = dayPackagePrices[dayCount];
        if (!price || price.trim() === '') {
          alert(`Vui lòng nhập giá cho gói ${dayCount} ngày`);
          return;
        }
        const priceValue = parseInt(parseFormattedPrice(price));
        if (isNaN(priceValue) || priceValue < 10000) {
          alert(`Giá cho gói ${dayCount} ngày phải từ 10.000 VNĐ trở lên`);
          return;
        }
        if (priceValue > 10000000) {
          alert(`Giá cho gói ${dayCount} ngày không được vượt quá 10.000.000 VNĐ`);
          return;
        }
      }
    }

    setLoading(true);

    try {
      const submitData = {
        code: formData.code,
        name: formData.name,
        location: formData.location,
        status: formData.status,
        hasMonthlyPackage: formData.has_monthly_package,
        requiresFaceVerification: formData.requires_face_verification,
        feeType: formData.fee_type,
        categoryName: formData.category_name || (categories.length > 0 ? categories[0] : 'Chung')
      };

      const response = await amenitiesApi.create(submitData);

      // Nếu có các gói tháng được chọn, tạo packages sau khi amenity được tạo
      const amenityId = response?.amenityId || response?.id || response?.data?.amenityId || response?.data?.id;

      if (amenityId) {
        if (selectedMonthCounts.length > 0) {
          for (const monthCount of selectedMonthCounts) {
            const price = packagePrices[monthCount] ? parseInt(packagePrices[monthCount].replace(/\./g, '')) : 0;
            try {
              await amenityPackageApi.create({
                amenityId: amenityId,
                name: `Gói ${monthCount} tháng`,
                monthCount: parseInt(monthCount),
                price: price,
                description: `Gói sử dụng ${monthCount} tháng`,
                status: 'ACTIVE'
              });
            } catch (err) {
              console.error('Error creating package:', err);
            }
          }
        }

        if (selectedDayCounts.length > 0) {
          for (const dayCount of selectedDayCounts) {
            const price = dayPackagePrices[dayCount] ? parseInt(dayPackagePrices[dayCount].replace(/\./g, '')) : 0;
            try {
              await amenityPackageApi.create({
                amenityId: amenityId,
                name: `Gói ${dayCount} ngày`,
                durationDays: parseInt(dayCount),
                monthCount: 0,
                periodUnit: 'Day',
                price: price,
                description: `Gói sử dụng ${dayCount} ngày`,
                status: 'ACTIVE'
              });
            } catch (err) {
              console.error('Error creating day package:', err);
            }
          }
        }
      }

      resetForm();
      onHide();
      onShowToast?.('Đã tạo mới thành công');
      onSuccess?.(response?.amenityId || response?.id);

    } catch (error) {
      let errorMessage = 'Có lỗi xảy ra khi tạo tiện ích. Vui lòng thử lại!';

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
      name: '',
      location: '',
      has_monthly_package: false,
      requires_face_verification: false,
      fee_type: 'Paid',
      status: 'ACTIVE',
      category_name: categories.length > 0 ? categories[0] : ''
    });
    setErrors({});
    setPackagePriceErrors({});
    setSelectedMonthCounts(['1']); // Reset về mặc định chọn 1 tháng
    setPackagePrices({ '1': '' }); // Reset giá
    setSelectedDayCounts([]);
    setDayPackagePrices({});
    setDayPackagePriceErrors({});
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
            <span>Thêm tiện ích mới</span>
          </div>
        </Modal.Title>
      </Modal.Header>

      <Modal.Body>
        <Form onSubmit={handleSubmit}>
          <Row>
            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Mã tiện ích <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="text"
                  name="code"
                  value={formData.code}
                  onChange={handleChange}
                  placeholder="VD: GYM_001"
                  isInvalid={!!errors.code}
                  maxLength={8}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.code}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Tên tiện ích <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="text"
                  name="name"
                  value={formData.name}
                  onChange={handleChange}
                  placeholder="VD: Phòng Gym"
                  isInvalid={!!errors.name}
                  maxLength={50}
                  autoComplete="off"
                  spellCheck="false"
                  autoCorrect="off"
                  autoCapitalize="off"
                  style={{
                    textTransform: 'none',
                    fontVariant: 'normal'
                  }}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.name}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>

          <Row>
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
                  placeholder="VD: Tầng 1, Tầng trệt..."
                  isInvalid={!!errors.location}
                  maxLength={50}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.location}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>Trạng thái</Form.Label>
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

          <Row>
            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>Loại phí</Form.Label>
                <Form.Select
                  name="fee_type"
                  value={formData.fee_type}
                  onChange={handleChange}
                >
                  <option value="Paid">Có phí</option>
                  <option value="Free">Miễn phí</option>
                </Form.Select>
              </Form.Group>
            </Col>

            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>Danh mục</Form.Label>
                <Form.Select
                  name="category_name"
                  value={formData.category_name}
                  onChange={handleChange}
                >
                  {categories.map(category => (
                    <option key={category} value={category}>{category}</option>
                  ))}
                </Form.Select>
              </Form.Group>
            </Col>
          </Row>

          <Row>
            <Col md={12}>
              <Form.Group className="mb-3">
                <Form.Check
                  type="switch"
                  id="requires-face-verification"
                  name="requires_face_verification"
                  label="Yêu cầu cư dân check-in bằng khuôn mặt"
                  checked={formData.requires_face_verification}
                  onChange={handleChange}
                />
                <Form.Text className="text-muted">
                  Bật tùy chọn này nếu tiện ích chỉ cho phép đặt lịch/check-in khi cư dân đã đăng ký khuôn mặt.
                </Form.Text>
              </Form.Group>
            </Col>
          </Row>


          {formData.fee_type === 'Paid' && (
            <Row>
              <Col md={12}>
                <Form.Group className="mb-3">
                  <Form.Label>Chọn các gói tháng và nhập giá</Form.Label>
                  <div className="mt-2">
                    {/* Gói ngày */}
                    {['3'].map((day) => {
                      const isSelected = selectedDayCounts.includes(day);
                      return (
                        <div key={`day-${day}`} className="mb-3 p-3 border rounded">
                          <div className="d-flex align-items-center gap-3">
                            <Form.Check
                              type="checkbox"
                              id={`day-${day}`}
                              label={`${day} ngày`}
                              checked={isSelected}
                              onChange={(e) => {
                                const nextSelected = e.target.checked
                                  ? [...selectedDayCounts, day]
                                  : selectedDayCounts.filter(d => d !== day);
                                setSelectedDayCounts(nextSelected);

                                const newPrices = { ...dayPackagePrices };
                                if (e.target.checked) {
                                  if (!newPrices[day]) {
                                    newPrices[day] = '';
                                  }
                                } else {
                                  delete newPrices[day];
                                }
                                setDayPackagePrices(newPrices);

                                setDayPackagePriceErrors(prev => {
                                  const updated = { ...prev };
                                  if (!e.target.checked) {
                                    delete updated[day];
                                  }
                                  return updated;
                                });

                                setFormData(prev => ({
                                  ...prev,
                                  has_monthly_package: nextSelected.length > 0 || selectedMonthCounts.length > 0
                                }));
                              }}
                              className="mb-0"
                            />
                            {isSelected && (
                              <div className="flex-grow-1" style={{ maxWidth: '300px' }}>
                                <InputGroup>
                                  <Form.Control
                                    type="text"
                                    placeholder="Nhập giá (VNĐ)"
                                    value={dayPackagePrices[day] || ''}
                                    onChange={(e) => {
                                      const formatted = formatPrice(e.target.value);
                                      const newPrices = { ...dayPackagePrices, [day]: formatted };
                                      setDayPackagePrices(newPrices);
                                      
                                      // Validate giá trong khoảng cho phép
                                      const priceValue = parseInt(parseFormattedPrice(formatted));
                                      let newErrors = {};
                                      
                                      if (formatted && (!isNaN(priceValue))) {
                                        if (priceValue < 10000) {
                                          newErrors[day] = 'Giá phải từ 10.000 VNĐ trở lên';
                                        } else if (priceValue > 10000000) {
                                          newErrors[day] = 'Giá không được vượt quá 10.000.000 VNĐ';
                                        } else {
                                          // Giá hợp lệ - Validate lại với gói tháng
                                          const errors = validateDayPackagePrices(newPrices, selectedDayCounts, packagePrices, selectedMonthCounts);
                                          newErrors = errors;
                                        }
                                      } else {
                                        // Nếu chưa nhập hoặc xoá giá
                                        const errors = validateDayPackagePrices(newPrices, selectedDayCounts, packagePrices, selectedMonthCounts);
                                        newErrors = errors;
                                      }
                                      setDayPackagePriceErrors(newErrors);
                                    }}
                                  />
                                  <InputGroup.Text>VNĐ</InputGroup.Text>
                                </InputGroup>
                                {dayPackagePriceErrors[day] && (
                                  <div className="text-danger small mt-1">
                                    {dayPackagePriceErrors[day]}
                                  </div>
                                )}
                              </div>
                            )}
                          </div>
                        </div>
                      );
                    })}

                    {/* Gói tháng */}
                    {['1', '3', '6', '12'].map((month) => {
                      const isSelected = selectedMonthCounts.includes(month);
                      return (
                        <div key={month} className="mb-3 p-3 border rounded">
                          <div className="d-flex align-items-center gap-3">
                            <Form.Check
                              type="checkbox"
                              id={`month-${month}`}
                              label={`${month} tháng`}
                              checked={isSelected}
                              onChange={(e) => {
                                if (e.target.checked) {
                                  const newSelected = [...selectedMonthCounts, month];
                                  setSelectedMonthCounts(newSelected);
                                  // Thêm giá mặc định nếu chưa có
                                  const newPrices = { ...packagePrices };
                                  if (!newPrices[month]) {
                                    newPrices[month] = '';
                                  }
                                  setPackagePrices(newPrices);
                                  
                                  // Validate lại khi thêm gói mới
                                  const priceErrors = validatePackagePrices(newPrices, newSelected);
                                  setPackagePriceErrors(priceErrors);
                                  if (selectedDayCounts.length > 0) {
                                    const dayErrors = validateDayPackagePrices(dayPackagePrices, selectedDayCounts, newPrices, newSelected);
                                    setDayPackagePriceErrors(dayErrors);
                                  }
                                } else {
                                  const newSelected = selectedMonthCounts.filter(m => m !== month);
                                  setSelectedMonthCounts(newSelected);
                                  // Xóa giá khi bỏ chọn
                                  const newPrices = { ...packagePrices };
                                  delete newPrices[month];
                                  setPackagePrices(newPrices);

                                  // Validate lại TẤT CẢ các gói còn lại sau khi bỏ chọn
                                  const priceErrors = validatePackagePrices(newPrices, newSelected);
                                  setPackagePriceErrors(priceErrors);
                                  
                                  // Validate lại: Giá gói tháng với gói ngày
                                  if (selectedDayCounts.length > 0) {
                                    const dayErrors = validateDayPackagePrices(dayPackagePrices, selectedDayCounts, newPrices, newSelected);
                                    setDayPackagePriceErrors(dayErrors);
                                  }
                                }
                                // Tự động bật has_monthly_package khi có ít nhất 1 gói được chọn
                                setFormData(prev => ({
                                  ...prev,
                                  has_monthly_package: (e.target.checked ? [...selectedMonthCounts, month] : selectedMonthCounts.filter(m => m !== month)).length > 0
                                }));
                              }}
                              className="mb-0"
                            />
                            {isSelected && (
                              <div className="flex-grow-1" style={{ maxWidth: '300px' }}>
                                <InputGroup>
                                  <Form.Control
                                    type="text"
                                    placeholder="Nhập giá (VNĐ)"
                                    value={packagePrices[month] || ''}
                                    onChange={(e) => {
                                      const formatted = formatPrice(e.target.value);
                                      const newPrices = { ...packagePrices, [month]: formatted };
                                      setPackagePrices(newPrices);

                                      // Validate giá trong khoảng cho phép
                                      const priceValue = parseInt(parseFormattedPrice(formatted));
                                      let newErrors = {};
                                      
                                      if (formatted && (!isNaN(priceValue))) {
                                        if (priceValue < 10000) {
                                          newErrors[month] = 'Giá phải từ 10.000 VNĐ trở lên';
                                        } else if (priceValue > 10000000) {
                                          newErrors[month] = 'Giá không được vượt quá 10.000.000 VNĐ';
                                        } else {
                                          // Giá hợp lệ trong khoảng - Validate LẠI TẤT CẢ các gói
                                          const allErrors = validatePackagePrices(newPrices, selectedMonthCounts);
                                          newErrors = allErrors;
                                        }
                                      } else {
                                        // Nếu chưa nhập hoặc xoá giá - validate lại các gói khác
                                        const allErrors = validatePackagePrices(newPrices, selectedMonthCounts);
                                        newErrors = allErrors;
                                      }
                                      
                                      setPackagePriceErrors(newErrors);
                                      
                                      // Validate real-time: Giá gói tháng với gói ngày
                                      if (selectedDayCounts.length > 0) {
                                        const dayErrors = validateDayPackagePrices(dayPackagePrices, selectedDayCounts, newPrices, selectedMonthCounts);
                                        setDayPackagePriceErrors(dayErrors);
                                      }
                                    }}
                                    onBlur={(e) => {
                                      // Validate giá khi blur
                                      const value = parseFormattedPrice(e.target.value);
                                      if (value && parseInt(value) <= 0) {
                                        const newErrors = { ...packagePriceErrors, [month]: 'Giá phải lớn hơn 0' };
                                        setPackagePriceErrors(newErrors);
                                      }
                                    }}
                                  />
                                  <InputGroup.Text>VNĐ</InputGroup.Text>
                                </InputGroup>
                                {packagePriceErrors[month] && (
                                  <div className="text-danger small mt-1">
                                    {packagePriceErrors[month]}
                                  </div>
                                )}
                              </div>
                            )}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                  <Form.Text className="text-muted d-block mt-2">
                    Chọn một hoặc nhiều gói tháng và nhập giá cho từng gói
                  </Form.Text>
                </Form.Group>
              </Col>
            </Row>
          )}
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
          variant="success"
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
