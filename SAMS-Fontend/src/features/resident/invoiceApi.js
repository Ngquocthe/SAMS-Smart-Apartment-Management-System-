import apiClient from '../../lib/apiClient';

// API endpoints for resident invoices
const INVOICE_ENDPOINTS = {
  MY_INVOICES: '/Invoice/my',
};

/**
 * Get all invoices for the current resident's apartment
 * @returns {Promise<Array>} Array of invoices with details
 */
export const getMyInvoices = async () => {
  try {
    const response = await apiClient.get(INVOICE_ENDPOINTS.MY_INVOICES);
    return response.data;
  } catch (error) {
    console.error('Error fetching my invoices:', error);
    throw error;
  }
};

/**
 * Get invoice details by ID
 * @param {string} invoiceId - The invoice ID
 * @returns {Promise<Object>} Invoice details
 */
export const getInvoiceDetails = async (invoiceId) => {
  try {
    const response = await apiClient.get(`${INVOICE_ENDPOINTS.MY_INVOICES}/${invoiceId}`);
    return response.data;
  } catch (error) {
    console.error('Error fetching invoice details:', error);
    throw error;
  }
};

/**
 * Create QR code for invoice payment
 * @param {string} invoiceId - The invoice ID
 * @returns {Promise<Object>} QR code and payment info
 */
export const createInvoicePaymentQR = async (invoiceId) => {
  try {
    const response = await apiClient.post('/Payment/invoice/create', {
      invoiceId
    });
    return response.data;
  } catch (error) {
    console.error('Error creating invoice payment QR:', error);
    throw error;
  }
};

/**
 * Verify invoice payment status
 * @param {string} invoiceId - The invoice ID
 * @param {number} orderCode - The order code from QR creation
 * @returns {Promise<Object>} Payment verification response
 */
export const verifyInvoicePayment = async (invoiceId, orderCode) => {
  try {
    const response = await apiClient.post('/Payment/invoice/verify', {
      invoiceId,
      orderCode
    });
    return response.data;
  } catch (error) {
    console.error('Error verifying invoice payment:', error);
    throw error;
  }
};

/**
 * Process payment for an invoice (Legacy - kept for backward compatibility)
 * @param {string} invoiceId - The invoice ID
 * @param {Object} paymentData - Payment information
 * @returns {Promise<Object>} Payment response
 */
export const processInvoicePayment = async (invoiceId, paymentData) => {
  try {
    const response = await apiClient.post(`/Invoice/${invoiceId}/payment`, paymentData);
    return response.data;
  } catch (error) {
    console.error('Error processing invoice payment:', error);
    throw error;
  }
};

/**
 * Download invoice PDF
 * @param {string} invoiceId - The invoice ID
 * @returns {Promise<Blob>} PDF file blob
 */
export const downloadInvoicePDF = async (invoiceId) => {
  try {
    const response = await apiClient.get(`/Invoice/${invoiceId}/pdf`, {
      responseType: 'blob'
    });
    return response.data;
  } catch (error) {
    console.error('Error downloading invoice PDF:', error);
    throw error;
  }
};

/**
 * Update invoice status
 * @param {string} invoiceId - The invoice ID
 * @param {string} status - New status (PAID, OVERDUE, CANCELLED, ISSUED)
 * @param {string} note - Optional note for the change
 * @returns {Promise<Object>} Update response
 */
export const updateInvoiceStatus = async (invoiceId, status, note = '') => {
  try {
    const response = await apiClient.patch(`/Invoice/${invoiceId}/status`, {
      status,
      note
    });
    return response.data;
  } catch (error) {
    console.error('Error updating invoice status:', error);
    throw error;
  }
};

/**
 * Helper function to categorize invoices by status
 * @param {Array} invoices - Array of invoices
 * @returns {Object} Categorized invoices
 */
export const categorizeInvoices = (invoices) => {
  return {
    unpaid: invoices.filter(invoice => 
      invoice.status === 'PENDING' || invoice.status === 'DRAFT' || invoice.status === 'ISSUED'
    ),
    paid: invoices.filter(invoice => 
      invoice.status === 'PAID'
    ),
    overdue: invoices.filter(invoice => 
      invoice.status === 'OVERDUE'
    )
  };
};

/**
 * Helper function to calculate total amounts
 * @param {Array} invoices - Array of invoices
 * @returns {Object} Total amounts
 */
export const calculateTotals = (invoices) => {
  return {
    totalInvoices: invoices.length,
    totalAmount: invoices.reduce((sum, invoice) => sum + invoice.totalAmount, 0),
    totalDebt: invoices
      .filter(invoice => invoice.status !== 'PAID' && invoice.status !== 'CANCELLED')
      .reduce((sum, invoice) => sum + invoice.totalAmount, 0),
    paidAmount: invoices
      .filter(invoice => invoice.status === 'PAID')
      .reduce((sum, invoice) => sum + invoice.totalAmount, 0)
  };
};

/**
 * Format invoice status for display
 * @param {string} status - API status
 * @returns {string} Display status
 */
export const formatInvoiceStatus = (status) => {
  const statusMap = {
    'DRAFT': 'Nháp',
    'PENDING': 'Chờ duyệt',
    'ISSUED': 'Chưa thanh toán',
    'PAID': 'Đã thanh toán',
    'CANCELLED': 'Đã hủy',
    'OVERDUE': 'Quá hạn'
  };
  return statusMap[status] || status;
};

/**
 * Get status color for UI
 * @param {string} status - API status
 * @returns {string} Ant Design color
 */
export const getInvoiceStatusColor = (status) => {
  const colorMap = {
    'DRAFT': 'default',
    'PENDING': 'processing',
    'ISSUED': 'warning',
    'PAID': 'success',
    'CANCELLED': 'error',
    'OVERDUE': 'error'
  };
  return colorMap[status] || 'default';
};