namespace SAMS_BE.Enums
{
    /// <summary>
    /// Trạng thái của xe
    /// </summary>
    public enum VehicleStatus
    {
        /// <summary>
        /// Chờ duyệt - Xe mới đăng ký, đang chờ admin xét duyệt
        /// </summary>
        PENDING,

        /// <summary>
        /// Đã duyệt - Xe đã được phê duyệt, có thể sử dụng
        /// </summary>
        ACTIVE,

        /// <summary>
        /// Bị từ chối - Yêu cầu đăng ký xe bị từ chối
        /// </summary>
        REJECTED,

        /// <summary>
        /// Không hoạt động - Xe đã bị vô hiệu hóa (hủy đăng ký, xe không còn sử dụng)
        /// </summary>
        INACTIVE
    }
}
