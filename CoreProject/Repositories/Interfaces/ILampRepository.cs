using CoreProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreProject.Repositories.Interfaces
{
    public interface ILampRepository : IRepository<Lamp>
    {
        /// <summary>
        /// Get all lamps with branch and timetable information
        /// </summary>
        Task<List<Lamp>> GetAllWithDetailsAsync();

        /// <summary>
        /// Get lamp by ID with branch and timetable information
        /// </summary>
        Task<Lamp?> GetByIdWithDetailsAsync(int id);

        /// <summary>
        /// Get lamp by DeviceID
        /// </summary>
        Task<Lamp?> GetByDeviceIdAsync(string deviceId);

        /// <summary>
        /// Get all lamps for a specific branch
        /// </summary>
        Task<List<Lamp>> GetByBranchIdAsync(int branchId);

        /// <summary>
        /// Check if DeviceID already exists
        /// </summary>
        Task<bool> DeviceIdExistsAsync(string deviceId, int? excludeLampId = null);
    }
}
