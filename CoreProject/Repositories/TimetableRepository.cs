using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Repositories
{
    public class TimetableRepository : Repository<Timetable>, ITimetableRepository
    {
        public TimetableRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Timetable>> GetTimetablesWithDetailsAsync()
        {
            return await _context.Timetables
                .IgnoreQueryFilters() // Include inactive timetables
                .Include(t => t.Branch)
                .Include(t => t.Configurations)
                    .ThenInclude(tc => tc.Configuration)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Timetable?> GetTimetableWithDetailsAsync(int timetableId)
        {
            return await _context.Timetables
                .IgnoreQueryFilters() // Include inactive timetables
                .Include(t => t.Branch)
                .Include(t => t.Configurations)
                    .ThenInclude(tc => tc.Configuration)
                .Include(t => t.Users)
                .FirstOrDefaultAsync(t => t.ID == timetableId);
        }

        public async Task<IEnumerable<Timetable>> GetTimetablesByBranchAsync(int branchId)
        {
            return await _context.Timetables
                .IgnoreQueryFilters() // Include inactive timetables
                .Where(t => t.BranchID == branchId)
                .Include(t => t.Branch)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<int> GetUserCountByTimetableAsync(int timetableId)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .CountAsync(u => u.TimetableID == timetableId);
        }

        public async Task<IEnumerable<TimetableConfiguration>> GetConfigurationsByTimetableAsync(int timetableId)
        {
            return await _context.TimetableConfigurations
                .Where(tc => tc.TimetableID == timetableId)
                .Include(tc => tc.Configuration)
                .ToListAsync();
        }
    }
}
