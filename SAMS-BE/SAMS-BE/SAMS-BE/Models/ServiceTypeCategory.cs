using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAMS_BE.Models
{
    [Table("service_type_categories", Schema = "building")]
    [Index(nameof(Name), IsUnique = true, Name = "UQ_stc_name")]
    public class ServiceTypeCategory
    {
        [Key]
        [Column("category_id")]
        public Guid CategoryId { get; set; }

        [Column("name"), StringLength(100)]
        public string Name { get; set; } = default!;

        [Column("description"), StringLength(255)]
        public string? Description { get; set; }

        [Column("created_at"), Precision(3)]
        public DateTime CreatedAt { get; set; }

        [InverseProperty(nameof(ServiceType.Category))]
        public ICollection<ServiceType> ServiceTypes { get; set; } = new List<ServiceType>();
    }
}
