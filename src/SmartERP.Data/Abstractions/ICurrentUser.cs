namespace SmartERP.Data.Abstractions;

public interface ICurrentUser
{
    double? UserId { get; }
    double? EmployeeId { get; }
    string? Username { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}