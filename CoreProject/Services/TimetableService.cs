using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using CoreProject.Services.IService;
using CoreProject.ViewModels;
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
    public class TimetableService : ITimetableService
    {
        private readonly ITimetableRepository _timetableRepo;
        private readonly IRepository<Branch> _branchRepo;
        private readonly IRepository<Configuration> _configurationRepo;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TimetableService> _logger;

        public TimetableService(
            ITimetableRepository timetableRepo,
            IRepository<Branch> branchRepo,
            IRepository<Configuration> configurationRepo,
            ApplicationDbContext context,
            ILogger<TimetableService> logger)
        {
            _timetableRepo = timetableRepo;
            _branchRepo = branchRepo;
            _configurationRepo = configurationRepo;
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<TimetableViewModel>> GetAllTimetablesAsync(ClaimsPrincipal currentUser, int? branchId = null)
        {
            try
            {
                _logger.LogInformation("Fetching all timetables - BranchId filter: {BranchId}", branchId);

                IEnumerable<Timetable> timetables;

                // Non-Admin: Filter by current user's branch
                if (!currentUser.IsInRole("Admin"))
                {
                    var currentBranchIdStr = currentUser.FindFirst("BranchID")?.Value;
                    if (int.TryParse(currentBranchIdStr, out int currentBranchId))
                    {
                        timetables = await _timetableRepo.GetTimetablesByBranchAsync(currentBranchId);
                        _logger.LogInformation("Non-admin user filtering by branch: {BranchId}", currentBranchId);
                    }
                    else
                    {
                        _logger.LogWarning("Non-admin user has no valid BranchID claim");
                        return Enumerable.Empty<TimetableViewModel>();
                    }
                }
                else
                {
                    // Admin: Get all or filter by branchId if provided
                    if (branchId.HasValue)
                    {
                        timetables = await _timetableRepo.GetTimetablesByBranchAsync(branchId.Value);
                    }
                    else
                    {
                        timetables = await _timetableRepo.GetTimetablesWithDetailsAsync();
                    }
                }

                var viewModels = new List<TimetableViewModel>();

                foreach (var timetable in timetables)
                {
                    var userCount = await _timetableRepo.GetUserCountByTimetableAsync(timetable.ID);
                    var configurations = await _timetableRepo.GetConfigurationsByTimetableAsync(timetable.ID);

                    viewModels.Add(new TimetableViewModel
                    {
                        Id = timetable.ID,
                        Name = timetable.Name,
                        BranchId = timetable.BranchID,
                        BranchName = timetable.Branch?.Name ?? "Unknown",
                        WorkingDayStartingHourMinimum = timetable.WorkingDayStartingHourMinimum,
                        WorkingDayStartingHourMaximum = timetable.WorkingDayStartingHourMaximum,
                        WorkingDayEndingHour = timetable.WorkingDayEndingHour,
                        AverageWorkingHours = timetable.AverageWorkingHours,
                        IsWorkingDayEndingHourEnable = timetable.IsWorkingDayEndingHourEnable,
                        IsActive = timetable.IsActive,
                        UserCount = userCount,
                        ConfigurationCount = configurations.Count()
                    });
                }

                return viewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching timetables");
                throw;
            }
        }

        public async Task<TimetableDetailsViewModel?> GetTimetableDetailsAsync(int timetableId)
        {
            try
            {
                _logger.LogInformation("Fetching timetable details for TimetableId: {TimetableId}", timetableId);

                var timetable = await _timetableRepo.GetTimetableWithDetailsAsync(timetableId);

                if (timetable == null)
                {
                    _logger.LogWarning("Timetable not found: {TimetableId}", timetableId);
                    return null;
                }

                var userCount = await _timetableRepo.GetUserCountByTimetableAsync(timetableId);
                var configurations = await _timetableRepo.GetConfigurationsByTimetableAsync(timetableId);

                var assignedUsers = await _context.Users
                    .IgnoreQueryFilters()
                    .Include(u => u.Department)
                    .Where(u => u.TimetableID == timetableId)
                    .ToListAsync();

                return new TimetableDetailsViewModel
                {
                    Id = timetable.ID,
                    Name = timetable.Name,
                    BranchId = timetable.BranchID,
                    BranchName = timetable.Branch?.Name ?? "Unknown",
                    WorkingDayStartingHourMinimum = timetable.WorkingDayStartingHourMinimum,
                    WorkingDayStartingHourMaximum = timetable.WorkingDayStartingHourMaximum,
                    WorkingDayEndingHour = timetable.WorkingDayEndingHour,
                    AverageWorkingHours = timetable.AverageWorkingHours,
                    IsWorkingDayEndingHourEnable = timetable.IsWorkingDayEndingHourEnable,
                    IsActive = timetable.IsActive,
                    UserCount = userCount,
                    Configurations = configurations.Select(c => new TimetableConfigurationDetailViewModel
                    {
                        Id = c.ID,
                        ConfigurationName = c.Configuration?.Name ?? "Unknown",
                        Value = c.Value
                    }).ToList(),
                    AssignedUsers = assignedUsers.Select(u => new TimetableUserViewModel
                    {
                        Id = u.Id,
                        DisplayName = u.DisplayName ?? "Unknown",
                        Email = u.Email,
                        DepartmentName = u.Department?.Name,
                        IsActive = u.IsActive
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching timetable details for TimetableId: {TimetableId}", timetableId);
                throw;
            }
        }

        public async Task<TimetableCreateViewModel> GetCreateTimetableViewModelAsync(ClaimsPrincipal currentUser)
        {
            try
            {
                IEnumerable<Branch> branches;

                // Non-Admin: only show their branch
                if (!currentUser.IsInRole("Admin"))
                {
                    var currentBranchIdStr = currentUser.FindFirst("BranchID")?.Value;
                    if (int.TryParse(currentBranchIdStr, out int currentBranchId))
                    {
                        var branch = await _branchRepo.GetByIdAsync(currentBranchId);
                        branches = branch != null ? new[] { branch } : Enumerable.Empty<Branch>();
                    }
                    else
                    {
                        branches = Enumerable.Empty<Branch>();
                    }
                }
                else
                {
                    branches = await _branchRepo.GetAllAsync();
                }

                return new TimetableCreateViewModel
                {
                    BranchId = branches.FirstOrDefault()?.ID ?? 0,
                    IsActive = true,
                    AverageWorkingHours = 8,
                    IsWorkingDayEndingHourEnable = true,

                    Branches = branches.Select(b => new SelectListItem
                    {
                        Value = b.ID.ToString(),
                        Text = b.Name
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating timetable view model");
                throw;
            }
        }

        public async Task<bool> CreateTimetableAsync(TimetableCreateViewModel model)
        {
            try
            {
                _logger.LogInformation("Creating new timetable: {Name}", model.Name);

                var timetable = new Timetable
                {
                    Name = model.Name,
                    BranchID = model.BranchId,
                    WorkingDayStartingHourMinimum = model.WorkingDayStartingHourMinimum,
                    WorkingDayStartingHourMaximum = model.WorkingDayStartingHourMaximum,
                    WorkingDayEndingHour = model.WorkingDayEndingHour,
                    AverageWorkingHours = model.AverageWorkingHours,
                    IsWorkingDayEndingHourEnable = model.IsWorkingDayEndingHourEnable,
                    IsActive = model.IsActive
                };

                await _timetableRepo.AddAsync(timetable);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Timetable created successfully: {Name}", model.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating timetable: {Name}", model.Name);
                return false;
            }
        }

        public async Task<TimetableEditViewModel?> GetEditTimetableViewModelAsync(int timetableId)
        {
            try
            {
                _logger.LogInformation("Loading edit form for TimetableId: {TimetableId}", timetableId);

                var timetable = await _timetableRepo.GetTimetableWithDetailsAsync(timetableId);

                if (timetable == null)
                {
                    _logger.LogWarning("Timetable not found: {TimetableId}", timetableId);
                    return null;
                }

                var branches = await _branchRepo.GetAllAsync();
                var configurations = await _timetableRepo.GetConfigurationsByTimetableAsync(timetableId);

                return new TimetableEditViewModel
                {
                    Id = timetable.ID,
                    Name = timetable.Name,
                    BranchId = timetable.BranchID,
                    WorkingDayStartingHourMinimum = timetable.WorkingDayStartingHourMinimum,
                    WorkingDayStartingHourMaximum = timetable.WorkingDayStartingHourMaximum,
                    WorkingDayEndingHour = timetable.WorkingDayEndingHour,
                    AverageWorkingHours = timetable.AverageWorkingHours,
                    IsWorkingDayEndingHourEnable = timetable.IsWorkingDayEndingHourEnable,
                    IsActive = timetable.IsActive,

                    Branches = branches.Select(b => new SelectListItem
                    {
                        Value = b.ID.ToString(),
                        Text = b.Name,
                        Selected = b.ID == timetable.BranchID
                    }),

                    Configurations = configurations.Select(c => new TimetableConfigurationViewModel
                    {
                        Id = c.ID,
                        ConfigurationId = c.ConfigurationID,
                        ConfigurationName = c.Configuration?.Name ?? "Unknown",
                        Value = c.Value
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for TimetableId: {TimetableId}", timetableId);
                throw;
            }
        }

        public async Task<bool> UpdateTimetableAsync(TimetableEditViewModel model)
        {
            try
            {
                _logger.LogInformation("Updating timetable: {TimetableId}", model.Id);

                var timetable = await _timetableRepo.GetByIdAsync(model.Id);

                if (timetable == null)
                {
                    _logger.LogWarning("Timetable not found for update: {TimetableId}", model.Id);
                    return false;
                }

                timetable.Name = model.Name;
                timetable.BranchID = model.BranchId;
                timetable.WorkingDayStartingHourMinimum = model.WorkingDayStartingHourMinimum;
                timetable.WorkingDayStartingHourMaximum = model.WorkingDayStartingHourMaximum;
                timetable.WorkingDayEndingHour = model.WorkingDayEndingHour;
                timetable.AverageWorkingHours = model.AverageWorkingHours;
                timetable.IsWorkingDayEndingHourEnable = model.IsWorkingDayEndingHourEnable;
                timetable.IsActive = model.IsActive;

                _timetableRepo.Update(timetable);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Timetable updated successfully: {TimetableId}", model.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timetable: {TimetableId}", model.Id);
                return false;
            }
        }

        public async Task<bool> DeleteTimetableAsync(int timetableId)
        {
            try
            {
                _logger.LogInformation("Deleting timetable: {TimetableId}", timetableId);

                var timetable = await _timetableRepo.GetByIdAsync(timetableId);

                if (timetable == null)
                {
                    _logger.LogWarning("Timetable not found for deletion: {TimetableId}", timetableId);
                    return false;
                }

                // Check if timetable has users
                var userCount = await _timetableRepo.GetUserCountByTimetableAsync(timetableId);
                if (userCount > 0)
                {
                    _logger.LogWarning("Cannot delete timetable with users: {TimetableId}", timetableId);
                    return false;
                }

                // Soft delete - mark as inactive
                timetable.IsActive = false;

                _timetableRepo.Update(timetable);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Timetable soft-deleted successfully: {TimetableId}", timetableId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting timetable: {TimetableId}", timetableId);
                return false;
            }
        }

        public async Task<bool> AddConfigurationToTimetableAsync(int timetableId, int configurationId, string value)
        {
            try
            {
                _logger.LogInformation("Adding configuration {ConfigurationId} to timetable {TimetableId}", configurationId, timetableId);

                var timetableConfiguration = new TimetableConfiguration
                {
                    TimetableID = timetableId,
                    ConfigurationID = configurationId,
                    Value = value
                };

                _context.TimetableConfigurations.Add(timetableConfiguration);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Configuration added successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding configuration to timetable");
                return false;
            }
        }

        public async Task<bool> UpdateConfigurationAsync(int timetableConfigurationId, string newValue)
        {
            try
            {
                _logger.LogInformation("Updating configuration: {TimetableConfigurationId}", timetableConfigurationId);

                var configuration = await _context.TimetableConfigurations.FindAsync(timetableConfigurationId);

                if (configuration == null)
                {
                    _logger.LogWarning("Configuration not found: {TimetableConfigurationId}", timetableConfigurationId);
                    return false;
                }

                configuration.Value = newValue;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Configuration updated successfully: {TimetableConfigurationId}", timetableConfigurationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration: {TimetableConfigurationId}", timetableConfigurationId);
                return false;
            }
        }

        public async Task<bool> RemoveConfigurationFromTimetableAsync(int timetableConfigurationId)
        {
            try
            {
                _logger.LogInformation("Removing configuration: {TimetableConfigurationId}", timetableConfigurationId);

                var configuration = await _context.TimetableConfigurations.FindAsync(timetableConfigurationId);

                if (configuration == null)
                {
                    _logger.LogWarning("Configuration not found: {TimetableConfigurationId}", timetableConfigurationId);
                    return false;
                }

                _context.TimetableConfigurations.Remove(configuration);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Configuration removed successfully: {TimetableConfigurationId}", timetableConfigurationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing configuration: {TimetableConfigurationId}", timetableConfigurationId);
                return false;
            }
        }
    }
}
