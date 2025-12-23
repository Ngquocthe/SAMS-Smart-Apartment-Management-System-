// src/apis/staffApi.js
import api from "../../lib/apiClient";

const pick = (obj, ...keys) =>
  keys.map((k) => obj?.[k]).find((v) => v !== undefined);

export const staffApi = {
  // GET /api/{schema}/staff -> PagedApiResponse<StaffListItemDto>
  async listStaff(schema, params) {
    const res = await api.get(`/${encodeURIComponent(schema)}/staff`, {
      params,
    });
    const data = res.data || {};

    const items = pick(data, "data", "Data") ?? [];
    const totalCount = pick(data, "totalCount", "TotalCount") ?? 0;
    const pageNumber =
      pick(data, "pageNumber", "PageNumber") ?? params?.page ?? 1;
    const pageSize =
      pick(data, "pageSize", "PageSize") ?? params?.pageSize ?? 10;

    return { items, totalCount, pageNumber, pageSize };
  },

  async getDetail(schema, staffCode) {
    console.log("🚀 ~ schema:", schema);

    const res = await api.get(
      `/${encodeURIComponent(schema)}/staff/${staffCode}`
    );
    return pick(res.data, "data", "Data");
  },

  // PUT /api/{schema}/staff/{staffCode} -> ApiResponse<object>
  async update(schema, staffCode, body) {
    const res = await api.put(
      `/${encodeURIComponent(schema)}/staff/${staffCode}`,
      body
    );
    return res.data;
  },

  // POST /api/{schema}/staff/{staffCode}/activate -> ApiResponse<object>
  async activate(schema, staffCode) {
    const res = await api.post(
      `/${encodeURIComponent(schema)}/staff/${staffCode}/activate`
    );
    return res.data;
  },

  // POST /api/{schema}/staff/{staffCode}/deactivate?date=YYYY-MM-DD -> ApiResponse<object>
  async deactivate(schema, staffCode, date) {
    const res = await api.post(
      `/${encodeURIComponent(schema)}/staff/${staffCode}/deactivate`,
      null,
      {
        params: date ? { date } : undefined,
      }
    );
    return res.data;
  },

  async createStaff(schema, payload) {
    const res = await api.post(`/${encodeURIComponent(schema)}/staff`, payload);

    return res.data;
  },

  async getWorkRoles(schema, params) {
    const res = await api.get(`/buildings/${schema}/work-roles`, { params });
    return Array.isArray(res?.data?.data) ? res.data.data : res?.data ?? [];
  },

  // Alias for update method
  async updateStaff(schema, staffCode, body) {
    return this.update(schema, staffCode, body);
  },
};
