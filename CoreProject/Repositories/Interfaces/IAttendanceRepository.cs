using CoreProject.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreProject.Repositories.Interfaces
{
    public interface IAttendanceRepository : IRepository<Attendance>
    {
        Task<Attendance?> GetTodayAttendanceAsync(int userId, DateTime date);
        Task<IEnumerable<Attendance>> GetUserAttendanceByDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<Attendance>> GetTeamAttendanceByDateAsync(IEnumerable<int> userIds, DateTime date);
        Task<IEnumerable<Attendance>> GetTeamAttendanceByDateRangeAsync(IEnumerable<int> userIds, DateTime startDate, DateTime endDate);
        Task<Attendance?> GetAttendanceWithRecordsAsync(int attendanceId);
        Task<IEnumerable<Attendance>> GetPendingHRPostsAsync(int branchId);
        Task<AttendanceRecord?> GetLastCheckRecordAsync(int attendanceId);
    }
}
