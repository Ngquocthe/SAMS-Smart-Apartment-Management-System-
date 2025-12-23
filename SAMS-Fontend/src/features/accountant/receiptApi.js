import api from "../../lib/apiClient";

// GET /api/Receipt?invoiceId=...&methodId=...&search=...&receivedFrom=...&receivedTo=...&page=1&pageSize=20
export async function listReceipts(params = {}) {
  // Helper to convert date string to ISO DateTime
  const toISODateTime = (dateStr) => {
    if (!dateStr) return undefined;
    try {
      // If it's already a Date object
      if (dateStr instanceof Date) {
        return dateStr.toISOString();
      }
      // If it's a string like "YYYY-MM-DD", convert to Date then ISO
      const date = new Date(dateStr);
      if (!isNaN(date.getTime())) {
        return date.toISOString();
      }
    } catch (e) {
      console.error("Error converting date:", e);
    }
    return undefined;
  };

  // Map frontend params to backend params
  const backendParams = {};
  
  if (params.search) backendParams.search = params.search;
  if (params.paymentMethod) backendParams.methodId = params.paymentMethod;
  if (params.dateFrom) backendParams.receivedFrom = toISODateTime(params.dateFrom);
  if (params.dateTo) {
    // For dateTo, set to end of day (23:59:59)
    const date = new Date(params.dateTo);
    date.setHours(23, 59, 59, 999);
    backendParams.receivedTo = date.toISOString();
  }
  if (params.invoiceId) backendParams.invoiceId = params.invoiceId;
  if (params.sortBy) backendParams.sortBy = params.sortBy;
  if (params.sortDir) backendParams.sortDir = params.sortDir;
  
  backendParams.page = params.page || 1;
  backendParams.pageSize = params.pageSize || 20;
  
  const res = await api.get("/Receipt", { params: backendParams });
  return res.data;
}

// GET /api/Receipt/{id}
export async function getReceiptById(id) {
  const res = await api.get(`/Receipt/${id}`);
  return res.data;
}

// GET /api/Receipt/invoice/{invoiceId}
export async function getReceiptByInvoiceId(invoiceId) {
  const res = await api.get(`/Receipt/invoice/${invoiceId}`);
  return res.data;
}

// GET /api/Invoice/unpaid - Lấy danh sách hóa đơn chưa thanh toán
export async function getUnpaidInvoices(params = {}) {
  const res = await api.get("/Invoice/unpaid", { params });
  return res.data;
}

// GET /api/PaymentMethod/active - Lấy danh sách phương thức thanh toán đang hoạt động
export async function getActivePaymentMethods() {
  const res = await api.get("/PaymentMethod/active");
  return res.data;
}

// POST /api/Receipt
export async function createReceipt(payload) {
  const res = await api.post("/Receipt", payload);
  return res.data;
}

// PUT /api/Receipt/{id}
export async function updateReceipt(id, payload) {
  const res = await api.put(`/Receipt/${id}`, payload);
  return res.data;
}

// DELETE /api/Receipt/{id}
export async function deleteReceipt(id) {
  const res = await api.delete(`/Receipt/${id}`);
  return res.data;
}

// POST /api/Receipt/from-payment - Tạo Receipt tự động từ thanh toán VietQR
export async function createReceiptFromPayment(payload) {
  const res = await api.post("/Receipt/from-payment", payload);
  return res.data;
}

