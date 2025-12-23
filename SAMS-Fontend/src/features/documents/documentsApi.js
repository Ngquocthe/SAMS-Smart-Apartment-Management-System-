import api from "../../lib/apiClient";

// Document APIs mapped to backend endpoints in DocumentsController
const documentsApi = {
    async search(query) {
        // Map tham số về đúng tên backend
        const rawStatus = (query.Status ?? query.status)?.toString().toUpperCase();
        const params = {
            Title: query.Title ?? query.keyword ?? undefined,
            Category: query.Category ?? query.category ?? undefined,
            Status: rawStatus || undefined,
            VisibilityScope: query.VisibilityScope ?? query.visibilityScope ?? undefined,
            Page: query.Page ?? query.page ?? query.pageIndex ?? 1,
            PageSize: query.PageSize ?? query.pageSize ?? 10,
        };
        const response = await api.get("/Documents", { params });
        return response.data; // { total, items }
    },

    async get(id) {
        const response = await api.get(`/Documents/${id}`);
        return response.data;
    },

    async getLatest(status) {
        const params = status ? { status: status.toString().toUpperCase() } : undefined;
        const response = await api.get("/Documents/latest", { params });
        return response.data;
    },

    async create(formData) {
        const response = await api.post("/Documents", formData, {
            headers: { "Content-Type": "multipart/form-data" },
            timeout: 600000, // 10 phút cho upload file lớn
        });
        return response.data;
    },

    async uploadVersion(id, formData) {
        const timeoutConfig = { timeout: 600000 }; // 10 phút cho upload file lớn
        try {
            // Thử endpoint /Documents/{id}/versions trước
            const response = await api.post(`/Documents/${id}/versions`, formData, {
                headers: { "Content-Type": "multipart/form-data" },
                ...timeoutConfig,
            });
            return response.data;
        } catch (error) {
            console.log('First endpoint failed, trying alternative...');
            // Nếu endpoint đầu tiên thất bại, thử endpoint khác
            try {
                const response = await api.post(`/Documents/${id}/upload-version`, formData, {
                    headers: { "Content-Type": "multipart/form-data" },
                    ...timeoutConfig,
                });
                return response.data;
            } catch (secondError) {
                console.log('Second endpoint also failed, trying third option...');
                // Thử endpoint thứ 3
                const response = await api.post(`/Documents/upload-version`, formData, {
                    headers: {
                        "Content-Type": "multipart/form-data",
                        "DocumentId": id
                    },
                    ...timeoutConfig,
                });
                return response.data;
            }
        }
    },

    async updateMetadata(id, dto) {
        await api.patch(`/Documents/${id}`, dto);
    },

    buildDownloadUrl(fileId) {
        const baseURL = api.defaults.baseURL || "/api";
        return `${baseURL.replace(/\/$/, "")}/Documents/files/${fileId}`;
    },

    buildViewUrl(fileId) {
        const baseURL = api.defaults.baseURL || "/api";
        return `${baseURL.replace(/\/$/, "")}/Documents/files/${fileId}/view`;
    },

    async changeStatus(id, payload) {
        await api.patch(`/Documents/${id}/status`, payload);
    },

    async softDelete(id, params) {
        await api.delete(`/Documents/${id}`, { params });
    },

    async restore(id, body) {
        await api.post(`/Documents/${id}/restore`, body ?? {});
    },

    async getLogs(id) {
        const response = await api.get(`/Documents/${id}/logs`);
        return response.data;
    },

    async getVersions(id) {
        const response = await api.get(`/Documents/${id}/versions`);
        return response.data;
    },

    async getCategories() {
        const response = await api.get("/Documents/categories");
        return response.data;
    },

    async getVisibilityScopes() {
        const response = await api.get("/Documents/visibility-scopes");
        return response.data;
    },

    async getResidentDocuments(params) {
        const response = await api.get("/Documents/resident", { params });
        return response.data;
    },

    async getReceptionistDocuments(params) {
        const response = await api.get("/Documents/receptionist", { params });
        return response.data;
    },

    async getAccountingDocuments(params) {
        const response = await api.get("/Documents/accountant", { params });
        return response.data;
    },
};

export default documentsApi;


