using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Inspection
{
    public int Id { get; set; }

    public int BlockId { get; set; }

    public int? RoomId { get; set; }

    public int ZoneId { get; set; }

    public int InspectorId { get; set; }

    public DateOnly InspectionDate { get; set; }

    public int Score { get; set; }

    public string? Comment { get; set; }

    public string? PhotoPath { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Block Block { get; set; } = null!;

    public virtual User Inspector { get; set; } = null!;

    public virtual Room? Room { get; set; }

    public virtual InspectionZone Zone { get; set; } = null!;
}
