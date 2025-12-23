using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.Models
{
    [Table("contract_documents")]
    public class ContractDocument
    {
        [Key]
        [Column("contract_document_id")]
        public Guid ContractDocumentId { get; set; }

        [Required]
        [Column("contract_id")]
        public Guid ContractId { get; set; }

        [Required]
        [Column("document_id")]
        public Guid DocumentId { get; set; }

        [Required]
        [Column("document_type")]
        [MaxLength(32)]
        public string DocumentType { get; set; } = "CONTRACT";

        public Contract Contract { get; set; } = null!;

        public Document Document { get; set; } = null!;
    }
}
