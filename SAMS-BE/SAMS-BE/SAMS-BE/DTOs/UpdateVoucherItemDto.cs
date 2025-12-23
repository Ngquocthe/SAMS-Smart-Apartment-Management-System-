using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class UpdateVoucherItemDto
    {
        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0")]
        public decimal? Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be greater than or equal to 0")]
        public decimal? UnitPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Amount must be greater than or equal to 0")]
        public decimal? Amount { get; set; }
        
        public Guid? ServiceTypeId { get; set; }
     
        public Guid? ApartmentId { get; set; }
    }
}