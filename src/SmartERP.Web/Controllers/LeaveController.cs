using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Exceptions.Constraint;
using SmartERP.Data.Exceptions.Constraint.Leave;
using SmartERP.Data.Exceptions.Read;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

public class LeaveController(
    ILeaveRepository _leaveRepository,
    IEmployeeRepository _employeeRepository,
    ILookupRepository _lookupRepository,
    ILogger<LeaveController> _logger
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(double? employeeId, double? statusId)
    {
        try
        {
            var statusItems = await _lookupRepository.GetLeaveStatusesAsync();
            var statuses = ResolveStatuses(statusItems);
            var requests = await _leaveRepository.GetRequestsAsync(employeeId, statusId);

            var model = new LeaveIndexViewModel
            {
                Requests = requests,
                EmployeeId = employeeId,
                StatusId = statusId,
                Statuses = statuses
            };

            if (employeeId is { } scopedEmployeeId)
            {
                var employee = await _employeeRepository.GetByIdAsync(scopedEmployeeId);

                if (employee == null) return NotFound();

                model.EmployeeName = $"{employee.FirstName} {employee.LastName}";
            }
            else
            {
                // Soft-deleted employees are already filtered out by the
                // repository, so the filter list and the rows agree.
                model.EmployeeOptions = (await _employeeRepository.GetAllAsync())
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .Select(e => new SelectListItem($"{e.FirstName} {e.LastName}", e.Id.ToString()));
            }

            // A blank option first: it binds back to a null statusId, which the
            // repository reads as "no status filter".
            model.StatusOptions =
                [
                    new SelectListItem("Any status", string.Empty, statusId is null),
                    .. statusItems.Select(s => new SelectListItem(
                        s.Title,
                        s.Id.ToString(),
                        statusId == s.Id
                    ))
                ];

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
    public async Task<IActionResult> Create(double? employeeId)
    {
        try
        {
            var model = new LeaveRequestCreateViewModel();

            if (employeeId is { } scopedEmployeeId)
            {
                var employee = await _employeeRepository.GetByIdAsync(scopedEmployeeId);

                if (employee == null) return NotFound();

                model.EmployeeId = employee.Id;
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
    public async Task<IActionResult> Create(LeaveRequestCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(model);
            return View(model);
        }

        try
        {
            var statuses = await ResolveStatusesAsync();

            // Both calendar gates run before anything is written: a second
            // open request is rejected outright by uq_lr_open_request anyway,
            // and an overlap with approved leave would double-book the person.
            var active = await _leaveRepository.GetActiveRequestsAsync(model.EmployeeId, statuses);

            var open = active.FirstOrDefault(r => r.StatusId == statuses.Pending);

            if (open is not null)
            {
                ModelState.AddModelError(
                    nameof(model.StartDate),
                    "This employee already has a leave request waiting for a decision."
                );
                await PopulateSelectListsAsync(model);
                return View(model);
            }

            var requestedStart = model.StartDate.ToDateTime(TimeOnly.MinValue);
            var requestedEnd = model.EndDate.ToDateTime(TimeOnly.MinValue);

            var clash = active.FirstOrDefault(
                r => r.StatusId == statuses.Approved
                     && r.StartDate <= requestedEnd
                     && r.EndDate >= requestedStart
            );

            if (clash is not null)
            {
                ModelState.AddModelError(
                    nameof(model.EndDate),
                    $"These dates overlap approved leave from {clash.StartDate.ToShortDateString()} to {clash.EndDate.ToShortDateString()}."
                );
                await PopulateSelectListsAsync(model);
                return View(model);
            }

            await _leaveRepository.CreateRequestAsync(new LeaveRequest
            {
                EmployeeId = model.EmployeeId,
                LeaveTypeId = model.LeaveTypeId,
                StatusId = statuses.Pending,
                StartDate = requestedStart,
                EndDate = requestedEnd,
                Reason = model.Reason ?? string.Empty
            });

            TempData["SuccessMessage"] = "Leave request submitted and waiting for a decision.";
            return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
        }
        catch (DuplicateOpenLeaveRequestException ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(nameof(model.StartDate), ex.Message);
        }
        catch (InvalidLeaveDatesException ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(nameof(model.EndDate), ex.Message);
        }
        catch (ConstraintException ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(string.Empty, UserMessages.GenericFailure);
        }

        await PopulateSelectListsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(double id, double? employeeId, double? statusId)
    {
        try
        {
            var statuses = await ResolveStatusesAsync();
            var request = await _leaveRepository.GetRequestByIdAsync(id);

            if (request == null) return NotFound();

            if (request.StatusId != statuses.Pending)
            {
                TempData["ErrorMessage"] = "Only a request waiting for a decision can be approved.";
                return RedirectToAction(nameof(Index), new { employeeId, statusId });
            }

            // The entitlement gate runs before the transaction so the user
            // sees how many days are actually left, not a generic failure.
            var balances = await _leaveRepository.GetBalancesAsync(
                request.EmployeeId,
                request.StartDate.Year
            );
            var balance = balances.FirstOrDefault(b => b.LeaveTypeId == request.LeaveTypeId);

            if (balance == null)
            {
                TempData["ErrorMessage"] =
                    $"No {request.LeaveTypeTitle} entitlement is set for {request.StartDate.Year}. Set it on the balances page first.";
                return RedirectToAction(
                    nameof(Balances),
                    new { employeeId = request.EmployeeId, year = request.StartDate.Year }
                );
            }

            if (balance.UsedDays + request.Days > balance.EntitlementDays)
            {
                TempData["ErrorMessage"] =
                    $"Only {balance.RemainingDays:0.#} of {balance.EntitlementDays:0.#} {request.LeaveTypeTitle} days are left for {request.StartDate.Year}, and this request needs {request.Days}.";
                return RedirectToAction(nameof(Index), new { employeeId, statusId });
            }

            await _leaveRepository.ApproveAsync(request, statuses);

            TempData["SuccessMessage"] =
                $"Request approved: {request.Days} day(s) deducted from the {request.StartDate.Year} balance.";
            return RedirectToAction(nameof(Index), new { employeeId, statusId });
        }
        catch (ConstraintException ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = "That request has already been decided.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
        }

        return RedirectToAction(nameof(Index), new { employeeId, statusId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(double id, double? employeeId, double? statusId)
    {
        try
        {
            var statuses = await ResolveStatusesAsync();
            await _leaveRepository.RejectAsync(id, statuses);

            TempData["SuccessMessage"] = "Request rejected. Nothing was deducted from the balance.";
            return RedirectToAction(nameof(Index), new { employeeId, statusId });
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = "That request is not waiting for a decision any more.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
        }

        return RedirectToAction(nameof(Index), new { employeeId, statusId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(double id, double? employeeId, double? statusId)
    {
        try
        {
            var statuses = await ResolveStatusesAsync();
            var request = await _leaveRepository.GetRequestByIdAsync(id);

            if (request == null) return NotFound();

            bool refunds = request.StatusId == statuses.Approved;
            int year = request.StartDate.Year;

            await _leaveRepository.CancelAsync(request, statuses);

            TempData["SuccessMessage"] = refunds
                ? $"Request cancelled: {request.Days} day(s) returned to the {year} balance."
                : "Request cancelled.";
            return RedirectToAction(nameof(Index), new { employeeId, statusId });
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = "That request can no longer be cancelled.";
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

        return RedirectToAction(nameof(Index), new { employeeId, statusId });
    }

    [HttpGet]
    public async Task<IActionResult> Balances(double employeeId, int? year)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);

            if (employee == null) return NotFound();

            int targetYear = year ?? DateTime.Today.Year;

            return View(await BuildBalancesViewModelAsync(employee, targetYear));
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
    public async Task<IActionResult> UpsertBalance(LeaveBalanceUpsertViewModel model)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(model.EmployeeId);

            if (employee == null) return NotFound();

            // Lowering an allowance under what is already consumed would trip
            // chk_bal_used; say which way it went instead of letting the
            // database reject it.
            var existing = (await _leaveRepository.GetBalancesAsync(model.EmployeeId, model.Year))
                .FirstOrDefault(b => b.LeaveTypeId == model.LeaveTypeId);

            if (existing != null && model.EntitlementDays < existing.UsedDays)
            {
                ModelState.AddModelError(
                    nameof(model.EntitlementDays),
                    $"{existing.UsedDays:0.#} day(s) of this type are already used in {model.Year}; the entitlement cannot go below that."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(
                    nameof(Balances),
                    await BuildBalancesViewModelAsync(employee, model.Year, model)
                );
            }

            await _leaveRepository.UpsertBalanceAsync(new LeaveBalance
            {
                EmployeeId = model.EmployeeId,
                LeaveTypeId = model.LeaveTypeId,
                Year = model.Year,
                EntitlementDays = model.EntitlementDays
            });

            TempData["SuccessMessage"] = $"Entitlement saved for {model.Year}.";
            return RedirectToAction(nameof(Balances), new { employeeId = model.EmployeeId, year = model.Year });
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

        return RedirectToAction(nameof(Balances), new { employeeId = model.EmployeeId, year = model.Year });
    }

    private async Task<LeaveBalancesViewModel> BuildBalancesViewModelAsync(
        EmployeeWithDetails employee,
        int year,
        LeaveBalanceUpsertViewModel? submitted = null
    )
    {
        var model = new LeaveBalancesViewModel
        {
            Employee = employee,
            Year = year,
            Balances = await _leaveRepository.GetBalancesAsync(employee.Id, year),
            Upsert = submitted ?? new LeaveBalanceUpsertViewModel
            {
                EmployeeId = employee.Id,
                Year = year
            }
        };

        model.LeaveTypes = await _lookupRepository.GetLeaveTypesAsync();
        model.LeaveTypeOptions = model.LeaveTypes
            .Select(t => new SelectListItem(t.Title, t.Id.ToString(), t.Id == model.Upsert.LeaveTypeId));

        // The upsert partial reads its dropdown from ViewData, since its own
        // model is the form VM rather than the page VM.
        ViewData["LeaveTypeOptions"] = model.LeaveTypeOptions;
        return model;
    }

    // Status ids are data: every action resolves them from leave_statuses by
    // title, so a rebuild that reseeds them cannot silently mis-bind a
    // transition.
    private async Task<LeaveStatusIds> ResolveStatusesAsync()
        => ResolveStatuses(await _lookupRepository.GetLeaveStatusesAsync());

    private static LeaveStatusIds ResolveStatuses(IEnumerable<LookupItem> statuses)
    {
        double IdOf(string title)
            => statuses.FirstOrDefault(s => s.Title == title)?.Id
               ?? throw new InvalidOperationException($"leave_statuses has no '{title}' row.");

        return new LeaveStatusIds(
            Pending: IdOf("Pending"),
            Approved: IdOf("Approved"),
            Rejected: IdOf("Rejected"),
            Cancelled: IdOf("Cancelled")
        );
    }

    private async Task PopulateSelectListsAsync(LeaveRequestCreateViewModel model)
    {
        var employees = await _employeeRepository.GetAllAsync();
        var leaveTypes = await _lookupRepository.GetLeaveTypesAsync();

        ViewData["Employees"] = employees
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Select(e => new SelectListItem(
                $"{e.FirstName} {e.LastName}",
                e.Id.ToString(),
                e.Id == model.EmployeeId
            ));
        ViewData["LeaveTypes"] = leaveTypes
            .Select(t => new SelectListItem(t.Title, t.Id.ToString(), t.Id == model.LeaveTypeId));
    }
}
