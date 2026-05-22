namespace DAL.Entities;

public class EvictionRequest
{
    public int      Id          { get; set; }
    public int      UserId      { get; set; }
    public int      EducatorId  { get; set; }
    public DateTime RequestedAt { get; set; }

    /// <summary>"Pending" | "Confirmed"</summary>
    public string Status { get; set; } = "Pending";

    public int?      ManagerId   { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public virtual User  Student  { get; set; } = null!;
    public virtual User  Educator { get; set; } = null!;
    public virtual User? Manager  { get; set; }
}
