import apiClient from '../../lib/apiClient';

export const assetsApi = {
  // Lấy tất cả tài sản
  getAll: async () => {
    try {
      const response = await apiClient.get('/assets');
      return response.data;
    } catch (error) {
      console.error('Error fetching assets:', error);
      throw error;
    }
  },

  // Lấy tài sản theo ID
  getById: async (id) => {
    try {
      const response = await apiClient.get(`/assets/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching asset with ID ${id}:`, error);
      throw error;
    }
  },

  // Tìm kiếm tài sản theo tên hoặc code
  search: async (searchTerm) => {
    try {
      const response = await apiClient.get('/assets/search', {
        params: { searchTerm }
      });
      return response.data;
    } catch (error) {
      console.error('Error searching assets:', error);
      throw error;
    }
  },

  // Lấy tài sản theo trạng thái
  getByStatus: async (status) => {
    try {
      const response = await apiClient.get(`/assets/status/${status}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching assets with status ${status}:`, error);
      throw error;
    }
  },

  // Lấy tài sản theo vị trí
  getByLocation: async (location) => {
    try {
      const response = await apiClient.get(`/assets/location/${location}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching assets by location ${location}:`, error);
      throw error;
    }
  },

  // Lấy tài sản theo danh mục
  getByCategory: async (categoryId) => {
    try {
      const response = await apiClient.get(`/assets/category/${categoryId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching assets by category ${categoryId}:`, error);
      throw error;
    }
  },

  // Lấy tài sản theo căn hộ
  getByApartment: async (apartmentId) => {
    try {
      const response = await apiClient.get(`/assets/apartment/${apartmentId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching assets by apartment ${apartmentId}:`, error);
      throw error;
    }
  },

  // Lấy tài sản theo block
  getByBlock: async (blockId) => {
    try {
      const response = await apiClient.get(`/assets/block/${blockId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching assets by block ${blockId}:`, error);
      throw error;
    }
  },

  // Lấy tài sản đã hết bảo hành
  getExpiredWarranty: async () => {
    try {
      const response = await apiClient.get('/assets/warranty/expired');
      return response.data;
    } catch (error) {
      console.error('Error fetching assets with expired warranty:', error);
      throw error;
    }
  },

  // Lấy tài sản sắp hết bảo hành
  getExpiringWarranty: async (days = 30) => {
    try {
      const response = await apiClient.get('/assets/warranty/expiring', {
        params: { days }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching assets with expiring warranty:', error);
      throw error;
    }
  },

  // Lấy tài sản đang hoạt động
  getActive: async () => {
    try {
      const response = await apiClient.get('/assets/active');
      return response.data;
    } catch (error) {
      console.error('Error fetching active assets:', error);
      throw error;
    }
  },

  // Lấy thống kê tài sản
  getStatistics: async () => {
    try {
      const response = await apiClient.get('/assets/statistics');
      return response.data;
    } catch (error) {
      console.error('Error fetching asset statistics:', error);
      throw error;
    }
  },

  // Lấy tất cả danh mục tài sản
  getCategories: async () => {
    try {
      const response = await apiClient.get('/assets/categories');
      return response.data;
    } catch (error) {
      console.error('Error fetching asset categories:', error);
      throw error;
    }
  },

  // Lấy danh mục tài sản theo ID
  getCategoryById: async (id) => {
    try {
      const response = await apiClient.get(`/assets/categories/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching category with ID ${id}:`, error);
      throw error;
    }
  },

  // Tạo tài sản mới
  create: async (assetData) => {
    try {
      const response = await apiClient.post('/assets/createasset', assetData);
      return response.data;
    } catch (error) {
      console.error('Error creating asset:', error.response?.data || error.message);
      throw error;
    }
  },

  // Cập nhật tài sản
  update: async (id, assetData) => {
    try {
      const response = await apiClient.put(`/assets/updateasset/${id}`, assetData);
      return response.data;
    } catch (error) {
      console.error(`Error updating asset with ID ${id}:`, error.response?.data || error.message);
      throw error;
    }
  },

  // Xóa mềm tài sản
  delete: async (id) => {
    try {
      const response = await apiClient.delete(`/assets/softdelete/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Error deleting asset with ID ${id}:`, error.response?.data || error.message);
      throw error;
    }
  },

  // ========== ASSET MAINTENANCE SCHEDULE ==========
  getMaintenanceSchedules: async (params = {}) => {
    try {
      const response = await apiClient.get('/asset-maintenance-schedules', { params });
      return response.data;
    } catch (error) {
      console.error('Error fetching maintenance schedules:', error);
      throw error;
    }
  },

  getMaintenanceSchedulesByAssetId: async (assetId) => {
    try {
      const response = await apiClient.get('/asset-maintenance-schedules', {
        params: { assetId }
      });
      return response.data;
    } catch (error) {
      console.error(`Error fetching maintenance schedules for asset ${assetId}:`, error);
      throw error;
    }
  },

  getMaintenanceScheduleById: async (scheduleId) => {
    try {
      const response = await apiClient.get(`/asset-maintenance-schedules/${scheduleId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching maintenance schedule ${scheduleId}:`, error);
      throw error;
    }
  },

  getReceptionists: async () => {
    try {
      const response = await apiClient.get('/asset-maintenance-schedules/receptionists');
      return response.data;
    } catch (error) {
      console.error('Error fetching receptionists:', error.response?.data || error.message);
      throw error;
    }
  },

  createMaintenanceSchedule: async (scheduleData) => {
    try {
      const response = await apiClient.post('/asset-maintenance-schedules', scheduleData);
      return response.data;
    } catch (error) {
      console.error('Error creating maintenance schedule:', error.response?.data || error.message);
      throw error;
    }
  },

  updateMaintenanceSchedule: async (scheduleId, scheduleData) => {
    try {
      const response = await apiClient.put(`/asset-maintenance-schedules/${scheduleId}`, scheduleData);
      return response.data;
    } catch (error) {
      console.error(`Error updating maintenance schedule ${scheduleId}:`, error.response?.data || error.message);
      throw error;
    }
  },

  deleteMaintenanceSchedule: async (scheduleId) => {
    try {
      const response = await apiClient.delete(`/asset-maintenance-schedules/${scheduleId}`);
      return response.data;
    } catch (error) {
      console.error(`Error deleting maintenance schedule ${scheduleId}:`, error.response?.data || error.message);
      throw error;
    }
  },

  getMaintenanceSchedulesByStatus: async (status) => {
    try {
      const response = await apiClient.get('/asset-maintenance-schedules', {
        params: { status }
      });
      return response.data;
    } catch (error) {
      console.error(`Error fetching maintenance schedules with status ${status}:`, error);
      throw error;
    }
  },

  searchMaintenanceSchedules: async (params = {}) => {
    try {
      const response = await apiClient.get('/asset-maintenance-schedules/search', { params });
      return response.data;
    } catch (error) {
      console.error('Error searching maintenance schedules:', error.response?.data || error.message);
      throw error;
    }
  },

  exportMaintenanceSchedules: async (scheduleIds = []) => {
    try {
      let response;
      try {
        response = await apiClient.post('/asset-maintenance-schedules/export', scheduleIds || []);
      } catch (firstError) {
        if (firstError.response?.status === 400) {
          response = await apiClient.post('/asset-maintenance-schedules/export', {
            scheduleIds: scheduleIds || []
          });
        } else {
          throw firstError;
        }
      }
      return response.data;
    } catch (error) {
      console.error('Error exporting maintenance schedules:', error.response?.data || error.message);
      throw error;
    }
  },

  importMaintenanceSchedules: async (jsonString) => {
    try {
      const response = await apiClient.post('/asset-maintenance-schedules/import', jsonString);
      return response.data;
    } catch (error) {
      console.error('Error importing maintenance schedules:', error.response?.data || error.message);
      throw error;
    }
  },

  getDueForReminder: async () => {
    try {
      const response = await apiClient.get('/asset-maintenance-schedules/due-for-reminder');
      return response.data;
    } catch (error) {
      console.error('Error fetching schedules due for reminder:', error.response?.data || error.message);
      throw error;
    }
  },

  getDueForMaintenance: async () => {
    try {
      const response = await apiClient.get('/asset-maintenance-schedules/due-for-maintenance');
      return response.data;
    } catch (error) {
      console.error('Error fetching schedules due for maintenance:', error.response?.data || error.message);
      throw error;
    }
  },

  // ========== ASSET MAINTENANCE HISTORY ==========

  getMaintenanceHistory: async (params = {}) => {
    try {
      const response = await apiClient.get('/asset-maintenance-history', { params });
      return Array.isArray(response.data) ? response.data : (response.data?.data || response.data?.items || []);
    } catch (error) {
      console.error('Error fetching maintenance history:', error.response?.data || error.message);
      throw error;
    }
  },

  // Lấy lịch sử bảo trì theo asset ID
  getMaintenanceHistoryByAssetId: async (assetId) => {
    try {
      const response = await apiClient.get(`/asset-maintenance-history/asset/${assetId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching maintenance history for asset ${assetId}:`, error);
      throw error;
    }
  },

  // Lấy lịch sử bảo trì theo schedule ID
  getMaintenanceHistoryByScheduleId: async (scheduleId) => {
    try {
      const response = await apiClient.get(`/asset-maintenance-history/schedule/${scheduleId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching maintenance history for schedule ${scheduleId}:`, error);
      throw error;
    }
  },

  // Lấy lịch sử bảo trì theo ticket ID
  getMaintenanceHistoryByTicketId: async (ticketId) => {
    try {
      const response = await apiClient.get(`/asset-maintenance-history/ticket/${ticketId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching maintenance history for ticket ${ticketId}:`, error);
      throw error;
    }
  },

  // Lấy lịch sử bảo trì theo ID
  getMaintenanceHistoryById: async (historyId) => {
    try {
      const response = await apiClient.get(`/asset-maintenance-history/${historyId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching maintenance history ${historyId}:`, error);
      throw error;
    }
  },

  // Tạo lịch sử bảo trì mới
  createMaintenanceHistory: async (historyData) => {
    try {
      const response = await apiClient.post('/asset-maintenance-history', historyData);
      return response.data;
    } catch (error) {
      console.error('Error creating maintenance history:', error.response?.data || error.message);
      throw error;
    }
  },

  // Cập nhật lịch sử bảo trì
  updateMaintenanceHistory: async (historyId, historyData) => {
    try {
      const response = await apiClient.put(`/asset-maintenance-history/${historyId}`, historyData);
      return response.data;
    } catch (error) {
      console.error(`Error updating maintenance history ${historyId}:`, error.response?.data || error.message);
      throw error;
    }
  }
};

export default assetsApi;
