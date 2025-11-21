using CoreProject.Services.IService;
using CoreProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace MvcCoreProject.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class DepartmentsController : Controller
    {
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(
            IDepartmentService departmentService,
            ILogger<DepartmentsController> logger)
        {
            _departmentService = departmentService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var departments = await _departmentService.GetAllDepartmentsAsync(User);
                return View(departments);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading departments");
                TempData["Error"] = "Unable to load departments. Please try again.";
                return View(Enumerable.Empty<DepartmentViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var department = await _departmentService.GetDepartmentDetailsAsync(id);
                if (department == null)
                {
                    TempData["Error"] = "Department not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(department);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading department details for DepartmentId: {DepartmentId}", id);
                TempData["Error"] = "Unable to load department details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = await _departmentService.GetCreateDepartmentViewModelAsync(User);
                return View(model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading create department form");
                TempData["Error"] = "Unable to load create department form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _departmentService.CreateDepartmentAsync(model);
                    if (result)
                    {
                        TempData["Success"] = $"Department '{model.Name}' created successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Error creating department. Please try again.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error creating department");
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                }
            }

            // If we got here, something failed - reload the form
            var reloadedModel = await _departmentService.GetCreateDepartmentViewModelAsync(User);
            reloadedModel.Name = model.Name;
            return View(reloadedModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var model = await _departmentService.GetEditDepartmentViewModelAsync(id, User);
                if (model == null)
                {
                    TempData["Error"] = "Department not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for DepartmentId: {DepartmentId}", id);
                TempData["Error"] = "Unable to load edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DepartmentEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _departmentService.UpdateDepartmentAsync(model);
                    if (result)
                    {
                        TempData["Success"] = $"Department '{model.Name}' updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Error updating department. Please try again.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error updating department: {DepartmentId}", model.Id);
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                }
            }

            // If we got here, something failed - reload the form
            var reloadedModel = await _departmentService.GetEditDepartmentViewModelAsync(model.Id, User);
            if (reloadedModel != null)
            {
                reloadedModel.Name = model.Name;
                reloadedModel.IsActive = model.IsActive;
                return View(reloadedModel);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _departmentService.DeleteDepartmentAsync(id);
                if (result)
                {
                    TempData["Success"] = "Department deactivated successfully!";
                }
                else
                {
                    TempData["Error"] = "Department not found or cannot be deleted (has users).";
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error deleting department: {DepartmentId}", id);
                TempData["Error"] = "An error occurred while deleting the department.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
