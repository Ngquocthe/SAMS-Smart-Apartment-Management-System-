import apiClient from '../../lib/apiClient';

export const dashboardApi = {
  // Lấy thống kê dashboard cho building manager
  getStatistics: async () => {
    try {
      const response = await apiClient.get('/buildingmanagement/dashboard/statistics');
      return response.data;
    } catch (error) {
      console.error('Error fetching dashboard statistics:', error);
      throw error;
    }
  },

  // Lấy dữ liệu tài chính từ kế toán
  getFinancialData: async (period = 'month') => {
    try {
      const response = await apiClient.get(`/JournalEntry/dashboard?period=${period}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching financial data:', error);
      throw error;
    }
  },

  // Lấy báo cáo thu chi theo tháng
  getMonthlyIncomeStatement: async (fromDate, toDate) => {
    try {
      const response = await apiClient.get('/JournalEntry/income-statement', {
        params: { from: fromDate, to: toDate }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching income statement:', error);
      throw error;
    }
  }
};

