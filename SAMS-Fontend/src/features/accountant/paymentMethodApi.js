import api from "../../lib/apiClient";

// GET /api/PaymentMethod/active
export async function listPaymentMethods() {
  const res = await api.get("/PaymentMethod/active");
  return res.data;
}

// GET /api/PaymentMethod
export async function listAllPaymentMethods(params = {}) {
  const res = await api.get("/PaymentMethod", { params });
  return res.data;
}

// GET /api/PaymentMethod/{id}
export async function getPaymentMethodById(id) {
  const res = await api.get(`/PaymentMethod/${id}`);
  return res.data;
}

