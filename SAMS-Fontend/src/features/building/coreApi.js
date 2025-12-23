// src/apis/coreApi.js
import api from "../../lib/apiClient";

// Helper lấy trường theo nhiều kiểu case (Data/data)
const pick = (obj, ...keys) =>
  keys.map((k) => obj?.[k]).find((v) => v !== undefined);

export const coreApi = {
  async getBuildings() {
    const res = await api.get(`/core/buildings`);
    const payload = pick(res.data, "data", "Data") ?? [];
    return Array.isArray(payload) ? payload : [];
  },

  async getAllBuildingsIncludingInactive() {
    const res = await api.get(`/core/buildings/all`);
    const payload = pick(res.data, "data", "Data") ?? [];
    return Array.isArray(payload) ? payload : [];
  },

  async createBuilding(payload) {
    console.log("🚀 PAYLOAD FORM DATA:");
    for (let [key, value] of payload.entries()) {
      console.log(key, value);
    }

    const res = await api.post(`/core/buildings`, payload);

    return res.data;
  },

  async updateBuildingStatus(id, payload) {
    const formData = new FormData();
    if (payload.status !== undefined) {
      formData.append("Status", payload.status);
    }

    const res = await api.put(`/core/buildings/${id}`, formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });

    return res.data;
  },
};
