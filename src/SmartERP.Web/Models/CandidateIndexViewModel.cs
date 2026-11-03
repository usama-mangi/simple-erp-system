using System.ComponentModel.DataAnnotations;
using SmartERP.Data.Models;

namespace SmartERP.Web.Models;

public class CandidateIndexViewModel
{
    public IEnumerable<Candidate> Candidates { get; set; } = Enumerable.Empty<Candidate>();
}
