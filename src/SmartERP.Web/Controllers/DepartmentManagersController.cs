using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Exceptions.Constraint.DepartmentManager;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

public class DepartmentManagersController(
    IDepartmentManagerRepository _managersRepository,
    IDepartmentRepository _departmentRepository,
    IEmployeeRepository _employeeRepository,
    ILogger<DepartmentManagersController> _logger
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(double departmentId)
    {
        try
        {
            var department = await _departmentRepository.GetByIdAsync(departmentId);

            if (department == null) return NotFound();

            var managers = await _managersRepository.GetHistoryAsync(departmentId);

            return View(new DepartmentManagerIndexViewModel
            {
                Department = department,
                Managers = managers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction("Index", "Department");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(double departmentId)
    {
        try
        {
            var department = await _departmentRepository.GetByIdAsync(departmentId);

            if (department == null) return NotFound();

            await PopulateSelectLists();

            return View(new DepartmentManagerCreateViewModel { DepartmentId = department.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Index), new { departmentId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentManagerCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists();
            return View(model);
        }

        try
        {
            // Both assign paths for this repo end the current assignment before
            // inserting: AssignAsync routes to sp_assign_manager (open
            // assignments) or to the .NET transaction pair (dated backfill).
            await _managersRepository.AssignAsync(new DepartmentManager
            {
                DepartmentId = model.DepartmentId,
                ManagerId = model.ManagerId,
                StartDate = model.StartDate.ToDateTime(TimeOnly.MinValue),
                EndDate = model.EndDate is { } endDate
                    ? endDate.ToDateTime(TimeOnly.MinValue)
                    : null
            });

            TempData["SuccessMessage"] = $"Manager assignment created successfully";
            return RedirectToAction(nameof(Index), new { departmentId = model.DepartmentId });
        }
        catch (DuplicateCurrentManagerException ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(nameof(model.ManagerId), ex.Message);
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
    // Spec's quick-assign form: one submit ends the current assignment and
    // starts the new one effective today (sp_assign_manager).
    public async Task<IActionResult> Assign(DepartmentManagerAssignViewModel model)
    {
        try
        {
            await _managersRepository.AssignAsync(new DepartmentManager
            {
                DepartmentId = model.DepartmentId,
                ManagerId = model.ManagerId,
                StartDate = DateTime.Today
            });

            TempData["SuccessMessage"] = "Manager assigned successfully";
        }
        catch (DuplicateCurrentManagerException ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = ex.Message;
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

        return RedirectToAction("Details", "Department", new { id = model.DepartmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(DepartmentManagerCloseViewModel model)
    {
        try
        {
            await _managersRepository.EndCurrentAsync(model.DepartmentId, model.EndDate);

            TempData["SuccessMessage"] = "Manager assignment closed successfully";
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

        return RedirectToAction(nameof(Index), new { departmentId = model.DepartmentId });
    }

    private static string FieldErrorKey(string fieldName) => fieldName switch
    {
        "ManagerId" => nameof(DepartmentManagerCreateViewModel.ManagerId),
        "StartDate" => nameof(DepartmentManagerCreateViewModel.StartDate),
        "EndDate" => nameof(DepartmentManagerCreateViewModel.EndDate),
        _ => string.Empty
    };

    private async Task PopulateSelectLists()
    {
        var employees = await _employeeRepository.GetAllAsync();

        // Ids come from the DB itself; employees soft-deleted rows are already
        // excluded by the repository read contract.
        ViewData["Managers"] = employees
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new SelectListItem($"{e.FirstName} {e.LastName}", e.Id.ToString()));
    }
}
