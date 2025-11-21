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
using System.Threading.Tasks;

namespace CoreProject.Services
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepo;
        private readonly IRepository<Organization> _organizationRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BranchService> _logger;

        public BranchService(
            IBranchRepository branchRepo,
            IRepository<Organization> organizationRepo,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<BranchService> logger)
        {
            _branchRepo = branchRepo;
            _organizationRepo = organizationRepo;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<BranchViewModel>> GetAllBranchesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all branches");

                var branches = await _branchRepo.GetBranchesWithDetailsAsync();

                var viewModels = new List<BranchViewModel>();

                foreach (var branch in branches)
                {
                    var userCount = await _branchRepo.GetUserCountByBranchAsync(branch.ID);

                    viewModels.Add(new BranchViewModel
                    {
                        Id = branch.ID,
                        Name = branch.Name,
                        TimeZone = branch.TimeZone,
                        Weekend = branch.Weekend,
                        IsMainBranch = branch.IsMainBranch,
                        IsActive = branch.IsActive,
                        DepartmentCount = branch.Departments?.Count ?? 0,
                        TimetableCount = branch.Timetables?.Count ?? 0,
                        UserCount = userCount,
                        CreatedAt = branch.CreatedAt
                    });
                }

                return viewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branches");
                throw;
            }
        }

        public async Task<BranchDetailsViewModel?> GetBranchDetailsAsync(int branchId)
        {
            try
            {
                _logger.LogInformation("Fetching branch details for BranchId: {BranchId}", branchId);

                var branch = await _branchRepo.GetBranchWithDetailsAsync(branchId);

                if (branch == null)
                {
                    _logger.LogWarning("Branch not found: {BranchId}", branchId);
                    return null;
                }

                var departments = await _branchRepo.GetDepartmentsByBranchAsync(branchId);
                var timetables = await _branchRepo.GetTimetablesByBranchAsync(branchId);
                var userCount = await _branchRepo.GetUserCountByBranchAsync(branchId);
                var activeUserCount = await _context.Users
                    .IgnoreQueryFilters()
                    .CountAsync(u => u.BranchID == branchId && u.IsActive);

                // Get HR staff and Managers
                var branchUsers = await _context.Users
                    .IgnoreQueryFilters()
                    .Include(u => u.Department)
                    .Where(u => u.BranchID == branchId)
                    .ToListAsync();

                var hrStaff = new List<StaffInfo>();
                var managers = new List<StaffInfo>();

                foreach (var user in branchUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    if (roles.Contains("HR"))
                    {
                        hrStaff.Add(new StaffInfo
                        {
                            Id = user.Id,
                            DisplayName = user.DisplayName ?? "Unknown",
                            Email = user.Email ?? "N/A",
                            Mobile = user.Mobile,
                            IsActive = user.IsActive,
                            DepartmentName = user.Department?.Name
                        });
                    }

                    if (roles.Contains("Manager"))
                    {
                        managers.Add(new StaffInfo
                        {
                            Id = user.Id,
                            DisplayName = user.DisplayName ?? "Unknown",
                            Email = user.Email ?? "N/A",
                            Mobile = user.Mobile,
                            IsActive = user.IsActive,
                            DepartmentName = user.Department?.Name
                        });
                    }
                }

                return new BranchDetailsViewModel
                {
                    Id = branch.ID,
                    Name = branch.Name,
                    OrganizationName = branch.Organization?.Name ?? "Unknown",
                    TimeZone = branch.TimeZone,
                    Weekend = branch.Weekend,
                    NationalHolidays = branch.NationalHolidays,
                    IsMainBranch = branch.IsMainBranch,
                    IsActive = branch.IsActive,
                    CreatedAt = branch.CreatedAt,
                    UpdatedAt = branch.UpdatedAt,
                    DepartmentCount = departments.Count(),
                    TimetableCount = timetables.Count(),
                    UserCount = userCount,
                    ActiveUserCount = activeUserCount,
                    Departments = departments.Select(d => new DepartmentInfo
                    {
                        Id = d.ID,
                        Name = d.Name,
                        IsActive = d.IsActive,
                        UserCount = _context.Users.IgnoreQueryFilters().Count(u => u.DepartmentID == d.ID)
                    }).ToList(),
                    Timetables = timetables.Select(t => new TimetableInfo
                    {
                        Id = t.ID,
                        Name = t.Name,
                        IsActive = t.IsActive,
                        AverageWorkingHours = t.AverageWorkingHours
                    }).ToList(),
                    HRStaff = hrStaff,
                    Managers = managers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branch details for BranchId: {BranchId}", branchId);
                throw;
            }
        }

        public async Task<BranchCreateViewModel> GetCreateBranchViewModelAsync()
        {
            try
            {
                var organizations = await _organizationRepo.GetAllAsync();

                var timeZones = new List<SelectListItem>
                {
                    new SelectListItem { Value = "0", Text = "UTC" },
                    new SelectListItem { Value = "1", Text = "Asia/Dubai (UAE)" },
                    new SelectListItem { Value = "2", Text = "Africa/Cairo (Egypt)" },
                    new SelectListItem { Value = "3", Text = "Europe/London (UK)" },
                    new SelectListItem { Value = "4", Text = "America/New_York (EST)" }
                };

                return new BranchCreateViewModel
                {
                    OrganizationId = organizations.FirstOrDefault()?.ID ?? 0,
                    IsActive = true,
                    Weekend = "Friday,Saturday",
                    TimetableName = "Standard Timetable",
                    AverageWorkingHours = 8,
                    IsWorkingDayEndingHourEnable = true,

                    Organizations = organizations.Select(o => new SelectListItem
                    {
                        Value = o.ID.ToString(),
                        Text = o.Name
                    }),

                    TimeZones = timeZones
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch view model");
                throw;
            }
        }

        public async Task<bool> CreateBranchAsync(BranchCreateViewModel model)
        {
            try
            {
                _logger.LogInformation("Creating new branch: {Name}", model.Name);

                var branch = new Branch
                {
                    Name = model.Name,
                    OrganizationID = model.OrganizationId,
                    TimeZone = model.TimeZone,
                    Weekend = model.Weekend,
                    NationalHolidays = model.NationalHolidays,
                    IsMainBranch = model.IsMainBranch,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                await _branchRepo.AddAsync(branch);
                await _context.SaveChangesAsync();

                // Create default timetable for the branch
                var timetable = new Timetable
                {
                    Name = model.TimetableName,
                    BranchID = branch.ID,
                    WorkingDayStartingHourMinimum = model.WorkingDayStartingHourMinimum,
                    WorkingDayStartingHourMaximum = model.WorkingDayStartingHourMaximum,
                    WorkingDayEndingHour = model.WorkingDayEndingHour,
                    AverageWorkingHours = model.AverageWorkingHours,
                    IsWorkingDayEndingHourEnable = model.IsWorkingDayEndingHourEnable,
                    IsActive = true
                };

                _context.Timetables.Add(timetable);

                // Create departments if provided
                if (model.DepartmentNames != null && model.DepartmentNames.Any())
                {
                    foreach (var deptName in model.DepartmentNames.Where(d => !string.IsNullOrWhiteSpace(d)))
                    {
                        var department = new Department
                        {
                            Name = deptName.Trim(),
                            BranchID = branch.ID,
                            IsActive = true
                        };
                        _context.Departments.Add(department);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Branch created successfully: {Name}", model.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch: {Name}", model.Name);
                return false;
            }
        }

        public async Task<BranchEditViewModel?> GetEditBranchViewModelAsync(int branchId)
        {
            try
            {
                _logger.LogInformation("Loading edit form for BranchId: {BranchId}", branchId);

                var branch = await _branchRepo.GetByIdAsync(branchId);

                if (branch == null)
                {
                    _logger.LogWarning("Branch not found: {BranchId}", branchId);
                    return null;
                }

                var organizations = await _organizationRepo.GetAllAsync();

                var timeZones = new List<SelectListItem>
                {
                    new SelectListItem { Value = "0", Text = "UTC" },
                    new SelectListItem { Value = "1", Text = "Asia/Dubai (UAE)" },
                    new SelectListItem { Value = "2", Text = "Africa/Cairo (Egypt)" },
                    new SelectListItem { Value = "3", Text = "Europe/London (UK)" },
                    new SelectListItem { Value = "4", Text = "America/New_York (EST)" }
                };

                return new BranchEditViewModel
                {
                    Id = branch.ID,
                    Name = branch.Name,
                    OrganizationId = branch.OrganizationID,
                    TimeZone = branch.TimeZone,
                    Weekend = branch.Weekend,
                    NationalHolidays = branch.NationalHolidays,
                    IsMainBranch = branch.IsMainBranch,
                    IsActive = branch.IsActive,

                    Organizations = organizations.Select(o => new SelectListItem
                    {
                        Value = o.ID.ToString(),
                        Text = o.Name,
                        Selected = o.ID == branch.OrganizationID
                    }),

                    TimeZones = timeZones.Select(tz => new SelectListItem
                    {
                        Value = tz.Value,
                        Text = tz.Text,
                        Selected = tz.Value == branch.TimeZone.ToString()
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for BranchId: {BranchId}", branchId);
                throw;
            }
        }

        public async Task<bool> UpdateBranchAsync(BranchEditViewModel model)
        {
            try
            {
                _logger.LogInformation("Updating branch: {BranchId}", model.Id);

                var branch = await _branchRepo.GetByIdAsync(model.Id);

                if (branch == null)
                {
                    _logger.LogWarning("Branch not found for update: {BranchId}", model.Id);
                    return false;
                }

                branch.Name = model.Name;
                branch.OrganizationID = model.OrganizationId;
                branch.TimeZone = model.TimeZone;
                branch.Weekend = model.Weekend;
                branch.NationalHolidays = model.NationalHolidays;
                branch.IsMainBranch = model.IsMainBranch;
                branch.IsActive = model.IsActive;
                branch.UpdatedAt = DateTime.UtcNow;

                _branchRepo.Update(branch);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Branch updated successfully: {BranchId}", model.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch: {BranchId}", model.Id);
                return false;
            }
        }

        public async Task<bool> DeleteBranchAsync(int branchId)
        {
            try
            {
                _logger.LogInformation("Deleting branch: {BranchId}", branchId);

                var branch = await _branchRepo.GetByIdAsync(branchId);

                if (branch == null)
                {
                    _logger.LogWarning("Branch not found for deletion: {BranchId}", branchId);
                    return false;
                }

                // Check if branch has users
                var userCount = await _branchRepo.GetUserCountByBranchAsync(branchId);
                if (userCount > 0)
                {
                    _logger.LogWarning("Cannot delete branch with users: {BranchId}", branchId);
                    return false;
                }

                // Soft delete - mark as inactive
                branch.IsActive = false;
                branch.UpdatedAt = DateTime.UtcNow;

                _branchRepo.Update(branch);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Branch soft-deleted successfully: {BranchId}", branchId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch: {BranchId}", branchId);
                return false;
            }
        }

        public async Task<bool> AddDepartmentToBranchAsync(int branchId, string departmentName)
        {
            try
            {
                _logger.LogInformation("Adding department {Name} to branch {BranchId}", departmentName, branchId);

                var department = new Department
                {
                    Name = departmentName,
                    BranchID = branchId,
                    IsActive = true
                };

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Department added successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding department to branch");
                return false;
            }
        }

        public async Task<bool> RemoveDepartmentFromBranchAsync(int departmentId)
        {
            try
            {
                _logger.LogInformation("Removing department: {DepartmentId}", departmentId);

                var department = await _context.Departments.FindAsync(departmentId);

                if (department == null)
                {
                    _logger.LogWarning("Department not found: {DepartmentId}", departmentId);
                    return false;
                }

                // Check if department has users
                var userCount = await _context.Users
                    .IgnoreQueryFilters()
                    .CountAsync(u => u.DepartmentID == departmentId);

                if (userCount > 0)
                {
                    _logger.LogWarning("Cannot delete department with users: {DepartmentId}", departmentId);
                    return false;
                }

                // Soft delete
                department.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Department removed successfully: {DepartmentId}", departmentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing department: {DepartmentId}", departmentId);
                return false;
            }
        }

        public async Task<bool> UpdateDepartmentAsync(int departmentId, string newName, bool isActive)
        {
            try
            {
                _logger.LogInformation("Updating department: {DepartmentId}", departmentId);

                var department = await _context.Departments.FindAsync(departmentId);

                if (department == null)
                {
                    _logger.LogWarning("Department not found: {DepartmentId}", departmentId);
                    return false;
                }

                department.Name = newName;
                department.IsActive = isActive;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Department updated successfully: {DepartmentId}", departmentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department: {DepartmentId}", departmentId);
                return false;
            }
        }
    }
}
