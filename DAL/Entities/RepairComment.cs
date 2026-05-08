using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class RepairComment
{
    public int Id { get; set; }

    public int RepairRequestId { get; set; }

    public int UserId { get; set; }

    public string Comment { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual RepairRequest RepairRequest { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
