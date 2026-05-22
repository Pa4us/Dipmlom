namespace DAL.Entities;

public class CheckInRequestItem
{
    public int    Id               { get; set; }
    public int    CheckInRequestId { get; set; }
    public string FullName          { get; set; } = null!;
    public string? PhoneNumber       { get; set; }
    public string? Faculty          { get; set; }
    public string? Group            { get; set; }
    public string BlockNumber       { get; set; } = null!;

    /// <summary>"Pending" | "CheckedIn"</summary>
    public string Status { get; set; } = "Pending";

    public int?   UserId            { get; set; }
    public string? GeneratedPassword { get; set; }

    public virtual CheckInRequest  Request { get; set; } = null!;
    public virtual User?           User    { get; set; }
}
