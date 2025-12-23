using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class UpdateReceiptDto
    {
        public DateTime? ReceivedDate { get; set; }

        public Guid? MethodId { get; set; }

      [StringLength(1000)]
        public string? Note { get; set; }
    }
}
