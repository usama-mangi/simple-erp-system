using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Abstractions;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

[Authorize]
public class RequisitionController(
    IRequisitionRepository _requisitionRepository,
    ILookupRepository _lookupRepository,
    IDepartmentRepository _departmentRepository,
    ILogger<RequisitionController> _logger
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var requisitions = await _requisitionRepository.GetAllRequisitionsAsync();

            var model = new RequisitionIndexViewModel
            {
                Requisitions = requisitions
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
            var model = new RequisitionCreateViewModel();

            // Use lookup repository for positions and statuses
            var positions = await _lookupRepository.GetPositionsAsync();
            ViewData["Positions"] = positions
                .Select(p => new SelectListItem(p.Title, p.Id.ToString()))
                .ToList();

            var statuses = await _lookupRepository.GetEmploymentStatusesAsync();
            ViewData["Statuses"] = statuses
                .Select(s => new SelectListItem(s.Title, s.Id.ToString()))
                .ToList();

            // Departments come from the department repository
            var departments = await _departmentRepository.GetAllAsync();
            ViewData["Departments"] = departments
                .Select(d => new SelectListItem(d.Name, d.Id.ToString()))
                .ToList();

            // Set default selected values
            if (ViewData["Departments"] is List<SelectListItem> deptList)
            {
                var defaultDept = deptList.FirstOrDefault(d => d.Text == "Human Resources");
                if (defaultDept != null) defaultDept.Selected = true;
            }
            if (ViewData["Positions"] is List<SelectListItem> posList)
            {
                var defaultPos = posList.FirstOrDefault(p => p.Text == "Software Engineer");
                if (defaultPos != null) defaultPos.Selected = true;
            }
            if (ViewData["Statuses"] is List<SelectListItem> statusList)
            {
                var defaultStatus = statusList.FirstOrDefault(s => s.Text == "Started");
                if (defaultStatus != null) defaultStatus.Selected = true;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RequisitionCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await Task.Delay(0);
            return View(model);
        }

        try
        {
            var requisition = new Requisition
            {
                Title = model.Title,
                Description = model.Description,
                DepartmentId = model.DepartmentId,
                PositionId = model.PositionId,
                Salary = model.Salary,
                Openings = model.Openings,
                StatusId = model.StatusId
            };

            await _requisitionRepository.CreateRequisitionAsync(requisition);

            TempData["SuccessMessage"] = $"Requisition created for {model.Title}.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Create));
        }
    }
}
