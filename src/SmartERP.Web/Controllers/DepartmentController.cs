using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Exceptions.Delete;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

[Authorize]
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
    public async Task<IActionResult> Details(decimal id)
    {
        try
        {
            var department = await _context.GetByIdAsync(id);

            if (department == null) return NotFound();

            var managers = await _managersRepository.GetHistoryAsync(id);

            // The assign form lists every non-deleted employee (soft-delete is
            // already filtered out by the repository read contract).
            var employees = await _employeeRepository.GetAllAsync();

            // One hierarchy read serves both headcount figures and the list of
            // departments that report here.
            var hierarchy = (await _context.GetHierarchyAsync()).ToList();
            var node = hierarchy.FirstOrDefault(n => n.Id == id);

            return View(new DepartmentDetailsViewModel
            {
                Department = department,
                Managers = managers,
                ManagerOptions = employees
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .Select(e => new SelectListItem($"{e.FirstName} {e.LastName}", e.Id.ToString())),
                Children = hierarchy.Where(n => n.ParentDepartmentId == id),
                DirectHeadcount = node?.DirectHeadcount ?? 0,
                RollupHeadcount = node?.RollupHeadcount ?? 0
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
        if (!ModelState.IsValid)
        {
            model.ParentOptions = await GetParentOptionsAsync(null);
            return View(model);
        }

        try
        {
            await _context.CreateAsync(new Department
            {
                Name = model.Name,
                Code = model.Code,
                Location = model.Location,
                Established = model.Established,
                ParentDepartmentId = model.ParentDepartmentId
            });

            return RedirectToAction(nameof(Index));
        }
        catch (ConstraintException ex)
        {
            _logger.LogError(ex, "Failed to create department");
            ModelState.AddModelError(ex.FieldName, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create department");
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        model.ParentOptions = await GetParentOptionsAsync(null);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(new DepartmentCreateViewModel
        {
            ParentOptions = await GetParentOptionsAsync(null)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(DepartmentUpdateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ParentOptions = await GetParentOptionsAsync(model.Id);
            return View(model);
        }

        try
        {
            Department? dept = await _context.GetByIdAsync(model.Id);

            if (dept == null) return NotFound();

            if (model.ParentDepartmentId == model.Id)
            {
                ModelState.AddModelError(nameof(model.ParentDepartmentId),
                    "A department cannot report to itself.");
                model.ParentOptions = await GetParentOptionsAsync(model.Id);
                return View(model);
            }

            if (model.ParentDepartmentId is { } parentId
                && await _context.WouldCreateCycleAsync(model.Id, parentId))
            {
                ModelState.AddModelError(nameof(model.ParentDepartmentId),
                    "A department cannot report to one of its own sub-departments.");
                model.ParentOptions = await GetParentOptionsAsync(model.Id);
                return View(model);
            }

            await _context.UpdateAsync(new Department
            {
                Id = model.Id,
                Name = model.Name,
                Code = model.Code,
                Location = model.Location,
                Established = model.Established,
                ParentDepartmentId = model.ParentDepartmentId
            });

            TempData["SuccessMessage"] = $"Department {model.Name} updated";

            return RedirectToAction(nameof(Index));
        }
        catch (ConstraintException ex)
        {
            _logger.LogError(ex, "Failed to update department");
            ModelState.AddModelError(ex.FieldName, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        model.ParentOptions = await GetParentOptionsAsync(model.Id);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Update(decimal id)
    {
        Department? department = await _context.GetByIdAsync(id);

        if (department == null) return NotFound();

        var model = MapToUpdateViewModel(department);
        model.ParentOptions = await GetParentOptionsAsync(department.Id);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Hierarchy()
    {
        try
        {
            return View(new DepartmentHierarchyViewModel
            {
                Nodes = await _context.GetHierarchyAsync()
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
        catch (ChildFoundException ex)
        {
            // Deleted department still has reports, jobs or a manager history.
            _logger.LogError(ex, "Department {DepartmentId} has dependent rows", model.Id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<IEnumerable<SelectListItem>> GetParentOptionsAsync(decimal? excludeDepartmentId)
    {
        var departments = await _context.GetParentCandidatesAsync(excludeDepartmentId);

        return departments.Select(d => new SelectListItem(d.Name, d.Id.ToString()));
    }

    private DepartmentUpdateViewModel MapToUpdateViewModel(Department department)
    {
        return new DepartmentUpdateViewModel
        {
            Id = department.Id,
            Name = department.Name,
            Code = department.Code,
            Location = department.Location,
            Established = department.Established,
            ParentDepartmentId = department.ParentDepartmentId
        };
    }
}