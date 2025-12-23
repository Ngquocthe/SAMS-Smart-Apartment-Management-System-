import api from "../../lib/apiClient";

// GET /api/Invoice?apartmentId=...&status=...&search=...&dueFrom=...&dueTo=...&page=1&pageSize=20&sortBy=DueDate&sortDir=desc
export async function listInvoices(params = {}) {
  const res = await api.get("/Invoice", { params });
  return res.data;
}

// GET /api/Invoice/{id}
export async function getInvoiceById(id) {
  const res = await api.get(`/Invoice/${id}`);
  return res.data;
}

// POST /api/Invoice
export async function createInvoice(payload) {
  const res = await api.post("/Invoice", payload);
  return res.data;
}

// PUT /api/Invoice/{id}
export async function updateInvoice(id, payload) {
  try {
    const res = await api.put(`/Invoice/${id}`, payload);
    return res.data;
  } catch (err) {
    const msg =
      err?.response?.data?.error ||
      err?.response?.data?.message ||
      err.message ||
      "Cập nhật hoá đơn thất bại";
    throw new Error(msg);
  }
}

export async function updateInvoiceStatus(id, payload) {
  try {
    const res = await api.patch(`/Invoice/${id}/status`, payload);
    return res.data;
  } catch (err) {
    const msg =
      err?.response?.data?.error ||
      err?.response?.data?.message ||
      err.message ||
      "Cập nhật trạng thái hoá đơn thất bại";
    throw new Error(msg);
  }
}
