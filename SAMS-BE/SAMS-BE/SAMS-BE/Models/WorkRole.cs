using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.Models
{
    [Table("work_roles", Schema = "building")]
    public class WorkRole
    {
        [Key]
        [Column("role_id")]
        public Guid RoleId { get; set; }

        [Column("role_key")]
        [Required, MaxLength(100)]
        public string RoleKey { get; set; } = null!;

        [Column("role_name")]
        [Required, MaxLength(200)]
        public string RoleName { get; set; } = null!;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation property (1:N)
        public ICollection<StaffProfile>? StaffProfiles { get; set; }
    }
}
