using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using CoreProject.Services.IService;
using CoreProject.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepo;
        private readonly ILogger<DashboardService> _logger;
        private readonly IRepository<ApplicationUser> _userRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardService(
            IDashboardRepository dashboardRepo,
            IRepository<ApplicationUser> userRepo,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardService> logger)
        {
            _dashboardRepo = dashboardRepo;
            _userRepo = userRepo;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<DashboardViewModel> GetDashboardStatsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Fetching dashboard statistics for user {UserId}", userId);

                // Get user's branch info and roles
                var user = await _userRepo.Query()
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    throw new Exception($"User {userId} not found");
                }

                // Check if user is Admin
                var roles = await _userManager.GetRolesAsync(user);
                bool isAdmin = roles.Contains("Admin");

                // Admin: Always see all data (no filter)
                // HR/Others in main branch: See all data (no filter)
                // HR/Others in specific branch: See only their branch data
                int? branchFilter = null;

                // Only apply filter if NOT Admin AND NOT in main branch
                if (!isAdmin && user.Branch != null && !user.Branch.IsMainBranch)
                {
                    branchFilter = user.BranchID;
                    _logger.LogInformation("Non-admin, non-main-branch user - filtering by branch: {BranchId}", user.BranchID);
                }
                else if (isAdmin)
                {
                    _logger.LogInformation("Admin user - showing all data");
                }
                else
                {
                    _logger.LogInformation("Main branch user - showing all data");
                }

                var model = new DashboardViewModel
                {
                    TotalUsers = await _dashboardRepo.GetTotalUsersAsync(branchFilter),
                    ActiveUsers = await _dashboardRepo.GetActiveUsersAsync(branchFilter),
                    TotalBranches = await _dashboardRepo.GetTotalBranchesAsync(),
                    TodayCheckIns = await _dashboardRepo.GetTodayCheckInsAsync(branchFilter),
                    PendingApprovals = await _dashboardRepo.GetPendingApprovalsAsync()
                };

                // Load chart data with branch filter
                await LoadMonthlyCheckInsAsync(model, branchFilter);
                await LoadDepartmentAttendanceAsync(model, branchFilter);
                await LoadRecentActivitiesAsync(model, branchFilter);

                _logger.LogInformation("Dashboard statistics loaded successfully for user {UserId}", userId);
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard statistics");
                throw;
            }
        }

        private async Task LoadMonthlyCheckInsAsync(DashboardViewModel model, int? branchFilter)
        {
            var attendanceQuery = _dashboardRepo.GetAttendanceRepo().Query();
            var sixMonthsAgo = DateTime.Today.AddMonths(-5).Date;

            // Apply branch filter if specified
            if (branchFilter.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => a.User!.BranchID == branchFilter.Value);
            }

            var monthlyData = await attendanceQuery
                .Where(a => a.Date >= sixMonthsAgo)
                .GroupBy(a => new { a.Date.Year, a.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .ToListAsync();

            // Fill in missing months with zero values
            var allMonths = Enumerable.Range(0, 6)
                .Select(i => DateTime.Today.AddMonths(-5 + i))
                .Select(date => new { date.Year, date.Month })
                .ToList();

            model.MonthlyCheckIns = allMonths
                .Select(m => new MonthlyCheckIn
                {
                    Month = new DateTime(m.Year, m.Month, 1).ToString("MMM yyyy"),
                    CheckIns = monthlyData
                        .FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Count ?? 0
                })
                .ToList();
        }

        private async Task LoadDepartmentAttendanceAsync(DashboardViewModel model, int? branchFilter)
        {
            var attendanceQuery = _dashboardRepo.GetAttendanceRepo().Query();
            var today = DateTime.Today;

            // Apply branch filter if specified
            if (branchFilter.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => a.User!.BranchID == branchFilter.Value);
            }

            var departmentData = await attendanceQuery
                .Include(a => a.User)
                    .ThenInclude(u => u!.Department)
                .Where(a => a.Date == today && a.User != null && a.User.Department != null)
                .GroupBy(a => new
                {
                    DeptId = a.User!.Department!.ID,
                    DeptName = a.User.Department.Name
                })
                .Select(g => new
                {
                    Department = g.Key.DeptName,
                    Present = g.Count(),
                    Total = g.First().User!.Department!.Users.Count(u => u.IsActive && (!branchFilter.HasValue || u.BranchID == branchFilter.Value))
                })
                .ToListAsync();

            model.DepartmentAttendance = departmentData
                .Select(d => new DepartmentAttendance
                {
                    Department = d.Department,
                    Present = d.Present,
                    Total = d.Total,
                    Percentage = d.Total > 0 ? (int)Math.Round((d.Present / (double)d.Total) * 100) : 0
                })
                .OrderByDescending(d => d.Percentage)
                .ToList();
        }

        private async Task LoadRecentActivitiesAsync(DashboardViewModel model, int? branchFilter)
        {
            var attendanceQuery = _dashboardRepo.GetAttendanceRepo().Query();

            // Apply branch filter if specified
            if (branchFilter.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => a.User!.BranchID == branchFilter.Value);
            }

            var recentData = await attendanceQuery
                .Include(a => a.User)
                .Where(a => a.User != null)
                .OrderByDescending(a => a.Date)
                .Take(20)
                .Select(a => new
                {
                    UserName = a.User!.DisplayName,
                    CheckInTime = a.FirstCheckIn,
                    CheckOutTime = a.LastCheckOut,
                    Date = a.Date
                })
                .ToListAsync();

            // Process in memory
            var activities = new List<RecentActivity>();

            foreach (var r in recentData)
            {
                // Check if CheckInTime is not null or empty
                if (!string.IsNullOrEmpty(r.CheckInTime))
                {
                    // Try to parse the time string
                    if (TimeSpan.TryParse(r.CheckInTime, out TimeSpan checkInTimeSpan))
                    {
                        activities.Add(new RecentActivity
                        {
                            UserName = r.UserName,
                            Action = "Checked In",
                            Time = r.Date.Add(checkInTimeSpan),
                            Icon = "bi-box-arrow-in-right",
                            Color = "success"
                        });
                    }
                }

                // Check if CheckOutTime is not null or empty
                if (!string.IsNullOrEmpty(r.CheckOutTime))
                {
                    // Try to parse the time string
                    if (TimeSpan.TryParse(r.CheckOutTime, out TimeSpan checkOutTimeSpan))
                    {
                        activities.Add(new RecentActivity
                        {
                            UserName = r.UserName,
                            Action = "Checked Out",
                            Time = r.Date.Add(checkOutTimeSpan),
                            Icon = "bi-box-arrow-left",
                            Color = "warning"
                        });
                    }
                }
            }

            model.RecentActivities = activities
                .OrderByDescending(a => a.Time)
                .Take(5)
                .ToList();

            if (!model.RecentActivities.Any())
            {
                model.RecentActivities = new List<RecentActivity>
        {
            new()
            {
                UserName = "System",
                Action = "No recent activity",
                Time = DateTime.Now,
                Icon = "bi-info-circle",
                Color = "secondary"
            }
        };
            }
        }
    
    }
}