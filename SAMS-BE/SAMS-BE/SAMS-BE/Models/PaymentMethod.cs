using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("payment_methods", Schema = "building")]
[Index("Code", Name = "UQ_payment_methods_code", IsUnique = true)]
public partial class PaymentMethod
{
    [Key]
    [Column("payment_method_id")]
    public Guid PaymentMethodId { get; set; }

    [Column("code")]
    [StringLength(64)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("active")]
    public bool Active { get; set; }

    [InverseProperty("Method")]
    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
