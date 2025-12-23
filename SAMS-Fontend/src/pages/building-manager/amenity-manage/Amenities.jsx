import React, { useState, useEffect, useMemo } from 'react';
import { Container, Row, Col, Card, Table, Badge, Button, Form, Spinner, Alert, Modal } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';
import { amenitiesApi } from '../../../features/building-management/amenitiesApi';
import CreateAmenity from './CreateAmenity';
import UpdateAmenity from './UpdateAmenity';
import Toast from '../../../components/Toast';

export default function Amenities() {
  const navigate = useNavigate();

  // State quản lý dữ liệu tiện ích
  const [amenities, setAmenities] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // State quản lý tìm kiếm và lọc
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [feeTypeFilter, setFeeTypeFilter] = useState('all');

  // State quản lý phân trang
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 12;

  // State quản lý modal tạo tiện ích
  const [showCreateModal, setShowCreateModal] = useState(false);

  // State quản lý modal cập nhật tiện ích
  const [showUpdateModal, setShowUpdateModal] = useState(false);
  const [selectedAmenity, setSelectedAmenity] = useState(null);

  // State quản lý toast notification
  const [showToast, setShowToast] = useState(false);
  const [toastMessage, setToastMessage] = useState('');
  const [toastType, setToastType] = useState('success');

  // State quản lý modal xóa tiện ích
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [amenityToDelete, setAmenityToDelete] = useState(null);

  // State để track tiện ích mới được thêm (để đảm bảo nó ở đầu danh sách)
  // Sử dụng Array thay vì Set để giữ thứ tự insertion
  const [newlyAddedAmenityIds, setNewlyAddedAmenityIds] = useState(() => {
    try {
      const saved = localStorage.getItem('newlyAddedAmenityIds');
      return saved ? JSON.parse(saved) : [];
    } catch {
      return [];
    }
  });

  // Effect để lưu newlyAddedAmenityIds vào localStorage khi có thay đổi
  useEffect(() => {
    try {
      localStorage.setItem('newlyAddedAmenityIds', JSON.stringify(newlyAddedAmenityIds));
    } catch { }
  }, [newlyAddedAmenityIds]);

  // Helper function: Loại bỏ dấu tiếng Việt để search
  const removeVietnameseTones = (str) => {
    if (!str) return '';
    return str
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/đ/g, 'd')
      .replace(/Đ/g, 'D')
      .toLowerCase();
  };

  /**
   * Effect để tải danh sách tiện ích khi component mount
   */
  useEffect(() => {
    /**
     * Hàm bất đồng bộ để lấy dữ liệu tiện ích từ API
     */
    const fetchAmenities = async () => {
      try {
        setLoading(true); // Bắt đầu loading
        setError(''); // Xóa lỗi cũ

        const data = await amenitiesApi.getAll(); // Gọi API lấy danh sách tiện ích
        setAmenities(data); // Cập nhật state với dữ liệu mới
        setLoading(false);
      } catch (err) {
        setError('Không thể tải danh sách tiện ích. Vui lòng thử lại sau.');
        setLoading(false);
        setAmenities([]); // Reset danh sách về rỗng
      }
    };

    fetchAmenities();
  }, []);

  /**
   * Lọc và sắp xếp danh sách tiện ích một cách tối ưu
   */
  const filteredAmenities = useMemo(() => {
    const filtered = amenities.filter(amenity => {
      // Chỉ hiển thị amenities chưa bị xóa mềm
      if (amenity.isDelete === true) return false;

      // Search không dấu - "be" sẽ match với "bể bơi", "Bể", v.v.
      const normalizedName = removeVietnameseTones(amenity.name || '');
      const normalizedLocation = removeVietnameseTones(amenity.location || '');
      const normalizedId = removeVietnameseTones(amenity.id || '');
      const normalizedSearch = removeVietnameseTones(searchTerm);

      const matchesSearch = !searchTerm ||
        normalizedName.includes(normalizedSearch) ||
        normalizedLocation.includes(normalizedSearch) ||
        normalizedId.includes(normalizedSearch);

      // Kiểm tra lọc theo trạng thái
      const matchesStatus = statusFilter === 'all' || amenity.status === statusFilter;

      // Kiểm tra lọc theo danh mục
      const matchesCategory = categoryFilter === 'all' ||
        (amenity.categoryName && amenity.categoryName.toLowerCase() === categoryFilter.toLowerCase());

      // Kiểm tra lọc theo loại phí
      const matchesFeeType = feeTypeFilter === 'all' || amenity.feeType === feeTypeFilter;

      return matchesSearch && matchesStatus && matchesCategory && matchesFeeType;
    });

    // Chỉ sort một lần khi có thay đổi
    return filtered.sort((a, b) => {
      // Ưu tiên tiện ích mới được thêm lên đầu
      const aIsNew = newlyAddedAmenityIds.includes(a.amenityId);
      const bIsNew = newlyAddedAmenityIds.includes(b.amenityId);

      if (aIsNew && bIsNew) {
        const aIndex = newlyAddedAmenityIds.indexOf(a.amenityId);
        const bIndex = newlyAddedAmenityIds.indexOf(b.amenityId);
        return bIndex - aIndex; // Mới nhất trước
      }

      if (aIsNew && !bIsNew) return -1;
      if (!aIsNew && bIsNew) return 1;

      // Sắp xếp theo thứ tự mới nhất cho tiện ích cũ
      if (a.createdDate && b.createdDate) {
        return new Date(b.createdDate) - new Date(a.createdDate);
      }
      if (a.amenityId && b.amenityId) {
        return b.amenityId.localeCompare(a.amenityId);
      }
      if (a.id && b.id) {
        return b.id - a.id;
      }
      return 0;
    });
  }, [amenities, searchTerm, statusFilter, categoryFilter, feeTypeFilter, newlyAddedAmenityIds]);

  // Lấy danh sách các category duy nhất từ dữ liệu
  const uniqueCategories = [...new Set(amenities
    .map(amenity => amenity.categoryName)
    .filter(category => category && category.trim() !== '')
  )].sort();

  // Logic phân trang
  const totalPages = Math.ceil(filteredAmenities.length / itemsPerPage); // Tính tổng số trang
  const startIndex = (currentPage - 1) * itemsPerPage; // Index bắt đầu của trang hiện tại
  const endIndex = startIndex + itemsPerPage; // Index kết thúc của trang hiện tại
  const currentAmenities = filteredAmenities.slice(startIndex, endIndex); // Lấy tiện ích của trang hiện tại

  /**
   * Effect để reset về trang 1 khi thay đổi bộ lọc hoặc tìm kiếm
   */
  useEffect(() => {
    setCurrentPage(1); // Về trang đầu tiên
  }, [searchTerm, statusFilter, categoryFilter, feeTypeFilter]); // Chạy khi searchTerm, statusFilter, categoryFilter hoặc feeTypeFilter thay đổi

  /**
   * Tính toán thống kê tiện ích
   */
  const stats = {
    total: amenities.length, // Tổng số tiện ích
    active: amenities.filter(a => a.status === 'ACTIVE').length, // Số tiện ích đang hoạt động
    inactive: amenities.filter(a => a.status === 'INACTIVE').length, // Số tiện ích ko hoạt động
    maintenance: amenities.filter(a => a.status === 'MAINTENANCE').length, // Số tiện ích đang bảo trì
    withFee: amenities.filter(a => a.feeType === 'Paid').length // Số tiện ích có phí
  };

  /**
   * Tạo badge hiển thị trạng thái tiện ích
   * @param {string} status - Trạng thái tiện ích (Active, Maintenance, Inactive)
   * @returns {JSX.Element} - Component Badge với màu sắc và text tương ứng
   */
  const getStatusBadge = (status) => {

    const statusConfig = {
      'ACTIVE': { variant: 'success', label: 'Hoạt động' },
      'MAINTENANCE': { variant: 'warning', label: 'Bảo trì' },
      'INACTIVE': { variant: 'secondary', label: 'Không hoạt động' }
    };
    const config = statusConfig[status] || statusConfig['INACTIVE']; // Mặc định là INACTIVE nếu không tìm thấy
    return <Badge bg={config.variant}>{config.label}</Badge>;
  };



  const handleCreateSuccess = async (newAmenityId) => {
    try {
      setLoading(true);
      const data = await amenitiesApi.getAll();

      // Tối ưu: Thêm tiện ích mới vào đầu danh sách thay vì sort
      if (newAmenityId) {
        const newAmenity = data.find(amenity => amenity.amenityId === newAmenityId);
        if (newAmenity) {
          // Thêm vào đầu danh sách
          setAmenities(prev => [newAmenity, ...prev.filter(a => a.amenityId !== newAmenityId)]);
          // Thêm vào danh sách tracking
          setNewlyAddedAmenityIds(prev => [...prev, newAmenityId]);
        } else {
          setAmenities(data);
        }
      } else {
        setAmenities(data);
      }

      setCurrentPage(1);
    } catch (err) {
      setError('Không thể tải lại danh sách tiện ích');
    } finally {
      setLoading(false);
    }
  };

  /**
   * Xử lý khi click nút edit
   */
  const handleEdit = (amenity) => {
    setSelectedAmenity(amenity);
    setShowUpdateModal(true);
  };

  /**
   * Xử lý khi click nút xóa
   */
  const handleDeleteClick = (amenity) => {
    setAmenityToDelete(amenity);
    setShowDeleteModal(true);
  };

  const handleConfirmDelete = async () => {
    if (!amenityToDelete) return;

    try {
      setLoading(true);
      setShowDeleteModal(false);

      await amenitiesApi.delete(amenityToDelete.amenityId);

      // Cập nhật danh sách amenities (ẩn amenity đã xóa)
      setAmenities(prev => prev.filter(a => a.amenityId !== amenityToDelete.amenityId));

      setToastMessage('Đã xóa tiện ích thành công');
      setToastType('success');
      setShowToast(true);
      setAmenityToDelete(null);
    } catch (error) {
      const errorMessage = error.response?.data?.message || 'Có lỗi xảy ra khi xóa tiện ích. Vui lòng thử lại!';
      setToastMessage(errorMessage);
      setToastType('error');
      setShowToast(true);
    } finally {
      setLoading(false);
    }
  };

  const handleCancelDelete = () => {
    setShowDeleteModal(false);
    setAmenityToDelete(null);
  };

  /**
   * Xử lý khi cập nhật tiện ích thành công
   */
  const handleUpdateSuccess = async () => {
    try {
      setLoading(true);
      const data = await amenitiesApi.getAll();
      setAmenities(data);
    } catch (err) {
      setError('Không thể tải lại danh sách tiện ích');
    } finally {
      setLoading(false);
    }
  };

  // Hiển thị loading spinner khi đang tải dữ liệu
  if (loading) {
    return (
      <Container className="d-flex justify-content-center align-items-center" style={{ height: '400px' }}>
        <Spinner animation="border" variant="primary" />
      </Container>
    );
  }

  // Hiển thị thông báo lỗi nếu có lỗi xảy ra
  if (error) {
    return (
      <Container>
        <Alert variant="danger">{error}</Alert>
      </Container>
    );
  }

  return (
    <Container fluid className="p-4">
      {/* Phần header với tiêu đề và mô tả */}
      <Row className="mb-4">
        <Col>
          <h2 className="mb-2">Quản lý tiện ích</h2>
        </Col>
      </Row>

      {/* Phần tìm kiếm, lọc và nút thêm mới */}
      <Row className="mb-4">
        <Col md={3}>
          {/* Ô tìm kiếm theo tên hoặc vị trí */}
          <Form.Control
            type="text"
            placeholder="Tìm kiếm tiện ích theo tên, vị trí..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </Col>
        <Col md={2}>
          {/* Dropdown lọc theo trạng thái */}
          <Form.Select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <option value="all">Tất cả trạng thái</option>
            <option value="ACTIVE">Hoạt động</option>
            <option value="MAINTENANCE">Bảo trì</option>
            <option value="INACTIVE">Không hoạt động</option>
          </Form.Select>
        </Col>
        <Col md={2}>
          {/* Dropdown lọc theo danh mục */}
          <Form.Select
            value={categoryFilter}
            onChange={(e) => setCategoryFilter(e.target.value)}
          >
            <option value="all">Tất cả danh mục</option>
            {uniqueCategories.map(category => (
              <option key={category} value={category}>{category}</option>
            ))}
          </Form.Select>
        </Col>
        <Col md={2}>
          {/* Dropdown lọc theo loại phí */}
          <Form.Select
            value={feeTypeFilter}
            onChange={(e) => setFeeTypeFilter(e.target.value)}
          >
            <option value="all">Tất cả loại phí</option>
            <option value="Paid">Có phí</option>
            <option value="Free">Miễn phí</option>
          </Form.Select>
        </Col>
        <Col md={3} className="text-end d-flex justify-content-end gap-2 align-items-center">
          <Button
            variant="info"
            onClick={() => navigate('/buildingmanagement/amenities/booking-history')}
            title="Lịch sử đăng kí"
            style={{ minWidth: '120px' }}
          >
            <i className="fas fa-history me-2"></i>
            Lịch sử
          </Button>
          {/* Nút thêm tiện ích mới */}
          <Button
            variant="success"
            onClick={() => { setShowCreateModal(true) }}
            title="Thêm tiện ích mới"
            style={{ minWidth: '120px' }}
          >
            Thêm mới
          </Button>
        </Col>
      </Row>

      {/* Các thẻ thống kê */}
      <Row className="mb-4 g-3">
        {/* Thẻ tổng số tiện ích */}
        <Col className="mb-3" style={{ flex: '1 1 0', minWidth: '0' }}>
          <Card className="h-100">
            <Card.Body>
              <div className="d-flex justify-content-between align-items-center">
                <div style={{ flex: '1', minWidth: '0' }}>
                  <h6 className="text-muted mb-1" style={{ fontSize: '0.875rem', whiteSpace: 'nowrap' }}>Tổng tiện ích</h6>
                  <h3 className="mb-0">{stats.total}</h3>
                </div>
              </div>
            </Card.Body>
          </Card>
        </Col>
        {/* Thẻ số tiện ích đang hoạt động */}
        <Col className="mb-3" style={{ flex: '1 1 0', minWidth: '0' }}>
          <Card className="h-100">
            <Card.Body>
              <div className="d-flex justify-content-between align-items-center">
                <div style={{ flex: '1', minWidth: '0' }}>
                  <h6 className="text-muted mb-1" style={{ fontSize: '0.875rem', whiteSpace: 'nowrap' }}>Đang hoạt động</h6>
                  <h3 className="mb-0 text-success">{stats.active}</h3>
                </div>
                <div className="bg-success bg-opacity-10 rounded p-2" style={{ flexShrink: 0 }}>
                  <div className="bg-success rounded-circle" style={{ width: '8px', height: '8px' }}></div>
                </div>
              </div>
            </Card.Body>
          </Card>
        </Col>
        <Col className="mb-3" style={{ flex: '1 1 0', minWidth: '0' }}>
          <Card className="h-100">
            <Card.Body>
              <div className="d-flex justify-content-between align-items-center">
                <div style={{ flex: '1', minWidth: '0' }}>
                  <h6 className="text-muted mb-1" style={{ fontSize: '0.875rem', whiteSpace: 'nowrap' }}>Không hoạt động</h6>
                  <h3 className="mb-0 text-secondary">{stats.inactive}</h3>
                </div>
                <div className="bg-secondary bg-opacity-10 rounded p-2" style={{ flexShrink: 0 }}>
                  <div className="bg-secondary rounded-circle" style={{ width: '8px', height: '8px' }}></div>
                </div>
              </div>
            </Card.Body>
          </Card>
        </Col>
        {/* Thẻ số tiện ích đang bảo trì */}
        <Col className="mb-3" style={{ flex: '1 1 0', minWidth: '0' }}>
          <Card className="h-100">
            <Card.Body>
              <div className="d-flex justify-content-between align-items-center">
                <div style={{ flex: '1', minWidth: '0' }}>
                  <h6 className="text-muted mb-1" style={{ fontSize: '0.875rem', whiteSpace: 'nowrap' }}>Bảo trì</h6>
                  <h3 className="mb-0 text-warning">{stats.maintenance}</h3>
                </div>
                <div className="bg-warning bg-opacity-10 rounded p-2" style={{ flexShrink: 0 }}>
                  <div className="bg-warning rounded-circle" style={{ width: '8px', height: '8px' }}></div>
                </div>
              </div>
            </Card.Body>
          </Card>
        </Col>
        {/* Thẻ số tiện ích có phí */}
        <Col className="mb-3" style={{ flex: '1 1 0', minWidth: '0' }}>
          <Card className="h-100">
            <Card.Body>
              <div className="d-flex justify-content-between align-items-center">
                <div style={{ flex: '1', minWidth: '0' }}>
                  <h6 className="text-muted mb-1" style={{ fontSize: '0.875rem', whiteSpace: 'nowrap' }}>Có phí</h6>
                  <h3 className="mb-0 text-info">{stats.withFee}</h3>
                </div>
              </div>
            </Card.Body>
          </Card>
        </Col>
      </Row>

      {/* Bảng danh sách tiện ích */}
      {currentAmenities.length > 0 ? (
        <Card>
          <Table responsive hover>
            <thead>
              <tr>
                <th>Tên tiện ích</th>
                <th>Vị trí</th>
                <th>Loại phí</th>
                <th>Face check-in</th>
                <th>Trạng thái</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {currentAmenities.map((amenity, index) => (
                <tr key={amenity.amenityId || amenity.id || index}>
                  <td><strong>{amenity.name}</strong></td>
                  <td className="text-muted">{amenity.location}</td>
                  <td>
                    <Badge bg={amenity.feeType === 'Free' ? 'success' : 'primary'}>
                      {amenity.feeType === 'Free' ? 'Miễn phí' : 'Có phí'}
                    </Badge>
                  </td>
                  <td>
                    <Badge bg={amenity.requiresFaceVerification ? 'danger' : 'secondary'}>
                      {amenity.requiresFaceVerification ? 'Bắt buộc' : 'Không'}
                    </Badge>
                  </td>
                  <td>{getStatusBadge(amenity.status)}</td>
                  <td>
                    <div className="d-flex gap-2">
                      <Button
                        variant="outline-primary"
                        size="sm"
                        onClick={() => handleEdit(amenity)}
                        title="Chỉnh sửa"
                      >
                        <i className="fas fa-pencil-alt"></i>
                      </Button>
                      <Button
                        variant="outline-danger"
                        size="sm"
                        onClick={() => handleDeleteClick(amenity)}
                        title="Xóa"
                      >
                        <i className="fas fa-trash"></i>
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </Card>
      ) : (
        <Card className="text-center py-5">
          <Card.Body>
            <i className="fas fa-search fa-3x text-muted mb-3"></i>
            <h5>Không tìm thấy tiện ích</h5>
            <p className="text-muted">Thử thay đổi từ khóa tìm kiếm của bạn.</p>
          </Card.Body>
        </Card>
      )}

      {/* Phân trang - chỉ hiển thị khi có nhiều hơn 1 trang */}
      {filteredAmenities.length > itemsPerPage && (
        <Row className="mt-4">
          <Col>
            <div className="d-flex justify-content-between align-items-center">
              {/* Thông tin hiển thị */}
              <div className="text-muted">
                Hiển thị {startIndex + 1} - {Math.min(endIndex, filteredAmenities.length)} trong {filteredAmenities.length} tiện ích
              </div>
              {/* Các nút điều hướng phân trang */}
              <div className="d-flex gap-2">
                {/* Nút về trang đầu */}
                <Button
                  variant="outline-secondary"
                  size="sm"
                  onClick={() => setCurrentPage(1)}
                  disabled={currentPage === 1}
                >
                  Đầu
                </Button>
                {/* Nút trang trước */}
                <Button
                  variant="outline-secondary"
                  size="sm"
                  onClick={() => setCurrentPage(currentPage - 1)}
                  disabled={currentPage === 1}
                >
                  Trước
                </Button>

                {/* Các nút số trang */}
                {Array.from({ length: totalPages }, (_, i) => i + 1).map(page => (
                  <Button
                    key={page}
                    variant={currentPage === page ? "primary" : "outline-secondary"}
                    size="sm"
                    onClick={() => setCurrentPage(page)}
                    className="px-3"
                  >
                    {page}
                  </Button>
                ))}

                {/* Nút trang sau */}
                <Button
                  variant="outline-secondary"
                  size="sm"
                  onClick={() => setCurrentPage(currentPage + 1)}
                  disabled={currentPage === totalPages}
                >
                  Sau
                </Button>
                {/* Nút về trang cuối */}
                <Button
                  variant="outline-secondary"
                  size="sm"
                  onClick={() => setCurrentPage(totalPages)}
                  disabled={currentPage === totalPages}
                >
                  Cuối
                </Button>
              </div>
            </div>
          </Col>
        </Row>
      )}

      {/* Modal tạo tiện ích mới */}
      <CreateAmenity
        show={showCreateModal}
        onHide={() => setShowCreateModal(false)}
        onSuccess={handleCreateSuccess}
        categories={uniqueCategories}
        existingAmenities={amenities}
        onShowToast={(message, type = 'success') => {
          setToastMessage(message);
          setToastType(type);
          setShowToast(true);
        }}
      />

      {/* Modal cập nhật tiện ích */}
      <UpdateAmenity
        show={showUpdateModal}
        onHide={() => setShowUpdateModal(false)}
        onSuccess={handleUpdateSuccess}
        categories={uniqueCategories}
        amenity={selectedAmenity}
        existingAmenities={amenities}
        onShowToast={(message, type = 'success') => {
          setToastMessage(message);
          setToastType(type);
          setShowToast(true);
        }}
      />

      {/* Modal xác nhận xóa */}
      <Modal
        show={showDeleteModal}
        onHide={handleCancelDelete}
        centered
        backdrop="static"
      >
        <Modal.Header closeButton>
          <Modal.Title>
            <div className="d-flex align-items-center gap-2">
              <i className="fas fa-exclamation-triangle text-warning"></i>
              <span>Xác nhận xóa</span>
            </div>
          </Modal.Title>
        </Modal.Header>

        <Modal.Body>
          <p className="mb-0">
            Bạn có chắc chắn muốn xóa tiện ích <strong>"{amenityToDelete?.name}"</strong> không?
          </p>
        </Modal.Body>

        <Modal.Footer>
          <Button
            variant="secondary"
            onClick={handleCancelDelete}
            disabled={loading}
          >
            Hủy
          </Button>
          <Button
            variant="danger"
            onClick={handleConfirmDelete}
            disabled={loading}
          >
            {loading ? (
              <>
                <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                Đang xóa...
              </>
            ) : (
              <>
                <i className="fas fa-trash me-2"></i>
                Xóa
              </>
            )}
          </Button>
        </Modal.Footer>
      </Modal>

      {/* Toast notification */}
      <Toast
        key={`${toastMessage}-${Date.now()}`}
        message={toastMessage}
        show={showToast}
        onClose={() => setShowToast(false)}
        type={toastType}
        duration={1500}
      />

    </Container>
  );
}