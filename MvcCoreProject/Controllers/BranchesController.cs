using CoreProject.Services.IService;
using CoreProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MvcCoreProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BranchesController : Controller
    {
        private readonly IBranchService _branchService;
        private readonly ILogger<BranchesController> _logger;

        public BranchesController(
            IBranchService branchService,
            ILogger<BranchesController> logger)
        {
            _branchService = branchService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var branches = await _branchService.GetAllBranchesAsync();
                return View(branches);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading branches");
                TempData["Error"] = "Unable to load branches. Please try again.";
                return View(System.Linq.Enumerable.Empty<BranchViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var branch = await _branchService.GetBranchDetailsAsync(id);
                if (branch == null)
                {
                    TempData["Error"] = "Branch not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(branch);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading branch details for BranchId: {BranchId}", id);
                TempData["Error"] = "Unable to load branch details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = await _branchService.GetCreateBranchViewModelAsync();
                return View(model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading create branch form");
                TempData["Error"] = "Unable to load create branch form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BranchCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _branchService.CreateBranchAsync(model);
                    if (result)
                    {
                        TempData["Success"] = $"Branch '{model.Name}' created successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Error creating branch. Please try again.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error creating branch");
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                }
            }

            // If we got here, something failed - reload the form
            var reloadedModel = await _branchService.GetCreateBranchViewModelAsync();
            reloadedModel.Name = model.Name;
            reloadedModel.TimeZone = model.TimeZone;
            reloadedModel.Weekend = model.Weekend;
            reloadedModel.TimetableName = model.TimetableName;
            return View(reloadedModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var model = await _branchService.GetEditBranchViewModelAsync(id);
                if (model == null)
                {
                    TempData["Error"] = "Branch not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for BranchId: {BranchId}", id);
                TempData["Error"] = "Unable to load edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BranchEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _branchService.UpdateBranchAsync(model);
                    if (result)
                    {
                        TempData["Success"] = $"Branch '{model.Name}' updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Error updating branch. Please try again.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error updating branch: {BranchId}", model.Id);
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                }
            }

            // If we got here, something failed - reload the form
            var reloadedModel = await _branchService.GetEditBranchViewModelAsync(model.Id);
            if (reloadedModel != null)
            {
                reloadedModel.Name = model.Name;
                reloadedModel.TimeZone = model.TimeZone;
                reloadedModel.Weekend = model.Weekend;
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
                var result = await _branchService.DeleteBranchAsync(id);
                if (result)
                {
                    TempData["Success"] = "Branch deactivated successfully!";
                }
                else
                {
                    TempData["Error"] = "Branch not found or cannot be deleted (has users).";
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch: {BranchId}", id);
                TempData["Error"] = "An error occurred while deleting the branch.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Department Management
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDepartment(int branchId, string departmentName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(departmentName))
                {
                    return Json(new { success = false, message = "Department name is required." });
                }

                var result = await _branchService.AddDepartmentToBranchAsync(branchId, departmentName);
                if (result)
                {
                    return Json(new { success = true, message = "Department added successfully!" });
                }

                return Json(new { success = false, message = "Failed to add department." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error adding department to branch {BranchId}", branchId);
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDepartment(int departmentId)
        {
            try
            {
                var result = await _branchService.RemoveDepartmentFromBranchAsync(departmentId);
                if (result)
                {
                    return Json(new { success = true, message = "Department removed successfully!" });
                }

                return Json(new { success = false, message = "Failed to remove department (may have users)." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error removing department {DepartmentId}", departmentId);
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDepartment(int departmentId, string departmentName, bool isActive)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(departmentName))
                {
                    return Json(new { success = false, message = "Department name is required." });
                }

                var result = await _branchService.UpdateDepartmentAsync(departmentId, departmentName, isActive);
                if (result)
                {
                    return Json(new { success = true, message = "Department updated successfully!" });
                }

                return Json(new { success = false, message = "Failed to update department." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error updating department {DepartmentId}", departmentId);
                return Json(new { success = false, message = "An error occurred." });
            }
        }
    }
}
