using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Exceptions.Constraint.EmployeeDetail;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

public class EmploymentController(
    IEmploymentRepository _employmentRepository,
    IEmployeeRepository _employeeRepository,
    IDepartmentRepository _departmentRepository,
    ILookupRepository _lookupRepository,
    ILogger<EmploymentController> _logger
) : Controller
{

    [HttpGet]
    public async Task<IActionResult> Index(double employeeId)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);

            if (employee == null) return NotFound();

            var details = await _employmentRepository.GetByEmployeeAsync(employeeId);
            await PopulateTitlesAsync();

            return View(new EmploymentIndexViewModel
            {
                Employee = employee,
                Details = details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction("Index", "Employee");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(double employeeId)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);

            if (employee == null) return NotFound();

            var currentAssignment = await _employmentRepository.GetCurrentAsync(employee.Id);

            ViewData["HasOpenAssignment"] = currentAssignment is not null;
            await PopulateSelectLists();

            return View(new EmploymentDetailCreateViewModel { EmployeeId = employee.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Index), new { employeeId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmploymentDetailCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists();
            return View(model);
        }

        try
        {
            var detail = new EmploymentDetail
            {
                EmployeeId = model.EmployeeId,
                JobTitle = model.JobTitle,
                Salary = model.Salary,
                StartDate = model.StartDate.ToDateTime(TimeOnly.MinValue),
                EndDate = model.EndDate is { } endDate
                    ? endDate.ToDateTime(TimeOnly.MinValue)
                    : null,
                DepartmentId = model.DepartmentId,
                EmploymentTypeId = model.EmploymentTypeId,
                EmploymentStatusId = model.EmploymentStatusId
            };

            // App-level gate first: a replacing assignment must start after the
            // one it ends, otherwise the old row's chk_det_end_date check would
            // reject the close mid-transaction. Reading the current row directly
            // (instead of the full history) keeps this check a single lookup.
            var currentAssignment = await _employmentRepository.GetCurrentAsync(model.EmployeeId);

            // Server re-checks the open row instead of trusting the checkbox:
            // replacing without one is a stale-link case, not an error path.
            if (currentAssignment is not null)
            {
                if (detail.StartDate <= currentAssignment.StartDate)
                {
                    ModelState.AddModelError(
                        nameof(model.StartDate),
                        $"The new assignment must start after the current one started ({currentAssignment.StartDate.ToShortDateString()})."
                    );
                    await PopulateSelectLists();
                    return View(model);
                }

                if (!model.ConfirmReplace)
                {
                    ModelState.AddModelError(
                        nameof(model.ConfirmReplace),
                        "This employee already has an open assignment. Tick the confirmation box to end it and take over with the new one."
                    );
                    await PopulateSelectLists();
                    return View(model);
                }

                await _employmentRepository.CreateReplacingCurrentAsync(detail);
            }
            else
            {
                await _employmentRepository.CreateAsync(detail);
            }

            TempData["SuccessMessage"] = $"Assignment {model.JobTitle} created successfully";
            return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
        }
        catch (DuplicateCurrentJobException ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(nameof(model.ConfirmReplace), ex.Message);
        }
        catch (ConstraintException ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(FieldErrorKey(ex.FieldName), ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(string.Empty, UserMessages.GenericFailure);
        }

        await PopulateSelectLists();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(EmploymentDetailCloseViewModel model)
    {
        try
        {
            await _employmentRepository.EndCurrentAsync(model.EmployeeId, model.EndDate);

            TempData["SuccessMessage"] = "Assignment closed successfully";
        }
        catch (ConstraintException ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
        }

        return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferDepartment(EmploymentTransferDepartmentViewModel model)
    {
        try
        {
            var currentAssignment = await _employmentRepository.GetCurrentAsync(model.EmployeeId);

            if (currentAssignment == null)
            {
                TempData["ErrorMessage"] = "This employee has no open assignment to transfer.";
            }
            else
            {
                await _employmentRepository.TransferCurrentDepartmentAsync(model.EmployeeId, model.DepartmentId);

                TempData["SuccessMessage"] = "Department changed successfully";
            }
        }
        catch (ConstraintException ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
        }

        return RedirectToAction("Details", "Employee", new { id = model.EmployeeId });
    }

    private static string FieldErrorKey(string fieldName) => fieldName switch
    {
        "JobTitle" => nameof(EmploymentDetailCreateViewModel.JobTitle),
        "Salary" => nameof(EmploymentDetailCreateViewModel.Salary),
        "StartDate" => nameof(EmploymentDetailCreateViewModel.StartDate),
        "EndDate" => nameof(EmploymentDetailCreateViewModel.EndDate),
        "DepartmentId" => nameof(EmploymentDetailCreateViewModel.DepartmentId),
        "EmploymentTypeId" => nameof(EmploymentDetailCreateViewModel.EmploymentTypeId),
        "EmploymentStatusId" => nameof(EmploymentDetailCreateViewModel.EmploymentStatusId),
        _ => string.Empty
    };

    private async Task PopulateTitlesAsync()
    {
        var types = await _lookupRepository.GetEmploymentTypesAsync();
        var statuses = await _lookupRepository.GetEmploymentStatusesAsync();
        var departments = await _departmentRepository.GetAllAsync();

        ViewData["EmploymentTypeTitles"] = types.ToDictionary(t => t.Id, t => t.Title);
        ViewData["EmploymentStatusTitles"] = statuses.ToDictionary(s => s.Id, s => s.Title);
        ViewData["DepartmentTitles"] = departments.ToDictionary(d => d.Id, d => d.Name);
    }

    private async Task PopulateSelectLists()
    {
        var departments = await _departmentRepository.GetAllAsync();
        var types = await _lookupRepository.GetEmploymentTypesAsync();
        var statuses = await _lookupRepository.GetEmploymentStatusesAsync();

        // Ids come from the lookup tables themselves; enum positions in code
        // cannot be trusted after lookups are reseeded or extended.
        ViewData["Departments"] = departments
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString()));
        ViewData["EmploymentTypes"] = types
            .Select(t => new SelectListItem(t.Title, t.Id.ToString()));
        ViewData["EmploymentStatuses"] = statuses
            .Select(s => new SelectListItem(s.Title, s.Id.ToString()));
    }
}
