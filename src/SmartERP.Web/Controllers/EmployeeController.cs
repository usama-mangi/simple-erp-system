using Microsoft.AspNetCore.Mvc;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;

namespace SmartERP.Web.Controllers;

public class EmployeeController(IEmployeeRepository _repository, ILogger<EmployeeController> _logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var employees = await _repository.GetAllAsync();
            return View(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            TempData["ErrorMessage"] = ex.Message;
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
            return View(detEmployee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            TempData["ErrorMessage"] = ex.Message;
            return View(null);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee employee)
    {
        return View(null);
    }
}