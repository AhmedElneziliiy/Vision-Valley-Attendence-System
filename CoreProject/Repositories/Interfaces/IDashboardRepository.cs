using CoreProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        IRepository<Attendance> GetAttendanceRepo();
        Task<int> GetTotalUsersAsync(int? branchFilter);
        Task<int> GetActiveUsersAsync(int? branchFilter);
        Task<int> GetTotalBranchesAsync();
        Task<int> GetTodayCheckInsAsync(int? branchFilter);
        Task<int> GetPendingApprovalsAsync();
    }
}
