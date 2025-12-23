import React, { useEffect, useMemo, useState } from 'react';
import { Container, Row, Col, Card, Table, Spinner, Alert, Form, Pagination, Button, Badge } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';
import { amenityBookingApi } from '../../../features/amenity-booking/amenityBookingApi';

export default function BookingHistoryPage() {
  const navigate = useNavigate();
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
  const [sortBy] = useState('CreatedAt');
  const [sortOrder] = useState('desc');

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalCount / pageSize)), [totalCount, pageSize]);

  // Tính toán items cho trang hiện tại
  const paginatedItems = useMemo(() => {
    const startIndex = (pageNumber - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    return items.slice(startIndex, endIndex);
  }, [items, pageNumber, pageSize]);

  // Reset về trang 1 khi filter thay đổi
  useEffect(() => {
    setPageNumber(1);
  }, [statusFilter, paymentFilter, fromDate, toDate, search]);

  // Fetch data khi filter hoặc pageNumber thay đổi
  useEffect(() => {
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, pageSize, statusFilter, paymentFilter, fromDate, toDate, sortBy, sortOrder, search]);

  const fetchData = async () => {
    try {
      setLoading(true);
      setError('');

      // Lấy tất cả dữ liệu: gọi API với pageSize lớn để lấy tất cả
      const query = { pageNumber: 1, pageSize: 10000 };
      if (statusFilter !== 'all') query.status = statusFilter;
      if (paymentFilter !== 'all') query.paymentStatus = paymentFilter;
      if (fromDate) query.fromDate = new Date(fromDate + 'T00:00:00').toISOString();
      if (toDate) query.toDate = new Date(toDate + 'T23:59:59').toISOString();
      if (sortBy) query.sortBy = sortBy;
      if (sortOrder) query.sortOrder = sortOrder;
      const data = await amenityBookingApi.getAll(query);

      const coerceItems = (payload) => {
        if (!payload) return [];
        if (Array.isArray(payload)) return payload;
        return (
          payload.items || payload.data || payload.results || payload.result || payload.records || payload.content || []
        );
      };

      const s = (search || '').toLowerCase();
      const list = coerceItems(data).filter((it) => {
        const amenity = (it.amenityName || '').toLowerCase();
        const apt = (it.apartmentCode || it.apartmentNumber || '').toLowerCase();
        const resident = (it.residentName || it.userName || '').toLowerCase();
        const matchSearch = !s || amenity.includes(s) || apt.includes(s) || resident.includes(s);
        const matchStatus = statusFilter === 'all' || it.status === statusFilter;
        const matchPayment = paymentFilter === 'all' || it.paymentStatus === paymentFilter;
        return matchSearch && matchStatus && matchPayment;
      });

      setItems(list);
      // Set totalCount: ưu tiên từ API, nếu không có thì dùng list.length sau khi filter
      const apiTotalCount = data?.totalCount ?? data?.total ?? data?.count ?? data?.totalRecords ?? null;
      if (apiTotalCount !== null && !search) {
        // Nếu có totalCount từ API và không có search filter, dùng totalCount từ API
        setTotalCount(Number(apiTotalCount));
      } else {
        // Nếu có search filter hoặc không có totalCount từ API, dùng list.length
        setTotalCount(list.length);
      }
    } catch (err) {
      setError('Không thể tải lịch sử đăng kí');
    } finally {
      setLoading(false);
    }
  };

  const formatDateTime = (s) => {
    if (!s) return '—';
    // Nếu là date string (YYYY-MM-DD), format thành DD/MM/YYYY
    if (typeof s === 'string' && s.match(/^\d{4}-\d{2}-\d{2}$/)) {
      const date = new Date(s + 'T00:00:00');
      return date.toLocaleDateString('vi-VN');
    }
    // Nếu là datetime string, format đầy đủ
    return new Date(s).toLocaleString('vi-VN');
  };

  // Get status badge (giống như role cư dân)
  const getStatusBadge = (status) => {
    const statusConfig = {
      'Pending': { variant: 'warning', label: 'Chờ duyệt' },
      'Confirmed': { variant: 'info', label: 'Đã xác nhận' },
      'Completed': { variant: 'success', label: 'Hoàn tất' },
      'Cancelled': { variant: 'danger', label: 'Đã hủy' },
      'Rejected': { variant: 'secondary', label: 'Bị từ chối' }
    };
    const config = statusConfig[status] || { variant: 'secondary', label: status };
    return (
      <Badge bg={config.variant}>
        {config.label}
      </Badge>
    );
  };

  // Get payment status badge (giống như role cư dân)
  const getPaymentStatusBadge = (status) => {
    const statusConfig = {
      'Unpaid': { variant: 'warning', label: 'Chưa thanh toán' },
      'Paid': { variant: 'success', label: 'Đã thanh toán' },
      'Refunded': { variant: 'info', label: 'Đã hoàn tiền' },
      'Overdue': { variant: 'danger', label: 'Quá hạn' }
    };
    const config = statusConfig[status] || { variant: 'secondary', label: status };
    return <Badge bg={config.variant}>{config.label}</Badge>;
  };


  return (
    <Container fluid className="p-4">
      {/* Header */}
      <Row className="mb-4">
        <Col>
          <div className="d-flex justify-content-between align-items-center">
            <div>
              <h2 className="mb-2">
                <i className="fas fa-history me-2"></i>
                Lịch sử đăng kí tiện ích
              </h2>
              <p className="text-muted">Quản lý và theo dõi lịch sử đăng ký tiện ích của cư dân</p>
            </div>
            <Button 
              variant="outline-secondary"
              onClick={() => navigate(-1)}
            >
              <i className="fas fa-arrow-left me-2"></i>
              Quay lại
            </Button>
          </div>
        </Col>
      </Row>

      {/* Filters */}
      <Card className="mb-4">
        <Card.Body>
          <Row className="g-2 align-items-end">
            <Col xs={12} md={4}>
              <Form.Label className="small fw-semibold mb-1">Tìm kiếm</Form.Label>
              <Form.Control
                size="sm"
                placeholder="Tìm theo tên, số căn hộ, tiện ích..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </Col>
            <Col xs={6} md={2}>
              <Form.Label className="small fw-semibold mb-1">Trạng thái</Form.Label>
              <Form.Select size="sm" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="all">Tất cả trạng thái</option>
                <option value="Pending">Chờ duyệt</option>
                <option value="Confirmed">Đã xác nhận</option>
                <option value="Completed">Hoàn tất</option>
                <option value="Cancelled">Đã hủy</option>
              </Form.Select>
            </Col>
            <Col xs={6} md={2}>
              <Form.Label className="small fw-semibold mb-1">Thanh toán</Form.Label>
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
              <Form.Control size="sm" type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
            </Col>
            <Col xs={6} md={2}>
              <Form.Label className="small fw-semibold mb-1">Ngày kết thúc</Form.Label>
              <Form.Control size="sm" type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
            </Col>
          </Row>
        </Card.Body>
      </Card>

      {/* Table */}
      <Card>
        <Card.Body>
          {loading ? (
            <div className="text-center py-5">
              <Spinner animation="border" />
            </div>
          ) : error ? (
            <Alert variant="danger">{error}</Alert>
          ) : items.length === 0 ? (
            <div className="text-center text-muted py-5">Chưa có lịch sử đăng kí</div>
          ) : (
            <>
              <div className="table-responsive">
                <Table striped hover className="align-middle">
                  <thead>
                    <tr>
                      <th>Người đặt</th>
                      <th>Số căn hộ</th>
                      <th>Tiện ích</th>
                      <th>Thời gian</th>
                      <th>Gói tháng</th>
                      <th>Tổng tiền</th>
                      <th>Trạng thái</th>
                      <th>Thanh toán</th>
                    </tr>
                  </thead>
                  <tbody>
                    {paginatedItems.map((it) => (
                      <tr key={it.bookingId || `${it.amenityId}-${it.startDate || it.startTime}`}>
                        <td>{it.residentName || it.userName || '—'}</td>
                        <td>{it.apartmentCode || it.apartmentNumber || '—'}</td>
                        <td><div className="fw-semibold">{it.amenityName || '—'}</div></td>
                        <td>
                          <div className="small text-muted">Bắt đầu: {formatDateTime(it.startDate || it.startTime)}</div>
                          <div className="small text-muted">Kết thúc: {formatDateTime(it.endDate || it.endTime)}</div>
                        </td>
                        <td>{it.packageName || '—'}</td>
                        <td>{(it.totalPrice || 0).toLocaleString('vi-VN')} VNĐ</td>
                        <td>
                          {getStatusBadge(it.status)}
                        </td>
                        <td>
                          {getPaymentStatusBadge(it.paymentStatus)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </Table>
              </div>

              {/* Pagination */}
              <Row className="mt-3">
                <Col md={6}>
                  <div className="text-muted small">
                    Tổng: {totalCount} bản ghi
                  </div>
                </Col>
                <Col md={6} className="d-flex justify-content-end">
                  {totalPages > 1 ? (
                    <Pagination className="mb-0">
                      <Pagination.First
                        onClick={() => setPageNumber(1)}
                        disabled={pageNumber === 1}
                      />
                      <Pagination.Prev 
                        disabled={pageNumber === 1} 
                        onClick={() => setPageNumber(Math.max(1, pageNumber - 1))} 
                      />
                      {[...Array(totalPages)].map((_, index) => {
                        const pageNum = index + 1;
                        if (
                          pageNum === 1 ||
                          pageNum === totalPages ||
                          (pageNum >= pageNumber - 1 && pageNum <= pageNumber + 1)
                        ) {
                          return (
                            <Pagination.Item
                              key={pageNum}
                              active={pageNum === pageNumber}
                              onClick={() => setPageNumber(pageNum)}
                            >
                              {pageNum}
                            </Pagination.Item>
                          );
                        } else if (
                          pageNum === pageNumber - 2 ||
                          pageNum === pageNumber + 2
                        ) {
                          return <Pagination.Ellipsis key={pageNum} disabled />;
                        }
                        return null;
                      })}
                      <Pagination.Next 
                        disabled={pageNumber === totalPages} 
                        onClick={() => setPageNumber(Math.min(totalPages, pageNumber + 1))} 
                      />
                      <Pagination.Last
                        onClick={() => setPageNumber(totalPages)}
                        disabled={pageNumber === totalPages}
                      />
                    </Pagination>
                  ) : (
                    <div className="text-muted small">
                      Trang {pageNumber} / {totalPages}
                    </div>
                  )}
                </Col>
              </Row>
            </>
          )}
        </Card.Body>
      </Card>
    </Container>
  );
}

