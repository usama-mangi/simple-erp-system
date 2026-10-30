using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Exceptions.Constraint.DepartmentManager;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

public class DepartmentManagersController(
    IDepartmentManagersRepository _managersRepository,
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

            var managers = await _managersRepository.GetAllByDepartmentAsync(departmentId);

            return View(new DepartmentManagerIndexViewModel
            {
                Department = department,
                Managers = managers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = ex.Message;
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
            TempData["ErrorMessage"] = ex.Message;
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
            await _managersRepository.CreateAsync(new DepartmentManager
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
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        await PopulateSelectLists();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(DepartmentManagerCloseViewModel model)
    {
        try
        {
            await _managersRepository.CloseAsync(model.Id, model.EndDate);

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
            TempData["ErrorMessage"] = ex.Message;
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
