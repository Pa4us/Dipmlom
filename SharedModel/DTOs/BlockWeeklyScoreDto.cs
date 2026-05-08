using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModel.DTOs
{
    public class BlockWeeklyScoreDto
    {
        public int BlockId { get; set; }
        public string BlockNumber { get; set; } = string.Empty;
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public decimal Score { get; set; }
        public DateOnly InspectionDate { get; set; }
    }

    public class FloorStatisticsDto
    {
        public int Floor { get; set; }
        public decimal AverageScore { get; set; }
        public int BlocksCount { get; set; }
        public List<BlockWeeklyScoreDto> BlockScores { get; set; } = new();
    }

    public class DormitoryStatisticsDto
    {
        public decimal AverageScore { get; set; }
        public int TotalBlocks { get; set; }
        public List<FloorStatisticsDto> Floors { get; set; } = new();
    }

    public class StudentRatingDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int EventPoints { get; set; }
        public int PenaltyPoints { get; set; }
        public decimal AverageCleanlinessScore { get; set; }
    }

    public class DateRangeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
