import api from "../../lib/apiClient";

export async function getInvoiceDetails(invoiceId) {
  if (!invoiceId) return [];
  const res = await api.get(`/InvoiceDetail/invoice/${invoiceId}`);
  return res.data;
}

export async function listInvoiceDetails(params = {}) {
  const res = await api.get("/InvoiceDetail", { params });
  return res.data;
}

export async function createInvoiceDetail(payload) {
  const res = await api.post("/InvoiceDetail", payload);
  return res.data;
}

export async function updateInvoiceDetail(id, payload) {
  const res = await api.put(`/InvoiceDetail/${id}`, payload);
  return res.data;
}

export async function deleteInvoiceDetail(id) {
  await api.delete(`/InvoiceDetail/${id}`);
}
