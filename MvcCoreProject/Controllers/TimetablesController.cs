using CoreProject.Services.IService;
using CoreProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MvcCoreProject.Controllers
{
    [Authorize(Roles = "Admin,HR,Manager")]
    public class TimetablesController : Controller
    {
        private readonly ITimetableService _timetableService;
        private readonly ILogger<TimetablesController> _logger;

        public TimetablesController(
            ITimetableService timetableService,
            ILogger<TimetablesController> logger)
        {
            _timetableService = timetableService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? branchId)
        {
            try
            {
                var timetables = await _timetableService.GetAllTimetablesAsync(User, branchId);
                ViewBag.BranchId = branchId;
                return View(timetables);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading timetables");
                TempData["Error"] = "Unable to load timetables. Please try again.";
                return View(System.Linq.Enumerable.Empty<TimetableViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var timetable = await _timetableService.GetTimetableDetailsAsync(id);
                if (timetable == null)
                {
                    TempData["Error"] = "Timetable not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(timetable);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading timetable details for TimetableId: {TimetableId}", id);
                TempData["Error"] = "Unable to load timetable details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = await _timetableService.GetCreateTimetableViewModelAsync(User);
                return View(model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading create timetable form");
                TempData["Error"] = "Unable to load create timetable form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(TimetableCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _timetableService.CreateTimetableAsync(model);
                    if (result)
                    {
                        TempData["Success"] = $"Timetable '{model.Name}' created successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Error creating timetable. Please try again.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error creating timetable");
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                }
            }

            // If we got here, something failed - reload the form
            var reloadedModel = await _timetableService.GetCreateTimetableViewModelAsync(User);
            reloadedModel.Name = model.Name;
            reloadedModel.BranchId = model.BranchId;
            reloadedModel.WorkingDayStartingHourMinimum = model.WorkingDayStartingHourMinimum;
            reloadedModel.WorkingDayStartingHourMaximum = model.WorkingDayStartingHourMaximum;
            reloadedModel.WorkingDayEndingHour = model.WorkingDayEndingHour;
            reloadedModel.AverageWorkingHours = model.AverageWorkingHours;
            reloadedModel.IsWorkingDayEndingHourEnable = model.IsWorkingDayEndingHourEnable;
            reloadedModel.IsActive = model.IsActive;
            return View(reloadedModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var model = await _timetableService.GetEditTimetableViewModelAsync(id);
                if (model == null)
                {
                    TempData["Error"] = "Timetable not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for TimetableId: {TimetableId}", id);
                TempData["Error"] = "Unable to load edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(TimetableEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _timetableService.UpdateTimetableAsync(model);
                    if (result)
                    {
                        TempData["Success"] = $"Timetable '{model.Name}' updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Error updating timetable. Please try again.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error updating timetable: {TimetableId}", model.Id);
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                }
            }

            // If we got here, something failed - reload the form
            var reloadedModel = await _timetableService.GetEditTimetableViewModelAsync(model.Id);
            if (reloadedModel != null)
            {
                reloadedModel.Name = model.Name;
                reloadedModel.BranchId = model.BranchId;
                reloadedModel.WorkingDayStartingHourMinimum = model.WorkingDayStartingHourMinimum;
                reloadedModel.WorkingDayStartingHourMaximum = model.WorkingDayStartingHourMaximum;
                reloadedModel.WorkingDayEndingHour = model.WorkingDayEndingHour;
                reloadedModel.AverageWorkingHours = model.AverageWorkingHours;
                reloadedModel.IsWorkingDayEndingHourEnable = model.IsWorkingDayEndingHourEnable;
                reloadedModel.IsActive = model.IsActive;
                return View(reloadedModel);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _timetableService.DeleteTimetableAsync(id);
                if (result)
                {
                    TempData["Success"] = "Timetable deactivated successfully!";
                }
                else
                {
                    TempData["Error"] = "Timetable not found or cannot be deleted (has users assigned).";
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error deleting timetable: {TimetableId}", id);
                TempData["Error"] = "An error occurred while deleting the timetable.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Configuration Management
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddConfiguration(int timetableId, int configurationId, string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return Json(new { success = false, message = "Configuration value is required." });
                }

                var result = await _timetableService.AddConfigurationToTimetableAsync(timetableId, configurationId, value);
                if (result)
                {
                    return Json(new { success = true, message = "Configuration added successfully!" });
                }

                return Json(new { success = false, message = "Failed to add configuration." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error adding configuration to timetable {TimetableId}", timetableId);
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateConfiguration(int timetableConfigurationId, string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return Json(new { success = false, message = "Configuration value is required." });
                }

                var result = await _timetableService.UpdateConfigurationAsync(timetableConfigurationId, value);
                if (result)
                {
                    return Json(new { success = true, message = "Configuration updated successfully!" });
                }

                return Json(new { success = false, message = "Failed to update configuration." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration {TimetableConfigurationId}", timetableConfigurationId);
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveConfiguration(int timetableConfigurationId)
        {
            try
            {
                var result = await _timetableService.RemoveConfigurationFromTimetableAsync(timetableConfigurationId);
                if (result)
                {
                    return Json(new { success = true, message = "Configuration removed successfully!" });
                }

                return Json(new { success = false, message = "Failed to remove configuration." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error removing configuration {TimetableConfigurationId}", timetableConfigurationId);
                return Json(new { success = false, message = "An error occurred." });
            }
        }
    }
}
