using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Residence
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RoomId { get; set; }

    public int BlockId { get; set; }

    public DateOnly MoveInDate { get; set; }

    public DateOnly? MoveOutDate { get; set; }

    public bool? IsCurrent { get; set; }

    public virtual Block Block { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
