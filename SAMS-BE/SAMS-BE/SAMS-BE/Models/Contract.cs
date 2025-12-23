using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.Models
{
    [Table("contracts")]
    public class Contract
    {
        [Key]
        [Column("contract_id")]
        public Guid ContractId { get; set; }

        [Required]
        [Column("apartment_id")]
        public Guid ApartmentId { get; set; }

        [Column("contract_code")]
        [MaxLength(64)]
        public string? ContractCode { get; set; }

        [Required]
        [Column("start_date")]
        public DateOnly StartDate { get; set; }

        [Column("end_date")]
        public DateOnly? EndDate { get; set; }

        [Required]
        [Column("status")]
        [MaxLength(32)]
        public string Status { get; set; } = "ACTIVE";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("created_by")]
        [MaxLength(190)]
        public string? CreatedBy { get; set; }

        /* ================= NAVIGATION ================= */

        public Apartment Apartment { get; set; } = null!;

        public ICollection<ContractDocument> ContractDocuments { get; set; }
            = new List<ContractDocument>();
    }
}
