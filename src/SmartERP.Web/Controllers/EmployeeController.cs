using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

public class EmployeeController(
    IEmployeeRepository _repository,
    IDepartmentRepository _departmentRepository,
    ILookupRepository _lookupRepository,
    ILogger<EmployeeController> _logger
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var employees = await _repository.GetAllAsync();
            await PopulateLookupTitlesAsync();
            return View(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Index), new List<Employee>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(double id)
    {
        try
        {
            var detEmployee = await _repository.GetByIdAsync(id);

            if (detEmployee == null) return NotFound();

            await PopulateLookupTitlesAsync(detEmployee.HasEmploymentDetails);
            return View(detEmployee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return View(null);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateSelectLists();
        return View(new EmployeeCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists();
            return View(model);
        }

        DateTime dateOfBirth = model.DateOfBirth.ToDateTime(TimeOnly.MinValue);

        try
        {
            // Details present -> persist employee and job assignment in one
            // transaction; otherwise the plain employee insert is enough.
            if (model.IncludeEmploymentDetails)
            {
                await _repository.CreateWithDetailsAsync(new EmployeeWithDetails
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Phone = model.Phone,
                    DateOfBirth = dateOfBirth,
                    GenderId = model.GenderId,
                    Address = model.Address,
                    JobTitle = model.JobTitle,
                    Salary = model.Salary,
                    StartDate = model.StartDate.ToDateTime(TimeOnly.MinValue),
                    DepartmentId = model.DepartmentId,
                    EmploymentTypeId = model.EmploymentTypeId,
                    EmploymentStatusId = model.EmploymentStatusId
                });
            }
            else
            {
                await _repository.CreateAsync(new Employee
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Phone = model.Phone,
                    DateOfBirth = dateOfBirth,
                    GenderId = model.GenderId,
                    Address = model.Address
                });
            }

            TempData["SuccessMessage"] = $"Employee {model.FirstName} {model.LastName} created successfully";
            return RedirectToAction(nameof(Index));
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

    [HttpGet]
    public async Task<IActionResult> Update(double id)
    {
        try
        {
            var employee = await _repository.GetByIdAsync(id);

            if (employee == null) return NotFound();

            await PopulateSelectLists();
            return View(MapToUpdateViewModel(employee));
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
    public async Task<IActionResult> Update(EmployeeUpdateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists();
            return View(model);
        }

        try
        {
            await _repository.UpdateAsync(new EmployeeWithDetails
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Phone = model.Phone,
                DateOfBirth = model.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                GenderId = model.GenderId,
                Address = model.Address,
                JobTitle = model.JobTitle,
                Salary = model.Salary,
                StartDate = model.StartDate.ToDateTime(TimeOnly.MinValue),
                DepartmentId = model.DepartmentId,
                EmploymentTypeId = model.EmploymentTypeId,
                EmploymentStatusId = model.EmploymentStatusId
            });

            TempData["SuccessMessage"] = $"Employee {model.FirstName} {model.LastName} updated successfully";
            return RedirectToAction(nameof(Index));
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

    [HttpGet]
    public async Task<IActionResult> Delete(double id)
    {
        try
        {
            var employee = await _repository.GetByIdAsync(id);

            if (employee == null) return NotFound();

            return View(new EmployeeDeleteViewModel
            {
                Id = employee.Id,
                DisplayName = $"{employee.FirstName} {employee.LastName}",
                HasDependencies = await _repository.HasDependenciesAsync(id)
            });
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
    public async Task<IActionResult> Delete(EmployeeDeleteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
        }

        var softDeleteMessage = model.HasDependencies
            ? $"{model.DisplayName} and all dependent records"
            : model.DisplayName;

        try
        {
            // Dependencies present -> explicit destructive purge of the employee
            // and every dependent record; otherwise a reversible soft delete.
            if (model.HasDependencies)
            {
                await _repository.DeleteWithDetailsAsync(model.Id);
            }
            else
            {
                await _repository.DeleteAsync(model.Id);
            }

            TempData["SuccessMessage"] = $"Employee {softDeleteMessage} deleted successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
        }

        return RedirectToAction(nameof(Index));
    }

    private EmployeeUpdateViewModel MapToUpdateViewModel(EmployeeWithDetails employee)
    {
        var model = new EmployeeUpdateViewModel
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Phone = employee.Phone,
            DateOfBirth = DateOnly.FromDateTime(employee.DateOfBirth),
            GenderId = employee.GenderId,
            Address = employee.Address,
            HasEmploymentDetails = employee.HasEmploymentDetails
        };

        if (employee.HasEmploymentDetails)
        {
            model.JobTitle = employee.JobTitle;
            model.Salary = employee.Salary;
            model.StartDate = DateOnly.FromDateTime(employee.StartDate);
            model.DepartmentId = employee.DepartmentId;
            model.EmploymentTypeId = employee.EmploymentTypeId;
            model.EmploymentStatusId = employee.EmploymentStatusId;
        }

        return model;
    }

    private static string FieldErrorKey(string fieldName) => fieldName switch
    {
        "Email" => nameof(EmployeeCreateViewModel.Email),
        "Phone" => nameof(EmployeeCreateViewModel.Phone),
        "Salary" => nameof(EmployeeCreateViewModel.Salary),
        "EndDate" => nameof(EmployeeCreateViewModel.StartDate),
        _ => string.Empty
    };

    private async Task PopulateSelectLists()
    {
        var departments = await _departmentRepository.GetAllAsync();
        var genders = await _lookupRepository.GetGendersAsync();
        var employmentTypes = await _lookupRepository.GetEmploymentTypesAsync();
        var employmentStatuses = await _lookupRepository.GetEmploymentStatusesAsync();

        // Ids come from the lookup tables themselves; enum positions in code
        // cannot be trusted after lookups are reseeded or extended.
        ViewData["Genders"] = genders
            .Select(g => new SelectListItem(g.Title, g.Id.ToString()));
        ViewData["Departments"] = departments
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString()));
        ViewData["EmploymentTypes"] = employmentTypes
            .Select(t => new SelectListItem(t.Title, t.Id.ToString()));
        ViewData["EmploymentStatuses"] = employmentStatuses
            .Select(s => new SelectListItem(s.Title, s.Id.ToString()));
    }

    private async Task PopulateLookupTitlesAsync(bool includeEmploymentLookups = false)
    {
        var genders = await _lookupRepository.GetGendersAsync();
        ViewData["GenderTitles"] = genders.ToDictionary(g => g.Id, g => g.Title);

        if (!includeEmploymentLookups) return;

        var types = await _lookupRepository.GetEmploymentTypesAsync();
        var statuses = await _lookupRepository.GetEmploymentStatusesAsync();
        var departments = await _departmentRepository.GetAllAsync();

        ViewData["EmploymentTypeTitles"] = types.ToDictionary(t => t.Id, t => t.Title);
        ViewData["EmploymentStatusTitles"] = statuses.ToDictionary(s => s.Id, s => s.Title);
        ViewData["DepartmentTitles"] = departments.ToDictionary(d => d.Id, d => d.Name);
    }

    private static string Title(Dictionary<double, string>? titles, double id)
        => titles is not null && titles.TryGetValue(id, out var title) ? title : "Unknown";
}
