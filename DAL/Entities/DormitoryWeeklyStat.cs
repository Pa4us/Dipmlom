using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class DormitoryWeeklyStat
{
    public int Id { get; set; }

    public int WeekNumber { get; set; }

    public int Year { get; set; }

    public decimal AverageScore { get; set; }

    public int TotalBlocks { get; set; }

    public DateTime? CalculatedAt { get; set; }
}
