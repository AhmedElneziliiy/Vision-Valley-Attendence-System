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
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _departmentRepo;
        private readonly IBranchRepository _branchRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DepartmentService> _logger;

        public DepartmentService(
            IDepartmentRepository departmentRepo,
            IBranchRepository branchRepo,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<DepartmentService> logger)
        {
            _departmentRepo = departmentRepo;
            _branchRepo = branchRepo;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<DepartmentViewModel>> GetAllDepartmentsAsync(ClaimsPrincipal currentUser)
        {
            try
            {
                _logger.LogInformation("Fetching all departments");

                var departments = await _departmentRepo.GetDepartmentsWithDetailsAsync();

                // Non-Admin users: filter by their branch
                if (!currentUser.IsInRole("Admin"))
                {
                    var currentBranchIdStr = currentUser.FindFirst("BranchID")?.Value;
                    if (int.TryParse(currentBranchIdStr, out int currentBranchId))
                    {
                        departments = departments.Where(d => d.BranchID == currentBranchId);
                    }
                    else
                    {
                        return Enumerable.Empty<DepartmentViewModel>();
                    }
                }

                var viewModels = new List<DepartmentViewModel>();

                foreach (var dept in departments)
                {
                    var userCount = await _departmentRepo.GetUserCountByDepartmentAsync(dept.ID);

                    viewModels.Add(new DepartmentViewModel
                    {
                        Id = dept.ID,
                        Name = dept.Name,
                        BranchName = dept.Branch?.Name ?? "Unknown",
                        BranchId = dept.BranchID,
                        IsActive = dept.IsActive,
                        UserCount = userCount,
                        CreatedAt = dept.CreatedAt
                    });
                }

                return viewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching departments");
                throw;
            }
        }

        public async Task<DepartmentDetailsViewModel?> GetDepartmentDetailsAsync(int departmentId)
        {
            try
            {
                _logger.LogInformation("Fetching department details for DepartmentId: {DepartmentId}", departmentId);

                var department = await _departmentRepo.GetDepartmentWithDetailsAsync(departmentId);

                if (department == null)
                {
                    _logger.LogWarning("Department not found: {DepartmentId}", departmentId);
                    return null;
                }

                var users = await _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.DepartmentID == departmentId)
                    .ToListAsync();

                var userInfoList = new List<UserInfo>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userInfoList.Add(new UserInfo
                    {
                        Id = user.Id,
                        DisplayName = user.DisplayName ?? "Unknown",
                        Email = user.Email ?? "N/A",
                        Mobile = user.Mobile,
                        IsActive = user.IsActive,
                        Roles = roles.ToList()
                    });
                }

                return new DepartmentDetailsViewModel
                {
                    Id = department.ID,
                    Name = department.Name,
                    BranchName = department.Branch?.Name ?? "Unknown",
                    BranchId = department.BranchID,
                    IsActive = department.IsActive,
                    CreatedAt = department.CreatedAt,
                    UpdatedAt = department.UpdatedAt,
                    UserCount = users.Count,
                    ActiveUserCount = users.Count(u => u.IsActive),
                    Users = userInfoList
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching department details for DepartmentId: {DepartmentId}", departmentId);
                throw;
            }
        }

        public async Task<DepartmentCreateViewModel> GetCreateDepartmentViewModelAsync(ClaimsPrincipal currentUser)
        {
            try
            {
                var branches = await _branchRepo.GetAllAsync();

                // Non-Admin users: only show their branch
                if (!currentUser.IsInRole("Admin"))
                {
                    var currentBranchIdStr = currentUser.FindFirst("BranchID")?.Value;
                    if (int.TryParse(currentBranchIdStr, out int currentBranchId))
                    {
                        branches = branches.Where(b => b.ID == currentBranchId);
                    }
                }

                return new DepartmentCreateViewModel
                {
                    BranchId = branches.FirstOrDefault()?.ID ?? 0,
                    IsActive = true,

                    Branches = branches.Select(b => new SelectListItem
                    {
                        Value = b.ID.ToString(),
                        Text = b.Name
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department view model");
                throw;
            }
        }

        public async Task<bool> CreateDepartmentAsync(DepartmentCreateViewModel model)
        {
            try
            {
                _logger.LogInformation("Creating new department: {Name}", model.Name);

                var department = new Department
                {
                    Name = model.Name,
                    BranchID = model.BranchId,
                    IsActive = model.IsActive
                };

                await _departmentRepo.AddAsync(department);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Department created successfully: {Name}", model.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department: {Name}", model.Name);
                return false;
            }
        }

        public async Task<DepartmentEditViewModel?> GetEditDepartmentViewModelAsync(int departmentId, ClaimsPrincipal currentUser)
        {
            try
            {
                _logger.LogInformation("Loading edit form for DepartmentId: {DepartmentId}", departmentId);

                var department = await _departmentRepo.GetDepartmentWithDetailsAsync(departmentId);

                if (department == null)
                {
                    _logger.LogWarning("Department not found: {DepartmentId}", departmentId);
                    return null;
                }

                var branches = await _branchRepo.GetAllAsync();

                // Non-Admin users: only show their branch
                if (!currentUser.IsInRole("Admin"))
                {
                    var currentBranchIdStr = currentUser.FindFirst("BranchID")?.Value;
                    if (int.TryParse(currentBranchIdStr, out int currentBranchId))
                    {
                        branches = branches.Where(b => b.ID == currentBranchId);
                    }
                }

                return new DepartmentEditViewModel
                {
                    Id = department.ID,
                    Name = department.Name,
                    BranchId = department.BranchID,
                    IsActive = department.IsActive,

                    Branches = branches.Select(b => new SelectListItem
                    {
                        Value = b.ID.ToString(),
                        Text = b.Name,
                        Selected = b.ID == department.BranchID
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for DepartmentId: {DepartmentId}", departmentId);
                throw;
            }
        }

        public async Task<bool> UpdateDepartmentAsync(DepartmentEditViewModel model)
        {
            try
            {
                _logger.LogInformation("Updating department: {DepartmentId}", model.Id);

                var department = await _departmentRepo.GetDepartmentWithDetailsAsync(model.Id);

                if (department == null)
                {
                    _logger.LogWarning("Department not found for update: {DepartmentId}", model.Id);
                    return false;
                }

                department.Name = model.Name;
                department.BranchID = model.BranchId;
                department.IsActive = model.IsActive;
                department.UpdatedAt = DateTime.UtcNow;

                _departmentRepo.Update(department);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Department updated successfully: {DepartmentId}", model.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department: {DepartmentId}", model.Id);
                return false;
            }
        }

        public async Task<bool> DeleteDepartmentAsync(int departmentId)
        {
            try
            {
                _logger.LogInformation("Deleting department: {DepartmentId}", departmentId);

                var department = await _departmentRepo.GetDepartmentWithDetailsAsync(departmentId);

                if (department == null)
                {
                    _logger.LogWarning("Department not found for deletion: {DepartmentId}", departmentId);
                    return false;
                }

                // Check if department has users
                var userCount = await _departmentRepo.GetUserCountByDepartmentAsync(departmentId);
                if (userCount > 0)
                {
                    _logger.LogWarning("Cannot delete department with users: {DepartmentId}", departmentId);
                    return false;
                }

                // Soft delete
                department.IsActive = false;
                department.UpdatedAt = DateTime.UtcNow;

                _departmentRepo.Update(department);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Department soft-deleted successfully: {DepartmentId}", departmentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department: {DepartmentId}", departmentId);
                return false;
            }
        }
    }
}
