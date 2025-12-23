import React, { useEffect, useMemo, useState } from 'react';
import { Modal, Table, Spinner, Alert, Row, Col, Form, Pagination, Badge } from 'react-bootstrap';
import { amenityBookingApi } from '../../features/amenity-booking/amenityBookingApi';

export default function BookingHistoryModal({ show, onHide, initialBookingId = null }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [statusFilter, setStatusFilter] = useState('all');
  const [paymentFilter, setPaymentFilter] = useState('all');
  const [search, setSearch] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [highlightId, setHighlightId] = useState(null);

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalCount / pageSize)), [totalCount, pageSize]);

  // Tính toán items cho trang hiện tại
  const paginatedItems = useMemo(() => {
    const startIndex = (pageNumber - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    return items.slice(startIndex, endIndex);
  }, [items, pageNumber, pageSize]);

  // Reset về trang 1 khi filter thay đổi
  useEffect(() => {
    if (!show) return;
    setPageNumber(1);
  }, [show, statusFilter, paymentFilter, search, fromDate, toDate]);

  // Fetch data khi show hoặc filter thay đổi
  useEffect(() => {
    if (!show) return;
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [show, statusFilter, paymentFilter, search, fromDate, toDate]);

  useEffect(() => {
    if (show && initialBookingId) {
      setHighlightId(initialBookingId);
    } else if (!show) {
      setHighlightId(null);
    }
  }, [show, initialBookingId]);

  const fetchData = async () => {
    try {
      setLoading(true);
      setError('');

      // Cư dân: chỉ lấy lịch sử của chính mình
      const data = await amenityBookingApi.getMyBookings();
      // Chuẩn hoá: chấp nhận nhiều định dạng trả về từ BE
      const coerceItems = (payload) => {
        if (!payload) return [];
        if (Array.isArray(payload)) return payload;
        return (
          payload.items ||
          payload.data ||
          payload.results ||
          payload.result ||
          payload.records ||
          payload.content ||
          []
        );
      };

      let list = coerceItems(data);
      // Sắp xếp mới nhất lên đầu (ưu tiên createdAt, sau đó startTime)
      const getComparableDate = (it) => {
        const created = it.createdAt || it.createdDate || it.createdOn || it.createdTime;
        const primary = created || it.startTime || it.endTime;
        return primary ? new Date(primary).getTime() : 0;
      };
      list = [...list].sort((a, b) => getComparableDate(b) - getComparableDate(a));

      // Filter client-side theo search
      const s = (search || '').toLowerCase();
      const matchSearch = (it) => {
        if (!s) return true;
        const amenity = (it.amenityName || '').toLowerCase();
        const apt = (it.apartmentCode || it.apartmentNumber || '').toLowerCase();
        const resident = (it.residentName || it.userName || '').toLowerCase();
        return amenity.includes(s) || apt.includes(s) || resident.includes(s);
      };
      list = list.filter((it) => {
        const matchStatus = statusFilter === 'all' || it.status === statusFilter;
        const matchPayment = paymentFilter === 'all' || it.paymentStatus === paymentFilter;
        // Lọc theo khoảng ngày: so sánh theo startTime
        const start = it.startDate ? new Date(it.startDate + 'T00:00:00') : (it.startTime ? new Date(it.startTime) : null);
        const inFrom = !fromDate || (start && start >= new Date(fromDate + 'T00:00:00'));
        const inTo = !toDate || (start && start <= new Date(toDate + 'T23:59:59'));
        return matchStatus && matchPayment && matchSearch(it) && inFrom && inTo;
      });
      setItems(Array.isArray(list) ? list : []);
      const total = list.length;
      setTotalCount(Number(total) || 0);
    } catch (err) {
      setError('Không thể tải lịch sử đăng kí');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!show || !highlightId) return;
    const timer = setTimeout(() => {
      const target = document.querySelector(`[data-booking-row="${highlightId}"]`);
      if (target) {
        target.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    }, 300);
    return () => clearTimeout(timer);
  }, [show, highlightId, items]);

  const formatDateTime = (s) => {
    if (!s) return '—';
    // Nếu là date string (YYYY-MM-DD), chỉ hiển thị ngày
    if (s.match(/^\d{4}-\d{2}-\d{2}$/)) {
      return new Date(s + 'T00:00:00').toLocaleDateString('vi-VN');
    }
    return new Date(s).toLocaleString('vi-VN');
  };
  const formatMoney = (v) => (v || v === 0 ? Number(v).toLocaleString('vi-VN') : '—');

  const renderStatusBadge = (status) => {
    const map = {
      Pending: { variant: 'secondary', label: 'Chờ duyệt' },
      Confirmed: { variant: 'info', label: 'Đã xác nhận' },
      Completed: { variant: 'success', label: 'Hoàn tất' },
      Cancelled: { variant: 'danger', label: 'Đã hủy' }
    };
    const v = map[status] || { variant: 'light', label: status || '—' };
    return <Badge bg={v.variant}>{v.label}</Badge>;
  };

  const renderPaymentBadge = (paymentStatus) => {
    const map = {
      Unpaid: { variant: 'warning', label: 'Chưa thanh toán' },
      Paid: { variant: 'success', label: 'Đã thanh toán' },
      Refunded: { variant: 'secondary', label: 'Hoàn tiền' },
      Overdue: { variant: 'danger', label: 'Quá hạn' }
    };
    const v = map[paymentStatus] || { variant: 'light', label: paymentStatus || '—' };
    return <Badge bg={v.variant}>{v.label}</Badge>;
  };

  return (
    <Modal show={show} onHide={onHide} size="xl" centered scrollable>
      <Modal.Header closeButton>
        <Modal.Title>
          Lịch sử đăng kí tiện ích
        </Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <Row className="g-2 align-items-end mb-3">
          <Col xs={12} md={4}>
            <Form.Control
              size="sm"
              placeholder="Tìm theo tên, số căn hộ, tiện ích..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </Col>
          <Col xs={6} md={2}>
            <Form.Select size="sm" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="all">Tất cả trạng thái</option>
              <option value="Pending">Chờ duyệt</option>
              <option value="Confirmed">Đã xác nhận</option>
              <option value="Completed">Hoàn tất</option>
              <option value="Cancelled">Đã hủy</option>
            </Form.Select>
          </Col>
          <Col xs={6} md={2}>
            <Form.Select size="sm" value={paymentFilter} onChange={(e) => setPaymentFilter(e.target.value)}>
              <option value="all">Tất cả thanh toán</option>
              <option value="Paid">Đã thanh toán</option>
              <option value="Unpaid">Chưa thanh toán</option>
              <option value="Overdue">Quá hạn</option>
              <option value="Refunded">Hoàn tiền</option>
            </Form.Select>
          </Col>
          <Col xs={6} md={2}>
            <Form.Label className="small fw-semibold mb-1">Ngày bắt đầu</Form.Label>
            <Form.Control
              size="sm"
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              placeholder="Từ ngày"
            />
          </Col>
          <Col xs={6} md={2}>
            <Form.Label className="small fw-semibold mb-1">Ngày kết thúc</Form.Label>
            <Form.Control
              size="sm"
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              placeholder="Đến ngày"
            />
          </Col>
        </Row>

        {loading ? (
          <div className="text-center py-5">
            <Spinner animation="border" />
          </div>
        ) : error ? (
          <Alert variant="danger">{error}</Alert>
        ) : items.length === 0 ? (
          <div className="text-center text-muted py-5">Chưa có lịch sử đăng kí</div>
        ) : (
          <div className="table-responsive">
            <Table striped hover className="align-middle">
              <thead>
                <tr>
                  <th>Tiện ích</th>
                  <th>Bắt đầu</th>
                  <th>Kết thúc</th>
                  <th>Loại</th>
                  <th>Tổng tiền</th>
                  <th>Trạng thái</th>
                  <th>Thanh toán</th>
                </tr>
              </thead>
              <tbody>
                {paginatedItems.map((it) => {
                  const rowId = it.bookingId || it.amenityBookingId || `${it.amenityId}-${it.startDate}`;
                  const isHighlighted = highlightId && rowId === highlightId;
                  return (
                  <tr
                    key={rowId}
                    data-booking-row={rowId}
                    className={isHighlighted ? 'table-info' : ''}
                  > 
                    <td><div className="fw-semibold">{it.amenityName || '—'}</div></td>
                    <td>{formatDateTime(it.startDate || it.startTime)}</td>
                    <td>{formatDateTime(it.endDate || it.endTime)}</td>
                    <td>{it.packageName || (it.packageId ? 'Gói tháng' : '—')}</td>
                    <td>{formatMoney(it.totalPrice)}{it.totalPrice || it.totalPrice === 0 ? ' VNĐ' : ''}</td>
                    <td>
                      {renderStatusBadge(it.status)}
                    </td>
                    <td>
                      {renderPaymentBadge(it.paymentStatus)}
                    </td>
                  </tr>
                )})}
              </tbody>
            </Table>
          </div>
        )}
      </Modal.Body>
      <Modal.Footer className="justify-content-between">
        <div className="text-muted small">
          Tổng: {totalCount} bản ghi
        </div>
        <div>
          <Pagination className="mb-0">
            <Pagination.Prev disabled={pageNumber === 1} onClick={() => setPageNumber(Math.max(1, pageNumber - 1))} />
            <Pagination.Item active>{pageNumber}</Pagination.Item>
            <Pagination.Next disabled={pageNumber === totalPages} onClick={() => setPageNumber(Math.min(totalPages, pageNumber + 1))} />
          </Pagination>
        </div>
      </Modal.Footer>
    </Modal>
  );
}


