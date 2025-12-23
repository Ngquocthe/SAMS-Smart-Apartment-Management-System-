import apiClient from '../../lib/apiClient';

/**
 * Resident Dashboard Service
 * Handles all API calls for resident dashboard
 */

class ResidentDashboardService {
  /**
   * Get resident profile information
   * @returns {Promise<Object>} Resident profile data
   */
  static async getResidentProfile() {
    try {
      const response = await apiClient.get('/Resident/profile');
      return response.data;
    } catch (error) {
      console.error('Error fetching resident profile:', error);
      throw error;
    }
  }

  /**
   * Get apartment information
   * @returns {Promise<Object>} Apartment details
   */
  static async getApartmentInfo() {
    try {
      const response = await apiClient.get('/Resident/apartment');
      return response.data;
    } catch (error) {
      console.error('Error fetching apartment info:', error);
      throw error;
    }
  }

  /**
   * Get dashboard statistics
   * @returns {Promise<Object>} Dashboard stats (bills, tickets, contracts, etc.)
   */
  static async getDashboardStats() {
    try {
      const response = await apiClient.get('/Resident/dashboard/stats');
      return response.data;
    } catch (error) {
      console.error('Error fetching dashboard stats:', error);
      throw error;
    }
  }

  /**
   * Get recent tickets/requests
   * @param {number} limit - Number of tickets to fetch
   * @returns {Promise<Array>} Recent tickets
   */
  static async getRecentTickets(limit = 5) {
    try {
      const response = await apiClient.get('/Resident/tickets/recent', {
        params: { limit }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching recent tickets:', error);
      throw error;
    }
  }

  /**
   * Get billing/debt chart data
   * @param {number} months - Number of months to fetch
   * @returns {Promise<Array>} Billing data for chart
   */
  static async getBillingChartData(months = 6) {
    try {
      const response = await apiClient.get('/Resident/billing/chart', {
        params: { months }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching billing chart:', error);
      throw error;
    }
  }

  /**
   * Get recent invoices
   * @param {number} limit - Number of invoices to fetch
   * @returns {Promise<Array>} Recent invoices
   */
  static async getRecentInvoices(limit = 5) {
    try {
      const response = await apiClient.get('/Resident/invoices/recent', {
        params: { limit }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching recent invoices:', error);
      throw error;
    }
  }

  /**
   * Get news and events
   * @param {number} limit - Number of items to fetch
   * @returns {Promise<Array>} News and events
   */
  static async getNewsAndEvents(limit = 6) {
    try {
      const response = await apiClient.get('/Announcement', {
        params: { 
          pageNumber: 1,
          pageSize: limit 
        }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching news and events:', error);
      throw error;
    }
  }

  /**
   * Get amenity booking schedule
   * @returns {Promise<Array>} Available amenity slots
   */
  static async getAmenitySchedule() {
    try {
      const response = await apiClient.get('/Resident/amenities/schedule');
      return response.data;
    } catch (error) {
      console.error('Error fetching amenity schedule:', error);
      throw error;
    }
  }

  /**
   * Book an amenity
   * @param {Object} bookingData - Booking details
   * @returns {Promise<Object>} Booking confirmation
   */
  static async bookAmenity(bookingData) {
    try {
      const response = await apiClient.post('/Resident/amenities/book', bookingData);
      return response.data;
    } catch (error) {
      console.error('Error booking amenity:', error);
      throw error;
    }
  }

  /**
   * Get contract information
   * @returns {Promise<Object>} Contract details
   */
  static async getContractInfo() {
    try {
      const response = await apiClient.get('/Resident/contract');
      return response.data;
    } catch (error) {
      console.error('Error fetching contract info:', error);
      throw error;
    }
  }

  /**
   * Get household members
   * @returns {Promise<Array>} List of household members
   */
  static async getHouseholdMembers() {
    try {
      const response = await apiClient.get('/Resident/household/members');
      return response.data;
    } catch (error) {
      console.error('Error fetching household members:', error);
      throw error;
    }
  }

  /**
   * Create a new ticket/request
   * @param {Object} ticketData - Ticket details
   * @returns {Promise<Object>} Created ticket
   */
  static async createTicket(ticketData) {
    try {
      const response = await apiClient.post('/Resident/tickets', ticketData);
      return response.data;
    } catch (error) {
      console.error('Error creating ticket:', error);
      throw error;
    }
  }
}

export default ResidentDashboardService;
