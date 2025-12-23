using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("meter_readings", Schema = "building")]
[Index("MeterId", "ReadingTime", Name = "IX_meter_readings_time")]
[Index("MeterId", "ReadingTime", Name = "UQ_meter_readings", IsUnique = true)]
public partial class MeterReading
{
    [Key]
    [Column("reading_id")]
    public Guid ReadingId { get; set; }

    [Column("meter_id")]
    public Guid MeterId { get; set; }

    [Column("reading_time")]
    [Precision(3)]
    public DateTime ReadingTime { get; set; }

    [Column("index_value", TypeName = "decimal(18, 6)")]
    public decimal IndexValue { get; set; }

    [Column("captured_by")]
    [StringLength(190)]
    public string? CapturedBy { get; set; }

    [Column("note")]
    [StringLength(500)]
    public string? Note { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("MeterId")]
    [InverseProperty("MeterReadings")]
    public virtual Meter Meter { get; set; } = null!;
}
