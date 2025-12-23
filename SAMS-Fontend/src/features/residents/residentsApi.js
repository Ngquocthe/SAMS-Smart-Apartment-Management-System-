import apiClient from '../../lib/apiClient';

const pick = (obj, ...keys) =>
    keys.map((k) => obj?.[k]).find((v) => v !== undefined);

export const residentsApi = {
    /**
     * Lấy danh sách cư dân có phân trang
     * GET /api/{schema}/Residents/paged
     */
    async getPaged(schema, params = {}) {
        const res = await apiClient.get(`/${encodeURIComponent(schema)}/Residents/paged`, {
            params: {
                pageNumber: params.pageNumber ?? params.page ?? 1,
                pageSize: params.pageSize ?? 20,
                apartmentId: params.apartmentId ?? undefined,
                q: params.q ?? params.search ?? undefined,
                sortBy: params.sortBy ?? undefined,
                sortDir: params.sortDir ?? undefined,
            },
        });
        const data = res.data || {};

        const items = pick(data, "data", "Data", "items", "Items") ?? [];
        const totalCount = pick(data, "totalCount", "TotalCount", "total", "Total") ?? 0;
        const pageNumber = pick(data, "pageNumber", "PageNumber") ?? params?.pageNumber ?? params?.page ?? 1;
        const pageSize = pick(data, "pageSize", "PageSize") ?? params?.pageSize ?? 20;

        return { items, totalCount, pageNumber, pageSize };
    },

    /**
     * Lấy tất cả cư dân với thông tin căn hộ
     */
    getAll: async () => {
        try {
            const response = await apiClient.get('/residents');
            return response.data;
        } catch (error) {
            console.error('Error fetching residents:', error);
            throw error;
        }
    },

    /**
     * Lấy cư dân theo ID
     */
    getById: async (residentId) => {
        try {
            const response = await apiClient.get(`/residents/${residentId}`);
            return response.data;
        } catch (error) {
            console.error('Error fetching resident:', error);
            throw error;
        }
    },

    /**
     * Lấy cư dân theo User ID
     */
    getByUserId: async (userId) => {
        try {
            const response = await apiClient.get(`/residents/by-user/${userId}`);
            return response.data;
        } catch (error) {
            console.error('Error fetching resident by user ID:', error);
            throw error;
        }
    },

    /**
     * Lấy cư dân theo Apartment ID
     */
    getByApartmentId: async (apartmentId) => {
        try {
            const response = await apiClient.get(`/residents/apartment/${apartmentId}`);
            return response.data;
        } catch (error) {
            console.error('Error fetching residents by apartment:', error);
            throw error;
        }
    }
};

export default residentsApi;





