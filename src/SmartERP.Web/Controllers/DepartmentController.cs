using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

public class DepartmentController(
    IDepartmentRepository _context,
    IDepartmentManagerRepository _managersRepository,
    IEmployeeRepository _employeeRepository,
    ILogger<DepartmentController> _logger
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var departments = await _context.GetAllAsync();
            return View(departments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(double id)
    {
        try
        {
            var department = await _context.GetByIdAsync(id);

            if (department == null) return NotFound();

            var managers = await _managersRepository.GetHistoryAsync(id);

            // The assign form lists every non-deleted employee (soft-delete is
            // already filtered out by the repository read contract).
            var employees = await _employeeRepository.GetAllAsync();

            return View(new DepartmentDetailsViewModel
            {
                Department = department,
                Managers = managers,
                ManagerOptions = employees
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .Select(e => new SelectListItem($"{e.FirstName} {e.LastName}", e.Id.ToString()))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _context.CreateAsync(new Department
            {
                Name = model.Name,
                Code = model.Code,
                Location = model.Location,
                Established = model.Established
            });

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create department");
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(DepartmentUpdateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            Department? dept = await _context.GetByIdAsync(model.Id);

            if (dept == null) return NotFound();

            await _context.UpdateAsync(new Department
            {
                Id = model.Id,
                Name = model.Name,
                Code = model.Code,
                Location = model.Location,
                Established = model.Established
            });

            TempData["SuccessMessage"] = $"Department {model.Name} created";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Update(double id)
    {
        Department? department = await _context.GetByIdAsync(id);

        if (department == null) return NotFound();

        return View(MapToUpdateViewModel(department));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(DepartmentDeleteViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            Department? dept = await _context.GetByIdAsync(model.Id);

            if (dept == null) return NotFound();

            var deptName = await _context.DeleteAsync(model.Id);

            TempData["SuccessMessage"] = $"Department {deptName} deleted successfully";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Index));
        }
    }

    private DepartmentUpdateViewModel MapToUpdateViewModel(Department department)
    {
        return new DepartmentUpdateViewModel
        {
            Id = department.Id,
            Name = department.Name,
            Code = department.Code,
            Location = department.Location,
            Established = department.Established
        };
    }
}