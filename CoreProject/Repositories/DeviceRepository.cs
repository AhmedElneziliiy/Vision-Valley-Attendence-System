using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Repositories
{
    public class DeviceRepository : Repository<Device>, IDeviceRepository
    {
        public DeviceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Device>> GetDevicesWithDetailsAsync()
        {
            return await _context.Devices
                .IgnoreQueryFilters() // Include devices from inactive branches
                .Include(d => d.Branch)
                .OrderBy(d => d.ID)
                .ToListAsync();
        }

        public async Task<Device?> GetDeviceWithDetailsAsync(int deviceId)
        {
            return await _context.Devices
                .IgnoreQueryFilters()
                .Include(d => d.Branch)
                .FirstOrDefaultAsync(d => d.ID == deviceId);
        }

        public async Task<IEnumerable<Device>> GetDevicesByBranchAsync(int branchId)
        {
            return await _context.Devices
                .IgnoreQueryFilters()
                .Where(d => d.BranchID == branchId)
                .Include(d => d.Branch)
                .OrderBy(d => d.ID)
                .ToListAsync();
        }

        public async Task<Device?> GetDeviceByAccessControlURLAsync(string accessControlURL)
        {
            return await _context.Devices
                .IgnoreQueryFilters()
                .Include(d => d.Branch)
                .FirstOrDefaultAsync(d => d.AccessControlURL == accessControlURL);
        }
    }
}
