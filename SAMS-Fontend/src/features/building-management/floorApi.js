import apiClient from '../../lib/apiClient';

const FLOOR_BASE_URL = '/Floor';

// Enum FloorType
export const FloorType = {
  BASEMENT: 'BASEMENT',       // Tầng hầm
  COMMERCIAL: 'COMMERCIAL',   // Tầng thương mại
  AMENITY: 'AMENITY',         // Tầng tiện ích chung
  SERVICE: 'SERVICE',         // Tầng dịch vụ
  RESIDENTIAL: 'RESIDENTIAL'  // Tầng căn hộ
};

// Helper function để lấy label tiếng Việt
export const getFloorTypeLabel = (type) => {
  const labels = {
    BASEMENT: 'Tầng hầm',
    COMMERCIAL: 'Tầng thương mại',
    AMENITY: 'Tầng tiện ích chung',
    SERVICE: 'Tầng dịch vụ',
    RESIDENTIAL: 'Tầng căn hộ'
  };
  return labels[type] || type;
};

// Helper function để lấy màu cho loại tầng
export const getFloorTypeColor = (type) => {
  const colors = {
    BASEMENT: 'gray',
    COMMERCIAL: 'blue',
    AMENITY: 'green',
    SERVICE: 'orange',
    RESIDENTIAL: 'purple'
  };
  return colors[type] || 'default';
};

/**
 * Floor API Service
 */
class FloorApiService {
  /**
   * GET /api/Floor - Lấy danh sách tất cả tầng
   * @returns {Promise<FloorResponseDto[]>}
   */
  async getAll() {
    const response = await apiClient.get(FLOOR_BASE_URL);
    return response.data;
  }

  /**
   * GET /api/Floor/{id} - Lấy chi tiết 1 tầng
   * @param {string} id - Floor ID
   * @returns {Promise<FloorResponseDto>}
   */
  async getById(id) {
    const response = await apiClient.get(`${FLOOR_BASE_URL}/${id}`);
    return response.data;
  }

  /**
   * POST /api/Floor/create-single - Tạo 1 tầng đơn lẻ
   * @param {CreateSingleFloorRequest} data
   * @returns {Promise<{success: boolean, message: string, createdFloor: FloorResponseDto}>}
   */
  async createSingle(data) {
    const payload = {
      floorNumber: data.floorNumber,
      floorType: data.floorType,
      name: data.name || null
    };
    const response = await apiClient.post(`${FLOOR_BASE_URL}/create-single`, payload);
    return response.data;
  }

  /**
   * POST /api/Floor/create-floors - Tạo nhiều tầng
   * @param {CreateFloorsRequest} data
   * @returns {Promise<{success: boolean, message: string, createdFloors: FloorResponseDto[], totalCreated: number, skippedFloors: number[]}>}
   */
  async createFloors(data) {
    const payload = {
      floorType: data.floorType,
      count: data.count,
      startFloor: data.startFloor || undefined,
      excludeFloors: data.excludeFloors || []
    };
    const response = await apiClient.post(`${FLOOR_BASE_URL}/create-floors`, payload);
    return response.data;
  }

  /**
   * PUT /api/Floor/{id} - Cập nhật tầng
   * @param {string} id - Floor ID
   * @param {UpdateFloorRequest} data - { name?: string, floorType?: FloorType }
   * @returns {Promise<FloorResponseDto>}
   */
  async update(id, data) {
    const payload = {};
    
    // Add name to payload if provided
    if (data.name !== undefined && data.name !== null) {
      payload.name = data.name.trim();
    }
    
    // Add floorType to payload if provided
    if (data.floorType !== undefined && data.floorType !== null) {
      payload.floorType = data.floorType;
    }
    
    const response = await apiClient.put(`${FLOOR_BASE_URL}/${id}`, payload);
    return response.data;
  }

  /**
   * DELETE /api/Floor/{id} - Xóa tầng
   * @param {string} id - Floor ID
   * @returns {Promise<{success: boolean, message: string}>}
   */
  async delete(id) {
    const response = await apiClient.delete(`${FLOOR_BASE_URL}/${id}`);
    return response.data;
  }

  /**
   * Helper: Tạo tầng hầm (BASEMENT)
   * @param {number} count - Số lượng tầng hầm (1-100)
   * @example createBasementFloors(3) => Tạo tầng -1, -2, -3
   */
  async createBasementFloors(count) {
    return this.createFloors({
      floorType: FloorType.BASEMENT,
      count: count
    });
  }

  /**
   * Helper: Tạo tầng thương mại (COMMERCIAL)
   * @param {number} count - Số lượng tầng
   * @param {number} startFloor - Tầng bắt đầu
   * @example createCommercialFloors(5, 1) => Tạo tầng 1,2,3,4,5
   */
  async createCommercialFloors(count, startFloor = 1) {
    return this.createFloors({
      floorType: FloorType.COMMERCIAL,
      count: count,
      startFloor: startFloor
    });
  }

  /**
   * Helper: Tạo tầng căn hộ (RESIDENTIAL)
   * @param {number} count - Số lượng tầng
   * @param {number} startFloor - Tầng bắt đầu
   * @param {number[]} excludeFloors - Danh sách tầng bỏ qua (VD: [13, 14])
   */
  async createResidentialFloors(count, startFloor, excludeFloors = []) {
    return this.createFloors({
      floorType: FloorType.RESIDENTIAL,
      count: count,
      startFloor: startFloor,
      excludeFloors: excludeFloors
    });
  }

  /**
   * Helper: Tạo tầng tiện ích (AMENITY)
   * @param {number} floorNumber - Số tầng
   * @param {string} name - Tên tầng tùy chỉnh
   */
  async createAmenityFloor(floorNumber, name = null) {
    return this.createSingle({
      floorNumber: floorNumber,
      floorType: FloorType.AMENITY,
      name: name
    });
  }
}

export const floorApi = new FloorApiService();
export default floorApi;
