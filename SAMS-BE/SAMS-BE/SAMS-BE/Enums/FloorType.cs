namespace SAMS_BE.Enums
{
    /// <summary>
    /// Loại tầng trong tòa nhà
    /// </summary>
    public enum FloorType
    {
        /// <summary>
        /// Tầng hầm (Basement) - Số tầng âm (-1, -2, -3, ...)
        /// </summary>
        BASEMENT,

        /// <summary>
        /// Tầng thương mại (Commercial) - Dành cho cửa hàng, văn phòng
        /// </summary>
        COMMERCIAL,

        /// <summary>
        /// Tầng tiện ích chung (AMENITY) - Dành cho tiện ích chung như gym, hồ bơi
        /// </summary>
        AMENITY,

        /// <summary>
        /// Tầng dịch vụ (Service) - Dành cho kỹ thuật, máy móc
        /// </summary>
        SERVICE,

        /// <summary>
        /// Tầng căn hộ (Residential) - Dành cho căn hộ ở
        /// </summary>
        RESIDENTIAL
    }
}
