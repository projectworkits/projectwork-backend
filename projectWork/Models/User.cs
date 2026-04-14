namespace projectWork.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public DateTime Verified { get; set; }
    public bool Admin { get; set; }
    public bool Collaborator { get; set; }
}
