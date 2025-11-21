using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Repositories
{
    public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
    {
        public AttendanceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Attendance?> GetTodayAttendanceAsync(int userId, DateTime date)
        {
            return await _context.Attendances
                .Include(a => a.Records.OrderBy(r => r.Time))
                .ThenInclude(r => r.Reason)
                .FirstOrDefaultAsync(a => a.UserID == userId && a.Date.Date == date.Date);
        }

        public async Task<IEnumerable<Attendance>> GetUserAttendanceByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.Attendances
                .Include(a => a.Records.OrderBy(r => r.Time))
                .ThenInclude(r => r.Reason)
                .Where(a => a.UserID == userId && a.Date.Date >= startDate.Date && a.Date.Date <= endDate.Date)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Attendance>> GetTeamAttendanceByDateAsync(IEnumerable<int> userIds, DateTime date)
        {
            return await _context.Attendances
                .Include(a => a.User)
                .ThenInclude(u => u.Department)
                .Include(a => a.Records.OrderBy(r => r.Time))
                .Where(a => userIds.Contains(a.UserID) && a.Date.Date == date.Date)
                .OrderBy(a => a.User.DisplayName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Attendance>> GetTeamAttendanceByDateRangeAsync(IEnumerable<int> userIds, DateTime startDate, DateTime endDate)
        {
            return await _context.Attendances
                .Include(a => a.User)
                .ThenInclude(u => u.Department)
                .Include(a => a.Records.OrderBy(r => r.Time))
                .Where(a => userIds.Contains(a.UserID) && a.Date.Date >= startDate.Date && a.Date.Date <= endDate.Date)
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.User.DisplayName)
                .ToListAsync();
        }

        public async Task<Attendance?> GetAttendanceWithRecordsAsync(int attendanceId)
        {
            return await _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Records.OrderBy(r => r.Time))
                .ThenInclude(r => r.Reason)
                .FirstOrDefaultAsync(a => a.ID == attendanceId);
        }

        public async Task<IEnumerable<Attendance>> GetPendingHRPostsAsync(int branchId)
        {
            return await _context.Attendances
                .Include(a => a.User)
                .ThenInclude(u => u.Department)
                .Where(a => !a.HRPosted && a.User.BranchID == branchId)
                .OrderBy(a => a.Date)
                .ThenBy(a => a.User.DisplayName)
                .ToListAsync();
        }

        public async Task<AttendanceRecord?> GetLastCheckRecordAsync(int attendanceId)
        {
            return await _context.AttendanceRecords
                .Where(r => r.AttendanceID == attendanceId)
                .OrderByDescending(r => r.Time)
                .FirstOrDefaultAsync();
        }
    }
}
