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
        Task<int> GetTotalUsersAsync();
        Task<int> GetActiveUsersAsync();
        Task<int> GetTotalBranchesAsync();
        Task<int> GetTodayCheckInsAsync();
        Task<int> GetPendingApprovalsAsync();
    }
}
