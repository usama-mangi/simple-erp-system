namespace SmartERP.Data.Models;

// Closed jobs within the reporting window, grouped by the employment status
// recorded on them (resigned vs terminated vs retired, etc.).
public record ExitStatusCount(string Status, int Exits);
