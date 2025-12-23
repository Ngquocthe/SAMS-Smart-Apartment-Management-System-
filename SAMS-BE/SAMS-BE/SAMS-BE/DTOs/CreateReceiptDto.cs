using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class CreateReceiptDto
    {
        // ?? Optional: N?u không cung c?p, h? th?ng s? t? ??ng generate
        [StringLength(64)]
        public string? ReceiptNo { get; set; }

        [Required]
        public Guid InvoiceId { get; set; }

        // ?? Optional: N?u không cung c?p, l?y th?i gian hi?n t?i
        public DateTime? ReceivedDate { get; set; }

        [Required]
        public Guid MethodId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal AmountTotal { get; set; }

        [StringLength(1000)]
        public string? Note { get; set; }

        // ?? Optional: N?u không cung c?p, l?y t? user hi?n t?i ?ang login
        public Guid? CreatedBy { get; set; }
    }
}
