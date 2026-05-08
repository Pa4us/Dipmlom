using SharedModel.DTOs;

namespace WebAPP.Models.ViewModels;

public class StudentDashboardViewModel
{
    public ResidenceDto? Residence { get; set; }
    public InspectionDto? LatestBlockInspection { get; set; }
    public StudentRatingDto? Rating { get; set; }
    public List<RepairRequestDto> MyRecentRequests { get; set; } = new();
    /// <summary>Ближайшие мероприятия (до 3 штук) для виджета на дашборде</summary>
    public List<EventDto> UpcomingEvents { get; set; } = new();
}

public class StudentRepairRequestsViewModel
{
    public List<RepairRequestDto> Requests { get; set; } = new();
    /// <summary>Текущее проживание студента (для автозаполнения блока/комнаты)</summary>
    public ResidenceDto? Residence { get; set; }
}

public class StudentPointsViewModel
{
    public StudentRatingDto? Rating { get; set; }
    public List<StudentPointDto> Points { get; set; } = new();
}

public class StudentEventsViewModel
{
    /// <summary>Предстоящие мероприятия (дата >= сегодня)</summary>
    public List<EventDto> UpcomingEvents { get; set; } = new();
    /// <summary>Прошедшие мероприятия</summary>
    public List<EventDto> PastEvents { get; set; } = new();
}
