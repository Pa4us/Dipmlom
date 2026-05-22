namespace SharedModel.DTOs;

public class CheckInRequestDto
{
    public int    Id          { get; set; }
    public int    ManagerId   { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status      { get; set; } = string.Empty;
    public int    TotalCount  { get; set; }
    public int    PendingCount { get; set; }
    public List<CheckInRequestItemDto> Items { get; set; } = new();
}

public class CheckInRequestItemDto
{
    public int    Id               { get; set; }
    public int    CheckInRequestId { get; set; }
    public string FullName         { get; set; } = string.Empty;
    public string? PhoneNumber     { get; set; }
    public string? Faculty         { get; set; }
    public string? Group           { get; set; }
    public string BlockNumber      { get; set; } = string.Empty;
    public string Status           { get; set; } = string.Empty;
    public int?   UserId           { get; set; }
    public string? Username        { get; set; }
    public string? GeneratedPassword { get; set; }
}

public class CreateCheckInRequestDto
{
    public int ManagerId { get; set; }
    public List<CheckInItemRowDto> Items { get; set; } = new();
}

public class CheckInItemRowDto
{
    public int    RowNumber      { get; set; }
    public string FullName       { get; set; } = string.Empty;
    public string? PhoneNumber    { get; set; }
    public string? Faculty       { get; set; }
    public string? Group         { get; set; }
    public string BlockNumber    { get; set; } = string.Empty;
    public string? Error         { get; set; }
}

public class CheckInPreviewDto
{
    public List<CheckInItemRowDto> ValidRows   { get; set; } = new();
    public List<CheckInItemRowDto> InvalidRows { get; set; } = new();
}

public class ProcessCheckInResultDto
{
    public int    ProcessedCount { get; set; }
    public List<CheckInRequestItemDto> CreatedItems { get; set; } = new();
}
