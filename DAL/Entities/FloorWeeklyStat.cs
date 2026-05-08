using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class FloorWeeklyStat
{
    public int Id { get; set; }

    public int Floor { get; set; }

    public int WeekNumber { get; set; }

    public int Year { get; set; }

    public decimal AverageScore { get; set; }

    public int BlocksCount { get; set; }

    public DateTime? CalculatedAt { get; set; }
}
