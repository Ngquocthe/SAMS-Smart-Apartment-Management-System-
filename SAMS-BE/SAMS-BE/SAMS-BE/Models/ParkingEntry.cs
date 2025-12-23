using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("parking_entries", Schema = "building")]
[Index("CardId", "EntryTime", Name = "IX_parking_entries_card_time")]
public partial class ParkingEntry
{
    [Key]
    [Column("parking_entry_id")]
    public Guid ParkingEntryId { get; set; }

    [Column("entry_time")]
    [Precision(3)]
    public DateTime EntryTime { get; set; }

    [Column("exit_time")]
    [Precision(3)]
    public DateTime? ExitTime { get; set; }

    [Column("card_id")]
    public Guid? CardId { get; set; }

    [Column("vehicle_id")]
    public Guid? VehicleId { get; set; }

    [Column("plate_snapshot")]
    [StringLength(255)]
    public string? PlateSnapshot { get; set; }

    [Column("entry_gate")]
    [StringLength(64)]
    public string? EntryGate { get; set; }

    [Column("exit_gate")]
    [StringLength(64)]
    public string? ExitGate { get; set; }

    [Column("fee_amount", TypeName = "decimal(18, 2)")]
    public decimal? FeeAmount { get; set; }

    [Column("fee_currency")]
    [StringLength(3)]
    [Unicode(false)]
    public string? FeeCurrency { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CardId")]
    [InverseProperty("ParkingEntries")]
    public virtual AccessCard? Card { get; set; }

    [ForeignKey("VehicleId")]
    [InverseProperty("ParkingEntries")]
    public virtual Vehicle? Vehicle { get; set; }
}
