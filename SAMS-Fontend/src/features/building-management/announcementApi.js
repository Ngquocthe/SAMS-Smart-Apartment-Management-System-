import apiClient from '../../lib/apiClient';

const ANNOUNCEMENT_BASE_URL = '/Announcement';

// DTOs cho type checking
export const AnnouncementStatus = {
  ACTIVE: 'ACTIVE',
  INACTIVE: 'INACTIVE',
  DRAFT: 'DRAFT'
};

export const AnnouncementScope = {
  ALL: 'ALL',
  RESIDENTS: 'RESIDENTS', 
  STAFF: 'STAFF'
};

export const AnnouncementType = {
  ANNOUNCEMENT: 'ANNOUNCEMENT', // Thông báo
  EVENT: 'EVENT' // Sự kiện
};

class AnnouncementApiService {
  // GET api/Announcement - Lấy danh sách với pagination
  async getAll(params = {}) {
    const response = await apiClient.get(`${ANNOUNCEMENT_BASE_URL}`, { params });
    return response.data;
  }

  // GET api/Announcement/{id} - Lấy chi tiết thông báo
  async getById(id) {
    const response = await apiClient.get(`${ANNOUNCEMENT_BASE_URL}/${id}`);
    return response.data;
  }

  // GET api/Announcement/active - Lấy thông báo đang hoạt động
  async getActive() {
    const response = await apiClient.get(`${ANNOUNCEMENT_BASE_URL}/active`);
    return response.data;
  }

  // GET api/Announcement/date-range - Lấy theo khoảng thời gian
  async getByDateRange(startDate, endDate) {
    const queryParams = new URLSearchParams({
      startDate: startDate, // yyyy-MM-dd format
      endDate: endDate
    });
    
    const response = await apiClient.get(`${ANNOUNCEMENT_BASE_URL}/date-range?${queryParams}`);
    return response.data;
  }

  // GET api/Announcement/scope/{scope} - Lấy theo phạm vi
  async getByScope(scope) {
    const response = await apiClient.get(`${ANNOUNCEMENT_BASE_URL}/scope/${scope}`);
    return response.data;
  }

  // POST api/Announcement - Tạo thông báo mới
  async create(createDto) {
    // Validate CreateAnnouncementDto according to API specification
    if (!createDto.title || createDto.title.length < 5 || createDto.title.length > 100) {
      throw new Error('Title must be between 5 and 100 characters');
    }
    if (!createDto.content || createDto.content.length < 10 || createDto.content.length > 5000) {
      throw new Error('Content must be between 10 and 5000 characters');
    }
    
    // Build payload while keeping local time format
    // Frontend sends format: YYYY-MM-DDTHH:mm:ss (ISO format without timezone)
    const payload = {
      title: createDto.title.trim(),
      content: createDto.content.trim(),
      visibleFrom: createDto.visibleFrom, // Keep original local time format
      visibleTo: createDto.visibleTo, // Keep original local time format
      visibilityScope: createDto.visibilityScope || 'ALL',
      status: createDto.status || 'ACTIVE',
      isPinned: Boolean(createDto.isPinned),
      type: createDto.type || 'ANNOUNCEMENT'
    };

    const response = await apiClient.post(ANNOUNCEMENT_BASE_URL, payload);
    return response.data;
  }

  // PUT api/Announcement/{id} - Cập nhật thông báo
  async update(id, updateDto) {
    // Validate UpdateAnnouncementDto according to API specification
    if (!updateDto.title || updateDto.title.length < 5 || updateDto.title.length > 100) {
      throw new Error('Title must be between 5 and 100 characters');
    }
    if (!updateDto.content || updateDto.content.length < 10 || updateDto.content.length > 5000) {
      throw new Error('Content must be between 10 and 5000 characters');
    }
    
    // Build payload while keeping local time format
    // Frontend sends format: YYYY-MM-DDTHH:mm:ss (ISO format without timezone)
    const payload = {
      announcementId: id,
      title: updateDto.title.trim(),
      content: updateDto.content.trim(),
      visibleFrom: updateDto.visibleFrom, // Keep original local time format
      visibleTo: updateDto.visibleTo, // Keep original local time format
      visibilityScope: updateDto.visibilityScope || "ALL",
      status: updateDto.status || "ACTIVE", 
      isPinned: Boolean(updateDto.isPinned),
      type: updateDto.type || "ANNOUNCEMENT"
    };

    const response = await apiClient.put(`${ANNOUNCEMENT_BASE_URL}/${id}`, payload);
    return response.data;
  }

  // DELETE api/Announcement/{id} - Xóa thông báo
  async delete(id) {
    const response = await apiClient.delete(`${ANNOUNCEMENT_BASE_URL}/${id}`);
    return response.data;
  }

  // Helper methods cho resident
  async getAnnouncements() {
    return await this.getByScope(AnnouncementScope.RESIDENTS);
  }

  async getEvents() {
    const announcements = await this.getActive();
    return announcements.filter(item => item.type === AnnouncementType.EVENT);
  }

  async getAnnouncementsOnly() {
    const announcements = await this.getActive();
    return announcements.filter(item => 
      item.type === AnnouncementType.ANNOUNCEMENT ||
      !item.type // fallback cho announcement không có type
    );
  }

  // GET api/Announcement/unread/count - Lấy số lượng thông báo chưa đọc
  async getUnreadCount(includeTypes = null) {
    const params = includeTypes ? { includeTypes } : {};
    const response = await apiClient.get(`${ANNOUNCEMENT_BASE_URL}/unread/count`, { params });
    return response.data;
  }

  // GET api/Announcement/unread - Lấy danh sách thông báo chưa đọc
  async getUnread(scope = null, includeTypes = null) {
    const params = {};
    if (scope) params.scope = scope;
    if (includeTypes) params.includeTypes = includeTypes;
    const response = await apiClient.get(`${ANNOUNCEMENT_BASE_URL}/unread`, { params });
    return response.data;
  }

  // POST api/Announcement/{id}/mark-as-read - Đánh dấu thông báo đã đọc
  async markAsRead(id) {
    const response = await apiClient.post(`${ANNOUNCEMENT_BASE_URL}/${id}/mark-as-read`);
    return response.data;
  }

  // Utility methods
  formatDate(date) {
    if (!date) return null;
    return new Date(date).toISOString().split('T')[0]; // yyyy-MM-dd
  }

  formatDateTime(date) {
    if (!date) return null;
    return new Date(date).toISOString(); // ISO format
  }

  isActive(announcement) {
    const now = new Date();
    const visibleFrom = new Date(announcement.visibleFrom);
    const visibleTo = announcement.visibleTo ? new Date(announcement.visibleTo) : null;
    
    return announcement.status === AnnouncementStatus.ACTIVE &&
           now >= visibleFrom &&
           (!visibleTo || now <= visibleTo);
  }
}

export const announcementApi = new AnnouncementApiService();
export default announcementApi;