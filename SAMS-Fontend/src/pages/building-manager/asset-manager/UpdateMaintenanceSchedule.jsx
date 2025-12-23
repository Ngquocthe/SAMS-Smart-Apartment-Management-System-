import React, { useState, useEffect, useMemo } from 'react';
import { Modal, Form, Button, Row, Col, Alert } from 'react-bootstrap';
import { assetsApi } from '../../../features/building-management/assetsApi';
import useNotification from '../../../hooks/useNotification';
import dayjs from 'dayjs';

export default function UpdateMaintenanceSchedule({ 
  show, 
  onHide, 
  onSuccess, 
  schedule,
  assets = [] 
}) {
  const { showNotification } = useNotification();
  
  // Lọc bỏ các tài sản có trạng thái "INACTIVE" (Không hoạt động) và "MAINTENANCE" (Đang bảo trì)
  // Tài sản đang bảo trì không thể chọn để cập nhật lịch (trừ tài sản hiện tại của schedule này)
  const activeAssets = useMemo(() => {
    const currentScheduleAssetId = schedule?.assetId || schedule?.asset?.assetId;
    return assets.filter(asset => {
      // Cho phép tài sản hiện tại của schedule này (để có thể cập nhật)
      if (currentScheduleAssetId && (asset.assetId === currentScheduleAssetId || String(asset.assetId) === String(currentScheduleAssetId))) {
        return !asset.isDelete;
      }
      // Các tài sản khác: loại bỏ INACTIVE và MAINTENANCE
      return asset.status !== 'INACTIVE' && 
             asset.status !== 'MAINTENANCE' && 
             !asset.isDelete;
    });
  }, [assets, schedule]);
  
  const [formData, setFormData] = useState({
    assetId: '',
    assetName: '',
    startDate: '',
    endDate: '',
    startTime: '',
    endTime: '',
    recurrence: '',
    recurrenceInterval: 1,
    notes: '',
    status: ''
  });
  const [hasTimeSlot, setHasTimeSlot] = useState(false);
  const [assetSearchTerm, setAssetSearchTerm] = useState('');
  const [showAssetSuggestions, setShowAssetSuggestions] = useState(false);
  const filteredAssetsForSearch = useMemo(() => {
    if (!assetSearchTerm.trim()) {
      return activeAssets.slice(0, 10);
    }
    
    const searchLower = assetSearchTerm.toLowerCase().trim();
    return activeAssets.filter(asset => {
      const assetName = (asset.assetName || asset.name || '').toLowerCase();
      const assetCode = (asset.code || '').toLowerCase();
      return assetName.includes(searchLower) || assetCode.includes(searchLower);
    }).slice(0, 10);
  }, [activeAssets, assetSearchTerm]);


  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});
  const [apiError, setApiError] = useState('');

  useEffect(() => {
    if (show && schedule) {
      const scheduleAssetId = schedule.assetId || schedule.asset?.assetId || '';
      const recurrenceType = schedule.recurrenceType || schedule.recurrence || '';
      const description = schedule.description || schedule.notes || '';
      const status = schedule.status || 'SCHEDULED';
      const startTime = schedule.startTime || '';
      const endTime = schedule.endTime || '';
      const hasTime = !!(startTime && endTime);
      
      const startTimeFormatted = startTime && startTime.length >= 5 
        ? startTime.substring(0, 5) 
        : startTime;
      const endTimeFormatted = endTime && endTime.length >= 5 
        ? endTime.substring(0, 5) 
        : endTime;
      
      setFormData({
        assetId: scheduleAssetId,
        assetName: schedule.asset?.assetName || schedule.assetName || schedule.asset?.name || '',
        startDate: schedule.startDate ? dayjs(schedule.startDate).format('YYYY-MM-DD') : '',
        endDate: schedule.endDate ? dayjs(schedule.endDate).format('YYYY-MM-DD') : '',
        startTime: startTimeFormatted,
        endTime: endTimeFormatted,
        recurrence: recurrenceType,
        recurrenceInterval: schedule.recurrenceInterval || 1,
        notes: description,
        status: status
      });
      setHasTimeSlot(hasTime);
      setAssetSearchTerm(schedule.asset?.assetName || schedule.assetName || schedule.asset?.name || '');
      setErrors({});
      setApiError('');
    }
  }, [show, schedule, assets]);

  const handleAssetSelect = (asset) => {
    setFormData(prev => ({
      ...prev,
      assetId: asset.assetId,
      assetName: asset.assetName || asset.name || ''
    }));
    setAssetSearchTerm(asset.assetName || asset.name || '');
    setShowAssetSuggestions(false);
    if (errors.assetId) {
      setErrors(prev => ({ ...prev, assetId: '' }));
    }
  };

  const handleAssetSearchChange = (e) => {
    const value = e.target.value;
    setAssetSearchTerm(value);
    setShowAssetSuggestions(true);
    
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
        // Chỉ validate ngày trong quá khứ nếu status là SCHEDULED (chưa bắt đầu)
        if (dateValue && dayjs(dateValue).isBefore(today) && updated.status === 'SCHEDULED') {
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
        
        // Real-time validation: Kiểm tra giờ bắt đầu không được trong quá khứ nếu là ngày hôm nay và status là SCHEDULED
        if (field === 'startTime' && timeValue && updated.status === 'SCHEDULED' && startDate.isSame(today, 'day')) {
          const startTimeStr = timeValue.length === 5 ? timeValue + ':00' : timeValue;
          const startDateTime = dayjs(`${updated.startDate} ${startTimeStr}`);
          
          if (startDateTime.isValid() && startDateTime.isBefore(now)) {
            setErrors(prev => ({
              ...prev,
              startTime: 'Giờ bắt đầu không được trong quá khứ'
            }));
            return;
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

    if (!formData.assetId) {
      newErrors.assetId = 'Vui lòng chọn tài sản';
    } else {
      const assetExists = activeAssets.some(a => 
        String(a.assetId) === String(formData.assetId) || 
        a.assetId === formData.assetId
      );
      
      if (!assetExists) {
        // Kiểm tra xem tài sản có tồn tại nhưng là INACTIVE không
        const inactiveAsset = assets.find(a => 
          (String(a.assetId) === String(formData.assetId) || a.assetId === formData.assetId) &&
          a.status === 'INACTIVE'
        );
        if (inactiveAsset) {
          newErrors.assetId = 'Không thể chọn tài sản đang ở trạng thái "Không hoạt động"';
        } else {
          newErrors.assetId = 'Tài sản không hợp lệ';
        }
      }
    }

    if (!formData.startDate) {
      newErrors.startDate = 'Vui lòng chọn ngày bắt đầu';
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
      
      // Kiểm tra giờ bắt đầu không được trong quá khứ nếu là ngày hôm nay và status là SCHEDULED
      if (formData.startDate && formData.startTime && formData.status === 'SCHEDULED') {
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

    if (!formData.status) {
      newErrors.status = 'Vui lòng chọn trạng thái';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setApiError('');

    if (!validateForm() || !schedule) {
      return;
    }

    try {
      setLoading(true);

      if (!formData.assetId) {
        setApiError('Vui lòng chọn tài sản.');
        return;
      }

      const selectedAsset = activeAssets.find(a => 
        String(a.assetId) === String(formData.assetId) || 
        a.assetId === formData.assetId
      );

      if (!selectedAsset) {
        // Kiểm tra xem tài sản có tồn tại nhưng là INACTIVE không
        const inactiveAsset = assets.find(a => 
          (String(a.assetId) === String(formData.assetId) || a.assetId === formData.assetId) &&
          a.status === 'INACTIVE'
        );
        if (inactiveAsset) {
          setApiError('Không thể chọn tài sản đang ở trạng thái "Không hoạt động". Vui lòng chọn tài sản khác.');
        } else {
          setApiError('Không tìm thấy thông tin tài sản. Vui lòng thử lại.');
        }
        return;
      }

      const scheduleId = schedule.id || schedule.scheduleId;
      
      if (!scheduleId) {
        setApiError('Không tìm thấy ID lịch bảo trì.');
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

      const scheduleData = {
        assetId: selectedAsset.assetId,
        startDate: formData.startDate,
        endDate: formData.endDate,
        recurrenceType: formData.recurrence ? formData.recurrence : null,
        recurrenceInterval: formData.recurrence ? (formData.recurrenceInterval || 1) : null,
        description: formData.notes && formData.notes.trim() ? formData.notes.trim() : null,
        status: formData.status
      };

      console.log('=== UpdateMaintenanceSchedule ===');
      console.log('ScheduleId:', scheduleId);
      console.log('Schedule Data:', scheduleData);
      console.log('API URL:', `/asset-maintenance-schedules/${scheduleId}`);

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
              scheduleData.startTime = startTimeFormatted;
              scheduleData.endTime = endTimeFormatted;
            }
          } else {
            scheduleData.startTime = startTimeFormatted;
            scheduleData.endTime = endTimeFormatted;
          }
        }
      }

      console.log('Final scheduleData before API call:', JSON.stringify(scheduleData, null, 2));
      
      const response = await assetsApi.updateMaintenanceSchedule(scheduleId, scheduleData);
      console.log('API Response:', response);
      
      showNotification('success', 'Thành công', 'Đã cập nhật lịch bảo trì thành công');
      onSuccess();
    } catch (error) {
      const errorData = error.response?.data;
      let errorMessage = 'Có lỗi xảy ra khi cập nhật lịch bảo trì. Vui lòng thử lại!';
      
      if (errorData?.message) {
        errorMessage = errorData.message;
      } else if (errorData?.error && errorData?.message) {
        errorMessage = errorData.message;
      } else if (errorData?.errors) {
        const errorMessages = [];
        if (errorData.errors['$.assetId']) {
          errorMessages.push('Tài sản không hợp lệ');
        }
        if (errorData.errors['updateDto']) {
          errorMessages.push(...errorData.errors['updateDto']);
        }
        if (errorMessages.length > 0) {
          errorMessage = errorMessages.join('. ');
        }
      } else if (errorData?.title) {
        errorMessage = errorData.title;
      }
      
      setApiError(errorMessage);
      showNotification('error', 'Lỗi', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setErrors({});
    setApiError('');
    onHide();
  };

  if (!schedule) {
    return null;
  }

  return (
    <Modal show={show} onHide={handleClose} size="lg" centered backdrop="static">
      <Modal.Header closeButton>
        <Modal.Title>
          <i className="fas fa-edit me-2"></i>
          Cập nhật lịch bảo trì
        </Modal.Title>
      </Modal.Header>
      
      <Form onSubmit={handleSubmit}>
        <Modal.Body>
          {apiError && (
            <Alert variant="danger" className="mb-3" dismissible onClose={() => setApiError('')}>
              {apiError}
            </Alert>
          )}

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
                  onBlur={() => setTimeout(() => setShowAssetSuggestions(false), 200)}
                  isInvalid={!!errors.assetId}
                  autoComplete="off"
                />
                <Form.Control.Feedback type="invalid">
                  {errors.assetId}
                </Form.Control.Feedback>
                
                {showAssetSuggestions && (
                  <div 
                    className="asset-suggestions-dropdown"
                    style={{
                      position: 'absolute',
                      top: '100%',
                      left: 0,
                      right: 0,
                      zIndex: 1000,
                      background: '#fff',
                      border: '1px solid #dee2e6',
                      borderRadius: '0 0 8px 8px',
                      maxHeight: 240,
                      overflowY: 'auto',
                      boxShadow: '0 6px 12px rgba(0,0,0,0.15)'
                    }}
                  >
                    {filteredAssetsForSearch.length === 0 ? (
                      <div className="p-2 text-muted text-center">
                        Không tìm thấy tài sản phù hợp
                      </div>
                    ) : (
                      filteredAssetsForSearch.map(asset => (
                        <div
                          key={asset.assetId}
                          onMouseDown={() => handleAssetSelect(asset)}
                          style={{
                            padding: '8px 12px',
                            cursor: 'pointer',
                            borderBottom: '1px solid #f1f1f1'
                          }}
                          onMouseEnter={(e) => {
                            e.currentTarget.style.backgroundColor = '#f5f5f5';
                          }}
                          onMouseLeave={(e) => {
                            e.currentTarget.style.backgroundColor = '#fff';
                          }}
                        >
                          <div style={{ fontWeight: 600 }}>
                            {asset.name}
                          </div>
                        </div>
                      ))
                    )}
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
                  min={formData.status === 'SCHEDULED' ? dayjs().format('YYYY-MM-DD') : undefined}
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
            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>
                  Trạng thái <span className="text-danger">*</span>
                </Form.Label>
                <Form.Select
                  name="status"
                  value={formData.status}
                  onChange={handleChange}
                  isInvalid={!!errors.status}
                >
                  <option value={formData.status}>
                    {formData.status === 'SCHEDULED' ? 'Đã lên lịch' : 
                     formData.status === 'IN_PROGRESS' ? 'Đang bảo trì' : 
                     formData.status === 'DONE' ? 'Đã hoàn thành' : 
                     formData.status === 'CANCELLED' ? 'Hủy' : formData.status}
                  </option>
                  {/* SCHEDULED -> CANCELLED only (removed IN_PROGRESS) */}
                  {formData.status === 'SCHEDULED' && (
                    <>
                      <option value="CANCELLED">Hủy</option>
                    </>
                  )}
                  {/* IN_PROGRESS -> DONE, CANCELLED (removed SCHEDULED) */}
                  {formData.status === 'IN_PROGRESS' && (
                    <>
                      <option value="DONE">Đã hoàn thành</option>
                      <option value="CANCELLED">Hủy</option>
                    </>
                  )}
                  {/* CANCELLED -> SCHEDULED */}
                  {formData.status === 'CANCELLED' && (
                    <option value="SCHEDULED">Đã lên lịch</option>
                  )}
                  {/* DONE -> không thể đổi */}
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
            {loading ? 'Đang cập nhật...' : 'Cập nhật'}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}

