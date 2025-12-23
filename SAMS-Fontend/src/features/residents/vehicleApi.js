import apiClient from '../../lib/apiClient';

/**
 * Vehicle API Service for Resident
 */
const vehicleApi = {
  /**
   * GET /api/VehicleType - Lấy danh sách loại xe
   * @returns {Promise<Array>} Danh sách loại xe
   */
  async getVehicleTypes() {
    try {
      const response = await apiClient.get('/VehicleType');
      return response.data;
    } catch (error) {
      console.error('Error fetching vehicle types:', error);
      throw error;
    }
  },

  /**
   * POST /api/resident-tickets/vehicle-registration - Tạo ticket đăng ký xe
   * @param {Object} data - CreateVehicleRegistrationTicketDto với attachmentFileIds
   * @returns {Promise<Object>}
   */
  async createVehicleRegistration(data) {
    try {
      const payload = {
        subject: data.subject,
        description: data.description || null,
        apartmentId: data.apartmentId || null,
        vehicleInfo: {
          vehicleTypeId: data.vehicleInfo.vehicleTypeId,
          licensePlate: data.vehicleInfo.licensePlate,
          color: data.vehicleInfo.color || null,
          brandModel: data.vehicleInfo.brandModel || null,
          meta: data.vehicleInfo.meta || null
        },
        attachmentFileIds: data.attachmentFileIds || []
      };

      const response = await apiClient.post('/resident-tickets/vehicle-registration', payload);
      return response.data;
    } catch (error) {
      console.error('Error creating vehicle registration:', error);
      throw error;
    }
  }
};

export default vehicleApi;
