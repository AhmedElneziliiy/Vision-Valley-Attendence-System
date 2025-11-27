using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using CoreProject.Services.IService;
using CoreProject.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly ApplicationDbContext _context;
        private readonly IRepository<Branch> _branchRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserService> _logger;
        private readonly IFaceEnrollmentService _faceEnrollmentService;

        public UserService(
            IUserRepository userRepo,
            IRepository<Branch> branchRepo,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<UserService> logger,
            IFaceEnrollmentService faceEnrollmentService)
        {
            _userRepo = userRepo;
            _branchRepo = branchRepo;
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _faceEnrollmentService = faceEnrollmentService;
        }

        public async Task<IEnumerable<UserViewModel>> GetFilteredUsersAsync(
            int? branchId,
            string? role,
            ClaimsPrincipal currentUser)
        {
            try
            {
                _logger.LogInformation("Fetching filtered users - BranchId: {BranchId}, Role: {Role}",
                    branchId, role);

                // Start with base query
                var usersQuery = _context.Users
                    .IgnoreQueryFilters() // Bypass query filters for user management
                    .Include(u => u.Branch)
                    .Include(u => u.Department)
                    .AsQueryable();

                // Non-Admin: Filter by current user's branch
                if (!currentUser.IsInRole("Admin"))
                {
                    var currentBranchIdStr = currentUser.FindFirst("BranchID")?.Value;
                    if (int.TryParse(currentBranchIdStr, out int currentBranchId))
                    {
                        usersQuery = usersQuery.Where(u => u.BranchID == currentBranchId);
                        _logger.LogInformation("Non-admin user filtering by branch: {BranchId}", currentBranchId);
                    }
                    else
                    {
                        _logger.LogWarning("Non-admin user has no valid BranchID claim");
                        return Enumerable.Empty<UserViewModel>();
                    }
                }

                // Apply branch filter
                if (branchId.HasValue)
                {
                    usersQuery = usersQuery.Where(u => u.BranchID == branchId.Value);
                }

                // Apply role filter
                if (!string.IsNullOrEmpty(role))
                {
                    var userIdsInRole = await _userRepo.GetUsersByRoleAsync(role);
                    var ids = userIdsInRole.Select(u => u.Id).ToList();
                    usersQuery = usersQuery.Where(u => ids.Contains(u.Id));
                }

                // Execute query
                var users = await usersQuery
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} users", users.Count);

                // Build ViewModels
                var viewModels = new List<UserViewModel>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    viewModels.Add(new UserViewModel
                    {
                        Id = user.Id,
                        DisplayName = user.DisplayName ?? "Unknown",
                        Email = user.Email ?? "N/A",
                        Mobile = user.Mobile,
                        Gender = user.Gender,
                        Address = user.Address,
                        BranchName = user.Branch?.Name ?? "No Branch",
                        DepartmentName = user.Department?.Name ?? "No Department",
                        Roles = roles.ToList(),
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        VacationBalance = user.VacationBalance
                    });
                }

                return viewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching filtered users");
                throw;
            }
        }

        public async Task<UserCreateViewModel?> GetCreateUserViewModelAsync()
        {
            return await GetCreateUserViewModelAsync(null);
        }

        public async Task<UserCreateViewModel> GetCreateUserViewModelAsync(int? selectedBranchId = null)
        {
            try
            {
                var branches = (await _branchRepo.GetAllAsync()).ToList();
                var roles = (await _userRepo.GetAllRolesAsync()).ToList();

                if (!branches.Any())
                {
                    _logger.LogWarning("No branches found in the system");
                    branches.Add(new Branch { ID = 0, Name = "— No branches available —" });
                }

                int branchId = selectedBranchId ?? branches.FirstOrDefault(b => b.ID != 0)?.ID ?? 0;

                var departments = branchId > 0
                    ? await _context.Departments
                           .Where(d => d.BranchID == branchId)
                           .OrderBy(d => d.Name)
                           .ToListAsync()
                    : new List<Department>();

                if (!departments.Any())
                {
                    departments.Add(new Department { ID = 0, Name = "— No departments available —" });
                }

                int defaultDepartmentId = departments.FirstOrDefault(d => d.ID != 0)?.ID ?? 0;

                return new UserCreateViewModel
                {
                    BranchId = branchId,
                    DepartmentId = defaultDepartmentId,
                    Role = roles.Contains("Employee") ? "Employee" : roles.FirstOrDefault() ?? "",

                    Branches = branches.Select(b => new SelectListItem
                    {
                        Value = b.ID.ToString(),
                        Text = b.Name,
                        Selected = b.ID == branchId
                    }),

                    Departments = departments.Select(d => new SelectListItem
                    {
                        Value = d.ID.ToString(),
                        Text = d.Name,
                        Selected = d.ID == defaultDepartmentId
                    }),

                    Roles = roles.Select(r => new SelectListItem
                    {
                        Value = r,
                        Text = r,
                        Selected = r == (roles.Contains("Employee") ? "Employee" : roles.FirstOrDefault())
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user view model");
                throw;
            }
        }

        public async Task<bool> CreateUserAsync(UserCreateViewModel model)
        {
            try
            {
                _logger.LogInformation("Creating new user: {Email}", model.Email);

                var timetable = await _context.Timetables
                    .FirstOrDefaultAsync(t => t.BranchID == model.BranchId);

                if (timetable == null)
                {
                    _logger.LogWarning("No timetable found for branch {BranchId}", model.BranchId);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    DisplayName = model.DisplayName,
                    Mobile = model.Mobile,
                    Gender = model.Gender,
                    Address = model.Address,
                    BranchID = model.BranchId,
                    DepartmentID = model.DepartmentId,
                    TimetableID = timetable?.ID,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    VacationBalance = 21
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("User creation error: {Code} - {Description}",
                            error.Code, error.Description);
                    }
                    return false;
                }

                await _userManager.AddToRoleAsync(user, model.Role);
                _logger.LogInformation("User created successfully: {Email} with role {Role}",
                    model.Email, model.Role);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", model.Email);
                return false;
            }
        }

        public async Task<IEnumerable<Branch>> GetBranchesAsync() => await _branchRepo.GetAllAsync();

        public async Task<IEnumerable<string>> GetRolesAsync() => await _userRepo.GetAllRolesAsync();

        public async Task<UserDetailsViewModel?> GetUserDetailsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Fetching user details for UserId: {UserId}", userId);

                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .Include(u => u.Branch)
                    .Include(u => u.Department)
                    .Include(u => u.Timetable)
                    .Include(u => u.Manager)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return null;
                }

                var roles = await _userManager.GetRolesAsync(user);

                return new UserDetailsViewModel
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName ?? "Unknown",
                    Email = user.Email ?? "N/A",
                    Mobile = user.Mobile,
                    Address = user.Address,
                    Gender = user.Gender,
                    VacationBalance = user.VacationBalance,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    BranchName = user.Branch?.Name ?? "No Branch",
                    DepartmentName = user.Department?.Name ?? "No Department",
                    TimetableName = user.Timetable?.Name,
                    Roles = roles.ToList(),
                    ManagerId = user.ManagerID,
                    ManagerName = user.Manager?.DisplayName,
                    ManagerEmail = user.Manager?.Email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user details for UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserEditViewModel?> GetEditUserViewModelAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Loading edit form for UserId: {UserId}", userId);

                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .Include(u => u.Branch)
                    .Include(u => u.Department)
                    .Include(u => u.Image)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return null;
                }

                var roles = await _userManager.GetRolesAsync(user);
                var allRoles = await _userRepo.GetAllRolesAsync();
                var branches = await _branchRepo.GetAllAsync();

                var departments = await _context.Departments
                    .Where(d => d.BranchID == user.BranchID)
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                var timetables = await _context.Timetables
                    .Where(t => t.BranchID == user.BranchID)
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                var managers = await _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.BranchID == user.BranchID && u.Id != userId)
                    .OrderBy(u => u.DisplayName)
                    .ToListAsync();

                return new UserEditViewModel
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName ?? "",
                    Email = user.Email ?? "",
                    Mobile = user.Mobile,
                    Address = user.Address,
                    Gender = user.Gender,
                    VacationBalance = user.VacationBalance,
                    BranchId = user.BranchID,
                    DepartmentId = user.DepartmentID,
                    TimetableId = user.TimetableID,
                    ManagerId = user.ManagerID,
                    Role = roles.FirstOrDefault() ?? "Employee",
                    IsActive = user.IsActive,
                    CurrentPhotoUrl = user.Image?.ImageUrl,

                    // Face Verification
                    IsFaceVerificationEnabled = user.IsFaceVerificationEnabled,
                    FaceEnrolledAt = user.FaceEnrolledAt,

                    Branches = branches.Select(b => new SelectListItem
                    {
                        Value = b.ID.ToString(),
                        Text = b.Name,
                        Selected = b.ID == user.BranchID
                    }),

                    Departments = departments.Select(d => new SelectListItem
                    {
                        Value = d.ID.ToString(),
                        Text = d.Name,
                        Selected = d.ID == user.DepartmentID
                    }),

                    Timetables = timetables.Select(t => new SelectListItem
                    {
                        Value = t.ID.ToString(),
                        Text = t.Name,
                        Selected = t.ID == user.TimetableID
                    }),

                    Managers = new[] { new SelectListItem { Value = "", Text = "-- No Manager --" } }
                        .Concat(managers.Select(m => new SelectListItem
                        {
                            Value = m.Id.ToString(),
                            Text = m.DisplayName ?? m.Email ?? "Unknown",
                            Selected = m.Id == user.ManagerID
                        })),

                    Roles = allRoles.Select(r => new SelectListItem
                    {
                        Value = r,
                        Text = r,
                        Selected = r == roles.FirstOrDefault()
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(UserEditViewModel model)
        {
            try
            {
                _logger.LogInformation("Updating user: {UserId}", model.Id);

                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == model.Id);

                if (user == null)
                {
                    _logger.LogWarning("User not found for update: {UserId}", model.Id);
                    return false;
                }

                // Update properties
                user.DisplayName = model.DisplayName;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.Mobile = model.Mobile;
                user.Address = model.Address;
                user.Gender = model.Gender;
                user.VacationBalance = model.VacationBalance;
                user.BranchID = model.BranchId;
                user.DepartmentID = model.DepartmentId;
                user.TimetableID = model.TimetableId;
                user.ManagerID = model.ManagerId;
                user.IsActive = model.IsActive;
                user.IsFaceVerificationEnabled = model.IsFaceVerificationEnabled;
                user.UpdatedAt = DateTime.UtcNow;

                // Handle profile photo upload
                if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
                {
                    try
                    {
                        // Validate file size (max 5MB)
                        if (model.ProfilePhoto.Length > 5 * 1024 * 1024)
                        {
                            _logger.LogWarning("Profile photo too large for user {UserId}: {Size} bytes", model.Id, model.ProfilePhoto.Length);
                            // Continue with update but skip photo
                        }
                        else
                        {
                            // Generate unique filename
                            var fileExtension = Path.GetExtension(model.ProfilePhoto.FileName);
                            var fileName = $"{user.Id}_{Guid.NewGuid()}{fileExtension}";
                            var uploadsFolder = Path.Combine("wwwroot", "uploads", "profiles");
                            var filePath = Path.Combine(uploadsFolder, fileName);

                            // Ensure directory exists
                            Directory.CreateDirectory(uploadsFolder);

                            // Save the file
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await model.ProfilePhoto.CopyToAsync(fileStream);
                            }

                            // Delete old photo if exists
                            var existingImage = await _context.UserImages.FirstOrDefaultAsync(i => i.UserID == user.Id);
                            if (existingImage != null)
                            {
                                // Delete old file
                                var oldFilePath = Path.Combine("wwwroot", existingImage.ImageUrl.TrimStart('/', '~'));
                                if (File.Exists(oldFilePath))
                                {
                                    File.Delete(oldFilePath);
                                }
                                _context.UserImages.Remove(existingImage);
                            }

                            // Create new UserImage record
                            var newImage = new UserImage
                            {
                                UserID = user.Id,
                                ImageUrl = $"/uploads/profiles/{fileName}"
                            };

                            _context.UserImages.Add(newImage);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("Profile photo uploaded successfully for user {UserId}: {FileName}", model.Id, fileName);
                        }
                    }
                    catch (Exception photoEx)
                    {
                        _logger.LogError(photoEx, "Error uploading profile photo for user {UserId}", model.Id);
                        // Continue with user update even if photo upload fails
                    }
                }

                // Update user via UserManager to handle username/email changes properly
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        _logger.LogError("User update error: {Code} - {Description}",
                            error.Code, error.Description);
                    }
                    return false;
                }

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                    if (!passwordResult.Succeeded)
                    {
                        _logger.LogError("Password update failed for user: {UserId}", model.Id);
                        foreach (var error in passwordResult.Errors)
                        {
                            _logger.LogError("Password error: {Code} - {Description}",
                                error.Code, error.Description);
                        }
                        // Don't fail the whole update if only password fails
                    }
                }

                // Update role if changed
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.FirstOrDefault() != model.Role)
                {
                    if (currentRoles.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    }
                    await _userManager.AddToRoleAsync(user, model.Role);
                    _logger.LogInformation("Updated role for user {UserId} to {Role}", model.Id, model.Role);
                }

                _logger.LogInformation("User updated successfully: {UserId}", model.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", model.Id);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Deleting user: {UserId}", userId);

                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found for deletion: {UserId}", userId);
                    return false;
                }

                // Soft delete - just mark as inactive
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User soft-deleted successfully: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ResetUserPasswordAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Resetting password and UDID for user: {UserId}", userId);

                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found for password reset: {UserId}", userId);
                    return false;
                }

                // Remove existing password
                await _userManager.RemovePasswordAsync(user);

                // Set new default password
                var result = await _userManager.AddPasswordAsync(user, "Pass@123");

                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to reset password for user {UserId}: {Errors}",
                        userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return false;
                }

                // Reset UDID to null (for MVC reset only)
                user.UDID = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password and UDID reset successfully for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password and UDID for user: {UserId}", userId);
                return false;
            }
        }
    }
}