namespace SmartERP.Data.Models;

public class AppUser
{
    public double Id { get; set; }
    public double EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Viewer";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = default;
}