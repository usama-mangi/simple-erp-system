namespace SmartERP.Data.Models;

// A department that currently has no open department_managers row. The alert
// stat: whoever established this program needs a manager, yesterday.
public record UnmanagedDepartment(string Code, string Name, string Location, DateTime Established);
