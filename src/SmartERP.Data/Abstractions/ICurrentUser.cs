namespace SmartERP.Data.Abstractions;

public interface ICurrentUser
{
    decimal? UserId { get; }
    decimal? EmployeeId { get; }
    string? Username { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}