import React, { useEffect, useMemo, useRef, useState } from "react";
import {
    Card,
    Table,
    Button,
    Space,
    Tag,
    Modal,
    Upload,
    Switch,
    Input,
    Typography,
    message,
    Divider,
    Descriptions,
    Tooltip,
    Alert,
} from "antd";
import {
    CameraOutlined,
    ReloadOutlined,
    HistoryOutlined,
    ExclamationCircleOutlined,
    VideoCameraOutlined,
    EyeOutlined,
} from "@ant-design/icons";
import dayjs from "dayjs";
import { amenityBookingApi } from "../../features/amenity-booking/amenityBookingApi";
import { amenitiesApi } from "../../features/building-management/amenitiesApi";
import { amenityPackageApi } from "../../features/amenity-booking/amenityPackageApi";
import { residentsApi } from "../../features/residents/residentsApi";
import QRPaymentModal from "../../components/QRPaymentModal";
import Webcam from "react-webcam";
import { Select } from "antd";
import apiClient from "../../lib/apiClient";
import { useLocation } from "react-router-dom";

const { Title, Text } = Typography;
const { TextArea } = Input;

/**
 * Chuyển đổi payload từ API thành mảng items
 * Hỗ trợ nhiều định dạng response khác nhau (items, data, results, etc.)
 */
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

/**
 * Chuyển đổi data URL (base64) từ webcam thành File object
 * Dùng để upload ảnh khuôn mặt chụp từ webcam lên server
 */
const dataURLtoFile = (dataUrl, fileName) => {
    //"data:image/jpeg;base64,/9j/4AAQSkZJRg..."
    const arr = dataUrl.split(",");
    const mime = arr[0].match(/:(.*?);/)[1];
    //Decode base64 thành binary string
    const bstr = atob(arr[1]);
    let n = bstr.length;
    //Tạo một mảng Uint8Array với độ dài bằng độ dài của binary string
    const u8arr = new Uint8Array(n);
    while (n--) {
        u8arr[n] = bstr.charCodeAt(n);
    }
    return new File([u8arr], fileName, { type: mime });
};

