using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Room
{
    public int Id { get; set; }

    public string RoomNumber { get; set; } = null!;

    public int BlockId { get; set; }

    public int Capacity { get; set; }

    public int CurrentOccupancy { get; set; }

    public bool? IsActive { get; set; }

    public virtual Block Block { get; set; } = null!;

    public virtual ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();

    public virtual ICollection<RepairRequest> RepairRequests { get; set; } = new List<RepairRequest>();

    public virtual ICollection<Residence> Residences { get; set; } = new List<Residence>();
}
