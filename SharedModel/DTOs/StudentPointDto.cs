using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModel.DTOs
{
    public class StudentPointDto : BaseDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserFullName { get; set; }
        public int Points { get; set; }
        public string PointsType { get; set; } = string.Empty; // "Award" или "Penalty"
        public string SourceType { get; set; } = string.Empty; // "Event", "Inspection", "Manual"
        public int? SourceId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateStudentPointDto
    {
        public int UserId { get; set; }
        public int Points { get; set; }
        public string PointsType { get; set; } = "Award";
        public string SourceType { get; set; } = "Manual";
        public int? SourceId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class UpdateStudentPointDto
    {
        public int Id { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class AwardPointsDto // Плюс баллы студенту
    {
        public int UserId { get; set; }
        public int Points { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string SourceType { get; set; } = "Manual";
        public int? SourceId { get; set; }
    }

    public class DeductPointsDto // Минус баллы студенту
    {
        public int UserId { get; set; }
        public int Points { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string SourceType { get; set; } = "Manual";
        public int? SourceId { get; set; }
    }
}
