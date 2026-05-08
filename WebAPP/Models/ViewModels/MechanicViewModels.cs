using SharedModel.DTOs;

namespace WebAPP.Models.ViewModels;

public class MechanicDashboardViewModel
{
    public List<RepairRequestDto> NewRequests { get; set; } = new();
    public List<RepairRequestDto> MyRequests { get; set; } = new();
}

public class RepairRequestDetailsViewModel
{
    public RepairRequestDto Request { get; set; } = new();
    public List<RepairCommentDto> Comments { get; set; } = new();
}
