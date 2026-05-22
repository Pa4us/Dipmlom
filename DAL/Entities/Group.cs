namespace DAL.Entities;

public class Group
{
    public int    Id        { get; set; }
    public string Name      { get; set; } = null!;
    public int?   FacultyId { get; set; }

    public virtual Faculty?        Faculty { get; set; }
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
