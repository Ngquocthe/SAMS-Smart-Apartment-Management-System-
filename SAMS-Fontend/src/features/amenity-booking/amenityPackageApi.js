import apiClient from '../../lib/apiClient';

export const amenityPackageApi = {
  /**
   * Lấy tất cả packages
   */
  getAll: async () => {
    try {
      const response = await apiClient.get('/amenity-packages');
      return response.data;
    } catch (error) {
      console.error('Error fetching packages:', error);
      throw error;
    }
  },

  /**
   * Lấy package theo ID
   */
  getById: async (id) => {
    try {
      const response = await apiClient.get(`/amenity-packages/${id}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching package:', error);
      throw error;
    }
  },

  /**
   * Lấy packages theo amenity ID
   */
  getByAmenityId: async (amenityId) => {
    try {
      const response = await apiClient.get(`/amenity-packages/by-amenity/${amenityId}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching packages by amenity:', error);
      throw error;
    }
  },

  /**
   * Lấy các packages đang active theo amenity ID
   */
  getActiveByAmenityId: async (amenityId) => {
    try {
      const response = await apiClient.get(`/amenity-packages/by-amenity/${amenityId}/active`);
      return response.data;
    } catch (error) {
      console.error('Error fetching active packages by amenity:', error);
      throw error;
    }
  },

  /**
   * Tạo package mới
   */
  create: async (packageData) => {
    try {
      const response = await apiClient.post('/amenity-packages', packageData);
      return response.data;
    } catch (error) {
      console.error('Error creating package:', error.response?.data || error.message);
      throw error;
    }
  },

  /**
   * Cập nhật package
   */
  update: async (id, packageData) => {
    try {
      const response = await apiClient.put(`/amenity-packages/${id}`, packageData);
      return response.data;
    } catch (error) {
      console.error(`Error updating package with ID ${id}:`, error.response?.data || error.message);
      throw error;
    }
  },

  /**
   * Xóa package
   */
  delete: async (id) => {
    try {
      const response = await apiClient.delete(`/amenity-packages/${id}`);
      return response.data;
    } catch (error) {
      console.error('Error deleting package:', error.response?.data || error.message);
      throw error;
    }
  }
};

export default amenityPackageApi;