const FaceCheckIn = () => {
    const location = useLocation();
    const [residentsLoading, setResidentsLoading] = useState(false);
    const [residents, setResidents] = useState([]);
    const [residentsPagination, setResidentsPagination] = useState({
        current: 1,
        pageSize: 10,
        total: 0,
    });

    const [historyLoading, setHistoryLoading] = useState(false);
    const [history, setHistory] = useState([]);
    const [historyPagination, setHistoryPagination] = useState({
        current: 1,
        pageSize: 10,
        total: 0,
    });

    const [checkInModalOpen, setCheckInModalOpen] = useState(false);
    const [selectedBooking, setSelectedBooking] = useState(null);
    const [fileList, setFileList] = useState([]);
    const [manualOverride, setManualOverride] = useState(false);
    const [skipVerification, setSkipVerification] = useState(false);
    const [notes, setNotes] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [isWebcamOpen, setIsWebcamOpen] = useState(false);
    const webcamRef = useRef(null);
    const [quickScanModalOpen, setQuickScanModalOpen] = useState(false);
    const quickScanWebcamRef = useRef(null);
    const [quickScanLoading, setQuickScanLoading] = useState(false);
    const [quickScanResult, setQuickScanResult] = useState(null);
    const [quickScanImage, setQuickScanImage] = useState("");
    const [scanAmenityId, setScanAmenityId] = useState(null);
    const [detailModalOpen, setDetailModalOpen] = useState(false);
    const [selectedResident, setSelectedResident] = useState(null);

    // State cho phần đăng ký tiện ích
    const [amenities, setAmenities] = useState([]);
    const [amenitiesLoading, setAmenitiesLoading] = useState(false);
    const [packages, setPackages] = useState([]);
    const [packagesLoading, setPackagesLoading] = useState(false);
    const [bookingModalOpen, setBookingModalOpen] = useState(false);
    const [bookingForm, setBookingForm] = useState({
        userId: null,
        amenityId: null,
        packageId: null,
        notes: "",
    });
    const [bookingSubmitting, setBookingSubmitting] = useState(false);
    const [calculatedPrice, setCalculatedPrice] = useState(null);
    const [calculatedDates, setCalculatedDates] = useState({
        startDate: null,
        endDate: null,
    });

    // Mở modal Quét nhanh nếu có query ?quickScan=true
    useEffect(() => {
        const searchParams = new URLSearchParams(location.search);
        const quickScan = searchParams.get("quickScan");
        if (quickScan && quickScan.toLowerCase() === "true") {
            setQuickScanModalOpen(true);
            setQuickScanResult(null);
            setQuickScanImage("");
            setScanAmenityId(null);
        }
    }, [location.search]);

    // State cho danh sách tất cả cư dân
    const [allResidents, setAllResidents] = useState([]);
    const [allResidentsLoading, setAllResidentsLoading] = useState(false);

    // State để lưu thông tin chi tiết của cư dân được chọn (có thông tin hasFaceRegistered)
    const [selectedResidentDetail, setSelectedResidentDetail] = useState(null);
    const [loadingResidentDetail, setLoadingResidentDetail] = useState(false);

    // State cho modal xem ảnh khuôn mặt
    const [faceImageModalOpen, setFaceImageModalOpen] = useState(false);
    const [faceImageUrl, setFaceImageUrl] = useState(null);

    // State cho modal cập nhật khuôn mặt
    const [updateFaceModalOpen, setUpdateFaceModalOpen] = useState(false);
    const [updatingFaceUserId, setUpdatingFaceUserId] = useState(null);
    const [updateFaceImageFile, setUpdateFaceImageFile] = useState(null);
    const [updateFacePreviewUrl, setUpdateFacePreviewUrl] = useState("");
    const [updateFaceImageError, setUpdateFaceImageError] = useState("");
    const [updateFaceSubmitting, setUpdateFaceSubmitting] = useState(false);

    const selectedResidentInfo = useMemo(() => {
        if (!bookingForm.userId) return null;
        // Ưu tiên dùng selectedResidentDetail (đã fetch từ API)
        if (selectedResidentDetail) {
            return selectedResidentDetail;
        }
        // Ưu tiên tìm trong danh sách residents (đã đăng ký tiện ích) vì có thông tin hasFaceRegistered chính xác hơn
        const fromRegistered = residents.find(
            (r) => r.userId === bookingForm.userId || r.residentId === bookingForm.userId
        );
        if (fromRegistered) {
            return fromRegistered;
        }
        // Nếu không tìm thấy trong residents, tìm trong allResidents
        const fromAll = allResidents.find((r) => r.userId === bookingForm.userId);
        return fromAll || null;
    }, [allResidents, bookingForm.userId, residents, selectedResidentDetail]);

    const residentHasFaceRegistered = !!selectedResidentInfo?.hasFaceRegistered;

    // State cho đăng ký khuôn mặt
    const [faceImageFile, setFaceImageFile] = useState(null);
    const [facePreviewUrl, setFacePreviewUrl] = useState("");
    const [faceImageError, setFaceImageError] = useState("");
    const [isFaceWebcamOpen, setIsFaceWebcamOpen] = useState(false);
    const faceWebcamRef = useRef(null);
    const faceWebcamStreamRef = useRef(null); // Lưu stream reference để stop dễ dàng hơn

    // State cho QR Payment
    const [showQRPayment, setShowQRPayment] = useState(false);
    const [paymentData, setPaymentData] = useState(null);

    /**
     * Tải lại danh sách cư dân đã đăng ký tiện ích
     */
    const reloadResidents = () => {
        fetchResidents(residentsPagination.current, residentsPagination.pageSize);
    };

    /**
     * Tải lại lịch sử check-in
     */
    const reloadHistory = () => {
        fetchHistory(historyPagination.current, historyPagination.pageSize);
    };

    useEffect(() => {
        fetchResidents(residentsPagination.current, residentsPagination.pageSize);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [residentsPagination.current, residentsPagination.pageSize]);

    useEffect(() => {
        fetchHistory(historyPagination.current, historyPagination.pageSize);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [historyPagination.current, historyPagination.pageSize]);

    useEffect(() => {
        fetchAmenities();
        fetchAllResidents();
    }, []);

    // Cleanup: Tắt webcam khi component unmount
    useEffect(() => {
        return () => {
            // Stop tất cả webcam streams khi component unmount
            stopFaceWebcamStream();
            if (webcamRef.current?.video?.srcObject) {
                const stream = webcamRef.current.video.srcObject;
                if (stream) {
                    stream.getTracks().forEach(track => track.stop());
                }
            }
            setIsFaceWebcamOpen(false);
            setIsWebcamOpen(false);
        };
    }, []);

    /**
     * Dừng webcam stream và giải phóng tài nguyên
     * Dùng để cleanup khi đóng modal hoặc component unmount
     */
    const stopFaceWebcamStream = () => {
        try {
            // Stop stream từ ref nếu có
            if (faceWebcamStreamRef.current) {
                faceWebcamStreamRef.current.getTracks().forEach(track => {
                    track.stop();
                    track.enabled = false;
                });
                faceWebcamStreamRef.current = null;
            }
            // Stop stream từ video element nếu có
            if (faceWebcamRef.current?.video?.srcObject) {
                const stream = faceWebcamRef.current.video.srcObject;
                if (stream) {
                    stream.getTracks().forEach(track => {
                        track.stop();
                        track.enabled = false;
                    });
                }
                // Clear srcObject
                if (faceWebcamRef.current.video) {
                    faceWebcamRef.current.video.srcObject = null;
                }
            }
            // Đảm bảo state được set về false
            setIsFaceWebcamOpen(false);
        } catch (error) {
            console.error("Error stopping webcam stream:", error);
        }
    };

    // Cleanup webcam khi isFaceWebcamOpen thay đổi thành false
    useEffect(() => {
        if (!isFaceWebcamOpen) {
            // Sử dụng setTimeout nhỏ để đảm bảo component đã unmount
            const timeoutId = setTimeout(() => {
                stopFaceWebcamStream();
            }, 50);
            return () => clearTimeout(timeoutId);
        }
    }, [isFaceWebcamOpen]);

    // Stop webcam khi modal đóng
    useEffect(() => {
        if (!bookingModalOpen && !updateFaceModalOpen) {
            // Stop webcam khi cả hai modal đều đóng
            setIsFaceWebcamOpen(false);
            // Sử dụng setTimeout để đảm bảo component đã unmount
            const timeoutId = setTimeout(() => {
                stopFaceWebcamStream();
            }, 100);
            return () => clearTimeout(timeoutId);
        }
    }, [bookingModalOpen, updateFaceModalOpen]);

    /**
     * Lấy danh sách tất cả cư dân trong hệ thống
     * Dùng để hiển thị trong dropdown chọn cư dân khi đăng ký tiện ích
     */
    const fetchAllResidents = async () => {
        try {
            setAllResidentsLoading(true);
            const data = await residentsApi.getAll();
            setAllResidents(Array.isArray(data) ? data : []);
        } catch (error) {
            console.error("Error loading all residents:", error);
            message.error("Không thể tải danh sách cư dân.");
        } finally {
            setAllResidentsLoading(false);
        }
    };

    /**
     * Lấy danh sách tất cả tiện ích đang hoạt động
     * Chỉ lấy những tiện ích có status ACTIVE và chưa bị xóa
     */
    const fetchAmenities = async () => {
        try {
            setAmenitiesLoading(true);
            const data = await amenitiesApi.getAll();
            const validAmenities = data.filter((amenity) => !amenity.isDelete && amenity.status === "ACTIVE");
            setAmenities(validAmenities);
        } catch (error) {
            console.error("Error loading amenities:", error);
            message.error("Không thể tải danh sách tiện ích.");
        } finally {
            setAmenitiesLoading(false);
        }
    };

    /**
     * Xử lý khi người dùng chọn tiện ích
     * Tự động load danh sách gói tiện ích tương ứng nếu tiện ích có hỗ trợ gói theo tháng
     */
    const handleAmenityChange = async (amenityId, config) => {
        const presetPackageId =
            config && typeof config === "object" && "presetPackageId" in config
                ? config.presetPackageId
                : null;

        setBookingForm((prev) => ({ ...prev, amenityId, packageId: presetPackageId ?? null }));
        setPackages([]);
        setCalculatedPrice(null);
        setCalculatedDates({ startDate: null, endDate: null });
        clearFaceImage();

        if (!amenityId) return;

        try {
            setPackagesLoading(true);
            const selectedAmenity = amenities.find((a) => a.amenityId === amenityId);
            if (selectedAmenity?.hasMonthlyPackage) {
                const packagesData = await amenityPackageApi.getActiveByAmenityId(amenityId);
                const packagesList = packagesData?.data || packagesData || [];
                setPackages(Array.isArray(packagesList) ? packagesList : []);
                if (presetPackageId && Array.isArray(packagesList)) {
                    const hasPackage = packagesList.some((pkg) => pkg.packageId === presetPackageId);
                    if (hasPackage) {
                        setTimeout(() => handlePackageChange(presetPackageId), 0);
                    }
                }
            }
        } catch (error) {
            console.error("Error loading packages:", error);
            message.error("Không thể tải danh sách gói tiện ích.");
        } finally {
            setPackagesLoading(false);
        }
    };

    /**
     * Xử lý khi người dùng chọn gói tiện ích
     * Tự động tính giá và thời gian sử dụng (ngày bắt đầu, ngày kết thúc) dựa trên số tháng của gói
     */
    const handlePackageChange = (packageId) => {
        setBookingForm((prev) => ({ ...prev, packageId }));
        setCalculatedPrice(null);
        setCalculatedDates({ startDate: null, endDate: null });

        if (!packageId) return;

        const selectedPackage = packages.find((p) => p.packageId === packageId);
        if (selectedPackage) {
            setCalculatedPrice(selectedPackage.price);

            // Tính toán ngày bắt đầu và kết thúc từ package
            if (selectedPackage.monthCount) {
                const today = new Date();
                today.setHours(0, 0, 0, 0);

                // Tính ngày kết thúc: thêm số tháng vào ngày hôm nay
                const endDate = new Date(today);
                const targetMonth = endDate.getMonth() + selectedPackage.monthCount;
                endDate.setMonth(targetMonth);

                // Xử lý trường hợp ngày không hợp lệ (ví dụ: 31/02)
                if (endDate.getDate() !== today.getDate()) {
                    // Nếu ngày thay đổi, đặt về ngày cuối cùng của tháng trước đó
                    endDate.setDate(0);
                }

                setCalculatedDates({
                    startDate: today,
                    endDate: endDate
                });
            }
        }
    };

    /**
     * Mở modal đăng ký/gia hạn tiện ích cho cư dân
     * Tự động điền thông tin nếu có amenityId và packageId (dùng khi gia hạn từ quick scan)
     */
    const openRenewalModal = async ({ userId, amenityId, packageId }) => {
        if (!userId) {
            message.warning("Không xác định được cư dân để gia hạn.");
            return;
        }

        setBookingModalOpen(true);
        setBookingForm({
            userId,
            amenityId: amenityId || null,
            packageId: packageId || null,
            notes: "",
        });
        setCalculatedPrice(null);
        setCalculatedDates({ startDate: null, endDate: null });
        clearFaceImage();

        if (amenityId) {
            await handleAmenityChange(amenityId, { presetPackageId: packageId || null });
        }
    };

    /**
     * Xử lý khi người dùng chọn ảnh khuôn mặt từ máy tính
     * Validate file (phải là ảnh, không quá 5MB) và tạo preview URL
     */
    const handleFaceImageChange = (file) => {
        if (!file) {
            clearFaceImage();
            return;
        }

        if (!file.type?.startsWith("image/")) {
            setFaceImageError("Vui lòng chọn tệp ảnh hợp lệ.");
            return;
        }

        if (file.size > 5 * 1024 * 1024) {
            setFaceImageError("Ảnh vượt quá giới hạn 5MB.");
            return;
        }

        setFaceImageError("");
        if (facePreviewUrl) {
            URL.revokeObjectURL(facePreviewUrl);
        }
        setFaceImageFile(file);
        setFacePreviewUrl(URL.createObjectURL(file));
    };

    /**
     * Xóa ảnh khuôn mặt đã chọn và giải phóng memory (revoke blob URL)
     */
    const clearFaceImage = () => {
        if (facePreviewUrl && facePreviewUrl.startsWith("blob:")) {
            URL.revokeObjectURL(facePreviewUrl);
        }
        setFaceImageFile(null);
        setFacePreviewUrl("");
        setFaceImageError("");
    };

    /**
     * Chụp ảnh từ webcam và chuyển thành File object
     * Tự động tắt webcam sau khi chụp để tiết kiệm tài nguyên
     */
    const handleFaceCameraCapture = () => {
        const imageSrc = faceWebcamRef.current?.getScreenshot();
        if (!imageSrc) {
            message.warning("Không thể chụp ảnh từ webcam. Vui lòng thử lại.");
            return;
        }

        try {
            const file = dataURLtoFile(imageSrc, `face-${Date.now()}.jpg`);
            handleFaceImageChange(file);
            // Stop webcam stream ngay sau khi chụp ảnh
            stopFaceWebcamStream();
            setIsFaceWebcamOpen(false);
            message.success("Đã chụp ảnh từ webcam.");
        } catch (error) {
            console.error("Error converting webcam image:", error);
            setFaceImageError("Không thể xử lý ảnh từ webcam.");
        }
    };

    /**
     * Đăng ký/cập nhật ảnh khuôn mặt cho cư dân
     * Gửi ảnh lên server qua API FaceRecognition/register
     * @returns {boolean} true nếu thành công, false nếu thất bại
     */
    const handleRegisterFace = async (userId) => {
        if (!faceImageFile) {
            message.warning("Vui lòng chọn hoặc chụp ảnh khuôn mặt");
            return;
        }

        try {
            const formData = new FormData();
            formData.append("UserId", userId.toString());
            formData.append("Image", faceImageFile);

            const response = await apiClient.post("/FaceRecognition/register", formData, {
                headers: {
                    "Content-Type": "multipart/form-data",
                },
            });

            if (response.data?.success) {
                message.success("Đã đăng ký khuôn mặt thành công.");
                clearFaceImage();
                return true;
            } else {
                message.error(response.data?.message || "Đăng ký khuôn mặt thất bại.");
                return false;
            }
        } catch (error) {
            const apiMessage =
                error.response?.data?.message ||
                error.response?.data?.error ||
                error.message;
            message.error(apiMessage || "Không thể đăng ký khuôn mặt.");
            return false;
        }
    };

    /**
     * Tạo đăng ký tiện ích mới cho cư dân
     * Tự động đăng ký khuôn mặt nếu cần (khi tiện ích yêu cầu xác thực khuôn mặt)
     * Hiển thị QR Payment modal nếu có phí thanh toán
     */
    const handleCreateBooking = async () => {
        if (!bookingForm.userId) {
            message.warning("Vui lòng chọn cư dân");
            return;
        }
        if (!bookingForm.amenityId) {
            message.warning("Vui lòng chọn tiện ích");
            return;
        }
        if (!bookingForm.packageId) {
            message.warning("Vui lòng chọn gói tiện ích");
            return;
        }

        const selectedAmenity = amenities.find((a) => a.amenityId === bookingForm.amenityId);
        const needsFaceRegistration =
            selectedAmenity?.requiresFaceVerification && !residentHasFaceRegistered;

        // Chỉ bắt buộc upload ảnh nếu chưa đăng ký khuôn mặt
        if (needsFaceRegistration && !faceImageFile) {
            message.warning("Tiện ích này yêu cầu đăng ký khuôn mặt. Vui lòng chụp hoặc tải ảnh khuôn mặt.");
            setFaceImageError("Vui lòng chụp hoặc tải ảnh khuôn mặt để tiếp tục.");
            return;
        }

        try {
            setBookingSubmitting(true);

            // Nếu có ảnh khuôn mặt mới (chưa đăng ký hoặc muốn cập nhật), đăng ký/cập nhật trước
            if (faceImageFile) {
                const faceRegistered = await handleRegisterFace(bookingForm.userId);
                if (!faceRegistered) {
                    return;
                }
            }

            const result = await amenityBookingApi.receptionistCreateForResident({
                userId: bookingForm.userId,
                amenityId: bookingForm.amenityId,
                packageId: bookingForm.packageId,
                notes: bookingForm.notes?.trim() || undefined,
            });

            if (result?.success === false) {
                message.error(result?.message || "Tạo đăng ký không thành công.");
            } else {
                const bookingId = result?.data?.bookingId || result?.data?.data?.bookingId || result?.bookingId;
                const selectedPackage = packages.find((p) => p.packageId === bookingForm.packageId);

                // Lấy giá từ nhiều nguồn: ưu tiên từ response backend, sau đó từ state, sau đó từ package
                const priceFromResponse = result?.data?.totalPrice || result?.data?.price || result?.data?.data?.totalPrice || result?.data?.data?.price;
                const finalPrice = priceFromResponse || calculatedPrice || selectedPackage?.price || 0;

                // Hiển thị QR Payment Modal nếu có giá > 0 và có bookingId
                if (finalPrice > 0 && bookingId) {
                    const payment = {
                        totalPrice: finalPrice,
                        amenityName: selectedAmenity?.name || "",
                        timeInfo: `Từ ngày đăng ký, ${selectedPackage?.monthCount || ""} tháng`,
                        packageName: selectedPackage?.name || "Gói tháng",
                        description: `Thanh toán đăng ký tiện ích ${selectedAmenity?.name || ""} - ${selectedPackage?.name || ""}`,
                        bookingId: bookingId,
                    };
                    setPaymentData(payment);
                    setShowQRPayment(true);
                } else {
                    message.success(result?.message || "Đã tạo đăng ký tiện ích thành công.");
                }

                // Tắt webcam trước khi đóng modal
                setIsFaceWebcamOpen(false);
                setBookingModalOpen(false);
                setBookingForm({
                    userId: null,
                    amenityId: null,
                    packageId: null,
                    notes: "",
                });
                setCalculatedPrice(null);
                setCalculatedDates({ startDate: null, endDate: null });
                clearFaceImage();
                setSelectedResidentDetail(null);
                reloadResidents();
            }
        } catch (error) {
            const apiMessage =
                error.response?.data?.message ||
                error.response?.data?.error ||
                error.message;
            message.error(apiMessage || "Không thể tạo đăng ký tiện ích.");
        } finally {
            setBookingSubmitting(false);
        }
    };


    /**
     * Lấy danh sách cư dân đã đăng ký tiện ích
     * Chỉ lấy những cư dân đã đăng ký khuôn mặt (hasFaceRegistered = true)
     * Áp dụng phân trang thủ công sau khi filter
     */
    const fetchResidents = async (pageNumber = 1, pageSize = 10) => {
        try {
            setResidentsLoading(true);
            // Lấy tất cả để filter theo hasFaceRegistered
            const data = await amenityBookingApi.getRegisteredResidents({
                pageNumber: 1,
                pageSize: 10000, // Lấy tất cả để filter
                sortBy: "TotalBookings",
                sortOrder: "desc",
            });

            let list = coerceItems(data);
            // Chỉ lấy những cư dân đã đăng ký khuôn mặt
            list = list.filter((resident) => resident.hasFaceRegistered === true);

            // Áp dụng phân trang thủ công sau khi filter
            const startIndex = (pageNumber - 1) * pageSize;
            const endIndex = startIndex + pageSize;
            const paginatedList = list.slice(startIndex, endIndex);

            setResidents(paginatedList);
            setResidentsPagination((prev) => ({
                ...prev,
                current: pageNumber,
                pageSize,
                total: list.length, // Tổng số sau khi filter
            }));
        } catch (error) {
            console.error("Error loading registered residents:", error);
            message.error("Không thể tải danh sách cư dân đã đăng ký tiện ích.");
        } finally {
            setResidentsLoading(false);
        }
    };

    /**
     * Lấy lịch sử check-in của cư dân
     * Hiển thị thông tin: cư dân, tiện ích, thời gian, độ khớp khuôn mặt, kết quả
     */
    const fetchHistory = async (pageNumber = 1, pageSize = 10) => {
        try {
            setHistoryLoading(true);
            const data = await amenityBookingApi.getCheckInHistory({
                pageNumber,
                pageSize,
            });

            const list = coerceItems(data);
            const total = data?.totalCount ?? data?.total ?? list.length ?? 0;
            setHistory(list);
            setHistoryPagination((prev) => ({
                ...prev,
                current: pageNumber,
                pageSize,
                total: Number(total),
            }));
        } catch (error) {
            console.error("Error loading face check-in history:", error);
            message.error("Không thể tải lịch sử check-in.");
        } finally {
            setHistoryLoading(false);
        }
    };

    /**
     * Xử lý khi người dùng thay đổi phân trang trong bảng cư dân
     */
    const handleResidentsTableChange = (pagination) => {
        const { current, pageSize } = pagination;
        setResidentsPagination((prev) => ({
            ...prev,
            current,
            pageSize,
        }));
    };

    /**
     * Xử lý khi người dùng thay đổi phân trang trong bảng lịch sử check-in
     */
    const handleHistoryTableChange = (pagination) => {
        const { current, pageSize } = pagination;
        setHistoryPagination((prev) => ({
            ...prev,
            current,
            pageSize,
        }));
    };

    /**
     * Mở modal check-in cho booking được chọn
     * Reset tất cả state liên quan đến check-in (ảnh, ghi chú, override, etc.)
     * Giữ lại function này để có thể sử dụng trong tương lai hoặc từ modal chi tiết
     */
    // eslint-disable-next-line no-unused-vars
    const openCheckInModal = (booking) => {
        setSelectedBooking(booking);
        setFileList([]);
        setManualOverride(false);
        setSkipVerification(false);
        setNotes("");
        setIsWebcamOpen(false);
        setCheckInModalOpen(true);
    };

    /**
     * Đóng modal check-in và reset tất cả state
     */
    const closeCheckInModal = () => {
        setCheckInModalOpen(false);
        setSelectedBooking(null);
        setFileList([]);
        setManualOverride(false);
        setSkipVerification(false);
        setNotes("");
        setIsWebcamOpen(false);
    };

    /**
     * Chụp ảnh từ webcam trong modal check-in
     * Chuyển ảnh thành File object và thêm vào fileList để upload
     */
    const handleCaptureFromWebcam = () => {
        const imageSrc = webcamRef.current?.getScreenshot();
        if (!imageSrc) {
            message.warning("Không thể chụp ảnh từ webcam. Vui lòng thử lại.");
            return;
        }

        try {
            const file = dataURLtoFile(imageSrc, `face-${Date.now()}.jpg`);
            const uploadFile = {
                uid: `${Date.now()}`,
                name: file.name,
                status: "done",
                originFileObj: file,
                url: imageSrc,
            };
            setFileList([uploadFile]);
            setIsWebcamOpen(false);
            message.success("Đã chụp ảnh từ webcam.");
        } catch (error) {
            console.error("Error capturing webcam image:", error);
            message.error("Không thể xử lý ảnh từ webcam.");
        }
    };

    /**
     * Xử lý submit check-in
     * Gửi ảnh khuôn mặt lên server để xác thực và check-in
     * Hỗ trợ manual override và skip verification nếu cần
     */
    const handleCheckInSubmit = async () => {
        if (!selectedBooking) {
            message.warning("Vui lòng chọn booking cần check-in.");
            return;
        }

        const file = fileList[0]?.originFileObj || fileList[0];
        if (!skipVerification && !file) {
            message.warning("Vui lòng tải ảnh khuôn mặt hoặc bật chế độ bỏ qua xác thực.");
            return;
        }

        try {
            setSubmitting(true);
            const payload = {
                faceImage: file ?? undefined,
                manualOverride,
                skipFaceVerification: skipVerification,
                notes: notes?.trim() || undefined,
            };

            const result = await amenityBookingApi.receptionistCheckIn(
                selectedBooking.bookingId,
                payload
            );

            if (result?.success === false) {
                message.error(result?.message || "Check-in không thành công.");
            } else {
                message.success(result?.message || "Đã check-in thành công.");
                closeCheckInModal();
                reloadResidents();
                reloadHistory();
            }
        } catch (error) {
            const apiMessage =
                error.response?.data?.message ||
                error.response?.data?.error ||
                error.message;
            message.error(apiMessage || "Không thể thực hiện check-in.");
        } finally {
            setSubmitting(false);
        }
    };

    const uploadProps = {
        accept: "image/*",
        maxCount: 1,
        fileList,
        onChange: ({ fileList: newFileList }) => {
            setFileList(newFileList.slice(-1));
        },
        beforeUpload: (file) => {
            setFileList([file]);
            return false;
        },
        onRemove: () => {
            setFileList([]);
        },
    };

    /**
     * Xử lý quét nhanh khuôn mặt cư dân
     * Chụp ảnh từ webcam, gửi lên server để nhận diện và tự động check-in nếu tìm thấy booking hợp lệ
     * Hiển thị kết quả nhận diện (thành công/thất bại, độ khớp, thông tin booking)
     */
    const handleQuickScan = async () => {
        if (!scanAmenityId) {
            message.warning("Vui lòng chọn tiện ích trước khi quét.");
            return;
        }
        const imageSrc = quickScanWebcamRef.current?.getScreenshot();
        if (!imageSrc) {
            message.warning("Không thể chụp ảnh từ webcam. Vui lòng thử lại.");
            return;
        }

        try {
            setQuickScanLoading(true);
            const file = dataURLtoFile(imageSrc, `scan-${Date.now()}.jpg`);
            setQuickScanImage(imageSrc);
            const response = await amenityBookingApi.receptionistScan({
                faceImage: file,
                amenityId: scanAmenityId,
            });

            const payload = response ?? {};
            const result = payload?.data ?? null;

            if (!payload?.success) {
                message.warning(
                    payload?.message || result?.message || "Không thể nhận diện cư dân."
                );
                setQuickScanResult(result ?? null);
                return;
            }

            setQuickScanResult(result ?? null);
            message.success(payload?.message || "Đã nhận diện và check-in thành công.");
            reloadResidents();
            reloadHistory();
        } catch (error) {
            const apiMessage =
                error.response?.data?.message ||
                error.response?.data?.error ||
                error.message;
            message.error(apiMessage || "Không thể nhận diện cư dân.");
            setQuickScanResult(null);
        } finally {
            setQuickScanLoading(false);
        }
    };

    /**
     * Gia hạn tiện ích từ kết quả quick scan
     * Đóng modal quick scan và mở modal đăng ký với thông tin đã điền sẵn
     */
    const handleRenewFromQuickScan = async () => {
        const bookingInfo = quickScanResult?.booking;
        const userId =
            quickScanResult?.userId ||
            quickScanResult?.residentId ||
            bookingInfo?.userId ||
            bookingInfo?.residentId;
        const amenityId = bookingInfo?.amenityId || scanAmenityId;
        const packageId = bookingInfo?.packageId || null;

        if (!userId) {
            message.warning("Không xác định được cư dân để gia hạn.");
            return;
        }
        if (!amenityId) {
            message.warning("Vui lòng chọn tiện ích trước khi gia hạn.");
            return;
        }

        setQuickScanModalOpen(false);
        setQuickScanResult(null);
        setQuickScanImage("");
        await openRenewalModal({ userId, amenityId, packageId });
    };


    /**
     * Format hiển thị khoảng thời gian của booking (ngày bắt đầu → ngày kết thúc)
     */
    const renderDateRange = (booking) => {
        const start = booking.startDate
            ? dayjs(booking.startDate).format("DD/MM/YYYY")
            : "—";
        const end = booking.endDate
            ? dayjs(booking.endDate).format("DD/MM/YYYY")
            : "—";
        return `${start} → ${end}`;
    };

    /**
     * Render tag hiển thị trạng thái check-in với màu sắc tương ứng
     * Thành công (xanh), Thất bại (đỏ), Override (cam), Bỏ qua xác thực (xanh dương)
     */
    const renderCheckInStatusTag = (record) => {
        if (!record) return null;
        if (!record.isSuccess) {
            return <Tag color="red">Thất bại</Tag>;
        }
        if (record.resultStatus === "ManualOverride") {
            return <Tag color="orange">Override</Tag>;
        }
        if (record.resultStatus === "SkippedVerification") {
            return <Tag color="blue">Bỏ qua xác thực</Tag>;
        }
        return <Tag color="green">Thành công</Tag>;
    };

    const residentsColumns = useMemo(
        () => [
            {
                title: "Cư dân",
                key: "resident",
                render: (_, record) => (
                    <Space>
                        {record.avatarUrl && (
                            <img
                                src={record.avatarUrl}
                                alt={record.fullName}
                                style={{
                                    width: 32,
                                    height: 32,
                                    borderRadius: "50%",
                                    objectFit: "cover",
                                }}
                            />
                        )}
                        <Space direction="vertical" size={0}>
                            <Text strong>{record.fullName || "—"}</Text>
                            <Text type="secondary" style={{ fontSize: 12 }}>
                                {record.username || record.email || "—"}
                            </Text>
                        </Space>
                    </Space>
                ),
            },
            {
                title: "Căn hộ",
                dataIndex: "apartmentCode",
                key: "apartmentCode",
                render: (value) => value || "—",
            },
            {
                title: "Đã đăng ký khuôn mặt",
                key: "hasFace",
                render: (_, record) => (
                    record.hasFaceRegistered ? (
                        <Tag color="green">Đã đăng ký</Tag>
                    ) : (
                        <Tag color="red">Chưa đăng ký</Tag>
                    )
                ),
            },
            {
                title: "Hành động",
                key: "actions",
                fixed: "right",
                render: (_, record) => (
                    <Button
                        type="primary"
                        icon={<EyeOutlined />}
                        onClick={() => {
                            setSelectedResident(record);
                            setDetailModalOpen(true);
                        }}
                    >
                        Xem chi tiết
                    </Button>
                ),
            },
        ],
        []
    );

    const historyColumns = useMemo(
        () => [
            {
                title: "Cư dân",
                dataIndex: "checkedInForFullName",
                key: "checkedInForFullName",
                render: (value, record) => (
                    <Space direction="vertical" size={0}>
                        <Text strong>{value || "—"}</Text>
                        <Text type="secondary" style={{ fontSize: 12 }}>
                            {record.apartmentCode || "—"}
                        </Text>
                    </Space>
                ),
            },
            {
                title: "Tiện ích",
                dataIndex: "amenityName",
                key: "amenityName",
                render: (value) => value || "—",
            },
            {
                title: "Thời gian",
                dataIndex: "checkedInAt",
                key: "checkedInAt",
                render: (value) =>
                    value ? dayjs(value).format("DD/MM/YYYY HH:mm") : "—",
            },
            {
                title: "Độ khớp",
                dataIndex: "similarity",
                key: "similarity",
                render: (value) =>
                    typeof value === "number" ? `${(value * 100).toFixed(1)}%` : "—",
            },
            {
                title: "Kết quả",
                key: "resultStatus",
                render: (_, record) => renderCheckInStatusTag(record),
            },
            {
                title: "Lễ tân",
                dataIndex: "checkedInByFullName",
                key: "checkedInByFullName",
                render: (value) => value || "—",
            },
            {
                title: "Ghi chú",
                dataIndex: "message",
                key: "message",
                ellipsis: true,
                render: (value) =>
                    value ? (
                        <Tooltip title={value}>
                            <Text>{value}</Text>
                        </Tooltip>
                    ) : (
                        "—"
                    ),
            },
        ],
        []
    );

    const selectedBookingInfo = useMemo(() => {
        if (!selectedBooking) return null;
        return {
            resident:
                selectedBooking.residentName ||
                selectedBooking.userName ||
                selectedBooking.userId,
            amenity: selectedBooking.amenityName,
            apartment: selectedBooking.apartmentCode,
            packageName: selectedBooking.packageName,
            monthCount: selectedBooking.monthCount,
            dateRange: renderDateRange(selectedBooking),
        };
    }, [selectedBooking]);

    return (
        <div className="p-6 space-y-4">
            <Card
                title={
                    <Space>
                        <CameraOutlined />
                        <span>Đăng ký tiện ích cho cư dân</span>
                    </Space>
                }
                extra={
                    <Button
                        type="primary"
                        onClick={() => {
                            setBookingModalOpen(true);
                            setBookingForm({
                                userId: null,
                                amenityId: null,
                                packageId: null,
                                notes: "",
                            });
                            setCalculatedPrice(null);
                            setPackages([]);
                        }}
                    >
                        Đăng ký mới
                    </Button>
                }
            >
                <Alert
                    message="Lễ tân có thể đăng ký tiện ích cho cư dân tại đây"
                    type="info"
                    showIcon
                    style={{ marginBottom: 16 }}
                />
            </Card>

            <Card
                title={
                    <Space>
                        <CameraOutlined />
                        <span>Cư dân đã đăng ký tiện ích</span>
                    </Space>
                }
                extra={
                    <Space>
                        <Button
                            icon={<VideoCameraOutlined />}
                            onClick={() => {
                                setQuickScanModalOpen(true);
                                setQuickScanResult(null);
                                setQuickScanImage("");
                                setScanAmenityId(null);
                            }}
                        >
                            Quét nhanh
                        </Button>
                        <Button
                            icon={<ReloadOutlined />}
                            onClick={reloadResidents}
                            loading={residentsLoading}
                        >
                            Tải lại
                        </Button>
                    </Space>
                }
            >
                <Table
                    rowKey="userId"
                    dataSource={residents}
                    columns={residentsColumns}
                    loading={residentsLoading}
                    pagination={{
                        current: residentsPagination.current,
                        pageSize: residentsPagination.pageSize,
                        total: residentsPagination.total,
                        showSizeChanger: true,
                        showTotal: (total, range) =>
                            `${range[0]}-${range[1]} trong ${total} cư dân`,
                    }}
                    onChange={handleResidentsTableChange}
                    scroll={{ x: 800 }}
                />
            </Card>

            <Card
                title={
                    <Space>
                        <HistoryOutlined />
                        <span>Lịch sử check-in</span>
                    </Space>
                }
                extra={
                    <Button
                        icon={<ReloadOutlined />}
                        onClick={reloadHistory}
                        loading={historyLoading}
                    >
                        Tải lại
                    </Button>
                }
            >
                <Table
                    rowKey="checkInId"
                    dataSource={history}
                    columns={historyColumns}
                    loading={historyLoading}
                    pagination={{
                        current: historyPagination.current,
                        pageSize: historyPagination.pageSize,
                        total: historyPagination.total,
                        showSizeChanger: true,
                        showTotal: (total, range) =>
                            `${range[0]}-${range[1]} trong ${total} lượt check-in`,
                    }}
                    onChange={handleHistoryTableChange}
                    scroll={{ x: 900 }}
                />
            </Card>

            <Modal
                title="Thực hiện check-in"
                open={checkInModalOpen}
                onCancel={closeCheckInModal}
                onOk={handleCheckInSubmit}
                okText="Xác nhận"
                cancelText="Hủy"
                confirmLoading={submitting}
            >
                {selectedBookingInfo ? (
                    <Space direction="vertical" style={{ width: "100%" }} size="large">
                        <Descriptions
                            size="small"
                            bordered
                            column={1}
                            labelStyle={{ width: 140 }}
                        >
                            <Descriptions.Item label="Cư dân">
                                {selectedBookingInfo.resident || "—"}
                            </Descriptions.Item>
                            <Descriptions.Item label="Tiện ích">
                                {selectedBookingInfo.amenity || "—"}
                            </Descriptions.Item>
                            <Descriptions.Item label="Căn hộ">
                                {selectedBookingInfo.apartment || "—"}
                            </Descriptions.Item>
                            <Descriptions.Item label="Gói tiện ích">
                                {selectedBookingInfo.packageName || "—"}
                            </Descriptions.Item>
                            <Descriptions.Item label="Thời hạn">
                                {selectedBookingInfo.dateRange}
                            </Descriptions.Item>
                        </Descriptions>

                        <Divider className="my-2" />

                        <div>
                            <Title level={5}>Ảnh khuôn mặt</Title>
                            <Text type="secondary">
                                Tải ảnh chụp trực tiếp từ camera hoặc thư viện. Nếu ảnh không
                                rõ, hãy chụp lại để tăng độ chính xác.
                            </Text>
                            <Space className="mt-3" wrap>
                                <Button
                                    icon={<VideoCameraOutlined />}
                                    onClick={() => setIsWebcamOpen((prev) => !prev)}
                                >
                                    {isWebcamOpen ? "Đóng webcam" : "Mở webcam"}
                                </Button>
                                {isWebcamOpen && (
                                    <Button
                                        type="primary"
                                        icon={<CameraOutlined />}
                                        onClick={handleCaptureFromWebcam}
                                    >
                                        Chụp ảnh
                                    </Button>
                                )}
                            </Space>
                            {isWebcamOpen && (
                                <Card className="mt-3">
                                    <Card.Body
                                        style={{
                                            display: "flex",
                                            flexDirection: "column",
                                            alignItems: "center",
                                            gap: "12px",
                                        }}
                                    >
                                        <Webcam
                                            ref={webcamRef}
                                            audio={false}
                                            screenshotFormat="image/jpeg"
                                            mirrored
                                            videoConstraints={{
                                                width: 480,
                                                facingMode: "user",
                                            }}
                                            style={{
                                                width: "100%",
                                                maxWidth: 480,
                                                borderRadius: 12,
                                            }}
                                        />
                                        <Text type="secondary">
                                            Đảm bảo khuôn mặt nằm giữa khung hình và đủ sáng.
                                        </Text>
                                    </Card.Body>
                                </Card>
                            )}
                            <Upload {...uploadProps} className="mt-3" listType="picture">
                                <Button icon={<CameraOutlined />}>Tải ảnh khuôn mặt</Button>
                            </Upload>
                        </div>

                        <div>
                            <Title level={5}>Tùy chọn nâng cao</Title>
                            <Space direction="vertical" style={{ width: "100%" }}>
                                <Space>
                                    <Switch
                                        checked={skipVerification}
                                        onChange={setSkipVerification}
                                    />
                                    <Text>Bỏ qua xác thực khuôn mặt</Text>
                                </Space>
                                <Space>
                                    <Switch
                                        checked={manualOverride}
                                        onChange={setManualOverride}
                                    />
                                    <Text>
                                        Áp dụng override (chấp nhận ngay cả khi xác thực thất bại)
                                    </Text>
                                </Space>
                            </Space>
                        </div>

                        <div>
                            <Title level={5}>Ghi chú</Title>
                            <TextArea
                                rows={3}
                                maxLength={500}
                                value={notes}
                                onChange={(e) => setNotes(e.target.value)}
                                placeholder="Ghi chú thêm (tuỳ chọn)"
                            />
                        </div>

                        {!skipVerification && !fileList.length ? (
                            <Tag icon={<ExclamationCircleOutlined />} color="warning">
                                Vui lòng tải ảnh trước khi xác nhận (hoặc bật bỏ qua xác thực)
                            </Tag>
                        ) : null}
                    </Space>
                ) : (
                    <Text type="secondary">
                        Chọn một booking trong danh sách để bắt đầu check-in.
                    </Text>
                )}
            </Modal>

            <Modal
                title="Quét nhanh cư dân"
                open={quickScanModalOpen}
                onCancel={() => {
                    setQuickScanModalOpen(false);
                    setQuickScanResult(null);
                    setQuickScanImage("");
                    setScanAmenityId(null);
                }}
                footer={null}
                width={720}
            >
                <Space direction="vertical" style={{ width: "100%" }} size="large">
                    <div>
                        <Title level={5}>Chọn tiện ích cần quét</Title>
                        <Text type="secondary">
                            Lựa chọn tiện ích giúp hệ thống kiểm tra đúng gói đăng ký tương ứng.
                        </Text>
                        <Select
                            className="mt-2"
                            placeholder="Chọn tiện ích"
                            value={scanAmenityId}
                            onChange={setScanAmenityId}
                            loading={amenitiesLoading}
                            allowClear
                            showSearch
                            optionFilterProp="label"
                            options={amenities
                                .filter((amenity) => amenity.requiresFaceVerification)
                                .map((amenity) => ({
                                    label: amenity.name,
                                    value: amenity.amenityId,
                                }))}
                        />
                        {amenities.filter((a) => a.requiresFaceVerification).length === 0 && (
                            <Alert
                                className="mt-2"
                                type="info"
                                message="Hiện chưa có tiện ích nào yêu cầu xác thực khuôn mặt. Hãy cập nhật danh sách tiện ích nếu cần."
                                showIcon
                            />
                        )}
                    </div>

                    <div>
                        <Title level={5}>Camera</Title>
                        <Text type="secondary">
                            Đặt khuôn mặt cư dân vào giữa khung hình, ánh sáng đầy đủ rồi bấm "Chụp & quét".
                        </Text>
                        <div className="mt-3 d-flex justify-content-center">
                            <Webcam
                                ref={quickScanWebcamRef}
                                audio={false}
                                screenshotFormat="image/jpeg"
                                mirrored
                                videoConstraints={{
                                    width: 640,
                                    facingMode: "user",
                                }}
                                style={{
                                    width: "100%",
                                    maxWidth: 640,
                                    borderRadius: 12,
                                    boxShadow: "0 8px 24px rgba(0,0,0,0.12)",
                                }}
                            />
                        </div>
                        <div className="mt-3 d-flex justify-content-center gap-2">
                            <Button
                                type="primary"
                                icon={<CameraOutlined />}
                                loading={quickScanLoading}
                                onClick={handleQuickScan}
                                disabled={!scanAmenityId}
                            >
                                Chụp & quét
                            </Button>
                            <Button
                                icon={<ReloadOutlined />}
                                onClick={() => {
                                    setQuickScanResult(null);
                                    setQuickScanImage("");
                                }}
                            >
                                Làm mới
                            </Button>
                        </div>
                    </div>

                    {quickScanImage && (
                        <div>
                            <Title level={5}>Ảnh vừa chụp</Title>
                            <img
                                src={quickScanImage}
                                alt="captured face"
                                style={{ width: "100%", maxWidth: 320, borderRadius: 12 }}
                            />
                        </div>
                    )}

                    {quickScanResult && (() => {
                        const bookingInfo = quickScanResult.booking;
                        const bookingExpired = bookingInfo?.endDate
                            ? dayjs().isAfter(dayjs(bookingInfo.endDate))
                            : false;
                        const recognizedUserId =
                            quickScanResult?.userId ||
                            quickScanResult?.residentId ||
                            bookingInfo?.userId ||
                            bookingInfo?.residentId;
                        const canRenew =
                            !!recognizedUserId &&
                            (!quickScanResult.success || bookingExpired || !bookingInfo);

                        return (
                            <Card type="inner" title="Kết quả quét">
                                <Space direction="vertical" style={{ width: "100%" }} size="middle">
                                    <Descriptions bordered size="small" column={1}>
                                        <Descriptions.Item label="Kết quả">
                                            {quickScanResult.success ? (
                                                <Tag color="green">Đã check-in</Tag>
                                            ) : (
                                                <Tag color="red">Không thành công</Tag>
                                            )}
                                        </Descriptions.Item>
                                        <Descriptions.Item label="Cư dân">
                                            {quickScanResult.residentName || "—"}
                                        </Descriptions.Item>
                                        <Descriptions.Item label="Độ khớp">
                                            {typeof quickScanResult.similarity === "number"
                                                ? `${(quickScanResult.similarity * 100).toFixed(1)}%`
                                                : "—"}
                                        </Descriptions.Item>
                                        <Descriptions.Item label="Thông điệp">
                                            {quickScanResult.message || "—"}
                                        </Descriptions.Item>
                                    </Descriptions>

                                    {bookingInfo ? (
                                        <Descriptions bordered size="small" column={1} title="Booking">
                                            <Descriptions.Item label="Tiện ích">
                                                {bookingInfo.amenityName || "—"}
                                            </Descriptions.Item>
                                            <Descriptions.Item label="Thời hạn">
                                                {bookingInfo.startDate
                                                    ? dayjs(bookingInfo.startDate).format("DD/MM/YYYY")
                                                    : "—"}{" "}
                                                →
                                                {" "}
                                                {bookingInfo.endDate
                                                    ? dayjs(bookingInfo.endDate).format("DD/MM/YYYY")
                                                    : "—"}
                                            </Descriptions.Item>
                                            <Descriptions.Item label="Trạng thái">
                                                <Tag color="blue">{bookingInfo.status}</Tag>
                                            </Descriptions.Item>
                                        </Descriptions>
                                    ) : null}

                                    {quickScanResult.checkIn ? (
                                        <Descriptions bordered size="small" column={1} title="Lịch sử check-in">
                                            <Descriptions.Item label="Thời gian">
                                                {dayjs(quickScanResult.checkIn.checkedInAt).format("DD/MM/YYYY HH:mm")}
                                            </Descriptions.Item>
                                            <Descriptions.Item label="Kết quả">
                                                <Tag color={quickScanResult.checkIn.isSuccess ? "green" : "red"}>
                                                    {quickScanResult.checkIn.isSuccess ? "Thành công" : "Thất bại"}
                                                </Tag>
                                            </Descriptions.Item>
                                            {typeof quickScanResult.checkIn.similarity === "number" && (
                                                <Descriptions.Item label="Độ khớp">
                                                    {(quickScanResult.checkIn.similarity * 100).toFixed(1)}%
                                                </Descriptions.Item>
                                            )}
                                        </Descriptions>
                                    ) : null}

                                    {quickScanResult.success && quickScanResult.alreadyCheckedInToday && (
                                        <Alert
                                            type="warning"
                                            showIcon
                                            message="Booking này đã được check-in hôm nay."
                                        />
                                    )}

                                    {!quickScanResult.success && !bookingInfo && (
                                        <Alert
                                            type="warning"
                                            showIcon
                                            message="Không tìm thấy booking còn hiệu lực."
                                        />
                                    )}

                                    {canRenew && (
                                        <Space direction="vertical" style={{ width: "100%" }} size="small">
                                            <Alert
                                                type="warning"
                                                showIcon
                                                message={
                                                    bookingExpired
                                                        ? "Gói tiện ích đã hết hạn. Vui lòng gia hạn để tiếp tục sử dụng."
                                                        : "Gói tiện ích không hợp lệ cho tiện ích này."
                                                }
                                            />
                                            <Button
                                                type="primary"
                                                onClick={handleRenewFromQuickScan}
                                                icon={<CameraOutlined />}
                                            >
                                                Gia hạn gói này
                                            </Button>
                                        </Space>
                                    )}
                                </Space>
                            </Card>
                        );
                    })()}
                </Space>
            </Modal>

            <Modal
                title="Chi tiết đăng ký tiện ích"
                open={detailModalOpen}
                onCancel={() => {
                    setDetailModalOpen(false);
                    setSelectedResident(null);
                }}
                footer={[
                    <Button key="close" onClick={() => {
                        setDetailModalOpen(false);
                        setSelectedResident(null);
                    }}>
                        Đóng
                    </Button>
                ]}
                width={800}
            >
                {selectedResident && (
                    <Space direction="vertical" style={{ width: "100%" }} size="large">
                        <Descriptions
                            bordered
                            size="small"
                            column={2}
                            title="Thông tin cư dân"
                        >
                            <Descriptions.Item label="Họ tên">
                                {selectedResident.fullName || "—"}
                            </Descriptions.Item>
                            <Descriptions.Item label="Username">
                                {selectedResident.username || "—"}
                            </Descriptions.Item>
                            <Descriptions.Item label="Email">
                                {selectedResident.email || "—"}
                            </Descriptions.Item>
                            <Descriptions.Item label="Số điện thoại">
                                {selectedResident.phone || "—"}
                            </Descriptions.Item>
                            <Descriptions.Item label="Căn hộ">
                                {selectedResident.apartmentCode || "—"}
                            </Descriptions.Item>
                            <Descriptions.Item label="Đã đăng ký khuôn mặt">
                                {selectedResident.hasFaceRegistered ? (
                                    <Space direction="vertical" size="small" style={{ width: "100%" }}>
                                        <Tag color="green" style={{ marginBottom: 4 }}>Đã đăng ký</Tag>
                                        <Space>
                                            <Button
                                                type="default"
                                                size="small"
                                                icon={<EyeOutlined />}
                                                onClick={async () => {
                                                    try {
                                                        const residentUserId = selectedResident.userId || selectedResident.id || selectedResident.residentId;
                                                        if (residentUserId) {
                                                            // Lấy URL ảnh từ thông tin resident đã có hoặc fetch lại nếu cần
                                                            let imageUrl = selectedResident?.user?.checkinPhotoUrl
                                                                || selectedResident?.checkinPhotoUrl

                                                            // Nếu chưa có trong selectedResident, fetch lại từ API
                                                            if (!imageUrl) {
                                                                const residentDetail = await residentsApi.getByUserId(residentUserId);
                                                                imageUrl = residentDetail?.user?.checkinPhotoUrl
                                                                    || residentDetail?.checkinPhotoUrl
                                                            }

                                                            if (imageUrl) {
                                                                setFaceImageUrl(imageUrl);
                                                                setFaceImageModalOpen(true);
                                                            } else {
                                                                message.warning("Không tìm thấy ảnh khuôn mặt đã đăng ký.");
                                                            }
                                                        }
                                                    } catch (error) {
                                                        console.error("Error fetching face image:", error);
                                                        message.error("Không thể tải ảnh khuôn mặt.");
                                                    }
                                                }}
                                                style={{ borderRadius: 6 }}
                                            >
                                                Xem khuôn mặt
                                            </Button>
                                            <Button
                                                type="primary"
                                                size="small"
                                                icon={<CameraOutlined />}
                                                onClick={() => {
                                                    const residentUserId = selectedResident.userId || selectedResident.id || selectedResident.residentId;
                                                    if (residentUserId) {
                                                        setUpdatingFaceUserId(residentUserId);
                                                        setUpdateFaceModalOpen(true);
                                                        setUpdateFaceImageFile(null);
                                                        setUpdateFacePreviewUrl("");
                                                        setUpdateFaceImageError("");
                                                    }
                                                }}
                                                style={{ borderRadius: 6 }}
                                            >
                                                Cập nhật
                                            </Button>
                                        </Space>
                                    </Space>
                                ) : (
                                    <Tag color="red">Chưa đăng ký</Tag>
                                )}
                            </Descriptions.Item>
                        </Descriptions>

                        <Divider />

                        <div>
                            {(() => {
                                // Lọc chỉ lấy booking đang hoạt động
                                const residentUserId = selectedResident.userId || selectedResident.id || selectedResident.residentId;

                                const activeBookings = selectedResident.bookings?.filter((b) => {
                                    const today = dayjs().startOf('day');
                                    const startDate = b.startDate ? dayjs(b.startDate).startOf('day') : null;
                                    const endDate = b.endDate ? dayjs(b.endDate).startOf('day') : null;
                                    const activeStatuses = ["Confirmed", "Active", "Completed"];

                                    // Kiểm tra status
                                    const hasActiveStatus = activeStatuses.includes(b.status);

                                    // Kiểm tra thời gian: ngày hiện tại phải nằm trong khoảng startDate và endDate
                                    const isWithinDateRange = startDate && endDate &&
                                        (today.isAfter(startDate) || today.isSame(startDate)) &&
                                        (today.isBefore(endDate) || today.isSame(endDate));

                                    return hasActiveStatus && isWithinDateRange;
                                }) || [];

                                const expiredBookings = selectedResident.bookings?.filter((b) => {
                                    if (!b.endDate) return false;
                                    const endDate = dayjs(b.endDate).endOf('day');
                                    const isExpired = dayjs().isAfter(endDate);
                                    // Chỉ lấy những tiện ích đã hết hạn và có trạng thái Completed
                                    return isExpired && b.status === "Completed";
                                }) || [];

                                return (
                                    <>
                                        <Title level={5}>Danh sách tiện ích đang hoạt động ({activeBookings.length})</Title>
                                        {activeBookings.length > 0 ? (
                                            <Table
                                                rowKey="bookingId"
                                                dataSource={activeBookings}
                                                pagination={false}
                                                size="small"
                                                columns={[
                                                    {
                                                        title: "Tiện ích",
                                                        key: "amenityName",
                                                        render: (_, record) => (
                                                            <Text strong>{record.amenityName || "—"}</Text>
                                                        ),
                                                    },
                                                    {
                                                        title: "Gói tiện ích",
                                                        key: "packageName",
                                                        render: (_, record) => (
                                                            <Space direction="vertical" size={0}>
                                                                <Text>{record.packageName || "—"}</Text>
                                                                {record.monthCount && (
                                                                    <Text type="secondary" style={{ fontSize: 12 }}>
                                                                        {record.monthCount} tháng
                                                                    </Text>
                                                                )}
                                                            </Space>
                                                        ),
                                                    },
                                                    {
                                                        title: "Hạn sử dụng",
                                                        key: "dateRange",
                                                        render: (_, record) => {
                                                            const startDate = record.startDate
                                                                ? dayjs(record.startDate).format("DD/MM/YYYY")
                                                                : "—";
                                                            const endDate = record.endDate
                                                                ? dayjs(record.endDate).format("DD/MM/YYYY")
                                                                : "—";
                                                            const today = dayjs().startOf('day');
                                                            const start = record.startDate ? dayjs(record.startDate).startOf('day') : null;
                                                            const end = record.endDate ? dayjs(record.endDate).startOf('day') : null;

                                                            const isActive = start && end &&
                                                                (today.isAfter(start) || today.isSame(start)) &&
                                                                (today.isBefore(end) || today.isSame(end));

                                                            const isExpired = end && today.isAfter(end);
                                                            const isUpcoming = start && today.isBefore(start);

                                                            return (
                                                                <Space direction="vertical" size={0}>
                                                                    <Text>
                                                                        {startDate} → {endDate}
                                                                    </Text>
                                                                    {isActive && (
                                                                        <Tag color="green">Đang hoạt động</Tag>
                                                                    )}
                                                                    {isExpired && (
                                                                        <Tag color="red">Đã hết hạn</Tag>
                                                                    )}
                                                                    {isUpcoming && (
                                                                        <Tag color="blue">Sắp bắt đầu</Tag>
                                                                    )}
                                                                </Space>
                                                            );
                                                        },
                                                    },
                                                    {
                                                        title: "Trạng thái",
                                                        key: "status",
                                                        render: (_, record) => {
                                                            const statusColors = {
                                                                "Pending": "orange",
                                                                "Confirmed": "blue",
                                                                "Active": "green",
                                                                "Completed": "cyan",
                                                                "Cancelled": "red",
                                                            };
                                                            return (
                                                                <Tag color={statusColors[record.status] || "default"}>
                                                                    {record.status || "—"}
                                                                </Tag>
                                                            );
                                                        },
                                                    },
                                                    {
                                                        title: "Thanh toán",
                                                        key: "paymentStatus",
                                                        render: (_, record) => {
                                                            const paymentColors = {
                                                                "Paid": "green",
                                                                "Unpaid": "red",
                                                                "Partial": "orange",
                                                            };
                                                            return (
                                                                <Tag color={paymentColors[record.paymentStatus] || "default"}>
                                                                    {record.paymentStatus === "Paid" ? "Đã thanh toán" :
                                                                        record.paymentStatus === "Unpaid" ? "Chưa thanh toán" :
                                                                            record.paymentStatus === "Partial" ? "Thanh toán một phần" :
                                                                                record.paymentStatus || "—"}
                                                                </Tag>
                                                            );
                                                        },
                                                    },
                                                ]}
                                            />
                                        ) : (
                                            <Alert
                                                type="info"
                                                message="Cư dân này không có tiện ích nào đang hoạt động."
                                            />
                                        )}

                                        <Divider />
                                        <Title level={5}>Tiện ích đã hết hạn ({expiredBookings.length})</Title>
                                        {expiredBookings.length > 0 ? (
                                            <Table
                                                rowKey="bookingId"
                                                dataSource={expiredBookings}
                                                pagination={false}
                                                size="small"
                                                columns={[
                                                    {
                                                        title: "Tiện ích",
                                                        dataIndex: "amenityName",
                                                        key: "expiredAmenity",
                                                    },
                                                    {
                                                        title: "Gói",
                                                        dataIndex: "packageName",
                                                        key: "expiredPackage",
                                                    },
                                                    {
                                                        title: "Hết hạn",
                                                        key: "expiredDate",
                                                        render: (_, record) =>
                                                            record.endDate
                                                                ? dayjs(record.endDate).format("DD/MM/YYYY")
                                                                : "—",
                                                    },
                                                    {
                                                        title: "Trạng thái",
                                                        dataIndex: "status",
                                                        key: "expiredStatus",
                                                        render: (status) => <Tag color="red">{status || "—"}</Tag>,
                                                    },
                                                    {
                                                        title: "Gia hạn",
                                                        key: "renewAction",
                                                        render: (_, record) => (
                                                            <Button
                                                                type="link"
                                                                onClick={() => {
                                                                    openRenewalModal({
                                                                        userId: residentUserId || record.userId || record.residentId,
                                                                        amenityId: record.amenityId,
                                                                        packageId: record.packageId,
                                                                    });
                                                                    setDetailModalOpen(false);
                                                                }}
                                                            >
                                                                Gia hạn
                                                            </Button>
                                                        ),
                                                    },
                                                ]}
                                            />
                                        ) : (
                                            <Alert
                                                type="info"
                                                message="Chưa có tiện ích nào hết hạn."
                                            />
                                        )}
                                    </>
                                );
                            })()}
                        </div>
                    </Space>
                )}
            </Modal>

            <Modal
                title="Đăng ký tiện ích cho cư dân"
                open={bookingModalOpen}
                destroyOnClose={true}
                onCancel={() => {
                    // Stop stream và set state trước khi đóng modal
                    setIsFaceWebcamOpen(false);
                    stopFaceWebcamStream();
                    setBookingModalOpen(false);
                    setBookingForm({
                        userId: null,
                        amenityId: null,
                        packageId: null,
                        notes: "",
                    });
                    setCalculatedPrice(null);
                    setCalculatedDates({ startDate: null, endDate: null });
                    setPackages([]);
                    clearFaceImage();
                    setSelectedResidentDetail(null);
                }}
                afterClose={() => {
                    // Đảm bảo webcam được tắt sau khi modal đóng hoàn toàn
                    setIsFaceWebcamOpen(false);
                    stopFaceWebcamStream();
                }}
                onOk={handleCreateBooking}
                okText="Đăng ký"
                cancelText="Hủy"
                confirmLoading={bookingSubmitting}
                width={700}
            >
                <Space direction="vertical" style={{ width: "100%" }} size="large">
                    <div>
                        <Text strong>Chọn cư dân <span style={{ color: "red" }}>*</span></Text>
                        <Select
                            style={{ width: "100%", marginTop: 8 }}
                            placeholder="Chọn cư dân"
                            value={bookingForm.userId}
                            onChange={async (value) => {
                                setBookingForm((prev) => ({ ...prev, userId: value }));
                                clearFaceImage();
                                setSelectedResidentDetail(null);

                                // Fetch thông tin chi tiết của cư dân để lấy hasFaceRegistered
                                if (value) {
                                    try {
                                        setLoadingResidentDetail(true);
                                        const detail = await residentsApi.getByUserId(value);
                                        setSelectedResidentDetail(detail);
                                    } catch (error) {
                                        console.error("Error fetching resident detail:", error);
                                        // Không hiển thị lỗi vì có thể dùng dữ liệu từ allResidents hoặc residents
                                    } finally {
                                        setLoadingResidentDetail(false);
                                    }
                                }
                            }}
                            showSearch
                            loading={allResidentsLoading || loadingResidentDetail}
                            filterOption={(input, option) =>
                                (option?.label ?? "").toLowerCase().includes(input.toLowerCase())
                            }
                            options={allResidents
                                .filter((r) => r.userId) // Chỉ lấy cư dân có userId
                                .map((r) => {
                                    const primaryApartment = r.apartments?.find((apt) => apt.isPrimary) || r.apartments?.[0];
                                    const apartmentCode = primaryApartment?.apartmentNumber || "—";
                                    return {
                                        value: r.userId,
                                        label: `${r.fullName || r.user?.fullName || ""} - Căn hộ: ${apartmentCode}`,
                                    };
                                })}
                        />
                        {bookingForm.userId && (
                            <Text type="secondary" style={{ fontSize: 12, display: "block", marginTop: 4 }}>
                                {(() => {
                                    const selected = allResidents.find((r) => r.userId === bookingForm.userId);
                                    const primaryApartment = selected?.apartments?.find((apt) => apt.isPrimary) || selected?.apartments?.[0];
                                    return primaryApartment
                                        ? `Căn hộ: ${primaryApartment.apartmentNumber}`
                                        : "Chưa có thông tin căn hộ";
                                })()}
                            </Text>
                        )}
                    </div>

                    {bookingForm.userId && bookingForm.amenityId && (() => {
                        const selectedAmenity = amenities.find((a) => a.amenityId === bookingForm.amenityId);
                        if (!selectedAmenity?.requiresFaceVerification) return null;

                        return (
                            <div>
                                <Title level={5}>
                                    Đăng ký khuôn mặt
                                    {!residentHasFaceRegistered && <span style={{ color: "red" }}> *</span>}
                                </Title>
                                {residentHasFaceRegistered ? (
                                    <Alert
                                        type="success"
                                        message="Cư dân đã đăng ký khuôn mặt. Bạn có thể bỏ qua hoặc cập nhật lại khuôn mặt mới."
                                        showIcon
                                        style={{ marginBottom: 8 }}
                                    />
                                ) : (
                                    <Text type="secondary" style={{ fontSize: 12, display: "block", marginBottom: 8 }}>
                                        Tiện ích này yêu cầu xác thực khuôn mặt. Vui lòng đăng ký khuôn mặt cho cư dân.
                                    </Text>
                                )}
                                <Space wrap style={{ marginBottom: 8 }}>
                                    <Button
                                        icon={<VideoCameraOutlined />}
                                        onClick={() => {
                                            if (isFaceWebcamOpen) {
                                                // Stop stream khi đóng webcam
                                                stopFaceWebcamStream();
                                            }
                                            setIsFaceWebcamOpen((prev) => !prev);
                                        }}
                                    >
                                        {isFaceWebcamOpen ? "Đóng webcam" : "Mở webcam"}
                                    </Button>
                                    {facePreviewUrl && (
                                        <Button onClick={clearFaceImage}>Xóa ảnh</Button>
                                    )}
                                </Space>
                                {isFaceWebcamOpen && bookingModalOpen && (
                                    <Card style={{ marginTop: 8 }}>
                                        <Webcam
                                            key={`booking-webcam-${bookingModalOpen}`}
                                            ref={faceWebcamRef}
                                            audio={false}
                                            screenshotFormat="image/jpeg"
                                            mirrored
                                            videoConstraints={{
                                                width: 480,
                                                facingMode: "user",
                                            }}
                                            onUserMedia={(stream) => {
                                                // Lưu stream reference để có thể stop sau
                                                faceWebcamStreamRef.current = stream;
                                            }}
                                            onUserMediaError={(error) => {
                                                console.error("Webcam error:", error);
                                                message.error("Không thể truy cập webcam.");
                                            }}
                                            onLoadedMetadata={() => {
                                                // Đảm bảo stream được lưu khi video đã load
                                                if (faceWebcamRef.current?.video?.srcObject) {
                                                    faceWebcamStreamRef.current = faceWebcamRef.current.video.srcObject;
                                                }
                                            }}
                                            style={{
                                                width: "100%",
                                                maxWidth: 480,
                                                borderRadius: 12,
                                            }}
                                        />
                                        <Button
                                            type="primary"
                                            icon={<CameraOutlined />}
                                            onClick={handleFaceCameraCapture}
                                            style={{ marginTop: 8, width: "100%" }}
                                        >
                                            Chụp ảnh
                                        </Button>
                                    </Card>
                                )}
                                <Upload
                                    accept="image/*"
                                    maxCount={1}
                                    beforeUpload={(file) => {
                                        handleFaceImageChange(file);
                                        return false;
                                    }}
                                    onRemove={clearFaceImage}
                                    showUploadList={false}
                                >
                                    <Button icon={<CameraOutlined />} style={{ marginTop: 8 }}>
                                        Tải ảnh từ máy
                                    </Button>
                                </Upload>
                                {facePreviewUrl && (
                                    <div style={{ marginTop: 8 }}>
                                        <img
                                            src={facePreviewUrl}
                                            alt="Face preview"
                                            style={{
                                                width: "100%",
                                                maxWidth: 200,
                                                borderRadius: 8,
                                                border: "1px solid #d9d9d9",
                                            }}
                                        />
                                    </div>
                                )}
                                {faceImageError && (
                                    <Alert
                                        message={faceImageError}
                                        type="error"
                                        style={{ marginTop: 8 }}
                                    />
                                )}
                            </div>
                        );
                    })()}

                    <div>
                        <Text strong>Chọn tiện ích <span style={{ color: "red" }}>*</span></Text>
                        <Select
                            style={{ width: "100%", marginTop: 8 }}
                            placeholder="Chọn tiện ích"
                            value={bookingForm.amenityId}
                            onChange={handleAmenityChange}
                            loading={amenitiesLoading}
                            showSearch
                            filterOption={(input, option) =>
                                (option?.label ?? "").toLowerCase().includes(input.toLowerCase())
                            }
                            options={amenities.map((a) => ({
                                value: a.amenityId,
                                label: `${a.name}${a.location ? ` - ${a.location}` : ""}`,
                            }))}
                        />
                    </div>

                    {bookingForm.amenityId && (
                        <div>
                            <Text strong>Chọn gói tiện ích <span style={{ color: "red" }}>*</span></Text>
                            <Select
                                style={{ width: "100%", marginTop: 8 }}
                                placeholder="Chọn gói tiện ích"
                                value={bookingForm.packageId}
                                onChange={handlePackageChange}
                                loading={packagesLoading}
                                disabled={packages.length === 0}
                            >
                                {packages.map((pkg) => (
                                    <Select.Option key={pkg.packageId} value={pkg.packageId}>
                                        {pkg.name} - {pkg.price?.toLocaleString("vi-VN")} VNĐ
                                    </Select.Option>
                                ))}
                            </Select>
                            {packages.length === 0 && !packagesLoading && (
                                <Text type="secondary" style={{ fontSize: 12, display: "block", marginTop: 4 }}>
                                    Tiện ích này chưa có gói hoặc không hỗ trợ gói theo tháng
                                </Text>
                            )}
                        </div>
                    )}

                    {(typeof calculatedPrice === "number" || (calculatedDates.startDate && calculatedDates.endDate)) && (
                        <Alert
                            message={
                                <Space direction="vertical" size={4} style={{ width: "100%" }}>
                                    {typeof calculatedPrice === "number" && (
                                        <Text strong>Giá: {calculatedPrice.toLocaleString("vi-VN")} VNĐ</Text>
                                    )}
                                    {calculatedDates.startDate && calculatedDates.endDate && (
                                        <>
                                            <Text type="secondary" style={{ fontSize: 12 }}>
                                                Thời gian: {dayjs(calculatedDates.startDate).format("DD/MM/YYYY")} → {dayjs(calculatedDates.endDate).format("DD/MM/YYYY")}
                                            </Text>
                                            <Space direction="vertical" style={{ width: "100%" }}>
                                                <Input
                                                    addonBefore="Ngày bắt đầu"
                                                    value={dayjs(calculatedDates.startDate).format("DD/MM/YYYY")}
                                                    readOnly
                                                />
                                                <Input
                                                    addonBefore="Ngày kết thúc"
                                                    value={dayjs(calculatedDates.endDate).format("DD/MM/YYYY")}
                                                    readOnly
                                                />
                                            </Space>
                                        </>
                                    )}
                                </Space>
                            }
                            type="info"
                        />
                    )}

                    <div>
                        <Text strong>Ghi chú (tùy chọn)</Text>
                        <TextArea
                            rows={3}
                            maxLength={500}
                            value={bookingForm.notes}
                            onChange={(e) => setBookingForm((prev) => ({ ...prev, notes: e.target.value }))}
                            placeholder="Ghi chú thêm (tùy chọn)"
                            style={{ marginTop: 8 }}
                        />
                    </div>
                </Space>
            </Modal>

            <QRPaymentModal
                open={showQRPayment}
                onCancel={() => {
                    setShowQRPayment(false);
                    setPaymentData(null);
                }}
                amenityData={paymentData}
                skipNavigation={true}
                onPaymentComplete={(success) => {
                    if (success) {
                        message.success("Thanh toán thành công. Đã tạo đăng ký tiện ích.");
                        reloadResidents(); // Reload danh sách cư dân để cập nhật booking mới
                    }
                }}
            />

            {/* Modal xem ảnh khuôn mặt */}
            <Modal
                title="Ảnh khuôn mặt đã đăng ký"
                open={faceImageModalOpen}
                onCancel={() => {
                    setFaceImageModalOpen(false);
                    setFaceImageUrl(null);
                }}
                footer={[
                    <Button key="close" onClick={() => {
                        setFaceImageModalOpen(false);
                        setFaceImageUrl(null);
                    }}>
                        Đóng
                    </Button>
                ]}
                width={500}
            >
                {faceImageUrl && (
                    <div style={{ textAlign: "center", padding: "20px 0" }}>
                        <img
                            src={faceImageUrl}
                            alt="Khuôn mặt đã đăng ký"
                            style={{
                                maxWidth: "100%",
                                maxHeight: "500px",
                                borderRadius: "8px",
                                border: "1px solid #d9d9d9",
                            }}
                            onError={(e) => {
                                message.error("Không thể tải ảnh khuôn mặt.");
                                setFaceImageModalOpen(false);
                            }}
                        />
                    </div>
                )}
            </Modal>

            {/* Modal cập nhật khuôn mặt */}
            <Modal
                title="Cập nhật khuôn mặt"
                open={updateFaceModalOpen}
                destroyOnClose={true}
                onCancel={() => {
                    // Stop stream và set state trước khi đóng modal
                    setIsFaceWebcamOpen(false);
                    stopFaceWebcamStream();
                    setUpdateFaceModalOpen(false);
                    setUpdatingFaceUserId(null);
                    setUpdateFaceImageFile(null);
                    setUpdateFacePreviewUrl("");
                    setUpdateFaceImageError("");
                }}
                afterClose={() => {
                    // Đảm bảo webcam được tắt sau khi modal đóng hoàn toàn
                    setIsFaceWebcamOpen(false);
                    stopFaceWebcamStream();
                }}
                onOk={async () => {
                    if (!updateFaceImageFile) {
                        setUpdateFaceImageError("Vui lòng chọn hoặc chụp ảnh khuôn mặt mới.");
                        return;
                    }

                    if (!updatingFaceUserId) {
                        message.error("Không xác định được cư dân.");
                        return;
                    }

                    try {
                        setUpdateFaceSubmitting(true);
                        const formData = new FormData();
                        formData.append("UserId", updatingFaceUserId.toString());
                        formData.append("Image", updateFaceImageFile);

                        const response = await apiClient.post("/FaceRecognition/register", formData, {
                            headers: {
                                "Content-Type": "multipart/form-data",
                            },
                        });

                        if (response.data?.success) {
                            message.success("Đã cập nhật khuôn mặt thành công.");
                            // Tắt webcam và stop stream trước khi đóng modal
                            stopFaceWebcamStream();
                            setIsFaceWebcamOpen(false);
                            setUpdateFaceModalOpen(false);
                            setUpdatingFaceUserId(null);
                            setUpdateFaceImageFile(null);
                            setUpdateFacePreviewUrl("");
                            setUpdateFaceImageError("");
                            // Reload thông tin cư dân để cập nhật
                            if (selectedResident?.userId) {
                                try {
                                    const detail = await residentsApi.getByUserId(selectedResident.userId);
                                    setSelectedResidentDetail(detail);
                                    // Cập nhật lại selectedResident trong danh sách
                                    const updatedResident = { ...selectedResident, ...detail };
                                    setSelectedResident(updatedResident);
                                } catch (error) {
                                    console.error("Error reloading resident detail:", error);
                                }
                            }
                        } else {
                            message.error(response.data?.message || "Cập nhật khuôn mặt thất bại.");
                        }
                    } catch (error) {
                        const apiMessage =
                            error.response?.data?.message ||
                            error.response?.data?.error ||
                            error.message;
                        message.error(apiMessage || "Không thể cập nhật khuôn mặt.");
                    } finally {
                        setUpdateFaceSubmitting(false);
                    }
                }}
                okText="Cập nhật"
                cancelText="Hủy"
                confirmLoading={updateFaceSubmitting}
                width={600}
            >
                <Space direction="vertical" style={{ width: "100%" }} size="large">
                    <Alert
                        message="Cập nhật khuôn mặt mới cho cư dân"
                        type="info"
                        showIcon
                    />

                    <Space wrap style={{ marginBottom: 8 }}>
                        <Button
                            icon={<VideoCameraOutlined />}
                            onClick={() => {
                                if (isFaceWebcamOpen) {
                                    // Stop stream khi đóng webcam
                                    stopFaceWebcamStream();
                                    setIsFaceWebcamOpen(false);
                                } else {
                                    setIsFaceWebcamOpen(true);
                                }
                            }}
                        >
                            {isFaceWebcamOpen ? "Đóng webcam" : "Mở webcam"}
                        </Button>
                        {updateFacePreviewUrl && (
                            <Button onClick={() => {
                                setUpdateFaceImageFile(null);
                                if (updateFacePreviewUrl && updateFacePreviewUrl.startsWith("blob:")) {
                                    URL.revokeObjectURL(updateFacePreviewUrl);
                                }
                                setUpdateFacePreviewUrl("");
                                setUpdateFaceImageError("");
                            }}>
                                Xóa ảnh
                            </Button>
                        )}
                    </Space>

                    {isFaceWebcamOpen && updateFaceModalOpen && (
                        <Card style={{ marginTop: 8 }}>
                            <Webcam
                                key={`update-webcam-${updateFaceModalOpen}`}
                                ref={faceWebcamRef}
                                audio={false}
                                screenshotFormat="image/jpeg"
                                mirrored
                                videoConstraints={{
                                    width: 480,
                                    facingMode: "user",
                                }}
                                onUserMedia={(stream) => {
                                    // Lưu stream reference để có thể stop sau
                                    faceWebcamStreamRef.current = stream;
                                }}
                                onUserMediaError={(error) => {
                                    console.error("Webcam error:", error);
                                    message.error("Không thể truy cập webcam.");
                                }}
                                onLoadedMetadata={() => {
                                    // Đảm bảo stream được lưu khi video đã load
                                    if (faceWebcamRef.current?.video?.srcObject) {
                                        faceWebcamStreamRef.current = faceWebcamRef.current.video.srcObject;
                                    }
                                }}
                                style={{
                                    width: "100%",
                                    maxWidth: 480,
                                    borderRadius: 12,
                                }}
                            />
                            <Button
                                type="primary"
                                icon={<CameraOutlined />}
                                onClick={() => {
                                    const imageSrc = faceWebcamRef.current?.getScreenshot();
                                    if (!imageSrc) {
                                        message.warning("Không thể chụp ảnh từ webcam. Vui lòng thử lại.");
                                        return;
                                    }

                                    try {
                                        const file = dataURLtoFile(imageSrc, `face-${Date.now()}.jpg`);
                                        setUpdateFaceImageFile(file);
                                        if (updateFacePreviewUrl && updateFacePreviewUrl.startsWith("blob:")) {
                                            URL.revokeObjectURL(updateFacePreviewUrl);
                                        }
                                        setUpdateFacePreviewUrl(URL.createObjectURL(file));
                                        // Stop webcam stream ngay sau khi chụp ảnh
                                        stopFaceWebcamStream();
                                        setIsFaceWebcamOpen(false);
                                        message.success("Đã chụp ảnh từ webcam.");
                                    } catch (error) {
                                        console.error("Error converting webcam image:", error);
                                        setUpdateFaceImageError("Không thể xử lý ảnh từ webcam.");
                                    }
                                }}
                                style={{ marginTop: 8, width: "100%" }}
                            >
                                Chụp ảnh
                            </Button>
                        </Card>
                    )}

                    <Upload
                        accept="image/*"
                        maxCount={1}
                        beforeUpload={(file) => {
                            if (!file.type?.startsWith("image/")) {
                                setUpdateFaceImageError("Vui lòng chọn tệp ảnh hợp lệ.");
                                return false;
                            }

                            if (file.size > 5 * 1024 * 1024) {
                                setUpdateFaceImageError("Ảnh vượt quá giới hạn 5MB.");
                                return false;
                            }

                            setUpdateFaceImageError("");
                            if (updateFacePreviewUrl && updateFacePreviewUrl.startsWith("blob:")) {
                                URL.revokeObjectURL(updateFacePreviewUrl);
                            }
                            setUpdateFaceImageFile(file);
                            setUpdateFacePreviewUrl(URL.createObjectURL(file));
                            return false;
                        }}
                        onRemove={() => {
                            setUpdateFaceImageFile(null);
                            if (updateFacePreviewUrl && updateFacePreviewUrl.startsWith("blob:")) {
                                URL.revokeObjectURL(updateFacePreviewUrl);
                            }
                            setUpdateFacePreviewUrl("");
                            setUpdateFaceImageError("");
                        }}
                        showUploadList={false}
                    >
                        <Button icon={<CameraOutlined />} style={{ marginTop: 8 }}>
                            Tải ảnh từ máy
                        </Button>
                    </Upload>

                    {updateFacePreviewUrl && (
                        <div style={{ marginTop: 8 }}>
                            <img
                                src={updateFacePreviewUrl}
                                alt="Face preview"
                                style={{
                                    width: "100%",
                                    maxWidth: 200,
                                    borderRadius: 8,
                                    border: "1px solid #d9d9d9",
                                }}
                            />
                        </div>
                    )}

                    {updateFaceImageError && (
                        <Alert
                            message={updateFaceImageError}
                            type="error"
                            style={{ marginTop: 8 }}
                        />
                    )}
                </Space>
            </Modal>
        </div>
    );
};

export default FaceCheckIn;


