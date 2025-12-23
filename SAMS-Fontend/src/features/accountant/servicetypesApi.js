import api from "../../lib/apiClient";

export async function listServiceType(params = {}) {
    const res = await api.get("/ServiceTypes", { params });
    return res.data;
}
export async function createServiceType(payload) {
    const res = await api.post("/ServiceTypes", payload);
    return res.data;
}

export async function deleteServiceType(id) {
  // 204 No Content
  await api.delete(`/ServiceTypes/${id}`);
}

export async function enableServiceType(id) {
  // 204 No Content
  await api.post(`/ServiceTypes/${id}/enable`);
}

export async function disableServiceType(id) {
  // 204 No Content
  await api.post(`/ServiceTypes/${id}/disable`);
}

export async function updateServiceType(id, payload) {
  try {
    const res = await api.put(`/ServiceTypes/${id}`, payload);
    return res.data; // 200 + body
  } catch (err) {
    const msg = err?.response?.data?.error
             || err?.response?.data?.message
             || err.message
             || "Update failed";
    throw new Error(msg);
  }
}

export async function listServiceTypeCategories() {
  const res = await api.get("/ServiceTypes/categories");
  return res.data;
}

