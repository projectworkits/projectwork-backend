namespace projectWork.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordSalt { get; set; }
    public string PasswordHash { get; set; }
    public bool Admin { get; set; } = false;
    public bool Collaborator { get; set; } = false;
}
