namespace SharedModel.DTOs;

public class EvictionRequestDto
{
    public int      Id            { get; set; }
    public int      UserId        { get; set; }
    public string   StudentName   { get; set; } = string.Empty;
    public string   StudentUsername { get; set; } = string.Empty;
    public string?  BlockNumber   { get; set; }
    public string?  RoomNumber    { get; set; }
    public int      EducatorId    { get; set; }
    public string   EducatorName  { get; set; } = string.Empty;
    public DateTime RequestedAt   { get; set; }
    public string   Status        { get; set; } = string.Empty;
    public int?     ManagerId     { get; set; }
    public DateTime? ConfirmedAt  { get; set; }
}

public class CreateEvictionRequestDto
{
    public int       EducatorId { get; set; }
    public List<int> UserIds    { get; set; } = new();
}

public class ConfirmEvictionDto
{
    public int       ManagerId          { get; set; }
    public List<int> EvictionRequestIds { get; set; } = new();
}
