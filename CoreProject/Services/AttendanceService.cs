using CoreProject.Context;
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
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IUserRepository _userRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendanceService> _logger;
        private readonly ITimezoneService _timezoneService;

        public AttendanceService(
            IAttendanceRepository attendanceRepo,
            IUserRepository userRepo,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<AttendanceService> logger,
            ITimezoneService timezoneService)
        {
            _attendanceRepo = attendanceRepo;
            _userRepo = userRepo;
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _timezoneService = timezoneService;
        }

        public async Task<CheckInOutResultViewModel> CheckInAsync(int userId)
        {
            try
            {
                // Get user's branch timezone and timetable
                var user = await _context.Users
                    .Include(u => u.Branch)
                    .Include(u => u.Timetable)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                var branchTimezone = user?.Branch?.TimeZone ?? 0; // Default to UTC if no branch

                // Get local time for the branch
                var branchNow = _timezoneService.GetBranchNow(branchTimezone);
                var today = branchNow.Date;

                // Store UTC time in database
                var utcNow = DateTime.UtcNow;
                var now = utcNow.TimeOfDay;

                var attendance = await _attendanceRepo.GetTodayAttendanceAsync(userId, today);

                // Convert UTC time to local time
                var localCheckInTime = _timezoneService.ConvertUtcTimeToLocal(now, utcNow.Date, branchTimezone);

                // Calculate attendance status based on timetable
                var (status, minutesLate) = CalculateAttendanceStatus(localCheckInTime, user?.Timetable);

                if (attendance == null)
                {
                    // Create new attendance record with branch local date and status
                    attendance = new Attendance
                    {
                        UserID = userId,
                        Date = today, // Local date for the branch
                        FirstCheckIn = localCheckInTime.ToString(@"hh\:mm"), // Local time
                        Status = status, // Calculated status
                        MinutesLate = minutesLate, // Minutes late/early
                        CreatedAt = DateTime.UtcNow
                    };

                    await _attendanceRepo.AddAsync(attendance);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Update existing attendance status (for multiple check-ins on same day)
                    attendance.Status = status;
                    attendance.MinutesLate = minutesLate;
                    attendance.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // Add check-in record with local time
                var record = new AttendanceRecord
                {
                    AttendanceID = attendance.ID,
                    Time = localCheckInTime, // Store local branch time
                    IsCheckIn = true,
                    IsAutomated = false
                };

                _context.AttendanceRecords.Add(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} checked in at {Time} (local time)", userId, localCheckInTime);

                return new CheckInOutResultViewModel
                {
                    Success = true,
                    Message = "Checked in successfully",
                    IsCheckIn = true,
                    Time = localCheckInTime, // Return local time
                    AttendanceId = attendance.ID
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-in for user {UserId}", userId);
                return new CheckInOutResultViewModel
                {
                    Success = false,
                    Message = "Failed to check in. Please try again."
                };
            }
        }

        public async Task<CheckInOutResultViewModel> CheckOutAsync(int userId)
        {
            try
            {
                // Get user's branch timezone
                var user = await _context.Users.Include(u => u.Branch).FirstOrDefaultAsync(u => u.Id == userId);
                var branchTimezone = user?.Branch?.TimeZone ?? 0;

                // Get local date for the branch
                var branchNow = _timezoneService.GetBranchNow(branchTimezone);
                var today = branchNow.Date;

                // Store UTC time in database
                var utcNow = DateTime.UtcNow;
                var now = utcNow.TimeOfDay;

                var attendance = await _attendanceRepo.GetTodayAttendanceAsync(userId, today);

                if (attendance == null)
                {
                    return new CheckInOutResultViewModel
                    {
                        Success = false,
                        Message = "No check-in found for today. Please check in first."
                    };
                }

                // Add check-out record with local time
                var localCheckOutTime = _timezoneService.ConvertUtcTimeToLocal(now, utcNow.Date, branchTimezone);

                var record = new AttendanceRecord
                {
                    AttendanceID = attendance.ID,
                    Time = localCheckOutTime, // Store local branch time
                    IsCheckIn = false,
                    IsAutomated = false
                };

                _logger.LogInformation("Creating check-out record for user {UserId} at {Time} (local time)", userId, localCheckOutTime);

                // Add check-out record first
                _context.AttendanceRecords.Add(record);
                await _context.SaveChangesAsync(); // Save the record first

                // Now reload the attendance with all records
                var updatedAttendance = await _attendanceRepo.GetTodayAttendanceAsync(userId, today);

                if (updatedAttendance != null)
                {
                    _logger.LogInformation("Total records after checkout: {Count}", updatedAttendance.Records.Count);

                    // Calculate duration with all records
                    var calculatedDuration = CalculateDuration(updatedAttendance.Records.ToList());

                    _logger.LogInformation("Calculated duration: {Duration} minutes", calculatedDuration);

                    // Convert UTC time to local time for display
                    var localLastCheckOut = _timezoneService.ConvertUtcTimeToLocal(now, utcNow.Date, branchTimezone);

                    // Update attendance fields
                    updatedAttendance.LastCheckOut = localLastCheckOut.ToString(@"hh\:mm"); // Local time
                    updatedAttendance.Duration = calculatedDuration;
                    updatedAttendance.UpdatedAt = DateTime.UtcNow;

                    // Explicitly mark properties as modified
                    _context.Entry(updatedAttendance).Property(a => a.Duration).IsModified = true;
                    _context.Entry(updatedAttendance).Property(a => a.LastCheckOut).IsModified = true;
                    _context.Entry(updatedAttendance).Property(a => a.UpdatedAt).IsModified = true;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} checked out at {Time} (local time). Duration saved: {Duration}", userId, localCheckOutTime, calculatedDuration);
                }

                return new CheckInOutResultViewModel
                {
                    Success = true,
                    Message = "Checked out successfully",
                    IsCheckIn = false,
                    Time = localCheckOutTime, // Return local time
                    AttendanceId = attendance.ID
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-out for user {UserId}", userId);
                return new CheckInOutResultViewModel
                {
                    Success = false,
                    Message = "Failed to check out. Please try again."
                };
            }
        }

        public async Task<TodayAttendanceViewModel?> GetTodayAttendanceStatusAsync(int userId)
        {
            try
            {
                // Get user's branch timezone
                var user = await _context.Users.Include(u => u.Branch).FirstOrDefaultAsync(u => u.Id == userId);
                var branchTimezone = user?.Branch?.TimeZone ?? 0;

                // Get local date for the branch
                var today = _timezoneService.GetBranchToday(branchTimezone);
                var attendance = await _attendanceRepo.GetTodayAttendanceAsync(userId, today);

                if (attendance == null)
                {
                    return new TodayAttendanceViewModel
                    {
                        Date = today,
                        IsCheckedIn = false,
                        Records = new List<AttendanceRecordViewModel>(),
                        AttendanceStatus = AttendanceStatus.Absent,
                        MinutesLate = null
                    };
                }

                var lastRecord = attendance.Records.OrderByDescending(r => r.Time).FirstOrDefault();

                return new TodayAttendanceViewModel
                {
                    AttendanceId = attendance.ID,
                    Date = attendance.Date,
                    FirstCheckIn = attendance.FirstCheckIn,
                    LastCheckOut = attendance.LastCheckOut,
                    Duration = attendance.Duration,
                    IsCheckedIn = lastRecord?.IsCheckIn ?? false,
                    AttendanceStatus = attendance.Status, // Include status
                    MinutesLate = attendance.MinutesLate, // Include minutes late
                    Records = attendance.Records.Select(r => new AttendanceRecordViewModel
                    {
                        Id = r.ID,
                        Time = r.Time,
                        IsCheckIn = r.IsCheckIn,
                        IsAutomated = r.IsAutomated,
                        FaceValidation = r.FaceValidation,
                        ReasonName = r.Reason?.DisplayName_En
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's attendance for user {UserId}", userId);
                return null;
            }
        }

        public async Task<IEnumerable<AttendanceViewModel>> GetMyAttendanceAsync(int userId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                // Get user's branch timezone
                var user = await _context.Users.Include(u => u.Branch).FirstOrDefaultAsync(u => u.Id == userId);
                var branchTimezone = user?.Branch?.TimeZone ?? 0;
                var branchToday = _timezoneService.GetBranchToday(branchTimezone);

                var start = startDate ?? branchToday.AddDays(-30);
                var end = endDate ?? branchToday;

                var attendances = await _attendanceRepo.GetUserAttendanceByDateRangeAsync(userId, start, end);

                return attendances.Select(a => new AttendanceViewModel
                {
                    Id = a.ID,
                    UserId = a.UserID,
                    UserName = a.User.DisplayName,
                    Date = a.Date,
                    FirstCheckIn = a.FirstCheckIn,
                    LastCheckOut = a.LastCheckOut,
                    Duration = a.Duration,
                    HRPosted = a.HRPosted,
                    HRUserName = a.HRUser?.DisplayName,
                    HRPostedDate = a.HRPostedDate,
                    AttendanceStatus = a.Status, // Include status
                    MinutesLate = a.MinutesLate, // Include minutes late
                    Records = a.Records.Select(r => new AttendanceRecordViewModel
                    {
                        Id = r.ID,
                        Time = r.Time,
                        IsCheckIn = r.IsCheckIn,
                        IsAutomated = r.IsAutomated,
                        FaceValidation = r.FaceValidation,
                        ReasonName = r.Reason?.DisplayName_En
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance for user {UserId}", userId);
                return Enumerable.Empty<AttendanceViewModel>();
            }
        }

        public async Task<AttendanceSummaryViewModel> GetMyAttendanceSummaryAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var attendances = await _attendanceRepo.GetUserAttendanceByDateRangeAsync(userId, startDate, endDate);
                var attendanceList = attendances.ToList();

                var totalDays = (endDate.Date - startDate.Date).Days + 1;
                var presentDays = attendanceList.Count(a => !string.IsNullOrEmpty(a.FirstCheckIn));
                var absentDays = totalDays - presentDays;
                var totalMinutes = attendanceList.Sum(a => a.Duration);
                var averageMinutes = presentDays > 0 ? totalMinutes / presentDays : 0;

                return new AttendanceSummaryViewModel
                {
                    TotalDays = totalDays,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    TotalMinutes = totalMinutes,
                    AverageMinutes = averageMinutes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance summary for user {UserId}", userId);
                return new AttendanceSummaryViewModel();
            }
        }

        public async Task<IEnumerable<TeamAttendanceViewModel>> GetTeamAttendanceAsync(ClaimsPrincipal currentUser, DateTime date)
        {
            try
            {
                var teamUserIds = await GetTeamUserIdsAsync(currentUser);
                var attendances = await _attendanceRepo.GetTeamAttendanceByDateAsync(teamUserIds, date);

                return attendances.Select(a => new TeamAttendanceViewModel
                {
                    UserId = a.UserID,
                    UserName = a.User.DisplayName,
                    Department = a.User.Department?.Name ?? "No Department",
                    Date = a.Date,
                    FirstCheckIn = a.FirstCheckIn,
                    LastCheckOut = a.LastCheckOut,
                    Duration = a.Duration,
                    Status = GetAttendanceStatus(a),
                    HRPosted = a.HRPosted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team attendance");
                return Enumerable.Empty<TeamAttendanceViewModel>();
            }
        }

        public async Task<IEnumerable<TeamAttendanceViewModel>> GetTeamAttendanceRangeAsync(ClaimsPrincipal currentUser, DateTime startDate, DateTime endDate)
        {
            try
            {
                var teamUserIds = await GetTeamUserIdsAsync(currentUser);
                var attendances = await _attendanceRepo.GetTeamAttendanceByDateRangeAsync(teamUserIds, startDate, endDate);

                return attendances.Select(a => new TeamAttendanceViewModel
                {
                    UserId = a.UserID,
                    UserName = a.User.DisplayName,
                    Department = a.User.Department?.Name ?? "No Department",
                    Date = a.Date,
                    FirstCheckIn = a.FirstCheckIn,
                    LastCheckOut = a.LastCheckOut,
                    Duration = a.Duration,
                    Status = GetAttendanceStatus(a),
                    HRPosted = a.HRPosted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team attendance range");
                return Enumerable.Empty<TeamAttendanceViewModel>();
            }
        }

        public async Task<AttendanceReportViewModel> GetAttendanceReportAsync(ClaimsPrincipal currentUser, DateTime startDate, DateTime endDate, int? userId = null)
        {
            try
            {
                IEnumerable<Attendance> attendances;

                if (userId.HasValue)
                {
                    attendances = await _attendanceRepo.GetUserAttendanceByDateRangeAsync(userId.Value, startDate, endDate);
                }
                else
                {
                    var teamUserIds = await GetTeamUserIdsAsync(currentUser);
                    attendances = await _attendanceRepo.GetTeamAttendanceByDateRangeAsync(teamUserIds, startDate, endDate);
                }

                var attendanceList = attendances.ToList();

                // Calculate summary
                var summary = new AttendanceSummaryViewModel
                {
                    TotalDays = (endDate.Date - startDate.Date).Days + 1,
                    PresentDays = attendanceList.Count(a => !string.IsNullOrEmpty(a.FirstCheckIn)),
                    AbsentDays = 0,
                    TotalMinutes = attendanceList.Sum(a => a.Duration),
                    AverageMinutes = attendanceList.Any() ? (int)attendanceList.Average(a => a.Duration) : 0
                };
                summary.AbsentDays = summary.TotalDays - summary.PresentDays;

                return new AttendanceReportViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Summary = summary,
                    Attendances = attendanceList.Select(a => new AttendanceViewModel
                    {
                        Id = a.ID,
                        UserId = a.UserID,
                        UserName = a.User?.DisplayName ?? "Unknown User",
                        Date = a.Date,
                        FirstCheckIn = a.FirstCheckIn,
                        LastCheckOut = a.LastCheckOut,
                        Duration = a.Duration,
                        HRPosted = a.HRPosted,
                        HRUserName = a.HRUser?.DisplayName,
                        HRPostedDate = a.HRPostedDate,
                        AttendanceStatus = a.Status,
                        MinutesLate = a.MinutesLate
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attendance report");
                return new AttendanceReportViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Summary = new AttendanceSummaryViewModel(),
                    Attendances = new List<AttendanceViewModel>()
                };
            }
        }

        public async Task<AttendanceReportViewModel> GetUserAttendanceReportAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var attendances = await _attendanceRepo.GetUserAttendanceByDateRangeAsync(userId, startDate, endDate);
                var attendanceList = attendances.ToList();

                // Get user details - ignore query filters to allow cross-branch access
                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .Include(u => u.Department)
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                // Calculate summary
                var summary = new AttendanceSummaryViewModel
                {
                    TotalDays = (endDate.Date - startDate.Date).Days + 1,
                    PresentDays = attendanceList.Count(a => !string.IsNullOrEmpty(a.FirstCheckIn)),
                    AbsentDays = 0,
                    TotalMinutes = attendanceList.Sum(a => a.Duration),
                    AverageMinutes = attendanceList.Any() ? (int)attendanceList.Average(a => a.Duration) : 0
                };
                summary.AbsentDays = summary.TotalDays - summary.PresentDays;

                return new AttendanceReportViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Summary = summary,
                    Attendances = attendanceList.Select(a => new AttendanceViewModel
                    {
                        Id = a.ID,
                        UserId = a.UserID,
                        UserName = user?.DisplayName ?? a.User.DisplayName,
                        Date = a.Date,
                        FirstCheckIn = a.FirstCheckIn,
                        LastCheckOut = a.LastCheckOut,
                        Duration = a.Duration,
                        HRPosted = a.HRPosted,
                        HRUserName = a.HRUser?.DisplayName,
                        HRPostedDate = a.HRPostedDate,
                        AttendanceStatus = a.Status,
                        MinutesLate = a.MinutesLate
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attendance report for user {UserId}", userId);
                return new AttendanceReportViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Summary = new AttendanceSummaryViewModel(),
                    Attendances = new List<AttendanceViewModel>()
                };
            }
        }

        public async Task<IEnumerable<AttendanceViewModel>> GetPendingHRPostsAsync(int branchId)
        {
            try
            {
                var attendances = await _attendanceRepo.GetPendingHRPostsAsync(branchId);

                return attendances.Select(a => new AttendanceViewModel
                {
                    Id = a.ID,
                    UserId = a.UserID,
                    UserName = a.User.DisplayName,
                    Date = a.Date,
                    FirstCheckIn = a.FirstCheckIn,
                    LastCheckOut = a.LastCheckOut,
                    Duration = a.Duration,
                    HRPosted = a.HRPosted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending HR posts for branch {BranchId}", branchId);
                return Enumerable.Empty<AttendanceViewModel>();
            }
        }

        public async Task<bool> PostToHRAsync(int attendanceId, int hrUserId)
        {
            try
            {
                var attendance = await _attendanceRepo.GetByIdAsync(attendanceId);

                if (attendance == null)
                {
                    return false;
                }

                attendance.HRPosted = true;
                attendance.HRUserID = hrUserId;
                attendance.HRPostedDate = DateTime.UtcNow;
                attendance.UpdatedAt = DateTime.UtcNow;

                _attendanceRepo.Update(attendance);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Attendance {AttendanceId} posted to HR by user {HRUserId}", attendanceId, hrUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting attendance {AttendanceId} to HR", attendanceId);
                return false;
            }
        }

        // Helper methods
        private async Task<List<int>> GetTeamUserIdsAsync(ClaimsPrincipal currentUser)
        {
            var userId = int.Parse(currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Load user with Branch to check if it's main branch
            var user = await _context.Users
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new List<int>();
            }

            // Admin role: Access ALL employees across all branches
            if (currentUser.IsInRole("Admin"))
            {
                return await _context.Users
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            // HR role: Access all employees if in main branch, otherwise only their branch
            if (currentUser.IsInRole("HR"))
            {
                if (user.Branch?.IsMainBranch == true)
                {
                    // HR in main branch: Access ALL employees
                    return await _context.Users
                        .Select(u => u.Id)
                        .ToListAsync();
                }
                else
                {
                    // HR in non-main branch: Access only employees in their branch
                    return await _context.Users
                        .Where(u => u.BranchID == user.BranchID)
                        .Select(u => u.Id)
                        .ToListAsync();
                }
            }

            // Manager role: Access subordinates only
            if (currentUser.IsInRole("Manager"))
            {
                return await _context.Users
                    .Where(u => u.ManagerID == userId || u.Id == userId)
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            // Regular employee: Access only their own data
            return new List<int> { userId };
        }

        private string GetAttendanceStatus(Attendance attendance)
        {
            if (string.IsNullOrEmpty(attendance.FirstCheckIn)) return "Absent";
            if (string.IsNullOrEmpty(attendance.LastCheckOut)) return "Checked In";
            return "Present";
        }

        public async Task<int> RecalculateAllDurationsAsync()
        {
            try
            {
                _logger.LogInformation("Starting recalculation of all attendance durations");

                var attendances = await _context.Attendances
                    .Include(a => a.Records)
                    .Where(a => !string.IsNullOrEmpty(a.FirstCheckIn) && !string.IsNullOrEmpty(a.LastCheckOut))
                    .ToListAsync();

                int updatedCount = 0;

                foreach (var attendance in attendances)
                {
                    var oldDuration = attendance.Duration;
                    var newDuration = CalculateDuration(attendance.Records.ToList());

                    if (oldDuration != newDuration)
                    {
                        attendance.Duration = newDuration;
                        _context.Entry(attendance).Property(a => a.Duration).IsModified = true;
                        updatedCount++;
                        _logger.LogInformation("Attendance ID {Id}: Updated duration from {Old} to {New} minutes",
                            attendance.ID, oldDuration, newDuration);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Recalculation complete. Updated {Count} attendance records", updatedCount);
                return updatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating durations");
                return 0;
            }
        }

        private int CalculateDuration(List<AttendanceRecord> records)
        {
            if (!records.Any())
            {
                _logger.LogWarning("CalculateDuration: No records provided");
                return 0;
            }

            int totalMinutes = 0;
            TimeSpan? lastCheckIn = null;

            var orderedRecords = records.OrderBy(r => r.Time).ToList();
            _logger.LogInformation("CalculateDuration: Processing {Count} records", orderedRecords.Count);

            foreach (var record in orderedRecords)
            {
                _logger.LogInformation("Record: Time={Time}, IsCheckIn={IsCheckIn}", record.Time, record.IsCheckIn);

                if (record.IsCheckIn)
                {
                    lastCheckIn = record.Time;
                    _logger.LogInformation("Check-in detected at {Time}", record.Time);
                }
                else if (lastCheckIn.HasValue)
                {
                    var duration = record.Time - lastCheckIn.Value;
                    var minutes = (int)duration.TotalMinutes;
                    totalMinutes += minutes;
                    _logger.LogInformation("Check-out detected at {Time}. Duration: {Minutes} minutes. Total so far: {Total}",
                        record.Time, minutes, totalMinutes);
                    lastCheckIn = null;
                }
                else
                {
                    _logger.LogWarning("Check-out at {Time} without matching check-in", record.Time);
                }
            }

            _logger.LogInformation("CalculateDuration: Final total minutes = {TotalMinutes}", totalMinutes);
            return totalMinutes;
        }

        public async Task<BranchAttendanceViewModel> GetBranchAttendanceAsync(ClaimsPrincipal currentUser, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = int.Parse(currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    throw new UnauthorizedAccessException("User not found");
                }

                var roles = currentUser.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                bool isAdmin = roles.Contains("Admin");
                bool isHROrManager = roles.Contains("HR") || roles.Contains("Manager");
                bool isInMainBranch = user.Branch?.IsMainBranch ?? false;

                // Determine access control
                bool canViewAllBranches = isAdmin || (isHROrManager && isInMainBranch);

                // Set date range - use branch's timezone to determine "today"
                var branchTimezone = user.Branch?.TimeZone ?? 0;
                var branchToday = _timezoneService.GetBranchToday(branchTimezone);
                var start = startDate ?? branchToday;
                var end = endDate ?? branchToday;
                bool isToday = start == branchToday && end == branchToday;

                _logger.LogInformation("GetBranchAttendance: User {UserId}, CanViewAll: {CanViewAll}, Start: {Start}, End: {End}",
                    userId, canViewAllBranches, start, end);

                // Get branches based on access control
                var branchesQuery = _context.Branches
                    .Include(b => b.Users)
                        .ThenInclude(u => u.Department)
                    .Where(b => b.IsActive);

                if (!canViewAllBranches)
                {
                    // Only show user's branch
                    branchesQuery = branchesQuery.Where(b => b.ID == user.BranchID);
                }

                var branches = await branchesQuery.ToListAsync();

                var branchAttendanceList = new List<BranchAttendanceData>();

                foreach (var branch in branches)
                {
                    var branchData = new BranchAttendanceData
                    {
                        BranchId = branch.ID,
                        BranchName = branch.Name,
                        IsMainBranch = branch.IsMainBranch,
                        Users = new List<UserAttendanceData>()
                    };

                    var activeUsers = branch.Users.Where(u => u.IsActive).ToList();
                    branchData.TotalUsers = activeUsers.Count;

                    // Get attendance records for all users in this branch for the date range
                    var userIds = activeUsers.Select(u => u.Id).ToList();
                    var attendances = await _context.Attendances
                        .Include(a => a.User)
                            .ThenInclude(u => u.Department)
                        .Where(a => userIds.Contains(a.UserID) && a.Date >= start && a.Date <= end)
                        .ToListAsync();

                    // For today view, also include users with no attendance (absent)
                    if (isToday)
                    {
                        foreach (var branchUser in activeUsers)
                        {
                            var attendance = attendances.FirstOrDefault(a => a.UserID == branchUser.Id && a.Date == branchToday);

                            var userData = new UserAttendanceData
                            {
                                UserId = branchUser.Id,
                                UserName = branchUser.DisplayName,
                                Email = branchUser.Email ?? "",
                                Department = branchUser.Department?.Name ?? "N/A",
                                FirstCheckIn = attendance?.FirstCheckIn,
                                LastCheckOut = attendance?.LastCheckOut,
                                Duration = attendance?.Duration ?? 0,
                                Status = GetAttendanceStatus(attendance?.FirstCheckIn, attendance?.LastCheckOut),
                                HRPosted = attendance?.HRPosted ?? false
                            };

                            branchData.Users.Add(userData);

                            // Update counters
                            if (userData.Status == "Present")
                                branchData.PresentUsers++;
                            else if (userData.Status == "Checked In")
                                branchData.CheckedInUsers++;
                            else
                                branchData.AbsentUsers++;
                        }
                    }
                    else
                    {
                        // Date range view - only show users who have attendance records
                        var groupedByUser = attendances.GroupBy(a => a.UserID);

                        foreach (var userGroup in groupedByUser)
                        {
                            var firstAttendance = userGroup.First();
                            var totalDuration = userGroup.Sum(a => a.Duration);
                            var presentDays = userGroup.Count(a => !string.IsNullOrEmpty(a.FirstCheckIn));

                            var userData = new UserAttendanceData
                            {
                                UserId = firstAttendance.UserID,
                                UserName = firstAttendance.User.DisplayName,
                                Email = firstAttendance.User.Email ?? "",
                                Department = firstAttendance.User.Department?.Name ?? "N/A",
                                FirstCheckIn = $"{presentDays} days",
                                LastCheckOut = "-",
                                Duration = totalDuration,
                                Status = "Present",
                                HRPosted = false
                            };

                            branchData.Users.Add(userData);
                            branchData.PresentUsers++;
                        }

                        branchData.AbsentUsers = branchData.TotalUsers - branchData.PresentUsers;
                    }

                    // Sort users by name
                    branchData.Users = branchData.Users.OrderBy(u => u.UserName).ToList();
                    branchAttendanceList.Add(branchData);
                }

                return new BranchAttendanceViewModel
                {
                    Branches = branchAttendanceList,
                    StartDate = start,
                    EndDate = end,
                    IsToday = isToday,
                    CanViewAllBranches = canViewAllBranches,
                    UserBranchId = user.BranchID
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branch attendance");
                throw;
            }
        }

        private string GetAttendanceStatus(string? firstCheckIn, string? lastCheckOut)
        {
            if (string.IsNullOrEmpty(firstCheckIn)) return "Absent";
            if (string.IsNullOrEmpty(lastCheckOut)) return "Checked In";
            return "Present";
        }

        /// <summary>
        /// Calculates attendance status (On Time, Late, Very Late, Early) based on check-in time and timetable
        /// </summary>
        private (AttendanceStatus status, int? minutesLate) CalculateAttendanceStatus(
            TimeSpan checkInTime,
            Timetable? timetable)
        {
            // If no timetable, mark as On Time by default
            if (timetable == null)
            {
                return (AttendanceStatus.OnTime, null);
            }

            // Parse timetable times (format: "HH:mm" e.g., "09:00")
            TimeSpan? startMin = ParseTimeString(timetable.WorkingDayStartingHourMinimum);
            TimeSpan? startMax = ParseTimeString(timetable.WorkingDayStartingHourMaximum);

            // If no start times defined, mark as On Time
            if (!startMin.HasValue && !startMax.HasValue)
            {
                return (AttendanceStatus.OnTime, null);
            }

            // Calculate status based on check-in time
            // Early: Before minimum start time
            if (startMin.HasValue && checkInTime < startMin.Value)
            {
                var minutesEarly = (int)(startMin.Value - checkInTime).TotalMinutes;
                return (AttendanceStatus.Early, -minutesEarly); // Negative means early
            }

            // On Time: Between minimum and maximum start time
            if (startMin.HasValue && startMax.HasValue)
            {
                if (checkInTime >= startMin.Value && checkInTime <= startMax.Value)
                {
                    return (AttendanceStatus.OnTime, 0);
                }
            }
            else if (startMin.HasValue && !startMax.HasValue)
            {
                // If only minimum is set, on time means after minimum
                return (AttendanceStatus.OnTime, 0);
            }

            // Late or Very Late: After maximum start time
            if (startMax.HasValue && checkInTime > startMax.Value)
            {
                var minutesLate = (int)(checkInTime - startMax.Value).TotalMinutes;

                // Very Late: More than 15 minutes late
                if (minutesLate > 15)
                {
                    return (AttendanceStatus.VeryLate, minutesLate);
                }

                // Late: 1-15 minutes late
                return (AttendanceStatus.Late, minutesLate);
            }

            // Default to On Time
            return (AttendanceStatus.OnTime, 0);
        }

        /// <summary>
        /// Parses time string in format "HH:mm" to TimeSpan
        /// </summary>
        private TimeSpan? ParseTimeString(string? timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
            {
                return null;
            }

            if (TimeSpan.TryParse(timeString, out var result))
            {
                return result;
            }

            _logger.LogWarning("Failed to parse time string: {TimeString}", timeString);
            return null;
        }

        #region Manual Attendance Management (Admin/HR)

        public async Task<IEnumerable<AttendanceViewModel>> GetUsersForManualAttendanceAsync(ClaimsPrincipal currentUser, int? branchId = null)
        {
            var teamUserIds = await GetTeamUserIdsAsync(currentUser);

            var query = _context.Users
                .Include(u => u.Branch)
                .Include(u => u.Department)
                .Where(u => teamUserIds.Contains(u.Id) && u.IsActive);

            if (branchId.HasValue)
            {
                query = query.Where(u => u.BranchID == branchId.Value);
            }

            var users = await query.OrderBy(u => u.DisplayName).ToListAsync();

            return users.Select(u => new AttendanceViewModel
            {
                UserId = u.Id,
                UserName = u.DisplayName,
                BranchName = u.Branch?.Name,
                DepartmentName = u.Department?.Name
            });
        }

        public async Task<AttendanceViewModel?> GetAttendanceByIdAsync(int attendanceId)
        {
            var attendance = await _context.Attendances
                .Include(a => a.User)
                    .ThenInclude(u => u.Branch)
                .Include(a => a.User)
                    .ThenInclude(u => u.Department)
                .Include(a => a.Records)
                .FirstOrDefaultAsync(a => a.ID == attendanceId);

            if (attendance == null) return null;

            return new AttendanceViewModel
            {
                Id = attendance.ID,
                UserId = attendance.UserID,
                UserName = attendance.User.DisplayName,
                BranchName = attendance.User.Branch?.Name,
                DepartmentName = attendance.User.Department?.Name,
                Date = attendance.Date,
                FirstCheckIn = attendance.FirstCheckIn,
                LastCheckOut = attendance.LastCheckOut,
                Duration = attendance.Duration,
                AttendanceStatus = attendance.Status,
                MinutesLate = attendance.MinutesLate,
                Records = attendance.Records.Select(r => new AttendanceRecordViewModel
                {
                    Id = r.ID,
                    Time = r.Time,
                    IsCheckIn = r.IsCheckIn,
                    IsAutomated = r.IsAutomated
                }).ToList()
            };
        }

        public async Task<(bool Success, string Message)> CreateManualAttendanceAsync(
            int userId, DateTime date, string? checkInTime, string? checkOutTime, int createdByUserId)
        {
            try
            {
                // Check if attendance already exists for this user and date
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.UserID == userId && a.Date.Date == date.Date);

                if (existingAttendance != null)
                {
                    return (false, "Attendance record already exists for this user on this date. Please edit the existing record instead.");
                }

                // Get user for status calculation
                var user = await _context.Users
                    .Include(u => u.Timetable)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return (false, "User not found.");
                }

                // Parse times
                TimeSpan? checkIn = null;
                TimeSpan? checkOut = null;

                if (!string.IsNullOrEmpty(checkInTime) && TimeSpan.TryParse(checkInTime, out var cin))
                {
                    checkIn = cin;
                }

                if (!string.IsNullOrEmpty(checkOutTime) && TimeSpan.TryParse(checkOutTime, out var cout))
                {
                    checkOut = cout;
                }

                // Calculate status if check-in time provided
                var (status, minutesLate) = checkIn.HasValue
                    ? CalculateAttendanceStatus(checkIn.Value, user.Timetable)
                    : (AttendanceStatus.Absent, (int?)null);

                // Create attendance record
                var attendance = new Attendance
                {
                    UserID = userId,
                    Date = date.Date,
                    FirstCheckIn = checkIn?.ToString(@"hh\:mm"),
                    LastCheckOut = checkOut?.ToString(@"hh\:mm"),
                    Status = status,
                    MinutesLate = minutesLate,
                    CreatedAt = DateTime.UtcNow
                };

                // Calculate duration if both times provided
                if (checkIn.HasValue && checkOut.HasValue)
                {
                    attendance.Duration = (int)(checkOut.Value - checkIn.Value).TotalMinutes;
                }

                await _context.Attendances.AddAsync(attendance);
                await _context.SaveChangesAsync();

                // Add records
                if (checkIn.HasValue)
                {
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        AttendanceID = attendance.ID,
                        Time = checkIn.Value,
                        IsCheckIn = true,
                        IsAutomated = false
                    });
                }

                if (checkOut.HasValue)
                {
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        AttendanceID = attendance.ID,
                        Time = checkOut.Value,
                        IsCheckIn = false,
                        IsAutomated = false
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Manual attendance created for user {UserId} on {Date} by user {CreatedBy}",
                    userId, date.ToString("yyyy-MM-dd"), createdByUserId);

                return (true, "Attendance record created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manual attendance for user {UserId}", userId);
                return (false, "An error occurred while creating the attendance record.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateManualAttendanceAsync(
            int attendanceId, string? checkInTime, string? checkOutTime, int updatedByUserId)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.User)
                        .ThenInclude(u => u.Timetable)
                    .Include(a => a.Records)
                    .FirstOrDefaultAsync(a => a.ID == attendanceId);

                if (attendance == null)
                {
                    return (false, "Attendance record not found.");
                }

                // Parse times
                TimeSpan? checkIn = null;
                TimeSpan? checkOut = null;

                if (!string.IsNullOrEmpty(checkInTime) && TimeSpan.TryParse(checkInTime, out var cin))
                {
                    checkIn = cin;
                }

                if (!string.IsNullOrEmpty(checkOutTime) && TimeSpan.TryParse(checkOutTime, out var cout))
                {
                    checkOut = cout;
                }

                // Update attendance
                attendance.FirstCheckIn = checkIn?.ToString(@"hh\:mm");
                attendance.LastCheckOut = checkOut?.ToString(@"hh\:mm");
                attendance.UpdatedAt = DateTime.UtcNow;

                // Recalculate status
                if (checkIn.HasValue)
                {
                    var (status, minutesLate) = CalculateAttendanceStatus(checkIn.Value, attendance.User?.Timetable);
                    attendance.Status = status;
                    attendance.MinutesLate = minutesLate;
                }
                else
                {
                    attendance.Status = AttendanceStatus.Absent;
                    attendance.MinutesLate = null;
                }

                // Recalculate duration
                if (checkIn.HasValue && checkOut.HasValue)
                {
                    attendance.Duration = (int)(checkOut.Value - checkIn.Value).TotalMinutes;
                }
                else
                {
                    attendance.Duration = 0;
                }

                // Remove old records and add new ones
                _context.AttendanceRecords.RemoveRange(attendance.Records);

                if (checkIn.HasValue)
                {
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        AttendanceID = attendance.ID,
                        Time = checkIn.Value,
                        IsCheckIn = true,
                        IsAutomated = false
                    });
                }

                if (checkOut.HasValue)
                {
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        AttendanceID = attendance.ID,
                        Time = checkOut.Value,
                        IsCheckIn = false,
                        IsAutomated = false
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Manual attendance updated for ID {AttendanceId} by user {UpdatedBy}",
                    attendanceId, updatedByUserId);

                return (true, "Attendance record updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manual attendance {AttendanceId}", attendanceId);
                return (false, "An error occurred while updating the attendance record.");
            }
        }

        public async Task<(bool Success, string Message)> ManualCheckInAsync(
            int userId, DateTime date, string checkInTime, int performedByUserId)
        {
            try
            {
                // Check if attendance exists
                var attendance = await _context.Attendances
                    .Include(a => a.User)
                        .ThenInclude(u => u.Timetable)
                    .FirstOrDefaultAsync(a => a.UserID == userId && a.Date.Date == date.Date);

                if (!TimeSpan.TryParse(checkInTime, out var checkIn))
                {
                    return (false, "Invalid check-in time format.");
                }

                if (attendance == null)
                {
                    // Create new attendance
                    return await CreateManualAttendanceAsync(userId, date, checkInTime, null, performedByUserId);
                }

                // Update existing attendance
                var (status, minutesLate) = CalculateAttendanceStatus(checkIn, attendance.User?.Timetable);

                attendance.FirstCheckIn = checkIn.ToString(@"hh\:mm");
                attendance.Status = status;
                attendance.MinutesLate = minutesLate;
                attendance.UpdatedAt = DateTime.UtcNow;

                // Add check-in record
                _context.AttendanceRecords.Add(new AttendanceRecord
                {
                    AttendanceID = attendance.ID,
                    Time = checkIn,
                    IsCheckIn = true,
                    IsAutomated = false
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Manual check-in for user {UserId} at {Time} by {PerformedBy}",
                    userId, checkInTime, performedByUserId);

                return (true, "Check-in recorded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing manual check-in for user {UserId}", userId);
                return (false, "An error occurred while recording check-in.");
            }
        }

        public async Task<(bool Success, string Message)> ManualCheckOutAsync(
            int attendanceId, string checkOutTime, int performedByUserId)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.Records)
                    .FirstOrDefaultAsync(a => a.ID == attendanceId);

                if (attendance == null)
                {
                    return (false, "Attendance record not found.");
                }

                if (string.IsNullOrEmpty(attendance.FirstCheckIn))
                {
                    return (false, "Cannot check out without a check-in. Please add check-in first.");
                }

                if (!TimeSpan.TryParse(checkOutTime, out var checkOut))
                {
                    return (false, "Invalid check-out time format.");
                }

                attendance.LastCheckOut = checkOut.ToString(@"hh\:mm");
                attendance.UpdatedAt = DateTime.UtcNow;

                // Add check-out record
                _context.AttendanceRecords.Add(new AttendanceRecord
                {
                    AttendanceID = attendance.ID,
                    Time = checkOut,
                    IsCheckIn = false,
                    IsAutomated = false
                });

                await _context.SaveChangesAsync();

                // Recalculate duration
                var updatedAttendance = await _context.Attendances
                    .Include(a => a.Records)
                    .FirstOrDefaultAsync(a => a.ID == attendanceId);

                if (updatedAttendance != null)
                {
                    updatedAttendance.Duration = CalculateDuration(updatedAttendance.Records.ToList());
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Manual check-out for attendance {AttendanceId} at {Time} by {PerformedBy}",
                    attendanceId, checkOutTime, performedByUserId);

                return (true, "Check-out recorded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing manual check-out for attendance {AttendanceId}", attendanceId);
                return (false, "An error occurred while recording check-out.");
            }
        }

        #endregion
    }
}
