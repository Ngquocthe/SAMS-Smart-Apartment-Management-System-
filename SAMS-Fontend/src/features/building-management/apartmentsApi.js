import apiClient from '../../lib/apiClient';

export const apartmentsApi = {
  // Lấy tất cả căn hộ
  getAll: async () => {
    try {
      const response = await apiClient.get('/Apartment');
      return response.data;
    } catch (error) {
      console.error('Error fetching apartments:', error);
      throw error;
    }
  },

  // Lấy căn hộ theo ID
  getById: async (apartmentId) => {
    try {
      const response = await apiClient.get(`/Apartment/${apartmentId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching apartment with ID ${apartmentId}:`, error);
      throw error;
    }
  },

  // Lấy căn hộ theo số căn hộ (VD: A0108)
  getByNumber: async (number) => {
    try {
      const response = await apiClient.get(`/Apartment/number/${number}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching apartment with number ${number}:`, error);
      throw error;
    }
  },

  // Lấy căn hộ theo floor ID
  getByFloor: async (floorId) => {
    try {
      const response = await apiClient.get(`/Apartment/floor/${floorId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching apartments by floor ${floorId}:`, error);
      throw error;
    }
  }
};

export default apartmentsApi;

