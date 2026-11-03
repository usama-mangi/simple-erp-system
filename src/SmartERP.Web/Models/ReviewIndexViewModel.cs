using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

public class ReviewIndexViewModel
{
    public IEnumerable<ReviewCycle> Cycles { get; set; } = [];
    public decimal? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public decimal? CycleId { get; set; }
    public IEnumerable<ManagerReview> Reviews { get; set; } = [];
}
