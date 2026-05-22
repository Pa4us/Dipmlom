namespace DAL.Entities;

public class CheckInRequest
{
    public int      Id        { get; set; }
    public int      ManagerId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>"Pending" | "Completed"</summary>
    public string Status { get; set; } = "Pending";

    public virtual User                          Manager { get; set; } = null!;
    public virtual ICollection<CheckInRequestItem> Items { get; set; } = new List<CheckInRequestItem>();
}
