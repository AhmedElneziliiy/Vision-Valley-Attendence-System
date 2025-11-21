using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Repositories
{
    public class LampRepository : Repository<Lamp>, ILampRepository
    {
        public LampRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Lamp>> GetAllWithDetailsAsync()
        {
            return await _context.Lamps
                .Include(l => l.Branch)
                .Include(l => l.Timetable)
                .OrderBy(l => l.Branch.Name)
                .ThenBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<Lamp?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Lamps
                .Include(l => l.Branch)
                .Include(l => l.Timetable)
                .FirstOrDefaultAsync(l => l.ID == id);
        }

        public async Task<Lamp?> GetByDeviceIdAsync(string deviceId)
        {
            return await _context.Lamps
                .Include(l => l.Branch)
                .Include(l => l.Timetable)
                .FirstOrDefaultAsync(l => l.DeviceID == deviceId);
        }

        public async Task<List<Lamp>> GetByBranchIdAsync(int branchId)
        {
            return await _context.Lamps
                .Include(l => l.Branch)
                .Include(l => l.Timetable)
                .Where(l => l.BranchID == branchId)
                .ToListAsync();
        }

        public async Task<bool> DeviceIdExistsAsync(string deviceId, int? excludeLampId = null)
        {
            var query = _context.Lamps
                .IgnoreQueryFilters()
                .Where(l => l.DeviceID == deviceId);

            if (excludeLampId.HasValue)
            {
                query = query.Where(l => l.ID != excludeLampId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
