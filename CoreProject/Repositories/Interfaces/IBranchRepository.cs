using CoreProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreProject.Repositories.Interfaces
{
    public interface IBranchRepository : IRepository<Branch>
    {
        Task<IEnumerable<Branch>> GetBranchesWithDetailsAsync();
        Task<Branch?> GetBranchWithDetailsAsync(int branchId);
        Task<IEnumerable<Department>> GetDepartmentsByBranchAsync(int branchId);
        Task<IEnumerable<Timetable>> GetTimetablesByBranchAsync(int branchId);
        Task<int> GetUserCountByBranchAsync(int branchId);
    }
}
