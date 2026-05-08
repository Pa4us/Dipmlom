using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class EventParticipant
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    public int PointsEarned { get; set; }

    public DateTime? ParticipatedAt { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
