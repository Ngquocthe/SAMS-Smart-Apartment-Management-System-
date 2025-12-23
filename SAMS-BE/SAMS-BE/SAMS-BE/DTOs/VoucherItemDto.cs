namespace SAMS_BE.DTOs
{
    /// <summary>
 /// DTO for VoucherItem (used in Ticket context)
    /// </summary>
    public class VoucherItemDto
    {
        public Guid VoucherItemsId { get; set; }
     
        public Guid VoucherId { get; set; }
        
   public string? Description { get; set; }
        
        public decimal? Quantity { get; set; }
        
        public decimal? UnitPrice { get; set; }

        public decimal? Amount { get; set; }
   
        public Guid? ServiceTypeId { get; set; }
        
 public Guid? ApartmentId { get; set; }
   
        public DateTime CreatedAt { get; set; }
    }
}
