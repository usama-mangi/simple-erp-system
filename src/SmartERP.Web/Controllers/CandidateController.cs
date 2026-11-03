using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;
using SmartERP.Data.Abstractions;

namespace SmartERP.Web.Controllers;

[Authorize]
public class CandidateController(
    ICandidateRepository _candidateRepository,
    IEmploymentRepository _employmentRepository,
    ILookupRepository _lookupRepository,
    ILogger<CandidateController> _logger
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var candidates = await _candidateRepository.GetAllCandidatesAsync();

            var model = new CandidateIndexViewModel
            {
                Candidates = candidates
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        try
        {
            var model = new CandidateCreateViewModel();

            await PopulateSelectListsAsync(model);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CandidateCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(model);
            return View(model);
        }

        try
        {
            var candidate = new Candidate
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Phone = model.Phone,
                ResumeUrl = model.ResumeUrl,
                Source = model.Source,
                StatusId = model.StatusId
            };

            await _candidateRepository.CreateCandidateAsync(candidate);

            TempData["SuccessMessage"] = $"Candidate created: {model.FirstName} {model.LastName}.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Create));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Hire(decimal candidateId)
    {
        try
        {
            var candidate = await _candidateRepository.GetCandidateByIdAsync(candidateId);
            if (candidate == null) return NotFound();

            // Check if candidate already has an employment assignment
            var existingAssignment = await _employmentRepository.GetByEmployeeAsync(candidateId);
            if (existingAssignment != null)
            {
                TempData["ErrorMessage"] = $"{candidate.FirstName} {candidate.LastName} already has an employment assignment.";
                return RedirectToAction(nameof(Index));
            }

            // Populate positions from lookup repository
            var positions = await _lookupRepository.GetPositionsAsync();
            ViewData["Positions"] = positions
                .Select(p => new SelectListItem(p.Title, p.Id.ToString()))
                .ToList();

            var model = new EmploymentDetailCreateViewModel
            {
                EmployeeId = candidateId,
                PositionId = candidate.StatusId,
                Salary = 0,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Hire(EmploymentDetailCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(new CandidateCreateViewModel());
            return View(model);
        }

        try
        {
            if (model.EmployeeId <= 0) return NotFound();

            var candidate = await _candidateRepository.GetCandidateByIdAsync(model.EmployeeId);
            if (candidate == null) return NotFound();

            // Create employment detail
            var detail = new EmploymentDetail
            {
                EmployeeId = model.EmployeeId,
                PositionId = model.PositionId,
                Salary = model.Salary,
                StartDate = model.StartDate.ToDateTime(TimeOnly.MinValue),
                EndDate = null,
                DepartmentId = 0,
                EmploymentTypeId = 0,
                EmploymentStatusId = 1
            };

            await _employmentRepository.CreateAsync(detail);

            // Update candidate status to Hired
            candidate.StatusId = 5; // Hired status
            await _candidateRepository.UpdateCandidateAsync(candidate);

            TempData["SuccessMessage"] = $"{candidate.FirstName} {candidate.LastName} has been hired successfully.";
            return RedirectToAction("Details", "Employee", new { id = model.EmployeeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Hire), new { employeeId = model.EmployeeId });
        }
    }

    private async Task PopulateSelectListsAsync(CandidateCreateViewModel model)
    {
        ViewData["Statuses"] = new List<SelectListItem>
        {
            new SelectListItem { Text = "Started", Value = "1" },
            new SelectListItem { Text = "Under Review", Value = "2" },
            new SelectListItem { Text = "Interview", Value = "3" },
            new SelectListItem { Text = "Offer", Value = "4" },
            new SelectListItem { Text = "Hired", Value = "5" },
            new SelectListItem { Text = "Rejected", Value = "6", Selected = false }
        };
    }
}
