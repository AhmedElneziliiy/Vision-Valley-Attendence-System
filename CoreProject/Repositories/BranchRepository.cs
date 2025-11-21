using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Repositories
{
    public class BranchRepository : Repository<Branch>, IBranchRepository
    {
        public BranchRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Branch>> GetBranchesWithDetailsAsync()
        {
            return await _context.Branches
                .Include(b => b.Organization)
                .Include(b => b.Departments)
                .Include(b => b.Timetables)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<Branch?> GetBranchWithDetailsAsync(int branchId)
        {
            return await _context.Branches
                .Include(b => b.Organization)
                .Include(b => b.Departments)
                .Include(b => b.Timetables)
                .Include(b => b.Users)
                .FirstOrDefaultAsync(b => b.ID == branchId);
        }

        public async Task<IEnumerable<Department>> GetDepartmentsByBranchAsync(int branchId)
        {
            return await _context.Departments
                .Where(d => d.BranchID == branchId)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Timetable>> GetTimetablesByBranchAsync(int branchId)
        {
            return await _context.Timetables
                .Where(t => t.BranchID == branchId)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<int> GetUserCountByBranchAsync(int branchId)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .CountAsync(u => u.BranchID == branchId);
        }
    }
}
