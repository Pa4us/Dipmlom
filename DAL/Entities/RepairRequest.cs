using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class RepairRequest
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int BlockId { get; set; }

    public int? RoomId { get; set; }

    public int RequestedById { get; set; }

    public int? AssignedToId { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual User? AssignedTo { get; set; }

    public virtual Block Block { get; set; } = null!;

    public virtual ICollection<RepairComment> RepairComments { get; set; } = new List<RepairComment>();

    public virtual User RequestedBy { get; set; } = null!;

    public virtual Room? Room { get; set; }
}
