using System.ComponentModel.DataAnnotations;
using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

public class RequisitionIndexViewModel
{
    public IEnumerable<Requisition> Requisitions { get; set; } = Enumerable.Empty<Requisition>();
}
