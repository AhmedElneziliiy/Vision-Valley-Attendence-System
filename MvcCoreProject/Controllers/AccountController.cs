using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Services.IService;
using CoreProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MvcCoreProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AccountController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _authService = authService;
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _environment = environment;
        }

        // GET: Account/Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users
                    .Include(u => u.Image)
                    .Include(u => u.Branch)
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "Employee";

                var model = new ProfileViewModel
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName ?? "Unknown",
                    Email = user.Email ?? "",
                    Mobile = user.Mobile,
                    Gender = user.Gender,
                    Address = user.Address,
                    BranchName = user.Branch?.Name ?? "No Branch",
                    DepartmentName = user.Department?.Name ?? "No Department",
                    RoleName = primaryRole,
                    VacationBalance = user.VacationBalance,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    ProfileImageUrl = user.Image?.ImageUrl
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile");
                TempData["Error"] = "Failed to load profile.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // POST: Account/UpdateProfile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors and try again.";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                // Update user properties
                user.DisplayName = model.DisplayName;
                user.Mobile = model.Mobile;
                user.Gender = model.Gender;
                user.Address = model.Address;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} updated their profile", userId);
                    TempData["Success"] = "Profile updated successfully!";
                }
                else
                {
                    TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                }

                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                TempData["Error"] = "Failed to update profile.";
                return RedirectToAction(nameof(Profile));
            }
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            _logger.LogInformation("ChangePassword called - ModelState.IsValid: {IsValid}", ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("ModelState is invalid: {Errors}", string.Join(", ", errors));
                TempData["Error"] = $"Validation errors: {string.Join(", ", errors)}";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    _logger.LogWarning("User not found for password change: {UserId}", userId);
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Login");
                }

                _logger.LogInformation("Attempting password change for user {UserId}", userId);
                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} changed their password successfully", userId);
                    TempData["Success"] = "Password changed successfully!";
                }
                else
                {
                    var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Password change failed for user {UserId}: {Errors}", userId, errorMessages);
                    TempData["Error"] = errorMessages;
                }

                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user");
                TempData["Error"] = "An unexpected error occurred while changing password.";
                return RedirectToAction(nameof(Profile));
            }
        }

        // POST: Account/UploadProfilePicture
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile? profileImage)
        {
            if (profileImage == null || profileImage.Length == 0)
            {
                TempData["Error"] = "Please select an image file.";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users
                    .Include(u => u.Image)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Only image files (jpg, jpeg, png, gif) are allowed.";
                    return RedirectToAction(nameof(Profile));
                }

                // Validate file size (max 5MB)
                if (profileImage.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Image size must be less than 5MB.";
                    return RedirectToAction(nameof(Profile));
                }

                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete old image if exists
                if (user.Image != null && !string.IsNullOrEmpty(user.Image.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, user.Image.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Generate unique filename
                var uniqueFileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(fileStream);
                }

                // Save or update UserImage record
                var imageUrl = $"/uploads/profiles/{uniqueFileName}";
                if (user.Image == null)
                {
                    user.Image = new UserImage
                    {
                        UserID = userId,
                        ImageUrl = imageUrl
                    };
                    _context.UserImages.Add(user.Image);
                }
                else
                {
                    user.Image.ImageUrl = imageUrl;
                    _context.Entry(user.Image).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} uploaded profile picture", userId);
                TempData["Success"] = "Profile picture updated successfully!";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture");
                TempData["Error"] = "Failed to upload profile picture.";
                return RedirectToAction(nameof(Profile));
            }
        }

        // POST: Account/DeleteProfilePicture
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfilePicture()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users
                    .Include(u => u.Image)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                if (user.Image != null)
                {
                    // Delete physical file
                    if (!string.IsNullOrEmpty(user.Image.ImageUrl))
                    {
                        var imagePath = Path.Combine(_environment.WebRootPath, user.Image.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    // Delete database record
                    _context.UserImages.Remove(user.Image);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} deleted profile picture", userId);
                    TempData["Success"] = "Profile picture removed successfully!";
                }
                else
                {
                    TempData["Info"] = "No profile picture to delete.";
                }

                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile picture");
                TempData["Error"] = "Failed to delete profile picture.";
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to Dashboard
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and password are required";
                return View();
            }

            try
            {
                var loginResponse = await _authService.LoginAsync(email, password);

                Response.Cookies.Append("VisionValley_JWT", loginResponse.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,  // Changed to false for HTTP deployment (set to true only if using HTTPS with SSL certificate)
                    SameSite = SameSiteMode.Lax,  // Changed from Strict to Lax to allow cookie across same-site requests
                    Expires = loginResponse.ExpiresAt
                });

                // Optionally store user info in session/claims
                TempData["WelcomeMessage"] = $"Welcome back, {loginResponse.User.DisplayName}!";

                return string.IsNullOrEmpty(returnUrl)
                    ? Redirect("/Dashboard")
                    : Redirect(returnUrl);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Login failed for {Email}: {Message}", email, ex.Message);
                ViewBag.Error = ex.Message;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}", email);
                ViewBag.Error = "An unexpected error occurred. Please try again.";
                return View();
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Response.Cookies.Delete("VisionValley_JWT");
            return RedirectToAction("Login");
        }
    }
}
