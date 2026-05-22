namespace DAL.Entities;

public class Faculty
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    public virtual ICollection<User>  Users  { get; set; } = new List<User>();
}
