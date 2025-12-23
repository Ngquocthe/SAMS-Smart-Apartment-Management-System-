import React, { useState, useEffect } from 'react';
import { Container } from 'react-bootstrap';
import { useLocation } from 'react-router-dom';
import BookingHistoryModal from './BookingHistoryModal';

/**
 * Wrapper component để hiển thị BookingHistoryModal khi vào từ menu "Lịch sử đăng ký"
 * Thay thế cho component cũ để tránh trùng lặp code với BookingHistoryModal
 */
export default function MyAmenityBookings() {
  const location = useLocation();
  const [showModal, setShowModal] = useState(true);
  const [bookingId, setBookingId] = useState(null);

  useEffect(() => {
    // Lấy bookingId từ location state nếu có (từ notification)
    if (location.state?.bookingId) {
      setBookingId(location.state.bookingId);
      // Clear state để không mở lại khi refresh
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  return (
    <Container fluid className="p-4">
      <BookingHistoryModal
        show={showModal}
        onHide={() => setShowModal(false)}
        initialBookingId={bookingId}
      />
    </Container>
  );
}
