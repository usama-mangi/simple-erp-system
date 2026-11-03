using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

[Authorize]
public class ReviewController(
    IReviewRepository _reviewRepository,
    IEmployeeRepository _employeeRepository,
    ILogger<ReviewController> _logger
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(decimal? employeeId)
    {
        try
        {
            var cycles = await _reviewRepository.GetCyclesAsync();

            var model = new ReviewIndexViewModel
            {
                Cycles = cycles,
                EmployeeId = employeeId
            };

            if (employeeId is { } scopedEmployeeId)
            {
                var employee = await _employeeRepository.GetByIdAsync(scopedEmployeeId);

                if (employee == null) return NotFound();

                model.EmployeeName = $"{employee.FirstName} {employee.LastName}";
                model.Reviews = await _reviewRepository.GetReviewsByEmployeeAsync(scopedEmployeeId);
            }

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
    public async Task<IActionResult> Create(decimal? employeeId, decimal? cycleId)
    {
        try
        {
            var model = new ReviewCreateViewModel();

            if (employeeId is { } scopedEmployeeId)
            {
                var employee = await _employeeRepository.GetByIdAsync(scopedEmployeeId);

                if (employee == null) return NotFound();

                model.EmployeeId = employee.Id;
                model.EmployeeName = $"{employee.FirstName} {employee.LastName}";
            }

            if (cycleId.HasValue)
            {
                var cycle = await _reviewRepository.GetCycleByIdAsync(cycleId.Value);

                if (cycle != null)
                {
                    model.CycleId = cycleId.Value;
                    model.CycleTitle = cycle.Title;
                }
            }

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
    public async Task<IActionResult> Create(ReviewCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(model);
            return View(model);
        }

        try
        {
            var employee = await _employeeRepository.GetByIdAsync(model.EmployeeId);

            if (employee == null)
            {
                return NotFound();
            }

            var review = new ManagerReview
            {
                EmployeeId = model.EmployeeId,
                ReviewerId = model.ReviewerId,
                ReviewCycleId = model.CycleId,
                Rating = model.Rating,
                Comments = model.Comments,
                ReviewDate = model.ReviewDate,
                CreatedAt = DateTime.UtcNow
            };

            await _reviewRepository.CreateReviewAsync(review);

            TempData["SuccessMessage"] = $"Review created for {model.EmployeeName}.";
            return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Create), new { employeeId = model.EmployeeId, cycleId = model.CycleId });
        }
    }

    private async Task PopulateSelectListsAsync(ReviewCreateViewModel model)
    {
        var employees = await _employeeRepository.GetAllAsync();
        var cycles = await _reviewRepository.GetCyclesAsync();

        ViewData["Employees"] = employees
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Select(e => new SelectListItem(
                $"{e.FirstName} {e.LastName}",
                e.Id.ToString(),
                e.Id == model.EmployeeId
            ));
        ViewData["Cycles"] = cycles
            .Select(c => new SelectListItem(c.Title, c.Id.ToString(), c.Id == model.CycleId));

        // Load reviewers from employees (excluding the employee being reviewed)
        var potentialReviewers = employees
            .Where(e => e.Id != model.EmployeeId)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Select(e => new SelectListItem(
                $"{e.FirstName} {e.LastName}",
                e.Id.ToString()
            ));
        ViewData["Reviewers"] = potentialReviewers;
    }
}
