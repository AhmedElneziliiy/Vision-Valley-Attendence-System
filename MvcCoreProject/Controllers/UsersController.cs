using CoreProject.Context;
using CoreProject.Services.IService;
using CoreProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MvcCoreProject.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            ApplicationDbContext context,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? branchId = null, string? role = null, string? search = null)
        {
            try
            {
                var users = await _userService.GetFilteredUsersAsync(branchId, role, User);

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(search))
                {
                    users = users.Where(u =>
                        u.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (u.Mobile != null && u.Mobile.Contains(search))
                    ).ToList();
                }

                ViewBag.Branches = await _userService.GetBranchesAsync();
                ViewBag.Roles = await _userService.GetRolesAsync();
                ViewBag.SelectedBranchId = branchId;
                ViewBag.SelectedRole = role;
                ViewBag.SearchTerm = search;
                ViewBag.TotalUsers = users.Count();
                ViewBag.ActiveUsers = users.Count(u => u.IsActive);

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users index");
                TempData["Error"] = "Unable to load users. Please try again.";
                return View(Enumerable.Empty<UserViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = await _userService.GetCreateUserViewModelAsync();
                if (model == null)
                {
                    TempData["Error"] = "Unable to load create user form. Please ensure branches and roles exist.";
                    return RedirectToAction(nameof(Index));
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create user form");
                TempData["Error"] = "Unable to load create user form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _userService.CreateUserAsync(model);
                    if (result)
                    {
                        TempData["Success"] = $"User '{model.DisplayName}' created successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Error creating user. The email may already be in use.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user");
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                }
            }

            // If we got here, something failed - reload the form
            var reloadedModel = await _userService.GetCreateUserViewModelAsync(model.BranchId);
            if (reloadedModel != null)
            {
                reloadedModel.Email = model.Email;
                reloadedModel.DisplayName = model.DisplayName;
                reloadedModel.Mobile = model.Mobile;
                reloadedModel.Role = model.Role;
                reloadedModel.Password = model.Password;
                return View(reloadedModel);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartments(int branchId)
        {
            try
            {
                var departments = await _context.Departments
                    .Where(d => d.BranchID == branchId)
                    .OrderBy(d => d.Name)
                    .Select(d => new { id = d.ID, name = d.Name })
                    .ToListAsync();

                return Json(new { success = true, data = departments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching departments for branch {BranchId}", branchId);
                return Json(new { success = false, message = "Error loading departments" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportUsers(int? branchId = null, string? role = null)
        {
            try
            {
                var users = await _userService.GetFilteredUsersAsync(branchId, role, User);

                // Create CSV content
                var csv = new System.Text.StringBuilder();

                // Add header
                csv.AppendLine("Full Name,Email,Mobile,Gender,Address,Branch,Department,Role,Status,Vacation Balance,Created At");

                // Add data rows
                foreach (var user in users)
                {
                    var genderDisplay = user.Gender == 'M' ? "Male" : user.Gender == 'F' ? "Female" : "Not Specified";
                    var statusDisplay = user.IsActive ? "Active" : "Inactive";
                    var roleDisplay = user.Roles.Any() ? string.Join(", ", user.Roles) : "No Role";

                    csv.AppendLine($"\"{user.DisplayName}\",\"{user.Email}\",\"{user.Mobile ?? ""}\",\"{genderDisplay}\",\"{user.Address ?? ""}\",\"{user.BranchName}\",\"{user.DepartmentName}\",\"{roleDisplay}\",\"{statusDisplay}\",\"{user.VacationBalance ?? 0}\",\"{user.CreatedAt:yyyy-MM-dd HH:mm}\"");
                }

                // Convert to bytes
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());

                // Return as file download
                var fileName = $"Users_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users");
                TempData["Error"] = "Unable to export users.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportUsersPdf(int? branchId = null, string? role = null)
        {
            try
            {
                var users = await _userService.GetFilteredUsersAsync(branchId, role, User);

                // Generate PDF
                var pdfService = new CoreProject.Services.PdfExportService();
                var pdfBytes = pdfService.GenerateUsersReportPdf(users);

                // Return as file download
                var fileName = $"Users_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users as PDF");
                TempData["Error"] = "Unable to export users as PDF.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userDetails = await _userService.GetUserDetailsAsync(id);
                if (userDetails == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(userDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for UserId: {UserId}", id);
                TempData["Error"] = "Unable to load user details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var model = await _userService.GetEditUserViewModelAsync(id);
                if (model == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for UserId: {UserId}", id);
                TempData["Error"] = "Unable to load edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _userService.UpdateUserAsync(model);
                    if (result)
                    {
                        TempData["Success"] = $"User '{model.DisplayName}' updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Error updating user. The email may already be in use.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user: {UserId}", model.Id);
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                }
            }

            // If we got here, something failed - reload the form
            var reloadedModel = await _userService.GetEditUserViewModelAsync(model.Id);
            if (reloadedModel != null)
            {
                reloadedModel.DisplayName = model.DisplayName;
                reloadedModel.Email = model.Email;
                reloadedModel.Mobile = model.Mobile;
                reloadedModel.Address = model.Address;
                reloadedModel.Gender = model.Gender;
                reloadedModel.VacationBalance = model.VacationBalance;
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
                var result = await _userService.DeleteUserAsync(id);
                if (result)
                {
                    TempData["Success"] = "User deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "User not found or could not be deleted.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                TempData["Error"] = "An error occurred while deleting the user.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            try
            {
                var result = await _userService.ResetUserPasswordAsync(id);
                if (result)
                {
                    TempData["Success"] = "Password and UDID reset successfully! New password is: Pass@123";
                }
                else
                {
                    TempData["Error"] = "User not found or password/UDID could not be reset.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {UserId}", id);
                TempData["Error"] = "An error occurred while resetting the password.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetTimetables(int branchId)
        {
            try
            {
                var timetables = await _context.Timetables
                    .Where(t => t.BranchID == branchId)
                    .OrderBy(t => t.Name)
                    .Select(t => new { id = t.ID, name = t.Name })
                    .ToListAsync();

                return Json(new { success = true, data = timetables });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching timetables for branch {BranchId}", branchId);
                return Json(new { success = false, message = "Error loading timetables" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetManagers(int branchId, int? excludeUserId = null)
        {
            try
            {
                var managersQuery = _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.BranchID == branchId);

                if (excludeUserId.HasValue)
                {
                    managersQuery = managersQuery.Where(u => u.Id != excludeUserId.Value);
                }

                var managers = await managersQuery
                    .OrderBy(u => u.DisplayName)
                    .Select(u => new { id = u.Id, name = u.DisplayName ?? u.Email })
                    .ToListAsync();

                return Json(new { success = true, data = managers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching managers for branch {BranchId}", branchId);
                return Json(new { success = false, message = "Error loading managers" });
            }
        }
    }
}