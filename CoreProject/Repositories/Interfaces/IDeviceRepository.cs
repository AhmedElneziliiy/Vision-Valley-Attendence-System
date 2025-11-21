using CoreProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreProject.Repositories.Interfaces
{
    public interface IDeviceRepository : IRepository<Device>
    {
        Task<IEnumerable<Device>> GetDevicesWithDetailsAsync();
        Task<Device?> GetDeviceWithDetailsAsync(int deviceId);
        Task<IEnumerable<Device>> GetDevicesByBranchAsync(int branchId);
        Task<Device?> GetDeviceByAccessControlURLAsync(string accessControlURL);
    }
}
