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
    public List<BlockDto> Blocks { get; set; } = new();
    public bool IsInspectionDay { get; set; }
    public bool AlreadyInspectedToday { get; set; }

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
