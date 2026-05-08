using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Block
{
    public int Id { get; set; }

    public string BlockNumber { get; set; } = null!;

    public int Floor { get; set; }

    public int BlockIndex { get; set; }

    public virtual ICollection<BlockWeeklyScore> BlockWeeklyScores { get; set; } = new List<BlockWeeklyScore>();

    public virtual ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();

    public virtual ICollection<RepairRequest> RepairRequests { get; set; } = new List<RepairRequest>();

    public virtual ICollection<Residence> Residences { get; set; } = new List<Residence>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
