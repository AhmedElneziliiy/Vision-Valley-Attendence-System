using CoreProject.Services.IService;
using CoreProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MvcCoreProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DevicesController : Controller
    {
        private readonly IDeviceService _deviceService;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(
            IDeviceService deviceService,
            ILogger<DevicesController> logger)
        {
            _deviceService = deviceService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? branchId)
        {
            try
            {
                var devices = await _deviceService.GetAllDevicesAsync(User, branchId);
                ViewBag.BranchId = branchId;
                return View(devices);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading devices");
                TempData["Error"] = "Unable to load devices. Please try again.";
                return View(System.Linq.Enumerable.Empty<DeviceViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var device = await _deviceService.GetDeviceDetailsAsync(id);
                if (device == null)
                {
                    TempData["Error"] = "Device not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(device);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading device details for DeviceId: {DeviceId}", id);
                TempData["Error"] = "Unable to load device details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = await _deviceService.GetCreateDeviceViewModelAsync(User);
                return View(model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading create device form");
                TempData["Error"] = "Unable to load create device form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeviceCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _deviceService.CreateDeviceAsync(model);
                    if (result)
                    {
                        TempData["Success"] = $"Device created successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Error creating device. Please try again.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error creating device");
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                }
            }

            // If we got here, something failed - reload the form
            var reloadedModel = await _deviceService.GetCreateDeviceViewModelAsync(User);
            reloadedModel.DeviceID = model.DeviceID;
            reloadedModel.DeviceType = model.DeviceType;
            reloadedModel.BranchId = model.BranchId;
            reloadedModel.CoverageArea = model.CoverageArea;
            reloadedModel.IsSignedIn = model.IsSignedIn;
            reloadedModel.IsSignedOut = model.IsSignedOut;
            reloadedModel.IsPassThrough = model.IsPassThrough;
            reloadedModel.Description = model.Description;
            reloadedModel.IsActive = model.IsActive;
            reloadedModel.AccessControlURL = model.AccessControlURL;
            reloadedModel.AccessControlState = model.AccessControlState;
            return View(reloadedModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var model = await _deviceService.GetEditDeviceViewModelAsync(id);
                if (model == null)
                {
                    TempData["Error"] = "Device not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for DeviceId: {DeviceId}", id);
                TempData["Error"] = "Unable to load edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DeviceEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _deviceService.UpdateDeviceAsync(model);
                    if (result)
                    {
                        TempData["Success"] = $"Device updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Error updating device. Please try again.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error updating device: {DeviceId}", model.Id);
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                }
            }

            // If we got here, something failed - reload the form
            var reloadedModel = await _deviceService.GetEditDeviceViewModelAsync(model.Id);
            if (reloadedModel != null)
            {
                reloadedModel.DeviceID = model.DeviceID;
                reloadedModel.DeviceType = model.DeviceType;
                reloadedModel.BranchId = model.BranchId;
                reloadedModel.CoverageArea = model.CoverageArea;
                reloadedModel.IsSignedIn = model.IsSignedIn;
                reloadedModel.IsSignedOut = model.IsSignedOut;
                reloadedModel.IsPassThrough = model.IsPassThrough;
                reloadedModel.Description = model.Description;
                reloadedModel.IsActive = model.IsActive;
                reloadedModel.AccessControlURL = model.AccessControlURL;
                reloadedModel.AccessControlState = model.AccessControlState;
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
                var result = await _deviceService.DeleteDeviceAsync(id);
                if (result)
                {
                    TempData["Success"] = "Device deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Device not found or cannot be deleted.";
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error deleting device: {DeviceId}", id);
                TempData["Error"] = "An error occurred while deleting the device.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
