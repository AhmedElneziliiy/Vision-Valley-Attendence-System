using CoreProject.ViewModels;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface IDeviceService
    {
        Task<IEnumerable<DeviceViewModel>> GetAllDevicesAsync(ClaimsPrincipal currentUser, int? branchId = null);
        Task<DeviceDetailsViewModel?> GetDeviceDetailsAsync(int deviceId);
        Task<DeviceCreateViewModel> GetCreateDeviceViewModelAsync(ClaimsPrincipal currentUser);
        Task<bool> CreateDeviceAsync(DeviceCreateViewModel model);
        Task<DeviceEditViewModel?> GetEditDeviceViewModelAsync(int deviceId);
        Task<bool> UpdateDeviceAsync(DeviceEditViewModel model);
        Task<bool> DeleteDeviceAsync(int deviceId);
    }
}
