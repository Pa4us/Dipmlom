using SharedModel.DTOs;

namespace WebAPP.Models.ViewModels;

public class ManagerDashboardViewModel
{
    public int TotalUsers       { get; set; }
    public int TotalStudents    { get; set; }
    public int TotalBlocks      { get; set; }
    public int TotalRooms       { get; set; }
    public int FreeRooms        { get; set; }
    public int CurrentResidents { get; set; }
    public List<UserDto> RecentUsers { get; set; } = new();
}

public class ManagerUsersViewModel
{
    public List<UserDto>  Users  { get; set; } = new();
    public List<RoleDto>  Roles  { get; set; } = new();
    public string?        Search { get; set; }
}

public class ManagerBlocksViewModel
{
    public List<BlockDto> Blocks   { get; set; } = new();
    public List<RoomDto>  AllRooms { get; set; } = new();
}

public class ManagerRoomsViewModel
{
    public List<RoomDto>  Rooms  { get; set; } = new();
    public List<BlockDto> Blocks { get; set; } = new();
}

public class ManagerRepairRequestsViewModel
{
    public List<RepairRequestDto> Requests { get; set; } = new();
    /// <summary>Фильтр по статусу (null = все)</summary>
    public string? StatusFilter { get; set; }
}

public class ManagerResidencesViewModel
{
    public List<ResidenceDto> CurrentResidences { get; set; } = new();
    /// <summary>Студенты без текущего проживания (для формы заселения)</summary>
    public List<UserDto>      Students          { get; set; } = new();
    public List<RoomDto>      Rooms             { get; set; } = new();
    public List<BlockDto>     Blocks            { get; set; } = new();
}
