using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartERP.Data.Models;
using SmartERP.Data.Repositories;
using SmartERP.Web.Models;

namespace SmartERP.Web.Controllers;

// The per-employee document vault. Metadata only — no file storage anywhere
// on the server; the page records what exists, not the bytes.
[Authorize]
public class DocumentsController(
    IDocumentRepository _context,
    IEmployeeRepository _employees,
    ILookupRepository _lookup,
    ILogger<DocumentsController> _logger
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(decimal employeeId)
    {
        try
        {
            var employee = await _employees.GetByIdAsync(employeeId);
            if (employee is null) return NotFound();

            return View(new DocumentsIndexViewModel
            {
                EmployeeId = employee.Id,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                Documents = await _context.GetForEmployeeAsync(employee.Id)
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
    public async Task<IActionResult> Create(decimal employeeId)
    {
        var employee = await FindEmployeeAsync(employeeId);
        if (employee is null) return NotFound();

        ViewData["EmployeeName"] = $"{employee.FirstName} {employee.LastName}";
        await PopulateTypeOptionsAsync();
        return View(new DocumentViewModel { EmployeeId = employee.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocumentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateTypeOptionsAsync();
            return View(model);
        }

        try
        {
            var employee = await FindEmployeeAsync(model.EmployeeId);
            if (employee is null) return NotFound();

            await _context.CreateAsync(MapToDocument(model));
            TempData["SuccessMessage"] = "Document recorded on the register.";
            return RedirectToAction(nameof(Index), new { employeeId = employee.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(string.Empty, UserMessages.GenericFailure);
            await PopulateTypeOptionsAsync();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(decimal id)
    {
        var document = await _context.GetByIdAsync(id);
        if (document is null) return NotFound();

        await PopulateTypeOptionsAsync();
        return View(MapToViewModel(document));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DocumentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateTypeOptionsAsync();
            return View(model);
        }

        try
        {
            var existing = await _context.GetByIdAsync(model.Id);
            if (existing is null) return NotFound();

            // The employee binding never changes on edit: it comes from the
            // stored row, not from the post, so a tampered form cannot move a
            // document onto someone else's vault.
            model.EmployeeId = existing.EmployeeId;
            var document = MapToDocument(model);
            await _context.UpdateAsync(document);
            TempData["SuccessMessage"] = "Document updated on the register.";
            return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            ModelState.AddModelError(string.Empty, UserMessages.GenericFailure);
            await PopulateTypeOptionsAsync();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(decimal id, decimal employeeId)
    {
        try
        {
            var existing = await _context.GetByIdAsync(id);
            if (existing is null) return NotFound();

            await _context.DeleteAsync(existing.Id);
            TempData["SuccessMessage"] = "Document removed from the register.";
            return RedirectToAction(nameof(Index), new { employeeId = existing.EmployeeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            TempData["ErrorMessage"] = UserMessages.GenericFailure;
            return RedirectToAction(nameof(Index), new { employeeId });
        }
    }

    private async Task<EmployeeWithDetails?> FindEmployeeAsync(decimal employeeId)
        => await _employees.GetByIdAsync(employeeId);

    private async Task PopulateTypeOptionsAsync()
    {
        var types = await _lookup.GetDocumentTypesAsync();
        ViewData["DocumentTypes"] = new SelectList(types, "Id", "Title");
    }

    private static Document MapToDocument(DocumentViewModel model) => new()
    {
        Id = model.Id,
        EmployeeId = model.EmployeeId,
        TypeId = model.TypeId,
        Title = model.Title.Trim(),
        IssuedOn = model.IssuedOn?.ToDateTime(TimeOnly.MinValue),
        ExpiresAt = model.ExpiresAt?.ToDateTime(TimeOnly.MinValue),
        Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim()
    };

    private static DocumentViewModel MapToViewModel(Document document) => new()
    {
        Id = document.Id,
        EmployeeId = document.EmployeeId,
        TypeId = document.TypeId,
        Title = document.Title,
        IssuedOn = document.IssuedOn is null ? null : DateOnly.FromDateTime(document.IssuedOn.Value),
        ExpiresAt = document.ExpiresAt is null ? null : DateOnly.FromDateTime(document.ExpiresAt.Value),
        Note = document.Note
    };
}
