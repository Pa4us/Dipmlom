using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class BlockWeeklyScore
{
    public int Id { get; set; }

    public int BlockId { get; set; }

    public int WeekNumber { get; set; }

    public int Year { get; set; }

    public decimal Score { get; set; }

    public DateOnly InspectionDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Block Block { get; set; } = null!;
}
