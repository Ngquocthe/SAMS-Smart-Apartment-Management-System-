import React, { useRef } from 'react';
import Webcam from 'react-webcam';
import { Modal, Form, Button, Card, Row, Col, Alert, Spinner } from 'react-bootstrap';

export default function ModalAmenityBooking({
  show,
  onHide,
  selectedAmenity,
  packages = [],
  bookingForm,
  formErrors,
  calculatedPrice,
  priceBreakdown,
  isSubmitting,
  onFormChange,
  onSubmit,
  formatPrice,
  calculatedDates,
  facePreviewUrl,
  faceImageError,
  isFaceWebcamOpen,
  onToggleFaceWebcam,
  onFaceCameraCapture,
  onFaceImageChange,
  onClearFaceImage,
  hasFaceRegistered = false
}) {
  // Format ngày theo định dạng DD/MM/YYYY
  const formatDate = (date) => {
    if (!date) return '';
    const d = new Date(date);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return `${day}/${month}/${year}`;
  };

  const webcamRef = useRef(null);

  const handleCaptureClick = () => {
    const screenshot = webcamRef.current?.getScreenshot();
    if (onFaceCameraCapture) {
      onFaceCameraCapture(screenshot);
    }
  };

  return (
    <Modal
      show={show}
      onHide={onHide}
      size="lg"
      centered
      backdrop="static"
    >
      <Modal.Header closeButton>
        <Modal.Title>
          Đăng ký tiện ích: {selectedAmenity?.name}
        </Modal.Title>
      </Modal.Header>

      <Modal.Body>
        {/* Thông tin tiện ích */}
        <Card className="mb-3 bg-light">
          <Card.Body>
            <Row>
              <Col md={6}>
                <p className="mb-2">
                  <strong>Vị trí:</strong> {selectedAmenity?.location || '—'}
                </p>
                {selectedAmenity?.categoryName && (
                  <p className="mb-2">
                    <strong>Danh mục:</strong> {selectedAmenity.categoryName}
                  </p>
                )}
              </Col>
              <Col md={6}>
                <p className="mb-2">
                  <strong>Loại phí:</strong>{' '}
                  <span className={selectedAmenity?.feeType === 'Free' ? 'text-success' : 'text-primary'}>
                    {selectedAmenity?.feeType === 'Free' ? 'Miễn phí' : 'Có phí'}
                  </span>
                </p>
                <p className="mb-0">
                  <strong>Trạng thái:</strong>{' '}
                  <span className={selectedAmenity?.status === 'ACTIVE' ? 'text-success' : 'text-warning'}>
                    {selectedAmenity?.status === 'ACTIVE' ? 'Hoạt động' : selectedAmenity?.status || '—'}
                  </span>
                </p>
              </Col>
            </Row>
          </Card.Body>
        </Card>

        <Form>
          {/* Chọn gói */}
          {packages.length > 0 && (
            <Form.Group className="mb-3">
              <Form.Label>
                Chọn gói <span className="text-danger">*</span>
              </Form.Label>
              <Form.Select
                name="packageId"
                value={bookingForm.packageId || ''}
                onChange={onFormChange}
                isInvalid={!!formErrors.packageId}
              >
                <option value="">-- Chọn gói --</option>
                {packages.map((pkg) => {
                  return (
                    <option key={pkg.packageId} value={pkg.packageId}>
                      {pkg.name}
                    </option>
                  );
                })}
              </Form.Select>
              <Form.Control.Feedback type="invalid">
                {formErrors.packageId}
              </Form.Control.Feedback>
              {packages.length === 0 && (
                <Form.Text className="text-muted">
                  Tiện ích này chưa có gói nào được thiết lập.
                </Form.Text>
              )}
            </Form.Group>
          )}

          {selectedAmenity?.requiresFaceVerification && (
            <Card className="mb-3 border-info">
              <Card.Body>
                <Card.Title>Đăng ký khuôn mặt</Card.Title>
                {hasFaceRegistered ? (
                  <Alert variant="success" className="mb-2">
                    Cư dân đã đăng ký khuôn mặt. Bạn có thể bỏ qua hoặc cập nhật lại khuôn mặt mới.
                  </Alert>
                ) : (
                  <p className="text-muted small mb-2">
                    Tiện ích này yêu cầu xác thực khuôn mặt. Ảnh sẽ được dùng để check-in tại lễ tân.
                  </p>
                )}
                <div className="d-flex gap-2 mb-2">
                  <Button variant="outline-primary" onClick={onToggleFaceWebcam}>
                    {isFaceWebcamOpen ? "Đóng webcam" : "Mở webcam"}
                  </Button>
                  <Button variant="outline-secondary" onClick={onClearFaceImage}>
                    Xóa ảnh
                  </Button>
                </div>
                {isFaceWebcamOpen && (
                  <div className="mb-3">
                    <Webcam
                      ref={webcamRef}
                      audio={false}
                      mirrored
                      screenshotFormat="image/jpeg"
                      videoConstraints={{
                        facingMode: "user",
                      }}
                      className="w-100 rounded"
                    />
                    <Button
                      variant="primary"
                      className="mt-2 w-100"
                      onClick={handleCaptureClick}
                    >
                      Chụp ảnh
                    </Button>
                  </div>
                )}
                <Form.Group className="mb-2">
                  <Form.Label>Hoặc tải ảnh từ máy</Form.Label>
                  <Form.Control
                    type="file"
                    accept="image/*"
                    onChange={onFaceImageChange}
                  />
                  <Form.Text className="text-muted">
                    Hỗ trợ tối đa 5MB, định dạng ảnh.
                  </Form.Text>
                </Form.Group>
                {facePreviewUrl && (
                  <div className="mb-2">
                    <img
                      src={facePreviewUrl}
                      alt="Face preview"
                      className="img-fluid rounded"
                      style={{ maxHeight: 220 }}
                    />
                  </div>
                )}
                {!facePreviewUrl && (
                  <Form.Text className="text-muted">
                    Ảnh này giúp lễ tân kiểm tra khi cư dân đến tiện ích.
                  </Form.Text>
                )}
                {faceImageError && (
                  <Alert variant="danger" className="mt-2">
                    {faceImageError}
                  </Alert>
                )}
              </Card.Body>
            </Card>
          )}

          {/* Hiển thị ngày bắt đầu và kết thúc sau khi chọn gói */}
          {bookingForm.packageId && calculatedDates.startDate && calculatedDates.endDate && (
            <Row className="mb-3">
              <Col md={6}>
                <Form.Group>
                  <Form.Label>
                    Ngày bắt đầu :
                  </Form.Label>
                  <Form.Control
                    type="text"
                    value={formatDate(calculatedDates.startDate)}
                    readOnly
                    className="bg-light"
                  />
                </Form.Group>
              </Col>
              <Col md={6}>
                <Form.Group>
                  <Form.Label>
                    Ngày kết thúc :
                  </Form.Label>
                  <Form.Control
                    type="text"
                    value={formatDate(calculatedDates.endDate)}
                    readOnly
                    className="bg-light"
                  />
                </Form.Group>
              </Col>
            </Row>
          )}

          {/* Ghi chú */}
          <Form.Group className="mb-3">
            <Form.Label>Ghi chú</Form.Label>
            <Form.Control
              as="textarea"
              rows={3}
              name="notes"
              value={bookingForm.notes}
              onChange={onFormChange}
              placeholder="Nhập ghi chú (nếu có)..."
              maxLength={1000}
              isInvalid={!!formErrors.notes}
            />
            <div className="d-flex justify-content-between">
              <Form.Control.Feedback type="invalid">
                {formErrors.notes}
              </Form.Control.Feedback>
              <Form.Text className="text-muted">
                {bookingForm.notes?.length || 0}/1000 ký tự
              </Form.Text>
            </div>
          </Form.Group>

          {/* Hiển thị giá nếu đã tính */}
          {calculatedPrice !== null && calculatedPrice > 0 && (
            <Alert variant="info" className="mb-3">
              <div className="d-flex justify-content-between align-items-center">
                <div>
                  <strong>Tổng chi phí:</strong>
                </div>
                <div className="text-end">
                  <h4 className="mb-0 text-primary">
                    {formatPrice(calculatedPrice)} VNĐ
                  </h4>
                </div>
              </div>
            </Alert>
          )}
        </Form>
      </Modal.Body>

      <Modal.Footer>
        <Button
          variant="secondary"
          onClick={onHide}
          disabled={isSubmitting}
        >
          Hủy
        </Button>
        <Button
          variant="success"
          onClick={onSubmit}
          disabled={isSubmitting || !bookingForm.packageId}
        >
          {isSubmitting ? (
            <>
              <Spinner animation="border" size="sm" className="me-2" />
              Đang xử lý...
            </>
          ) : (
            <>
              Xác nhận đăng ký
            </>
          )}
        </Button>
      </Modal.Footer>
    </Modal>
  );
}
