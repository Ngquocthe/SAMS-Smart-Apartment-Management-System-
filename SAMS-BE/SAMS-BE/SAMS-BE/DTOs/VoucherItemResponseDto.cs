namespace SAMS_BE.DTOs
{
    /// <summary>
    /// DTO for VoucherItem response
    /// </summary>
    public class VoucherItemResponseDto
    {
        public Guid VoucherItemsId { get; set; }

        public Guid? ServiceTypeId { get; set; }

        public string ServiceTypeName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal? Quantity { get; set; }

        public decimal? UnitPrice { get; set; }

        public decimal? Amount { get; set; }

        public Guid? ApartmentId { get; set; }
    }
}
