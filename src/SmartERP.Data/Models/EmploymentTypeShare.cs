namespace SmartERP.Data.Models;

// Open jobs grouped by employment_type title: workforce mix.
public record EmploymentTypeShare(string Type, int Headcount);
