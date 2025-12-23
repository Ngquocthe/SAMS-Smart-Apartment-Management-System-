import api from "../../lib/apiClient";

const residentTicketsApi = {
    // Lấy danh sách tickets của resident
    async getMyTickets(query = {}) {
        const params = {
            Status: query.status || undefined,
            Category: query.category || undefined,
            Priority: query.priority || undefined,
            FromDate: query.fromDate || undefined,
            ToDate: query.toDate || undefined,
            Page: query.page || 1,
            PageSize: query.pageSize || 20,
        };
        const res = await api.get("/resident-tickets/my-tickets", { params });
        return res.data;
    },

    // Lấy chi tiết ticket
    async getTicketById(id) {
        const res = await api.get(`/resident-tickets/${id}`);
        return res.data;
    },

    // Tạo phiếu bảo trì
    async createMaintenanceTicket(dto) {
        const res = await api.post("/resident-tickets/maintenance", dto);
        return res.data;
    },

    // Tạo phiếu khiếu nại
    async createComplaintTicket(dto) {
        const res = await api.post("/resident-tickets/complaint", dto);
        return res.data;
    },

    // Thêm comment
    async addComment(dto) {
        const res = await api.post("/resident-tickets/comments", dto);
        return res.data;
    },

    // Lấy comments của ticket
    async getComments(ticketId) {
        const res = await api.get(`/resident-tickets/${ticketId}/comments`);
        return res.data;
    },

    async getTicketInvoices(ticketId) {
        const res = await api.get(`/resident-tickets/${ticketId}/invoices`);
        return res.data;
    },

    // Upload file
    async uploadFile(file, note = '') {
        const formData = new FormData();
        formData.append('file', file);
        if (note) {
            formData.append('note', note);
        }

        const res = await api.post("/resident-tickets/files/upload", formData, {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
        });
        return res.data;
    },

    async getStatistics() {
        const res = await api.get("/resident-tickets/statistics");
        return res.data;
    },

    // Thêm attachment vào ticket
    async addAttachment(ticketId, fileId, note = '') {
        const res = await api.post(`/resident-tickets/${ticketId}/attachments`, {
            fileId,
            note
        });
        return res.data;
    },

    // Lấy attachments của ticket
    async getAttachments(ticketId) {
        const res = await api.get(`/resident-tickets/${ticketId}/attachments`);
        return res.data;
    },

    // Xóa attachment
    async deleteAttachment(attachmentId) {
        await api.delete(`/resident-tickets/attachments/${attachmentId}`);
    },
};

export default residentTicketsApi;