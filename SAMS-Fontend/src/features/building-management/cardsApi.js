import apiClient from '../../lib/apiClient';

export const cardsApi = {
  // Lấy danh sách tất cả thẻ ra vào
  getAll: async () => {
    try {
      const response = await apiClient.get('/AccessCard');
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy thẻ ra vào theo ID
  getById: async (cardId) => {
    try {
      const response = await apiClient.get(`/AccessCard/${cardId}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy thẻ ra vào theo User ID
  getByUser: async (userId) => {
    try {
      const response = await apiClient.get(`/AccessCard/user/${userId}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy thẻ ra vào theo Apartment ID
  getByApartment: async (apartmentId) => {
    try {
      const response = await apiClient.get(`/AccessCard/apartment/${apartmentId}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy thẻ ra vào theo trạng thái
  getByStatus: async (status) => {
    try {
      const response = await apiClient.get(`/AccessCard/status/${status}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy thẻ ra vào theo loại thẻ ID
  getByCardType: async (cardTypeId) => {
    try {
      const response = await apiClient.get(`/AccessCard/card-type/${cardTypeId}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Tạo thẻ mới
  create: async (createData) => {
    try {
      const response = await apiClient.post('/AccessCard/createcard', createData);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Cập nhật thẻ
  update: async (id, updateData) => {
    try {
      const response = await apiClient.put(`/AccessCard/updatecard/${id}`, updateData);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Xóa mềm thẻ
  softDelete: async (id) => {
    try {
      const response = await apiClient.delete(`/AccessCard/softdelete/${id}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy thống kê thẻ
  getCardStats: async () => {
    try {
      const [allCards, activeCards, inactiveCards, expiredCards] = await Promise.all([
        cardsApi.getAll(),
        cardsApi.getByStatus('ACTIVE'),
        cardsApi.getByStatus('INACTIVE'),
        cardsApi.getByStatus('EXPIRED')
      ]);

      return {
        total: allCards.length,
        active: activeCards.length,
        inactive: inactiveCards.length,
        expired: expiredCards.length
      };
    } catch (error) {
      throw error;
    }
  },

  // Lấy danh sách tất cả loại thẻ
  getCardTypes: async () => {
    try {
      const response = await apiClient.get('/AccessCard/card-types');
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy khả năng của thẻ theo card ID
  getCardCapabilities: async (cardId) => {
    try {
      const response = await apiClient.get(`/AccessCard/${cardId}/capabilities`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Tạo thẻ mới với khả năng
  createWithCapabilities: async (createData) => {
    try {
      const response = await apiClient.post('/AccessCard/createcard-with-capabilities', createData);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Cập nhật thẻ với khả năng
  updateWithCapabilities: async (id, updateData) => {
    try {
      const response = await apiClient.put(`/AccessCard/updatecard-with-capabilities/${id}`, updateData);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Cập nhật khả năng của thẻ
  updateCardCapabilities: async (cardId, capabilities) => {
    try {
      const response = await apiClient.put(`/AccessCard/update-capabilities/${cardId}`, capabilities);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // ========== CARD AUDIT LOGS ==========

  // Lấy audit logs của thẻ theo card ID (sử dụng history endpoint)
  getCardAuditLogsByCardId: async (cardId) => {
    try {
      const response = await apiClient.get(`/AccessCard/${cardId}/history`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy audit logs với pagination và filtering (sử dụng history endpoint)
  getCardAuditLogs: async (query = {}) => {
    try {
      const response = await apiClient.get('/AccessCard/history', { params: query });
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy recent changes của thẻ (sử dụng history endpoint)
  getRecentCardChanges: async (cardId, limit = 10) => {
    try {
      const response = await apiClient.get(`/AccessCard/${cardId}/recent-access?limit=${limit}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy change summary (sử dụng access-summary endpoint)
  getCardChangeSummary: async (cardId = null) => {
    try {
      const params = cardId ? { cardId } : {};
      const response = await apiClient.get('/AccessCard/access-summary', { params });
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy change statistics (sử dụng statistics endpoint)
  getChangeStatistics: async (fromDate, toDate) => {
    try {
      const response = await apiClient.get('/AccessCard/statistics', {
        params: { fromDate, toDate }
      });
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy audit logs theo action type (sử dụng history endpoint)
  getCardAuditLogsByActionType: async (actionType) => {
    try {
      const response = await apiClient.get(`/AccessCard/history/event/${actionType}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy audit logs theo field name (sử dụng history endpoint)
  getCardAuditLogsByFieldName: async (fieldName) => {
    try {
      const response = await apiClient.get(`/AccessCard/history/field/${fieldName}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Lấy audit logs theo người thay đổi (sử dụng history endpoint với filter)
  getCardAuditLogsByChangedBy: async (changedBy) => {
    try {
      const response = await apiClient.get('/AccessCard/history', { 
        params: { createdBy: changedBy } 
      });
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Tạo audit log mới (sử dụng history endpoint)
  createCardAuditLog: async (auditLogData) => {
    try {
      const response = await apiClient.post('/AccessCard/history', auditLogData);
      return response.data;
    } catch (error) {
      throw error;
    }
  }
};

export default cardsApi;