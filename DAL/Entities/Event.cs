using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Event
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly EventDate { get; set; }

    public string? Location { get; set; }

    public int OrganizerId { get; set; }

    public int? PointsAwarded { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();

    public virtual User Organizer { get; set; } = null!;
}
