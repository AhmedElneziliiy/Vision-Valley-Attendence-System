using CoreProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreProject.Repositories.Interfaces
{
    public interface ITimetableRepository : IRepository<Timetable>
    {
        Task<IEnumerable<Timetable>> GetTimetablesWithDetailsAsync();
        Task<Timetable?> GetTimetableWithDetailsAsync(int timetableId);
        Task<IEnumerable<Timetable>> GetTimetablesByBranchAsync(int branchId);
        Task<int> GetUserCountByTimetableAsync(int timetableId);
        Task<IEnumerable<TimetableConfiguration>> GetConfigurationsByTimetableAsync(int timetableId);
    }
}
