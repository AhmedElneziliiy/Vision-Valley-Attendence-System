using CoreProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface ILampService
    {
        /// <summary>
        /// Get all lamps with details
        /// </summary>
        Task<List<Lamp>> GetAllLampsAsync();

        /// <summary>
        /// Get lamp by ID
        /// </summary>
        Task<Lamp?> GetLampByIdAsync(int id);

        /// <summary>
        /// Get lamp by DeviceID
        /// </summary>
        Task<Lamp?> GetLampByDeviceIdAsync(string deviceId);

        /// <summary>
        /// Create a new lamp
        /// </summary>
        Task<(bool Success, string Message)> CreateLampAsync(Lamp lamp);

        /// <summary>
        /// Update an existing lamp
        /// </summary>
        Task<(bool Success, string Message)> UpdateLampAsync(Lamp lamp);

        /// <summary>
        /// Delete a lamp (soft delete)
        /// </summary>
        Task<(bool Success, string Message)> DeleteLampAsync(int id);

        /// <summary>
        /// Toggle manual override for a lamp
        /// </summary>
        Task<(bool Success, string Message)> ToggleManualOverrideAsync(int id, bool enable, int? state = null);

        /// <summary>
        /// Send immediate state change command to lamp
        /// </summary>
        Task<(bool Success, string Message)> SendStateChangeAsync(int id, bool turnOn);

        /// <summary>
        /// Get all branches for dropdown
        /// </summary>
        Task<List<Branch>> GetBranchesAsync();

        /// <summary>
        /// Get all timetables for dropdown
        /// </summary>
        Task<List<Timetable>> GetTimetablesAsync();

        /// <summary>
        /// Check if DeviceID is unique
        /// </summary>
        Task<bool> IsDeviceIdUniqueAsync(string deviceId, int? excludeLampId = null);
    }
}
