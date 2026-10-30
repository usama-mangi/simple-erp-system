using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Exceptions.Constraint.EmployeeDetail;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

public class EmploymentController(
    IEmploymentDetailsRepository _detailsRepository,
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

            var details = await _detailsRepository.GetAllByEmployeeAsync(employeeId);
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
            TempData["ErrorMessage"] = ex.Message;
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

            await PopulateSelectLists();

            return View(new EmploymentDetailCreateViewModel { EmployeeId = employee.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = ex.Message;
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
            await _detailsRepository.CreateAsync(new EmploymentDetail
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
            });

            TempData["SuccessMessage"] = $"Assignment {model.JobTitle} created successfully";
            return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
        }
        catch (DuplicateCurrentJobException ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(nameof(model.JobTitle), ex.Message);
        }
        catch (ConstraintException ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(FieldErrorKey(ex.FieldName), ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(string.Empty, ex.Message);
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
            await _detailsRepository.CloseAsync(model.Id, model.EndDate);

            TempData["SuccessMessage"] = "Assignment closed successfully";
        }
        catch (ConstraintException ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = $"{ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
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
