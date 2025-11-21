using CoreProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreProject.Repositories.Interfaces
{
    public interface IDepartmentRepository : IRepository<Department>
    {
        Task<IEnumerable<Department>> GetDepartmentsWithDetailsAsync();
        Task<Department?> GetDepartmentWithDetailsAsync(int departmentId);
        Task<IEnumerable<Department>> GetDepartmentsByBranchAsync(int branchId);
        Task<int> GetUserCountByDepartmentAsync(int departmentId);
    }
}
