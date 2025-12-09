using CoreProject.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MvcCoreProject.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IDashboardService dashboardService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var stats = await _dashboardService.GetDashboardStatsAsync(userId);

                // Set user-friendly welcome message
                var userName = User.Identity?.Name ?? "User";
                ViewBag.WelcomeMessage = $"Welcome back, {userName}!";
                ViewBag.CurrentTime = DateTime.Now.ToString("dddd, MMMM dd, yyyy");

                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "Unable to load dashboard. Please try again.";
                return View(new CoreProject.ViewModels.DashboardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> RefreshStats()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var stats = await _dashboardService.GetDashboardStatsAsync(userId);
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing dashboard stats");
                return Json(new { success = false, message = "Failed to refresh stats" });
            }
        }
    }
}