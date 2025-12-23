import React, { useState, useEffect, useMemo } from 'react';
import { Modal, Form, Button, Row, Col } from 'react-bootstrap';
import { assetsApi } from '../../../features/building-management/assetsApi';
import useNotification from '../../../hooks/useNotification';
import dayjs from 'dayjs';

export default function CreateMaintenanceSchedule({ 
  show, 
  onHide, 
  onSuccess, 
  assets = [] 
}) {
  const { showNotification } = useNotification();
  
  // Lọc bỏ các tài sản có trạng thái "INACTIVE" (Không hoạt động) và "MAINTENANCE" (Đang bảo trì)
  // Tài sản đang bảo trì không thể tạo lịch bảo trì mới
  const activeAssets = useMemo(() => {
    return assets.filter(asset => 
      asset.status !== 'INACTIVE' && 
      asset.status !== 'MAINTENANCE' && 
      !asset.isDelete
    );
  }, [assets]);
  
  const [formData, setFormData] = useState({
    assetId: '',
    assetName: '', // Thêm field để hiển thị tên tài sản đã chọn
    startDate: '',
    endDate: '',
    startTime: '',
    endTime: '',
    recurrence: '',
    recurrenceInterval: 1,
    notes: ''
  });
  const [hasTimeSlot, setHasTimeSlot] = useState(false);
  const [assetSearchTerm, setAssetSearchTerm] = useState(''); // Từ khóa tìm kiếm
  const [showAssetSuggestions, setShowAssetSuggestions] = useState(false); // Hiển thị dropdown suggestions

  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});

  // Filter assets theo từ khóa tìm kiếm
  const filteredAssetsForSearch = useMemo(() => {
    if (!assetSearchTerm.trim()) {
      return activeAssets.slice(0, 10); // Hiển thị 10 tài sản đầu tiên nếu không có từ khóa
    }
    
    const searchLower = assetSearchTerm.toLowerCase().trim();
    return activeAssets.filter(asset => {
      const assetName = (asset.assetName || asset.name || '').toLowerCase();
      return assetName.includes(searchLower);
    }).slice(0, 10); // Giới hạn tối đa 10 kết quả
  }, [activeAssets, assetSearchTerm]);

  useEffect(() => {
    if (show) {
      // Reset form khi mở modal
      setFormData({
        assetId: '',
        assetName: '',
        startDate: '',
        endDate: '',
        startTime: '',
        endTime: '',
        recurrence: '',
        recurrenceInterval: 1,
        notes: ''
      });
      setAssetSearchTerm('');
      setShowAssetSuggestions(false);
      setHasTimeSlot(false);
      setErrors({});
    }
  }, [show]);

  // Cập nhật assetSearchTerm khi formData.assetName thay đổi (khi đã chọn tài sản từ nguồn khác)
  useEffect(() => {
    if (formData.assetName && formData.assetId && assetSearchTerm !== formData.assetName) {
      setAssetSearchTerm(formData.assetName);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [formData.assetName, formData.assetId]);

  const resetForm = () => {
      setFormData({
        assetId: '',
        assetName: '',
        startDate: '',
        endDate: '',
        startTime: '',
        endTime: '',
        recurrence: '',
        recurrenceInterval: 1,
        notes: ''
      });
      setAssetSearchTerm('');
      setShowAssetSuggestions(false);
      setHasTimeSlot(false);
      setErrors({});
  };

  // Xử lý khi chọn tài sản từ suggestions
  const handleAssetSelect = (asset) => {
    // Tự động điền recurrenceType và recurrenceInterval nếu tài sản có maintenanceFrequency
    const updatedFormData = {
      assetId: asset.assetId,
      assetName: asset.assetName || asset.name || ''
    };
    
    // Lấy maintenanceFrequency (có thể là camelCase hoặc PascalCase)
    const maintenanceFrequency = asset.maintenanceFrequency || asset.MaintenanceFrequency;
    
    // Nếu tài sản có maintenanceFrequency, tự động điền recurrence
    if (maintenanceFrequency && maintenanceFrequency > 0) {
      updatedFormData.recurrence = 'DAILY';
      updatedFormData.recurrenceInterval = maintenanceFrequency;
    }
    
    setFormData(prev => ({
      ...prev,
      ...updatedFormData
    }));
    setAssetSearchTerm(asset.assetName || asset.name || '');
    setShowAssetSuggestions(false);
    if (errors.assetId) {
      setErrors(prev => ({ ...prev, assetId: '' }));
    }
  };

  // Xử lý khi người dùng nhập tìm kiếm tài sản
  const handleAssetSearchChange = (e) => {
    const value = e.target.value;
    setAssetSearchTerm(value);
    setShowAssetSuggestions(true);
    
    // Nếu xóa hết hoặc thay đổi, reset selection
    if (!value.trim() || value !== formData.assetName) {
      setFormData(prev => ({ ...prev, assetId: '', assetName: '' }));
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    
    setFormData(prev => ({
      ...prev,
      [name]: name === 'recurrenceInterval' ? parseInt(value) || 1 : value
    }));

    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: '' }));
    }
  };

  const handleDateChange = (field, value) => {
    const dateValue = value ? dayjs(value).format('YYYY-MM-DD') : '';
    setFormData(prev => {
      const updated = {
        ...prev,
        [field]: dateValue
      };
      
      const startDate = field === 'startDate' ? dayjs(dateValue) : dayjs(updated.startDate);
      const endDate = field === 'endDate' ? dayjs(dateValue) : dayjs(updated.endDate);
      const isSameDay = startDate.isValid() && endDate.isValid() && startDate.isSame(endDate, 'day');
      
      if (field === 'startDate') {
        const today = dayjs().startOf('day');
        if (dateValue && dayjs(dateValue).isBefore(today)) {
          setErrors(prev => ({
            ...prev,
            startDate: 'Ngày bắt đầu không được trong quá khứ'
          }));
        } else if (updated.endDate && dateValue && dayjs(dateValue).isAfter(dayjs(updated.endDate))) {
          setErrors(prev => ({
            ...prev,
            startDate: 'Ngày bắt đầu phải trước ngày kết thúc'
          }));
        } else {
          setErrors(prev => ({ ...prev, startDate: '' }));
        }
      }
      
      if (field === 'endDate' && updated.startDate) {
        if (dateValue && dayjs(dateValue).isBefore(dayjs(updated.startDate))) {
          setErrors(prev => ({
            ...prev,
            endDate: 'Ngày kết thúc phải sau ngày bắt đầu'
          }));
        } else {
          setErrors(prev => ({ ...prev, endDate: '' }));
        }
      }
      
      if (!isSameDay && hasTimeSlot) {
        setErrors(prev => ({ ...prev, startTime: '', endTime: '' }));
      }
      
      return updated;
    });
  };

  const handleTimeChange = (field, value) => {
    const timeValue = value || '';
    
    setFormData(prev => {
      const updated = {
        ...prev,
        [field]: timeValue
      };
      
      setTimeout(() => {
        if (!updated.startDate || !updated.endDate) {
          return;
        }
        
        const startDate = dayjs(updated.startDate);
        const endDate = dayjs(updated.endDate);
        const isSameDay = startDate.isSame(endDate, 'day');
        const today = dayjs().startOf('day');
        const now = dayjs();
        
        // Real-time validation: Kiểm tra giờ bắt đầu không được trong quá khứ nếu là ngày hôm nay
        if (field === 'startTime' && timeValue && startDate.isSame(today, 'day')) {
          const startTimeStr = timeValue.length === 5 ? timeValue + ':00' : timeValue;
          const startDateTime = dayjs(`${updated.startDate} ${startTimeStr}`);
          
          if (startDateTime.isValid() && startDateTime.isBefore(now)) {
            setErrors(prev => ({
              ...prev,
              startTime: 'Giờ bắt đầu không được trong quá khứ'
            }));
            return; // Dừng validation tiếp
          }
        }
        
        if (field === 'endTime' && updated.startTime && timeValue && isSameDay) {
          const startTimeStr = updated.startTime.length === 5 ? updated.startTime + ':00' : updated.startTime;
          const endTimeStr = timeValue.length === 5 ? timeValue + ':00' : timeValue;
          const startTime = dayjs(startTimeStr, ['HH:mm:ss', 'HH:mm']);
          const endTime = dayjs(endTimeStr, ['HH:mm:ss', 'HH:mm']);
          if (!endTime.isValid() || !startTime.isValid()) {
            return;
          }
          if (endTime.isBefore(startTime) || endTime.isSame(startTime)) {
            setErrors(prev => ({
              ...prev,
              endTime: 'Khi cùng ngày, giờ kết thúc phải sau giờ bắt đầu'
            }));
          } else {
            setErrors(prev => ({ ...prev, endTime: '' }));
          }
        }
        
        if (field === 'startTime' && updated.endTime && timeValue && isSameDay) {
          const startTimeStr = timeValue.length === 5 ? timeValue + ':00' : timeValue;
          const endTimeStr = updated.endTime.length === 5 ? updated.endTime + ':00' : updated.endTime;
          const startTime = dayjs(startTimeStr, ['HH:mm:ss', 'HH:mm']);
          const endTime = dayjs(endTimeStr, ['HH:mm:ss', 'HH:mm']);
          if (!endTime.isValid() || !startTime.isValid()) {
            return;
          }
          if (startTime.isAfter(endTime) || startTime.isSame(endTime)) {
            setErrors(prev => ({
              ...prev,
              startTime: 'Khi cùng ngày, giờ bắt đầu phải trước giờ kết thúc'
            }));
          } else {
            setErrors(prev => ({ ...prev, startTime: '' }));
          }
        }
        
        if (!isSameDay) {
          setErrors(prev => ({ ...prev, startTime: '', endTime: '' }));
        }
      }, 0);
      
      return updated;
    });
  };

  const validateForm = () => {
    const newErrors = {};

    if (!formData.assetId || !formData.assetName) {
      newErrors.assetId = 'Vui lòng chọn tài sản';
    } else {
      const assetExists = activeAssets.some(a => 
        String(a.assetId) === String(formData.assetId) || 
        a.assetId === formData.assetId
      );
      
      if (!assetExists) {
        newErrors.assetId = 'Tài sản không hợp lệ. Vui lòng chọn lại từ danh sách gợi ý.';
      }
    }

    if (!formData.startDate) {
      newErrors.startDate = 'Vui lòng chọn ngày bắt đầu';
    } else {
      const today = dayjs().startOf('day');
      if (dayjs(formData.startDate).isBefore(today)) {
        newErrors.startDate = 'Ngày bắt đầu không được trong quá khứ';
      }
    }

    if (!formData.endDate) {
      newErrors.endDate = 'Vui lòng chọn ngày kết thúc';
    }

    if (formData.startDate && formData.endDate) {
      if (dayjs(formData.startDate).isAfter(dayjs(formData.endDate))) {
        newErrors.startDate = 'Ngày bắt đầu phải trước ngày kết thúc';
        newErrors.endDate = 'Ngày kết thúc phải sau ngày bắt đầu';
      }
    }

    if (hasTimeSlot) {
      if (!formData.startTime) {
        newErrors.startTime = 'Vui lòng chọn giờ bắt đầu';
      }
      if (!formData.endTime) {
        newErrors.endTime = 'Vui lòng chọn giờ kết thúc';
      }
      
      // Kiểm tra giờ bắt đầu không được trong quá khứ nếu là ngày hôm nay
      if (formData.startDate && formData.startTime) {
        const startDate = dayjs(formData.startDate);
        const today = dayjs().startOf('day');
        
        if (startDate.isSame(today, 'day')) {
          const now = dayjs();
          const startTimeStr = formData.startTime.length === 5 ? formData.startTime + ':00' : formData.startTime;
          const startDateTime = dayjs(`${formData.startDate} ${startTimeStr}`);
          
          if (startDateTime.isValid() && startDateTime.isBefore(now)) {
            newErrors.startTime = 'Giờ bắt đầu không được trong quá khứ';
          }
        }
      }
      
      if (formData.startTime && formData.endTime && formData.startDate && formData.endDate) {
        const startDate = dayjs(formData.startDate);
        const endDate = dayjs(formData.endDate);
        const isSameDay = startDate.isSame(endDate, 'day');
        
        if (isSameDay) {
          const startTimeStr = formData.startTime.length === 5 ? formData.startTime + ':00' : formData.startTime;
          const endTimeStr = formData.endTime.length === 5 ? formData.endTime + ':00' : formData.endTime;
          const startTime = dayjs(startTimeStr, ['HH:mm:ss', 'HH:mm']);
          const endTime = dayjs(endTimeStr, ['HH:mm:ss', 'HH:mm']);
          if (startTime.isValid() && endTime.isValid()) {
            if (endTime.isBefore(startTime) || endTime.isSame(startTime)) {
              newErrors.endTime = 'Khi cùng ngày, giờ kết thúc phải sau giờ bắt đầu';
            }
          }
        }
      }
    }

    if (formData.recurrence && formData.recurrenceInterval < 1) {
      newErrors.recurrenceInterval = 'Khoảng lặp lại phải lớn hơn 0';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    try {
      setLoading(true);

      if (!formData.assetId) {
        showNotification('error', 'Lỗi', 'Tài sản không hợp lệ. Vui lòng chọn lại tài sản.');
        setLoading(false);
        return;
      }

      const selectedAsset = activeAssets.find(a => 
        String(a.assetId) === String(formData.assetId) || 
        a.assetId === parseInt(formData.assetId, 10)
      );
      
      if (!selectedAsset) {
        showNotification('error', 'Lỗi', 'Không tìm thấy thông tin tài sản. Vui lòng thử lại.');
        setLoading(false);
        return;
      }

      const assetGuid = selectedAsset.assetId;
      
      if (!assetGuid) {
        showNotification('error', 'Lỗi', 'Tài sản không có GUID hợp lệ. Vui lòng kiểm tra lại.');
        setLoading(false);
        return;
      }

      const formatTimeForBackend = (timeValue) => {
        if (!timeValue || !timeValue.trim()) return null;
        const trimmed = timeValue.trim();
        if (trimmed.length === 5 && trimmed.includes(':') && trimmed.split(':').length === 2) {
          return `${trimmed}:00`;
        }
        return trimmed;
      };

      const payload = {
        assetId: assetGuid,
        startDate: formData.startDate,
        endDate: formData.endDate,
        status: "SCHEDULED",
        reminderDays: 3
      };

      if (hasTimeSlot && formData.startTime && formData.endTime) {
        const startDate = dayjs(formData.startDate);
        const endDate = dayjs(formData.endDate);
        const isSameDay = startDate.isSame(endDate, 'day');
        
        const startTimeFormatted = formatTimeForBackend(formData.startTime);
        const endTimeFormatted = formatTimeForBackend(formData.endTime);
        
        if (startTimeFormatted && endTimeFormatted) {
          if (isSameDay) {
            const startTimeObj = dayjs(startTimeFormatted, 'HH:mm:ss');
            const endTimeObj = dayjs(endTimeFormatted, 'HH:mm:ss');
            if (startTimeObj.isValid() && endTimeObj.isValid() && endTimeObj.isAfter(startTimeObj)) {
              payload.startTime = startTimeFormatted;
              payload.endTime = endTimeFormatted;
            }
          } else {
            payload.startTime = startTimeFormatted;
            payload.endTime = endTimeFormatted;
          }
        }
      }

      if (formData.recurrence) {
        payload.recurrenceType = formData.recurrence;
        payload.recurrenceInterval = formData.recurrenceInterval || 1;
      }

      if (formData.notes && formData.notes.trim()) {
        payload.description = formData.notes.trim();
      }

      await assetsApi.createMaintenanceSchedule(payload);
      
      showNotification('success', 'Thành công', 'Đã tạo lịch bảo trì thành công');
      resetForm();
      onSuccess();
    } catch (error) {
      const errorData = error.response?.data;
      let errorMessage = 'Có lỗi xảy ra khi tạo lịch bảo trì. Vui lòng thử lại!';
      
      if (errorData?.message) {
        errorMessage = errorData.message;
      } else if (errorData?.error && errorData?.message) {
        errorMessage = errorData.message;
      } else if (errorData?.errors) {
        const errorMessages = [];
        if (errorData.errors['$.assetId']) {
          errorMessages.push('Tài sản không hợp lệ');
        }
        if (errorData.errors['createDto']) {
          errorMessages.push(...errorData.errors['createDto']);
        }
        if (errorMessages.length > 0) {
          errorMessage = errorMessages.join('. ');
        }
      } else if (errorData?.title) {
        errorMessage = errorData.title;
      }
      
      // Chỉ hiển thị notification, không hiển thị Alert trong modal
      showNotification('error', 'Lỗi', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    resetForm();
    onHide();
  };

  return (
    <Modal show={show} onHide={handleClose} size="lg" centered backdrop="static">
      <Modal.Header closeButton>
        <Modal.Title>
          Tạo lịch bảo trì mới
        </Modal.Title>
      </Modal.Header>
      
      <Form onSubmit={handleSubmit}>
        <Modal.Body>
          <Row>
            <Col md={12}>
              <Form.Group className="mb-3" style={{ position: 'relative' }}>
                <Form.Label>
                  Tài sản <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="text"
                  placeholder="Nhập tên tài sản để tìm kiếm..."
                  value={assetSearchTerm}
                  onChange={handleAssetSearchChange}
                  onFocus={() => setShowAssetSuggestions(true)}
                  onBlur={() => {
                    // Delay để cho phép click vào suggestion
                    setTimeout(() => setShowAssetSuggestions(false), 200);
                  }}
                  isInvalid={!!errors.assetId}
                  autoComplete="off"
                />
                <Form.Control.Feedback type="invalid">
                  {errors.assetId}
                </Form.Control.Feedback>
                
                {/* Dropdown suggestions */}
                {showAssetSuggestions && filteredAssetsForSearch.length > 0 && (
                  <div
                    style={{
                      position: 'absolute',
                      top: '100%',
                      left: 0,
                      right: 0,
                      zIndex: 1000,
                      backgroundColor: '#fff',
                      border: '1px solid #ced4da',
                      borderTop: 'none',
                      borderRadius: '0 0 0.375rem 0.375rem',
                      maxHeight: '200px',
                      overflowY: 'auto',
                      boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)'
                    }}
                    onMouseDown={(e) => {
                      // Ngăn blur event khi click vào dropdown
                      e.preventDefault();
                    }}
                  >
                    {filteredAssetsForSearch.map(asset => (
                      <div
                        key={asset.assetId}
                        onClick={() => handleAssetSelect(asset)}
                        style={{
                          padding: '8px 12px',
                          cursor: 'pointer',
                          borderBottom: '1px solid #f0f0f0',
                          transition: 'background-color 0.2s'
                        }}
                        onMouseEnter={(e) => {
                          e.currentTarget.style.backgroundColor = '#f5f5f5';
                        }}
                        onMouseLeave={(e) => {
                          e.currentTarget.style.backgroundColor = '#fff';
                        }}
                      >
                        {asset.assetName || asset.name || 'Tài sản'}
                      </div>
                    ))}
                  </div>
                )}
                
                {showAssetSuggestions && assetSearchTerm.trim() && filteredAssetsForSearch.length === 0 && (
                  <div
                    style={{
                      position: 'absolute',
                      top: '100%',
                      left: 0,
                      right: 0,
                      zIndex: 1000,
                      backgroundColor: '#fff',
                      border: '1px solid #ced4da',
                      borderTop: 'none',
                      borderRadius: '0 0 0.375rem 0.375rem',
                      padding: '12px',
                      textAlign: 'center',
                      color: '#6c757d',
                      boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)'
                    }}
                  >
                    Không tìm thấy tài sản nào
                  </div>
                )}
              </Form.Group>
            </Col>
          </Row>

          <Row>
            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Ngày bắt đầu <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="date"
                  name="startDate"
                  value={formData.startDate}
                  onChange={(e) => handleDateChange('startDate', e.target.value)}
                  min={dayjs().format('YYYY-MM-DD')}
                  isInvalid={!!errors.startDate}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.startDate}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Ngày kết thúc <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="date"
                  name="endDate"
                  value={formData.endDate}
                  onChange={(e) => handleDateChange('endDate', e.target.value)}
                  min={formData.startDate || dayjs().format('YYYY-MM-DD')}
                  isInvalid={!!errors.endDate}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.endDate}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>

          <Row>
            <Col md={12}>
              <Form.Group className="mb-3">
                <Form.Check
                  type="checkbox"
                  id="hasTimeSlot"
                  label="Chọn khung giờ cụ thể"
                  checked={hasTimeSlot}
                  onChange={(e) => {
                    setHasTimeSlot(e.target.checked);
                    if (!e.target.checked) {
                      setFormData(prev => ({ ...prev, startTime: '', endTime: '' }));
                      setErrors(prev => ({ ...prev, startTime: '', endTime: '' }));
                    }
                  }}
                />
                <Form.Text className="text-muted">
                  Nếu không chọn, hệ thống sẽ tự động hiểu từ 0h00 đến 23h59
                </Form.Text>
              </Form.Group>
            </Col>
          </Row>

          {hasTimeSlot && (
            <Row>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>
                    Giờ bắt đầu <span className="text-danger">*</span>
                  </Form.Label>
                  <Form.Control
                    type="time"
                    name="startTime"
                    value={formData.startTime ? formData.startTime.substring(0, 5) : ''}
                    onChange={(e) => handleTimeChange('startTime', e.target.value)}
                    isInvalid={!!errors.startTime}
                  />
                  <Form.Control.Feedback type="invalid">
                    {errors.startTime}
                  </Form.Control.Feedback>
                </Form.Group>
              </Col>

              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>
                    Giờ kết thúc <span className="text-danger">*</span>
                  </Form.Label>
                  <Form.Control
                    type="time"
                    name="endTime"
                    value={formData.endTime ? formData.endTime.substring(0, 5) : ''}
                    onChange={(e) => handleTimeChange('endTime', e.target.value)}
                    isInvalid={!!errors.endTime}
                  />
                  <Form.Control.Feedback type="invalid">
                    {errors.endTime}
                  </Form.Control.Feedback>
                </Form.Group>
              </Col>
            </Row>
          )}

          <Row>
            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>Lặp lại</Form.Label>
                <Form.Select
                  name="recurrence"
                  value={formData.recurrence}
                  onChange={handleChange}
                >
                  <option value="">Không lặp lại</option>
                  <option value="DAILY">Hàng ngày</option>
                  <option value="WEEKLY">Hàng tuần</option>
                  <option value="MONTHLY">Hàng tháng</option>
                  <option value="YEARLY">Hàng năm</option>
                </Form.Select>
              </Form.Group>
            </Col>

            {formData.recurrence && (
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>Khoảng lặp lại</Form.Label>
                  <Form.Control
                    type="number"
                    name="recurrenceInterval"
                    value={formData.recurrenceInterval}
                    onChange={handleChange}
                    min="1"
                    placeholder="VD: 2 (mỗi 2 ngày/tuần/tháng/năm)"
                    isInvalid={!!errors.recurrenceInterval}
                  />
                  <Form.Text className="text-muted">
                    VD: 2 = mỗi 2 {formData.recurrence === 'DAILY' ? 'ngày' : 
                                  formData.recurrence === 'WEEKLY' ? 'tuần' :
                                  formData.recurrence === 'MONTHLY' ? 'tháng' : 'năm'}
                  </Form.Text>
                  <Form.Control.Feedback type="invalid">
                    {errors.recurrenceInterval}
                  </Form.Control.Feedback>
                </Form.Group>
              </Col>
            )}
          </Row>

          <Row>
            <Col md={12}>
              <Form.Group className="mb-3">
                <Form.Label>Ghi chú</Form.Label>
                <Form.Control
                  as="textarea"
                  rows={3}
                  name="notes"
                  value={formData.notes}
                  onChange={handleChange}
                  placeholder="Nhập ghi chú về lịch bảo trì..."
                  maxLength={1000}
                />
              </Form.Group>
            </Col>
          </Row>
        </Modal.Body>

        <Modal.Footer>
          <Button variant="secondary" onClick={handleClose} disabled={loading}>
            Hủy
          </Button>
          <Button variant="primary" type="submit" disabled={loading}>
            {loading ? 'Đang tạo...' : 'Tạo lịch bảo trì'}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}

