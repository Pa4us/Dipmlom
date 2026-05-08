using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class InspectionZone
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public virtual ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
}
