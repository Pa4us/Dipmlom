using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class StudentPoint
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int Points { get; set; }

    public string PointsType { get; set; } = null!;

    public string SourceType { get; set; } = null!;

    public int? SourceId { get; set; }

    public string Reason { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
