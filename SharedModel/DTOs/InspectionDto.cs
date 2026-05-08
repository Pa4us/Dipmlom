using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModel.DTOs
{
    public class InspectionDto: BaseDto
    {
        public int BlockId { get; set; }
        public string? BlockNumber { get; set; }
        public int Floor { get; set; }
        public int? RoomId { get; set; }
        public string? RoomNumber { get; set; }
        public int ZoneId { get; set; }
        public string? ZoneName { get; set; }
        public int InspectorId { get; set; }
        public string? InspectorName { get; set; }
        public DateOnly InspectionDate { get; set; }
        public int Score { get; set; }
        public string? Comment { get; set; }
        public string? PhotoPath { get; set; }
    }

    public class CreateInspectionDto
    {
        public int BlockId { get; set; }
        public int? RoomId { get; set; }
        public int ZoneId { get; set; }
        public int InspectorId { get; set; }   // заполняется из JWT токена в контроллере
        public DateOnly InspectionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public int Score { get; set; }
        public string? Comment { get; set; }
        public string? PhotoBase64 { get; set; } // для фото
    }

    public class UpdateInspectionDto
    {
        public int Id { get; set; }
        public int Score { get; set; }
        public string? Comment { get; set; }
        public string? PhotoBase64 { get; set; } // для обновления фото
    }

    public class InspectionReportDto
    {
        public int BlockId { get; set; }
        public string BlockNumber { get; set; } = string.Empty;
        public int AverageScore { get; set; }
        public List<InspectionDto> Inspections { get; set; } = new();
    }
}
