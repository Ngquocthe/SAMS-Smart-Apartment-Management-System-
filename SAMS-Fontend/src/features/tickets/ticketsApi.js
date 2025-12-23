import api from "../../lib/apiClient";

const ticketsApi = {
    async search(query) {
        const params = {
            Status: query.status || undefined,
            Priority: query.priority || undefined,
            Category: query.category || undefined,
            Search: query.search || query.keyword || undefined,
            FromDate: query.fromDate || undefined,
            ToDate: query.toDate || undefined,
            Page: query.page || 1,
            PageSize: query.pageSize || 10,
        };
        const res = await api.get("/Ticket", { params });
        return res.data; // { total, items }
    },

    async getById(id) {
        const res = await api.get(`/Ticket/${id}`);
        return res.data;
    },

    async create(dto) {
        const res = await api.post("/Ticket", dto);
        return res.data;
    },

    async update(dto) {
        const res = await api.put("/Ticket", dto);
        return res.data;
    },

    async remove(id) {
        await api.delete(`/Ticket/${id}`);
    },

    async assign(payload) {
        const res = await api.post("/Ticket/assign", payload);
        return res.data;
    },

    async changeStatus(payload) {
        const res = await api.post("/Ticket/status", payload);
        return res.data;
    },

    async getComments(ticketId) {
        const res = await api.get(`/Ticket/${ticketId}/comments`);
        return res.data;
    },

    async addComment(dto) {
        const res = await api.post("/Ticket/comments", dto);
        return res.data;
    },
};

export default ticketsApi;


