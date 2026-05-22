using SharedModel.DTOs;

namespace WebAPP.Models.ViewModels;

/// <summary>Главная страница инспектора (как у студента + блок проверок)</summary>
public class InspectorDashboardViewModel
{
    public ResidenceDto? Residence { get; set; }
    public InspectionDto? LatestBlockInspection { get; set; }
    public StudentRatingDto? Rating { get; set; }
    public List<RepairRequestDto> MyRecentRequests { get; set; } = new();

    // Раздел проверок
    public List<InspectionDto> MyInspections { get; set; } = new();
    /// <summary>Все блоки системы</summary>
    public List<BlockDto> Blocks { get; set; } = new();
    /// <summary>Блоки, ещё не проверенные сегодня (доступны для выбора)</summary>
    public List<BlockDto> AvailableBlocks { get; set; } = new();
    /// <summary>Проверки, проведённые сегодня (любым инспектором)</summary>
    public List<InspectionDto> TodayInspections { get; set; } = new();
    public bool IsInspectionDay { get; set; }

    public static Dictionary<int, string> Zones => new()
    {
        { 1, "Комната" },
        { 2, "Коридор" },
        { 3, "Туалет/Душевая" },
        { 4, "Кухня" }
    };
}

public class InspectorRepairRequestsViewModel
{
    public List<RepairRequestDto> Requests { get; set; } = new();
    public ResidenceDto? Residence { get; set; }
}

public class InspectorPointsViewModel
{
    public StudentRatingDto? Rating { get; set; }
    public List<StudentPointDto> Points { get; set; } = new();
}
