using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using CoreProject.Services.IService;
using CoreProject.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepo;
        private readonly IRepository<Branch> _branchRepo;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(
            IDeviceRepository deviceRepo,
            IRepository<Branch> branchRepo,
            ApplicationDbContext context,
            ILogger<DeviceService> logger)
        {
            _deviceRepo = deviceRepo;
            _branchRepo = branchRepo;
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<DeviceViewModel>> GetAllDevicesAsync(ClaimsPrincipal currentUser, int? branchId = null)
        {
            try
            {
                _logger.LogInformation("Fetching all devices - BranchId filter: {BranchId}", branchId);

                IEnumerable<Device> devices;

                // Admins can see all devices, others only see their branch
                if (!currentUser.IsInRole("Admin"))
                {
                    var currentBranchIdStr = currentUser.FindFirst("BranchID")?.Value;
                    if (int.TryParse(currentBranchIdStr, out int currentBranchId))
                    {
                        devices = await _deviceRepo.GetDevicesByBranchAsync(currentBranchId);
                        _logger.LogInformation("Non-admin user filtering by branch: {BranchId}", currentBranchId);
                    }
                    else
                    {
                        _logger.LogWarning("Non-admin user has no valid BranchID claim");
                        return Enumerable.Empty<DeviceViewModel>();
                    }
                }
                else
                {
                    // Admin: Get all or filter by branchId if provided
                    if (branchId.HasValue)
                    {
                        devices = await _deviceRepo.GetDevicesByBranchAsync(branchId.Value);
                    }
                    else
                    {
                        devices = await _deviceRepo.GetDevicesWithDetailsAsync();
                    }
                }

                var viewModels = devices.Select(device => new DeviceViewModel
                {
                    Id = device.ID,
                    DeviceID = device.DeviceID,
                    DeviceType = device.DeviceType,
                    CoverageArea = device.CoverageArea,
                    BranchId = device.BranchID,
                    BranchName = device.Branch?.Name ?? "Unknown",
                    IsSignedIn = device.IsSignedIn,
                    IsSignedOut = device.IsSignedOut,
                    IsPassThrough = device.IsPassThrough,
                    Description = device.Description,
                    IsActive = device.IsActive,
                    AccessControlURL = device.AccessControlURL,
                    AccessControlState = device.AccessControlState
                }).ToList();

                return viewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching devices");
                throw;
            }
        }

        public async Task<DeviceDetailsViewModel?> GetDeviceDetailsAsync(int deviceId)
        {
            try
            {
                _logger.LogInformation("Fetching device details for DeviceId: {DeviceId}", deviceId);

                var device = await _deviceRepo.GetDeviceWithDetailsAsync(deviceId);

                if (device == null)
                {
                    _logger.LogWarning("Device not found: {DeviceId}", deviceId);
                    return null;
                }

                return new DeviceDetailsViewModel
                {
                    Id = device.ID,
                    DeviceID = device.DeviceID,
                    DeviceType = device.DeviceType,
                    CoverageArea = device.CoverageArea,
                    BranchId = device.BranchID,
                    BranchName = device.Branch?.Name ?? "Unknown",
                    IsSignedIn = device.IsSignedIn,
                    IsSignedOut = device.IsSignedOut,
                    IsPassThrough = device.IsPassThrough,
                    Description = device.Description,
                    IsActive = device.IsActive,
                    AccessControlURL = device.AccessControlURL,
                    AccessControlState = device.AccessControlState
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching device details for DeviceId: {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task<DeviceCreateViewModel> GetCreateDeviceViewModelAsync(ClaimsPrincipal currentUser)
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

                return new DeviceCreateViewModel
                {
                    BranchId = branches.FirstOrDefault()?.ID ?? 0,
                    IsSignedIn = true,
                    IsSignedOut = true,
                    IsPassThrough = false,
                    IsActive = true,
                    AccessControlState = 1,

                    Branches = branches.Select(b => new SelectListItem
                    {
                        Value = b.ID.ToString(),
                        Text = b.Name
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating device view model");
                throw;
            }
        }

        public async Task<bool> CreateDeviceAsync(DeviceCreateViewModel model)
        {
            try
            {
                _logger.LogInformation("Creating new device for branch: {BranchId}", model.BranchId);

                var device = new Device
                {
                    DeviceID = model.DeviceID,
                    DeviceType = !string.IsNullOrEmpty(model.DeviceType) ? model.DeviceType[0] : (char?)null,
                    CoverageArea = model.CoverageArea,
                    BranchID = model.BranchId,
                    IsSignedIn = model.IsSignedIn,
                    IsSignedOut = model.IsSignedOut,
                    IsPassThrough = model.IsPassThrough,
                    Description = model.Description,
                    IsActive = model.IsActive,
                    AccessControlURL = model.AccessControlURL,
                    AccessControlState = model.AccessControlState
                };

                await _deviceRepo.AddAsync(device);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Device created successfully: DeviceId {DeviceId}", device.ID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating device");
                return false;
            }
        }

        public async Task<DeviceEditViewModel?> GetEditDeviceViewModelAsync(int deviceId)
        {
            try
            {
                _logger.LogInformation("Loading edit form for DeviceId: {DeviceId}", deviceId);

                var device = await _deviceRepo.GetDeviceWithDetailsAsync(deviceId);

                if (device == null)
                {
                    _logger.LogWarning("Device not found: {DeviceId}", deviceId);
                    return null;
                }

                var branches = await _branchRepo.GetAllAsync();

                return new DeviceEditViewModel
                {
                    Id = device.ID,
                    DeviceID = device.DeviceID,
                    DeviceType = device.DeviceType?.ToString(),
                    CoverageArea = device.CoverageArea,
                    BranchId = device.BranchID,
                    IsSignedIn = device.IsSignedIn ?? false,
                    IsSignedOut = device.IsSignedOut ?? false,
                    IsPassThrough = device.IsPassThrough,
                    Description = device.Description,
                    IsActive = device.IsActive,
                    AccessControlURL = device.AccessControlURL,
                    AccessControlState = device.AccessControlState,

                    Branches = branches.Select(b => new SelectListItem
                    {
                        Value = b.ID.ToString(),
                        Text = b.Name,
                        Selected = b.ID == device.BranchID
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for DeviceId: {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task<bool> UpdateDeviceAsync(DeviceEditViewModel model)
        {
            try
            {
                _logger.LogInformation("Updating device: {DeviceId}", model.Id);

                var device = await _deviceRepo.GetByIdAsync(model.Id);

                if (device == null)
                {
                    _logger.LogWarning("Device not found for update: {DeviceId}", model.Id);
                    return false;
                }

                device.DeviceID = model.DeviceID;
                device.DeviceType = !string.IsNullOrEmpty(model.DeviceType) ? model.DeviceType[0] : (char?)null;
                device.CoverageArea = model.CoverageArea;
                device.BranchID = model.BranchId;
                device.IsSignedIn = model.IsSignedIn;
                device.IsSignedOut = model.IsSignedOut;
                device.IsPassThrough = model.IsPassThrough;
                device.Description = model.Description;
                device.IsActive = model.IsActive;
                device.AccessControlURL = model.AccessControlURL;
                device.AccessControlState = model.AccessControlState;

                _deviceRepo.Update(device);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Device updated successfully: {DeviceId}", model.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device: {DeviceId}", model.Id);
                return false;
            }
        }

        public async Task<bool> DeleteDeviceAsync(int deviceId)
        {
            try
            {
                _logger.LogInformation("Deleting device: {DeviceId}", deviceId);

                var device = await _deviceRepo.GetByIdAsync(deviceId);

                if (device == null)
                {
                    _logger.LogWarning("Device not found for deletion: {DeviceId}", deviceId);
                    return false;
                }

                _deviceRepo.Delete(device);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Device deleted successfully: {DeviceId}", deviceId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting device: {DeviceId}", deviceId);
                return false;
            }
        }
    }
}
