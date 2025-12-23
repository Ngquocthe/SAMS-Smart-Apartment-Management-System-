import apiClient from '../../lib/apiClient';

const buildQueryString = (query = {}) => {
  const params = new URLSearchParams();
  Object.entries(query).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') return;
    params.append(key, value);
  });
  const queryString = params.toString();
  return queryString ? `?${queryString}` : '';
};

export const amenityBookingApi = {
  /**
   * Lấy tất cả booking với phân trang và lọc
   */
  getAll: async (query = {}) => {
    try {
      const params = new URLSearchParams();
      if (query.pageNumber) params.append('pageNumber', query.pageNumber);
      if (query.pageSize) params.append('pageSize', query.pageSize);
      if (query.status) params.append('status', query.status);
      if (query.paymentStatus) params.append('paymentStatus', query.paymentStatus);
      if (query.amenityId) params.append('amenityId', query.amenityId);
      if (query.apartmentId) params.append('apartmentId', query.apartmentId);
      if (query.userId) params.append('userId', query.userId);
      if (query.fromDate) params.append('fromDate', query.fromDate);
      if (query.toDate) params.append('toDate', query.toDate);
      if (query.sortBy) params.append('sortBy', query.sortBy);
      if (query.sortOrder) params.append('sortOrder', query.sortOrder);

      const response = await apiClient.get(`/amenitybooking?${params.toString()}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Lấy booking theo ID
   */
  getById: async (id) => {
    try {
      const response = await apiClient.get(`/amenitybooking/${id}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Lấy danh sách booking của user hiện tại
   */
  getMyBookings: async () => {
    try {
      const response = await apiClient.get('/amenitybooking/my-bookings');
      const payload = response.data;

      if (Array.isArray(payload)) return payload;
      if (Array.isArray(payload?.data)) return payload.data;
      if (Array.isArray(payload?.items)) return payload.items;
      if (Array.isArray(payload?.data?.items)) return payload.data.items;

      return [];
    } catch (error) {
      throw error;
    }
  },

  /**
   * Lấy booking theo amenity
   */
  getByAmenity: async (amenityId) => {
    try {
      const response = await apiClient.get(`/amenitybooking/by-amenity/${amenityId}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Lấy booking theo apartment
   */
  getByApartment: async (apartmentId) => {
    try {
      const response = await apiClient.get(`/amenitybooking/by-apartment/${apartmentId}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Lấy booking theo user (dành cho quản lý)
   */
  getByUser: async (userId) => {
    try {
      const response = await apiClient.get(`/amenitybooking/by-user/${userId}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Kiểm tra tính khả dụng
   * Backend tự động tính ngày từ package, chỉ cần amenityId và packageId
   */
  checkAvailability: async (amenityId, packageId) => {
    try {
      const params = new URLSearchParams({
        amenityId: amenityId,
        packageId: packageId
      });

      const response = await apiClient.get(`/amenitybooking/check-availability?${params.toString()}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Tính toán giá booking (backend tự động tính ngày từ package)
   */
  calculatePrice: async (amenityId, packageId) => {
    try {
      const response = await apiClient.post('/amenitybooking/calculate-price', {
        amenityId,
        packageId
      });
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Tạo booking mới
   * bookingData: { amenityId, packageId, notes?, apartmentId? }
   * Backend tự động tính startDate (ngày đăng ký) và endDate (startDate + số tháng của package)
   */
  create: async (bookingData) => {
    try {
      const response = await apiClient.post('/amenitybooking', bookingData);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Tạo booking mới có kèm ảnh mặt (dành cho tiện ích yêu cầu face verification)
   */
  createWithFace: async (payload) => {
    try {
      const formData = new FormData();
      formData.append('AmenityId', payload.amenityId);
      formData.append('PackageId', payload.packageId);
      if (payload.notes) {
        formData.append('Notes', payload.notes);
      }
      if (payload.apartmentId) {
        formData.append('ApartmentId', payload.apartmentId);
      }
      if (payload.faceImage) {
        formData.append('FaceImage', payload.faceImage);
      }

      const response = await apiClient.post('/amenitybooking/with-face', formData);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Cập nhật booking
   */
  update: async (id, bookingData) => {
    try {
      const response = await apiClient.put(`/amenitybooking/${id}`, bookingData);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Hủy booking
   */
  cancel: async (id, reason = null) => {
    try {
      const response = await apiClient.post(`/amenitybooking/${id}/cancel`, { reason });
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Xác nhận booking (admin/staff)
   */
  confirm: async (id) => {
    try {
      const response = await apiClient.post(`/amenitybooking/${id}/confirm`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Hoàn thành booking (admin/staff)
   */
  complete: async (id) => {
    try {
      const response = await apiClient.post(`/amenitybooking/${id}/complete`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Cập nhật trạng thái thanh toán (admin/staff)
   */
  updatePaymentStatus: async (id, paymentStatus) => {
    try {
      const response = await apiClient.patch(`/amenitybooking/${id}/payment-status`, {
        paymentStatus
      });
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Lấy danh sách cư dân đã đăng ký tiện ích (cho lễ tân)
   */
  getRegisteredResidents: async (query = {}) => {
    try {
      const response = await apiClient.get(
        `/amenitybooking/registered-residents${buildQueryString(query)}`
      );
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Lấy lịch sử check-in tiện ích (cho lễ tân)
   */
  getCheckInHistory: async (query = {}) => {
    try {
      const response = await apiClient.get(
        `/amenitybooking/check-ins${buildQueryString(query)}`
      );
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Lễ tân tạo booking nhanh cho cư dân
   */
  receptionistCreateForResident: async (payload) => {
    try {
      const response = await apiClient.post(
        '/amenitybooking/receptionist/create-for-resident',
        payload
      );
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Lễ tân check-in cư dân với ảnh khuôn mặt
   */
  receptionistCheckIn: async (bookingId, payload) => {
    try {
      const formData = new FormData();
      if (payload?.faceImage) {
        formData.append('FaceImage', payload.faceImage);
      }
      formData.append('ManualOverride', JSON.stringify(!!payload?.manualOverride));
      formData.append('SkipFaceVerification', JSON.stringify(!!payload?.skipFaceVerification));
      if (payload?.notes) {
        formData.append('Notes', payload.notes);
      }

      const response = await apiClient.post(
        `/amenitybooking/${bookingId}/receptionist-check-in`,
        formData,
        {
          headers: { 'Content-Type': 'multipart/form-data' }
        }
      );
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Lễ tân quét nhanh khuôn mặt và tự động check-in
   */
  receptionistScan: async ({ faceImage, amenityId }) => {
    try {
      const formData = new FormData();
      if (faceImage) {
        formData.append('FaceImage', faceImage);
      }
      if (amenityId) {
        formData.append('AmenityId', amenityId);
      }

      const response = await apiClient.post(
        '/amenitybooking/receptionist/scan',
        formData,
        {
          headers: { 'Content-Type': 'multipart/form-data' }
        }
      );
      return response.data;
    } catch (error) {
      throw error;
    }
  }
};

export default amenityBookingApi;

