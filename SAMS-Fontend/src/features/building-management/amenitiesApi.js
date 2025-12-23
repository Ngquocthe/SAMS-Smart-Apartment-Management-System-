import apiClient from '../../lib/apiClient';

export const amenitiesApi = {
  getAll: async () => {
    try {
      const response = await apiClient.get('/buildingmanagement/amenities');
      return response.data;
    } catch (error) {
      console.error('Error fetching amenities:', error);
      throw error;
    }
  },

  create: async (amenityData) => {
    try {
      const response = await apiClient.post('/buildingmanagement/createamenity', amenityData);
      return response.data;
    } catch (error) {
      console.error('Error creating amenity:', error.response?.data || error.message);
      throw error;
    }
  },

  update: async (id, amenityData) => {
    try {
      const response = await apiClient.put(`/buildingmanagement/updateamenity/${id}`, amenityData);
      return response.data;
    } catch (error) {
      console.error(`Error updating amenity with ID ${id}:`, error.response?.data || error.message);
      throw error;
    }
  },

  delete: async (amenityId) => {
    try {
      const response = await apiClient.delete(`/buildingmanagement/deleteamenity/${amenityId}`);
      return response.data;
    } catch (error) {
      console.error('Error deleting amenity:', error.response?.data || error.message);
      throw error;
    }
  }
};

export default amenitiesApi;
