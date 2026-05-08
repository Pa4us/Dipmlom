using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public int RoleId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();

    public virtual ICollection<RepairComment> RepairComments { get; set; } = new List<RepairComment>();

    public virtual ICollection<RepairRequest> RepairRequestAssignedTos { get; set; } = new List<RepairRequest>();

    public virtual ICollection<RepairRequest> RepairRequestRequestedBies { get; set; } = new List<RepairRequest>();

    public virtual ICollection<Residence> Residences { get; set; } = new List<Residence>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<StudentPoint> StudentPoints { get; set; } = new List<StudentPoint>();
}
