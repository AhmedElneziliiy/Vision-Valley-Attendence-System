using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IRepository<ApplicationUser> _userRepo;
        private readonly IRepository<Branch> _branchRepo;
        private readonly IRepository<Attendance> _attendanceRepo;

        public DashboardRepository(
            IRepository<ApplicationUser> userRepo,
            IRepository<Branch> branchRepo,
            IRepository<Attendance> attendanceRepo)
        {
            _userRepo = userRepo;
            _branchRepo = branchRepo;
            _attendanceRepo = attendanceRepo;
        }
        public IRepository<Attendance> GetAttendanceRepo() => _attendanceRepo;

        public Task<int> GetTotalUsersAsync() => _userRepo.CountAsync();

        public Task<int> GetActiveUsersAsync() => _userRepo.CountAsync(u => u.IsActive);

        public Task<int> GetTotalBranchesAsync() => _branchRepo.CountAsync();

        public Task<int> GetTodayCheckInsAsync() =>
            _attendanceRepo.CountAsync(a => a.Date == DateTime.Today);

        public Task<int> GetPendingApprovalsAsync() =>
            _attendanceRepo.CountAsync(a => !a.HRPosted);
    }
}
