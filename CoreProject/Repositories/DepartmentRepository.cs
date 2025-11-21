using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Repositories
{
    public class DepartmentRepository : Repository<Department>, IDepartmentRepository
    {
        public DepartmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Department>> GetDepartmentsWithDetailsAsync()
        {
            return await _context.Departments
                .IgnoreQueryFilters()
                .Include(d => d.Branch)
                .OrderBy(d => d.Branch.Name)
                .ThenBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<Department?> GetDepartmentWithDetailsAsync(int departmentId)
        {
            return await _context.Departments
                .IgnoreQueryFilters()
                .Include(d => d.Branch)
                .Include(d => d.Users)
                .FirstOrDefaultAsync(d => d.ID == departmentId);
        }

        public async Task<IEnumerable<Department>> GetDepartmentsByBranchAsync(int branchId)
        {
            return await _context.Departments
                .IgnoreQueryFilters()
                .Where(d => d.BranchID == branchId)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<int> GetUserCountByDepartmentAsync(int departmentId)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .CountAsync(u => u.DepartmentID == departmentId);
        }
    }
}
