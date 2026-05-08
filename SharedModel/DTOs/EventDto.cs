using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModel.DTOs
{
    public class EventDto: BaseDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly EventDate { get; set; }
        public string? Location { get; set; }
        public int OrganizerId { get; set; }
        public string? OrganizerName { get; set; }
        public int PointsAwarded { get; set; }
        public int ParticipantsCount { get; set; }
    }

    public class CreateEventDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly EventDate { get; set; }
        public string? Location { get; set; }
        public int PointsAwarded { get; set; }
        public int OrganizerId { get; set; }  // заполняется из JWT токена в контроллере
    }

    public class UpdateEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly EventDate { get; set; }
        public string? Location { get; set; }
        public int PointsAwarded { get; set; }
    }

    public class RegisterForEventDto
    {
        public int EventId { get; set; }
        public int StudentId { get; set; }
    }
}
