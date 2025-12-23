import React, { useState, useEffect, useMemo, useRef } from 'react';
import { Container, Row, Col, Card, Button, Badge, Spinner, Alert, Pagination, Form } from 'react-bootstrap';
import { useLocation } from 'react-router-dom';
import { amenitiesApi } from '../../features/building-management/amenitiesApi';
import { amenityBookingApi } from '../../features/amenity-booking/amenityBookingApi';
import { amenityPackageApi } from '../../features/amenity-booking/amenityPackageApi';
import { residentsApi } from '../../features/residents/residentsApi';
import { useUser } from '../../hooks/useUser';
import Toast from '../../components/Toast';
import ModalAmenityBooking from './ModalAmenityBooking';
import BookingHistoryModal from './BookingHistoryModal';
import QRPaymentModal from '../../components/QRPaymentModal';
import '../../styles/AmenityBooking.css';
import dayjs from 'dayjs';

export default function AmenityBooking() {
  const getStatusLabel = (amenity) => {
    if (amenity.isUnderMaintenance) {
      return 'Đang bảo trì';
    }
    if ((amenity.status || '').toUpperCase() === 'INACTIVE') {
      return 'Không hoạt động';
    }
    return 'Đang hoạt động';
  };

  const getStatusColor = (amenity) => {
    if (amenity.isUnderMaintenance) {
      return '#f97316';
    }
    if ((amenity.status || '').toUpperCase() === 'INACTIVE') {
      return '#dc2626';
    }
    return '#16a34a';
  };

  const location = useLocation();
  const { user, fetchUserData } = useUser();
  const hasTriedFetchUserRef = useRef(false);

  // State quản lý danh sách tiện ích
  const [amenities, setAmenities] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // State quản lý filter và search
  const [searchTerm, setSearchTerm] = useState("");
  const [feeTypeFilter, setFeeTypeFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");
  const [categoryFilter, setCategoryFilter] = useState("all");

  // State quản lý phân trang
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 6;

  // State quản lý modal booking
  const [showBookingModal, setShowBookingModal] = useState(false);
  const [selectedAmenity, setSelectedAmenity] = useState(null);
  const [packages, setPackages] = useState([]);
  const [showHistory, setShowHistory] = useState(false);
  const [historyBookingId, setHistoryBookingId] = useState(null);
  const [submittingBooking, setSubmittingBooking] = useState(false);

  // State quản lý form booking
  const [bookingForm, setBookingForm] = useState({
    packageId: '',
    notes: ''
  });

  // State quản lý ngày tính toán từ package
  const [calculatedDates, setCalculatedDates] = useState({
    startDate: null,
    endDate: null
  });

  // State quản lý giá và availability
  const [calculatedPrice, setCalculatedPrice] = useState(null);
  const [priceBreakdown, setPriceBreakdown] = useState(null);
  const [formErrors, setFormErrors] = useState({});
  const facePreviewUrlRef = useRef("");
  const [faceImageFile, setFaceImageFile] = useState(null);
  const [facePreviewUrl, setFacePreviewUrl] = useState("");
  const [faceImageError, setFaceImageError] = useState("");
  const [isFaceWebcamOpen, setIsFaceWebcamOpen] = useState(false);
  const [hasFaceRegistered, setHasFaceRegistered] = useState(false);
  const MAX_FACE_IMAGE_SIZE = 5 * 1024 * 1024; // 5MB
  // Apartment is resolved on BE; FE no longer tracks it

  const releaseFacePreviewUrl = (url) => {
    if (url && url.startsWith("blob:")) {
      URL.revokeObjectURL(url);
    }
  };

  const updateFacePreviewUrl = (url) => {
    releaseFacePreviewUrl(facePreviewUrlRef.current);
    facePreviewUrlRef.current = url || "";
    setFacePreviewUrl(url);
  };

  const clearFaceImage = () => {
    updateFacePreviewUrl("");
    setFaceImageFile(null);
    setFaceImageError("");
  };

  const handleFaceImageFile = (file, previewUrl = null) => {
    if (!file) return;

    if (!file.type?.startsWith("image/")) {
      setFaceImageError("Vui lòng chọn file ảnh hợp lệ.");
      return;
    }

    if (file.size > MAX_FACE_IMAGE_SIZE) {
      setFaceImageError("Kích thước ảnh tối đa là 5MB.");
      return;
    }

    setFaceImageFile(file);
    setFaceImageError("");
    const preview = previewUrl || URL.createObjectURL(file);
    updateFacePreviewUrl(preview);
  };

  const handleFaceFileInput = (event) => {
    const file = event.target.files?.[0];
    if (file) {
      handleFaceImageFile(file);
    }
    if (event.target) {
      event.target.value = "";
    }
  };

  const dataURLToFile = (dataUrl, fileName) => {
    //data:image/jpeg;base64,....
    const arr = dataUrl.split(",");
    const mimeMatch = arr[0].match(/:(.*?);/);
    const mime = mimeMatch ? mimeMatch[1] : "image/jpeg";
    const bstr = atob(arr[1]);
    let n = bstr.length;
    const u8arr = new Uint8Array(n);
    while (n--) {
      u8arr[n] = bstr.charCodeAt(n);
    }
    return new File([u8arr], fileName, { type: mime });
  };

  const handleFaceCameraCapture = (screenshot) => {
    if (!screenshot) {
      setFaceImageError("Không thể chụp ảnh khuôn mặt. Vui lòng thử lại.");
      return;
    }

    const faceFile = dataURLToFile(screenshot, `face-${Date.now()}.jpg`);
    handleFaceImageFile(faceFile, screenshot);
  };

  const handleToggleFaceWebcam = () => {
    setIsFaceWebcamOpen((prev) => !prev);
  };

  useEffect(() => {
    return () => {
      releaseFacePreviewUrl(facePreviewUrlRef.current);
    };
  }, []);

  const closeBookingModal = () => {
    setShowBookingModal(false);
    clearFaceImage();
    setIsFaceWebcamOpen(false);
  };

  // State quản lý QR Payment
  const [showQRPayment, setShowQRPayment] = useState(false);
  const [paymentData, setPaymentData] = useState(null);

  // State quản lý toast
  const [showToast, setShowToast] = useState(false);
  const [toastMessage, setToastMessage] = useState("");
  const [toastType, setToastType] = useState("success");

  // Lấy danh sách tiện ích khi component mount
  useEffect(() => {
    fetchAmenities();
  }, []);

  // Nếu user trên store chưa có apartment, cố gắng fetch lại từ BE bằng keycloak sub (chỉ 1 lần)
  useEffect(() => {
    if (hasTriedFetchUserRef.current) return;
    const hasApartmentInfo = !!(
      user?.apartmentId ||
      user?.primaryApartmentId ||
      user?.apartment?.apartmentId ||
      (Array.isArray(user?.apartments) && user.apartments[0]?.apartmentId)
    );
    const sub =
      typeof window !== "undefined" ? window.keycloak?.tokenParsed?.sub : null;
    if (!hasApartmentInfo && sub) {
      hasTriedFetchUserRef.current = true;
      fetchUserData(sub).catch(() => { });
    }
  }, [fetchUserData, user]);

  // No apartment fetch fallback needed; BE will infer from user token

  // Lắng nghe state từ navigation (ví dụ khi bấm thông báo hoặc từ menu "Lịch sử đăng ký")
  useEffect(() => {
    if (location.state?.openHistoryModal) {
      setHistoryBookingId(location.state?.bookingId || null);
      setShowHistory(true);
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  const fetchAmenities = async () => {
    try {
      setLoading(true);
      setError("");
      const data = await amenitiesApi.getAll();
      // Lọc tiện ích không bị xóa
      const validAmenities = data.filter((amenity) => !amenity.isDelete);
      setAmenities(validAmenities);
    } catch (err) {
      setError("Không thể tải danh sách tiện ích. Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  };

  // Lọc và tìm kiếm tiện ích
  const filteredAmenities = useMemo(() => {
    return amenities.filter((amenity) => {
      // Filter theo search term
      const matchesSearch =
        !searchTerm ||
        amenity.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (amenity.location &&
          amenity.location.toLowerCase().includes(searchTerm.toLowerCase()));

      // Filter theo loại phí
      const matchesFeeType =
        feeTypeFilter === "all" || amenity.feeType === feeTypeFilter;

      // Filter theo trạng thái
      const matchesStatus =
        statusFilter === "all" || amenity.status === statusFilter;

      // Filter theo danh mục
      const matchesCategory =
        categoryFilter === "all" || amenity.categoryName === categoryFilter;

      return (
        matchesSearch && matchesFeeType && matchesStatus && matchesCategory
      );
    });
  }, [amenities, searchTerm, feeTypeFilter, statusFilter, categoryFilter]);

  // Lấy danh sách categories unique
  const uniqueCategories = [
    ...new Set(
      amenities
        .map((amenity) => amenity.categoryName)
        .filter((category) => category && category.trim() !== "")
    ),
  ].sort();

  // Pagination
  const totalPages = Math.ceil(filteredAmenities.length / itemsPerPage);
  const startIndex = (currentPage - 1) * itemsPerPage;
  const endIndex = startIndex + itemsPerPage;
  const currentAmenities = filteredAmenities.slice(startIndex, endIndex);

  // Reset về trang 1 khi filter thay đổi
  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm, feeTypeFilter, statusFilter, categoryFilter]);

  // Format giá tiền
  const formatPrice = (price) => {
    if (!price || price === 0) return "0";
    const numPrice = typeof price === "string" ? parseFloat(price) : price;
    return numPrice.toLocaleString("vi-VN");
  };


  // Xử lý khi click nút đăng ký
  const handleBookClick = async (amenity) => {
    if (amenity.isUnderMaintenance) {
      setToastType("warning");
      setToastMessage("Tiện ích hiện đang bảo trì. Vui lòng đăng ký sau khi bảo trì hoàn tất.");
      setShowToast(true);
      return;
    }

    setSelectedAmenity(amenity);
    resetBookingForm();
    clearFaceImage();
    setIsFaceWebcamOpen(false);
    setShowBookingModal(true);

    // Fetch thông tin đăng ký khuôn mặt của cư dân
    if (user?.userId) {
      try {
        const residentDetail = await residentsApi.getByUserId(user.userId);
        setHasFaceRegistered(residentDetail?.hasFaceRegistered || false);
      } catch (err) {
        console.error("Error fetching resident face registration status:", err);
        setHasFaceRegistered(false);
      }
    }

    // Load packages cho amenity này (bao gồm cả gói ngày và gói tháng)
    if (amenity.amenityId) {
      try {
        const packagesData = await amenityPackageApi.getActiveByAmenityId(amenity.amenityId);
        const packagesList = packagesData?.data || packagesData || [];
        setPackages(Array.isArray(packagesList) ? packagesList : []);
      } catch (err) {
        setPackages([]);
      }
    } else {
      setPackages([]);
    }
  };

  // Reset form booking
  const resetBookingForm = () => {
    setBookingForm({
      packageId: '',
      notes: ''
    });
    setCalculatedPrice(null);
    setPriceBreakdown(null);
    setFormErrors({});
    setCalculatedDates({
      startDate: null,
      endDate: null
    });
  };

  // Xử lý thay đổi form
  const handleFormChange = (e) => {
    const { name, value } = e.target;
    const newFormData = {
      ...bookingForm,
      [name]: value,
    };

    setBookingForm(newFormData);

    // Real-time validation cho notes
    if (name === 'notes') {
      const newErrors = { ...formErrors };

      if (value.trim().length > 0 && value.trim().length < 3) {
        newErrors.notes = 'Ghi chú phải có ít nhất 3 ký tự';
      } else if (value.length > 1000) {
        newErrors.notes = 'Ghi chú không được vượt quá 1000 ký tự';
      } else if (value.trim() && !/^[a-zA-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ0-9\s,.!?-]+$/.test(value)) {
        newErrors.notes = 'Ghi chú không được chứa ký tự đặc biệt';
      } else {
        delete newErrors.notes;
      }

      setFormErrors(newErrors);
    }

    // Khi chọn package, tự động tính giá
    if (name === 'packageId') {
      setCalculatedPrice(null);

      // Tính toán ngày bắt đầu và kết thúc từ package
      if (value) {
        const selectedPackage = packages.find(p => p.packageId === value);
        if (selectedPackage) {
          const today = new Date();
          today.setHours(0, 0, 0, 0);

          let endDate = new Date(today);

          // Tính EndDate dựa trên period_unit
          if (selectedPackage.periodUnit === 'Day' && selectedPackage.durationDays) {
            // Tính theo ngày
            endDate.setDate(today.getDate() + selectedPackage.durationDays);
          } else if (selectedPackage.monthCount) {
            // Tính theo tháng
            const targetMonth = endDate.getMonth() + selectedPackage.monthCount;
            endDate.setMonth(targetMonth);

            // Xử lý trường hợp ngày không hợp lệ (ví dụ: 31/02)
            if (endDate.getDate() !== today.getDate()) {
              // Nếu ngày thay đổi, đặt về ngày cuối cùng của tháng trước đó
              endDate.setDate(0);
            }
          }

          setCalculatedDates({
            startDate: today,
            endDate: endDate
          });

          // Tự động tính giá khi chọn package
          if (selectedAmenity?.amenityId) {
            amenityBookingApi.calculatePrice(selectedAmenity.amenityId, value)
              .then(priceResult => {
                setCalculatedPrice(priceResult.totalPrice || priceResult.price || selectedPackage.price);
                setPriceBreakdown({
                  packageName: selectedPackage.name,
                  months: selectedPackage.monthCount,
                  days: selectedPackage.durationDays,
                  periodUnit: selectedPackage.periodUnit
                });
              })
              .catch(() => {
                // Nếu không tính được giá từ API, dùng giá từ package
                setCalculatedPrice(selectedPackage.price);
                setPriceBreakdown({
                  packageName: selectedPackage.name,
                  months: selectedPackage.monthCount,
                  days: selectedPackage.durationDays,
                  periodUnit: selectedPackage.periodUnit
                });
              });
          } else {
            // Nếu chưa có amenity, dùng giá từ package
            setCalculatedPrice(selectedPackage.price);
            setPriceBreakdown({
              packageName: selectedPackage.name,
              months: selectedPackage.monthCount,
              days: selectedPackage.durationDays,
              periodUnit: selectedPackage.periodUnit
            });
          }
        } else {
          setCalculatedDates({
            startDate: null,
            endDate: null
          });
        }
      } else {
        setCalculatedDates({
          startDate: null,
          endDate: null
        });
        setCalculatedPrice(null);
        setPriceBreakdown(null);
      }
    }

    // Validate real-time
    const newErrors = { ...formErrors };

    // Validate package
    if (name === 'packageId' && !value) {
      newErrors.packageId = 'Vui lòng chọn gói';
    } else if (name === 'packageId') {
      delete newErrors.packageId;
    }

    setFormErrors(newErrors);
  };

  // Validate form
  const validateForm = () => {
    const errors = {};

    // Validate package
    if (!bookingForm.packageId) {
      errors.packageId = 'Vui lòng chọn gói';
    }

    return errors;
  };

  // Xử lý submit booking - Tạo booking và hiển thị QR Payment Modal
  const handleSubmitBooking = async () => {
    if (submittingBooking) {
      return;
    }

    const errors = validateForm();
    if (Object.keys(errors).length > 0) {
      setFormErrors(errors);
      return;
    }

    if (!bookingForm.packageId) {
      setToastMessage('Vui lòng chọn gói');
      setToastType('warning');
      setShowToast(true);
      return;
    }

    if (!selectedAmenity) {
      setToastMessage('Không xác định tiện ích');
      setToastType('error');
      setShowToast(true);
      return;
    }

    if (!calculatedPrice || calculatedPrice === 0) {
      const selectedPackage = packages.find(p => p.packageId === bookingForm.packageId);
      if (selectedPackage && selectedPackage.price) {
        setCalculatedPrice(selectedPackage.price);
      } else {
        setToastMessage('Không thể lấy thông tin giá. Vui lòng thử lại.');
        setToastType('error');
        setShowToast(true);
        return;
      }
    }

    const requiresFace = selectedAmenity?.requiresFaceVerification;
    // Chỉ yêu cầu ảnh nếu tiện ích yêu cầu xác thực khuôn mặt VÀ cư dân chưa đăng ký khuôn mặt
    if (requiresFace && !hasFaceRegistered && !faceImageFile) {
      setFaceImageError("Tiện ích yêu cầu ảnh khuôn mặt. Vui lòng cung cấp ảnh.");
      return;
    }

    const resolvedApartmentId =
      user?.apartmentId ||
      user?.primaryApartmentId ||
      user?.apartment?.apartmentId ||
      (Array.isArray(user?.apartments) && user.apartments[0]?.apartmentId) ||
      null;

    setSubmittingBooking(true);

    try {
      let createdBooking;

      if (requiresFace) {
        const payload = {
          amenityId: selectedAmenity.amenityId,
          packageId: bookingForm.packageId,
          notes: bookingForm.notes || null,
          apartmentId: resolvedApartmentId,
          faceImage: faceImageFile,
        };

        createdBooking = await amenityBookingApi.createWithFace(payload);
      } else {
        const bookingData = {
          amenityId: selectedAmenity.amenityId,
          packageId: bookingForm.packageId,
          notes: bookingForm.notes || null,
          apartmentId: resolvedApartmentId,
        };

        createdBooking = await amenityBookingApi.create(bookingData);
      }

      let bookingId = null;
      let isSuccess = false;

      if (createdBooking?.success && createdBooking?.data?.bookingId) {
        bookingId = createdBooking.data.bookingId;
        isSuccess = true;
      } else if (createdBooking?.success && createdBooking?.data?.data?.bookingId) {
        bookingId = createdBooking.data.data.bookingId;
        isSuccess = true;
      } else if (createdBooking?.data?.bookingId) {
        bookingId = createdBooking.data.bookingId;
        isSuccess = true;
      } else if (createdBooking?.bookingId) {
        bookingId = createdBooking.bookingId;
        isSuccess = true;
      } else if (createdBooking?.data?.data?.bookingId) {
        bookingId = createdBooking.data.data.bookingId;
        isSuccess = true;
      }

      if (isSuccess && bookingId) {
        const selectedPackage = packages.find(p => p.packageId === bookingForm.packageId);
        let timeInfo = '';
        if (selectedPackage?.periodUnit === 'Day' && selectedPackage?.durationDays) {
          timeInfo = `Từ ngày đăng ký, ${selectedPackage.durationDays} ngày`;
        } else if (selectedPackage?.monthCount) {
          timeInfo = `Từ ngày đăng ký, ${selectedPackage.monthCount} tháng`;
        }

        const payment = {
          amount: calculatedPrice,
          amenityName: selectedAmenity.name,
          timeInfo: timeInfo,
          packageName: selectedPackage?.name || 'Gói tháng',
          description: `Thanh toán đăng ký tiện ích ${selectedAmenity.name} - ${selectedPackage?.name || ''}`,
          bookingId: bookingId,
          items: [
            {
              name: `${selectedAmenity.name} - ${selectedPackage?.name || 'Gói tháng'}`,
              quantity: 1,
              price: calculatedPrice,
            },
          ],
        };

        setPaymentData(payment);
        closeBookingModal();
        setShowQRPayment(true);
      } else {
        throw new Error(createdBooking?.message || 'Không thể tạo booking - Missing booking ID');
      }
    } catch (error) {
      let errorMessage = 'Có lỗi xảy ra khi tạo booking!';
      if (error.response?.data?.message) {
        errorMessage = error.response.data.message;

        // Nếu lỗi liên quan đến face verification, hiển thị trong faceImageError
        if (error.response.data.requiresFaceVerification ||
          errorMessage.toLowerCase().includes('face verification') ||
          errorMessage.toLowerCase().includes('khuôn mặt')) {
          setFaceImageError(errorMessage);
          return;
        }
      } else if (error.response?.data?.errors) {
        const errorKeys = Object.keys(error.response.data.errors);
        if (errorKeys.length > 0) {
          errorMessage =
            error.response.data.errors[errorKeys[0]][0] || errorMessage;
        }
      } else if (error.message) {
        errorMessage = error.message;
      }

      setToastMessage(errorMessage);
      setToastType("error");
      setShowToast(true);
    } finally {
      setSubmittingBooking(false);
    }
  };



  // Hiển thị loading
  if (loading) {
    return (
      <Container
        className="d-flex justify-content-center align-items-center"
        style={{ height: "400px" }}
      >
        <Spinner animation="border" variant="primary" />
      </Container>
    );
  }

  // Hiển thị lỗi
  if (error) {
    return (
      <Container>
        <Alert variant="danger">{error}</Alert>
      </Container>
    );
  }

  return (
    <Container fluid className="p-4">
      {/* Header */}
      <Row className="mb-4">
        <Col>
          <Card className="bg-primary text-white">
            <Card.Body>
              <Card.Title className="mb-2">
                Đăng ký sử dụng tiện ích
              </Card.Title>
            </Card.Body>
          </Card>
        </Col>
      </Row>
      {/* Search và Filters */}
      <Row className="mb-4">
        <Col md={12}>
          <Card className="filter-card shadow-sm">
            <Card.Body>
              <Row className="g-3">
                {/* Search */}
                <Col md={4}>
                  <Form.Label className="text-muted mb-1">
                    <strong>Tìm kiếm :</strong>
                  </Form.Label>
                  <Form.Control
                    type="text"
                    placeholder="Tìm theo tên hoặc vị trí..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                  />
                </Col>

                {/* Filter Loại phí */}
                <Col md={2}>
                  <Form.Label className="text-muted mb-1">
                    <strong>Loại phí :</strong>
                  </Form.Label>
                  <Form.Select
                    value={feeTypeFilter}
                    onChange={(e) => setFeeTypeFilter(e.target.value)}
                    className="custom-select"
                  >
                    <option value="all">Tất cả</option>
                    <option value="Paid">Có phí</option>
                    <option value="Free">Miễn phí</option>
                  </Form.Select>
                </Col>

                {/* Filter Trạng thái */}
                <Col md={2}>
                  <Form.Label className="text-muted mb-1">
                    <strong>Trạng thái :</strong>
                  </Form.Label>
                  <Form.Select
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                    className="custom-select"
                  >
                    <option value="all">Tất cả</option>
                    <option value="ACTIVE">Hoạt động</option>
                    <option value="MAINTENANCE">Bảo trì</option>
                    <option value="INACTIVE">Không hoạt động</option>
                  </Form.Select>
                </Col>

                {/* Filter Danh mục */}
                <Col md={2}>
                  <Form.Label className="text-muted mb-1">
                    <strong>Danh mục :</strong>
                  </Form.Label>
                  <Form.Select
                    value={categoryFilter}
                    onChange={(e) => setCategoryFilter(e.target.value)}
                    className="custom-select"
                  >
                    <option value="all">Tất cả danh mục</option>
                    {uniqueCategories.map((category) => (
                      <option key={category} value={category}>
                        {category}
                      </option>
                    ))}
                  </Form.Select>
                </Col>

                {/* Nút lịch sử đặt vào cuối hàng filter */}
                <Col md={2} className="d-flex align-items-end">
                  <Button
                    variant="outline-info"
                    className="w-100"
                    onClick={() => setShowHistory(true)}
                    title="Lịch sử đăng kí"
                  >
                    <i className="fas fa-history me-2"></i>
                    Lịch sử đăng kí
                  </Button>
                </Col>
              </Row>

              {/* Thông tin kết quả */}
              <div className="mt-3 pt-3 border-top">
                <small className="text-muted">
                  <i className="fas fa-info-circle me-1"></i>
                  Tìm thấy{" "}
                  <strong className="text-primary">
                    {filteredAmenities.length}
                  </strong>{" "}
                  tiện ích
                  {filteredAmenities.length !== amenities.length &&
                    ` (lọc từ ${amenities.length} tiện ích)`}
                </small>
              </div>
            </Card.Body>
          </Card>
        </Col>
      </Row>

      {/* Danh sách tiện ích */}
      <Row className="g-4">
        {currentAmenities.length > 0 ? (
          currentAmenities.map((amenity) => {
            const maintenanceStart = amenity.maintenanceStart ? dayjs(amenity.maintenanceStart) : null;
            const maintenanceEnd = amenity.maintenanceEnd ? dayjs(amenity.maintenanceEnd) : null;

            return (
              <Col key={amenity.amenityId} md={6} lg={4} className="mb-4">
                <Card className="h-100 shadow-sm">
                  <Card.Header className="d-flex justify-content-end align-items-center">
                    <Badge
                      bg={amenity.feeType === "Free" ? "success" : "primary"}
                    >
                      {amenity.feeType === "Free" ? "Miễn phí" : "Có phí"}
                    </Badge>
                  </Card.Header>

                  <Card.Body className="d-flex flex-column">
                    <Card.Title className="mb-2">{amenity.name}</Card.Title>

                    <Row className="mb-2">
                      <Col>
                        <span className="text-muted">Vị trí: </span>
                        <strong>{amenity.location}</strong>
                      </Col>
                      {amenity.categoryName && (
                        <Col>
                          <span className="text-muted">Danh mục: </span>
                          <strong>{amenity.categoryName}</strong>
                        </Col>
                      )}
                    </Row>

                    <div className="mb-3">
                      <div className="mb-2">
                        <span className="text-muted">Trạng thái: </span>
                        <strong style={{ color: getStatusColor(amenity) }}>
                          {getStatusLabel(amenity)}
                        </strong>
                      </div>

                      {amenity.isUnderMaintenance && (
                        <Alert variant="warning" className="mt-2 mb-0">
                          <div>
                            <strong>Tiện ích đang bảo trì từ:</strong>
                          </div>
                          <div>
                            {maintenanceStart ? maintenanceStart.format('HH:mm') : ''} ngày {maintenanceStart ? maintenanceStart.format('DD/MM/YYYY') : 'Đang cập nhật'}
                            {maintenanceEnd ? ` -> ${maintenanceEnd.format('HH:mm')} ngày ${maintenanceEnd.format('DD/MM/YYYY')}` : ''}
                          </div>
                        </Alert>
                      )}
                    </div>

                    {amenity.feeType !== 'Free' && (
                      <Button
                        variant={amenity.isUnderMaintenance ? "secondary" : "primary"}
                        className="mt-auto"
                        onClick={() => handleBookClick(amenity)}
                        disabled={amenity.isUnderMaintenance}
                      >
                        {amenity.isUnderMaintenance ? 'Tạm ngừng đăng ký' : 'Đăng ký ngay'}
                      </Button>
                    )}
                  </Card.Body>
                </Card>
              </Col>
            );
          })
        ) : (
          <Col>
            <Card className="text-center py-5 empty-state">
              <Card.Body>
                <div className="empty-icon mb-3">
                  <i className="fas fa-search fa-3x text-muted"></i>
                </div>
                <h5>Không tìm thấy tiện ích</h5>
                <p className="text-muted">
                  {searchTerm ||
                    feeTypeFilter !== "all" ||
                    statusFilter !== "all" ||
                    categoryFilter !== "all"
                    ? "Không có tiện ích nào phù hợp với bộ lọc của bạn. Thử thay đổi bộ lọc."
                    : "Hiện tại chưa có tiện ích nào có thể đăng ký."}
                </p>
                {(searchTerm ||
                  feeTypeFilter !== "all" ||
                  statusFilter !== "all" ||
                  categoryFilter !== "all") && (
                    <Button
                      variant="outline-primary"
                      onClick={() => {
                        setSearchTerm("");
                        setFeeTypeFilter("all");
                        setStatusFilter("ACTIVE");
                        setCategoryFilter("all");
                      }}
                    >
                      <i className="fas fa-redo me-2"></i>
                      Xóa bộ lọc
                    </Button>
                  )}
              </Card.Body>
            </Card>
          </Col>
        )}
      </Row>

      {/* Pagination */}
      {totalPages > 1 && (
        <Row className="mt-4">
          <Col className="d-flex justify-content-center">
            <Pagination>
              <Pagination.First
                onClick={() => setCurrentPage(1)}
                disabled={currentPage === 1}
              />
              <Pagination.Prev
                onClick={() => setCurrentPage(currentPage - 1)}
                disabled={currentPage === 1}
              />

              {[...Array(totalPages)].map((_, index) => {
                const pageNumber = index + 1;
                // Hiển thị các trang gần current page
                if (
                  pageNumber === 1 ||
                  pageNumber === totalPages ||
                  (pageNumber >= currentPage - 1 &&
                    pageNumber <= currentPage + 1)
                ) {
                  return (
                    <Pagination.Item
                      key={pageNumber}
                      active={pageNumber === currentPage}
                      onClick={() => setCurrentPage(pageNumber)}
                    >
                      {pageNumber}
                    </Pagination.Item>
                  );
                } else if (
                  pageNumber === currentPage - 2 ||
                  pageNumber === currentPage + 2
                ) {
                  return <Pagination.Ellipsis key={pageNumber} disabled />;
                }
                return null;
              })}

              <Pagination.Next
                onClick={() => setCurrentPage(currentPage + 1)}
                disabled={currentPage === totalPages}
              />
              <Pagination.Last
                onClick={() => setCurrentPage(totalPages)}
                disabled={currentPage === totalPages}
              />
            </Pagination>
          </Col>
        </Row>
      )}

      {/* Thông tin phân trang */}
      {filteredAmenities.length > 0 && (
        <Row className="mt-2">
          <Col className="text-center">
            <small className="text-muted">
              Hiển thị {startIndex + 1} -{" "}
              {Math.min(endIndex, filteredAmenities.length)} trong số{" "}
              {filteredAmenities.length} tiện ích
            </small>
          </Col>
        </Row>
      )}

      {/* Modal đăng ký tiện ích */}
      <ModalAmenityBooking
        show={showBookingModal}
        onHide={closeBookingModal}
        selectedAmenity={selectedAmenity}
        packages={packages}
        bookingForm={bookingForm}
        formErrors={formErrors}
        calculatedPrice={calculatedPrice}
        priceBreakdown={priceBreakdown}
        isSubmitting={submittingBooking}
        onFormChange={handleFormChange}
        onSubmit={handleSubmitBooking}
        formatPrice={formatPrice}
        calculatedDates={calculatedDates}
        facePreviewUrl={facePreviewUrl}
        faceImageError={faceImageError}
        isFaceWebcamOpen={isFaceWebcamOpen}
        onToggleFaceWebcam={handleToggleFaceWebcam}
        onFaceCameraCapture={handleFaceCameraCapture}
        onFaceImageChange={handleFaceFileInput}
        onClearFaceImage={clearFaceImage}
        hasFaceRegistered={hasFaceRegistered}
      />

      {/* Toast notification */}
      <Toast
        message={toastMessage}
        show={showToast}
        onClose={() => setShowToast(false)}
        type={toastType}
        duration={2500}
      />

      {/* Booking history modal */}
      <BookingHistoryModal
        show={showHistory}
        onHide={() => {
          setShowHistory(false);
          setHistoryBookingId(null);
        }}
        isManager={false}
        initialBookingId={historyBookingId}
      />

      {/* QR Payment Modal */}
      <QRPaymentModal
        open={showQRPayment}
        onCancel={() => setShowQRPayment(false)}
        amenityData={paymentData ? {
          amenityName: paymentData.amenityName,
          bookingDate: new Date().toISOString().split('T')[0],
          timeSlot: paymentData.timeInfo,
          packageName: paymentData.packageName,
          totalPrice: paymentData.amount,
          bookingId: paymentData.bookingId,
          description: paymentData.description
        } : null}
        onPaymentComplete={(success) => {
          setShowQRPayment(false);
          if (success) {
            // Payment successful - booking status already updated by QRPaymentModal
            // Reset form
            resetBookingForm();
          }
        }}
      />
    </Container>
  );
}
