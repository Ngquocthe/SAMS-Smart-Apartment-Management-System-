import api from "../../lib/apiClient";

export async function listServicePrices(serviceTypeId, params = {}) {
  const res = await api.get(`/ServiceTypes/${serviceTypeId}/prices`, { params });
  return res.data;
}

export async function createServicePrice(serviceTypeId, payload, { autoClosePrevious = true } = {}) {
  const res = await api.post(
    `/ServiceTypes/${serviceTypeId}/prices`,
    payload,
    { params: { autoClosePrevious } }
  );
  return res.data;
}

export async function updateServicePrice(serviceTypeId, priceId, payload) {
  const res = await api.put(`/ServiceTypes/${serviceTypeId}/prices/${priceId}`, payload);
  return res.data;
}

export async function cancelServicePrice(serviceTypeId, priceId) {
  await api.delete(`/ServiceTypes/${serviceTypeId}/prices/${priceId}`);
  return true;
}

export async function getCurrentPrice(serviceTypeId, asOfDate = null) {
  const params = asOfDate ? { asOfDate } : {};
  const res = await api.get(`/ServiceTypes/${serviceTypeId}/current-price`, { params });
  return res.data;
}