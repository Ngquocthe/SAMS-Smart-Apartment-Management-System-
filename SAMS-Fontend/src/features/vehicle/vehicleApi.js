import api from "../../lib/apiClient";

/**
 * Lấy danh sách xe của cư dân hiện tại
 */
export const getMyVehicles = async () => {
  const response = await api.get("/vehicles/my-vehicles");
  return response.data;
};

/**
 * Tạo ticket hủy đăng ký xe
 * @param {Object} data - { vehicleId, subject, description, attachmentFileIds }
 */
export const cancelVehicleRegistration = async (data) => {
  const response = await api.post("/vehicles/cancel", data);
  return response.data;
};

/**
 * Format trạng thái xe
 */
export const getVehicleStatusText = (status) => {
  const statusMap = {
    PENDING: "Chờ duyệt",
    ACTIVE: "Đang hoạt động",
    REJECTED: "Bị từ chối",
    INACTIVE: "Không hoạt động",
  };
  return statusMap[status] || status;
};

/**
 * Màu sắc cho trạng thái xe
 */
export const getVehicleStatusColor = (status) => {
  const colorMap = {
    PENDING: "orange",
    ACTIVE: "green",
    REJECTED: "red",
    INACTIVE: "default",
  };
  return colorMap[status] || "default";
};
