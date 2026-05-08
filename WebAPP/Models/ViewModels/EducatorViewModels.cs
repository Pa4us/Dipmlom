using SharedModel.DTOs;

namespace WebAPP.Models.ViewModels;

public class EducatorDashboardViewModel
{
    public DormitoryStatisticsDto Stats { get; set; } = new();
    public List<InspectionDto> RecentInspections { get; set; } = new();
    public List<BlockWeeklyScoreDto> BestBlocks { get; set; } = new();
    public List<BlockWeeklyScoreDto> WorstBlocks { get; set; } = new();
}

public class InspectionsFilterViewModel
{
    public List<InspectionDto> Inspections { get; set; } = new();
    public List<BlockDto> Blocks { get; set; } = new();
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public int? SelectedBlockId { get; set; }
    public int? SelectedFloor { get; set; }
}

public class PointsViewModel
{
    public List<StudentRatingDto> Ratings { get; set; } = new();
    public List<UserDto> Students { get; set; } = new();
    /// <summary>Прошедшие мероприятия для начисления баллов всем участникам</summary>
    public List<EventDto> PastEvents { get; set; } = new();
    /// <summary>Участники каждого мероприятия: EventId → список UserDto</summary>
    public Dictionary<int, List<UserDto>> EventParticipants { get; set; } = new();
}

public class EventsViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public List<UserDto> Students { get; set; } = new();
    /// <summary>Участники каждого мероприятия: EventId → список UserDto</summary>
    public Dictionary<int, List<UserDto>> EventParticipants { get; set; } = new();
}

public class InspectionScheduleViewModel
{
    public HashSet<int> EnabledDays { get; set; } = new();
}

/// <summary>Карточка студента с полной информацией для страницы «Студенты»</summary>
public class StudentCardDto
{
    public UserDto User { get; set; } = null!;
    public ResidenceDto? Residence { get; set; }
    public StudentRatingDto? Rating { get; set; }
}

public class StudentsListViewModel
{
    public List<StudentCardDto> Students { get; set; } = new();
    public string? Search { get; set; }
}
