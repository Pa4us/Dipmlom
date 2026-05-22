using SharedModel.DTOs;

namespace WebAPP.Models.ViewModels;

public class EducatorDashboardViewModel
{
    public DormitoryStatisticsDto Stats { get; set; } = new();
    public List<InspectionDto> RecentInspections { get; set; } = new();
    public List<BlockWeeklyScoreDto> BestBlocks  { get; set; } = new();
    public List<BlockWeeklyScoreDto> WorstBlocks { get; set; } = new();

    // ─── Фильтры ──────────────────────────────────────────────────────────
    public List<BlockDto> Blocks        { get; set; } = new();
    public List<int>      Floors        { get; set; } = new();
    public string?        DateFrom      { get; set; }
    public string?        DateTo        { get; set; }
    public int?           SelectedBlockId { get; set; }
    public int?           SelectedFloor   { get; set; }
    public bool           IsFiltered    => !string.IsNullOrEmpty(DateFrom)
                                        || !string.IsNullOrEmpty(DateTo)
                                        || SelectedBlockId.HasValue
                                        || SelectedFloor.HasValue;
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
    public List<EventDto> PastEvents { get; set; } = new();
    public Dictionary<int, List<UserDto>> EventParticipants { get; set; } = new();
    public List<SanitaryBlockInfo> SanitaryBlocks { get; set; } = new();
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
    public int SelectedDay { get; set; } = 2; // 0=Пн … 6=Вс, по умолчанию среда
    public List<DateOnly> MonthDates { get; set; } = new();
}

public class SanitaryBlockInfo
{
    public BlockDto Block { get; set; } = null!;
    public int LastScore { get; set; }
    public DateOnly LastInspectionDate { get; set; }
    public List<UserDto> Residents { get; set; } = new();
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

public class ImportResidencesPreviewViewModel
{
    public ImportResidencesResultDto Result { get; set; } = new();
    public string ValidRowsJson { get; set; } = "[]";
}

public class ImportResidencesResultViewModel
{
    public ImportResidencesResultDto Result { get; set; } = new();
}

// ─── Заселение (воспитатель) ──────────────────────────────────────────────

public class EducatorCheckInViewModel
{
    /// <summary>Все ожидающие позиции заявок — плоский список для мультивыбора.</summary>
    public List<CheckInRequestItemDto> PendingItems { get; set; } = new();
    public int                         TotalPending  => PendingItems.Count;
}

public class EducatorCheckInResultViewModel
{
    public List<CheckInRequestItemDto> CreatedItems { get; set; } = new();
    public int                         ProcessedCount { get; set; }
}

// ─── Выселение (воспитатель) ──────────────────────────────────────────────

public class EducatorEvictionViewModel
{
    public List<StudentCardDto>     Residents        { get; set; } = new();
    public List<EvictionRequestDto> PendingEvictions { get; set; } = new();
    public string?                  Search           { get; set; }
}
