using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using CoreProject.Services.IService;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    public class LampService : ILampService
    {
        private readonly ILampRepository _lampRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<Timetable> _timetableRepository;
        private readonly ApplicationDbContext _context;
        private readonly LampWebSocketHandler _webSocketHandler;

        public LampService(
            ILampRepository lampRepository,
            IRepository<Branch> branchRepository,
            IRepository<Timetable> timetableRepository,
            ApplicationDbContext context,
            LampWebSocketHandler webSocketHandler)
        {
            _lampRepository = lampRepository;
            _branchRepository = branchRepository;
            _timetableRepository = timetableRepository;
            _context = context;
            _webSocketHandler = webSocketHandler;
        }

        public async Task<List<Lamp>> GetAllLampsAsync()
        {
            return await _lampRepository.GetAllWithDetailsAsync();
        }

        public async Task<Lamp?> GetLampByIdAsync(int id)
        {
            return await _lampRepository.GetByIdWithDetailsAsync(id);
        }

        public async Task<Lamp?> GetLampByDeviceIdAsync(string deviceId)
        {
            return await _lampRepository.GetByDeviceIdAsync(deviceId);
        }

        public async Task<(bool Success, string Message)> CreateLampAsync(Lamp lamp)
        {
            try
            {
                // Validate DeviceID uniqueness
                if (await _lampRepository.DeviceIdExistsAsync(lamp.DeviceID))
                {
                    return (false, $"Device ID '{lamp.DeviceID}' already exists. Please use a unique Device ID.");
                }

                // Validate Branch exists
                var branch = await _branchRepository.GetByIdAsync(lamp.BranchID);
                if (branch == null)
                {
                    return (false, "Selected branch does not exist.");
                }

                // Validate Timetable exists
                var timetable = await _timetableRepository.GetByIdAsync(lamp.TimetableID);
                if (timetable == null)
                {
                    return (false, "Selected timetable does not exist.");
                }

                // Set default values
                lamp.CreatedAt = DateTime.UtcNow;
                lamp.IsActive = true;
                lamp.CurrentState = 0; // Default OFF
                lamp.IsConnected = false;
                lamp.ManualOverride = false;

                await _lampRepository.AddAsync(lamp);
                await _context.SaveChangesAsync();

                return (true, "Lamp created successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating lamp: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateLampAsync(Lamp lamp)
        {
            try
            {
                // Validate lamp exists
                var existingLamp = await _lampRepository.GetByIdAsync(lamp.ID);
                if (existingLamp == null)
                {
                    return (false, "Lamp not found.");
                }

                // Validate DeviceID uniqueness (excluding current lamp)
                if (await _lampRepository.DeviceIdExistsAsync(lamp.DeviceID, lamp.ID))
                {
                    return (false, $"Device ID '{lamp.DeviceID}' already exists. Please use a unique Device ID.");
                }

                // Validate Branch exists
                var branch = await _branchRepository.GetByIdAsync(lamp.BranchID);
                if (branch == null)
                {
                    return (false, "Selected branch does not exist.");
                }

                // Validate Timetable exists
                var timetable = await _timetableRepository.GetByIdAsync(lamp.TimetableID);
                if (timetable == null)
                {
                    return (false, "Selected timetable does not exist.");
                }

                // Update fields
                existingLamp.DeviceID = lamp.DeviceID;
                existingLamp.Name = lamp.Name;
                existingLamp.Description = lamp.Description;
                existingLamp.BranchID = lamp.BranchID;
                existingLamp.TimetableID = lamp.TimetableID;
                existingLamp.UpdatedAt = DateTime.UtcNow;

                _lampRepository.Update(existingLamp);
                await _context.SaveChangesAsync();

                return (true, "Lamp updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating lamp: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteLampAsync(int id)
        {
            try
            {
                var lamp = await _lampRepository.GetByIdAsync(id);
                if (lamp == null)
                {
                    return (false, "Lamp not found.");
                }

                // Soft delete
                lamp.IsActive = false;
                lamp.UpdatedAt = DateTime.UtcNow;

                _lampRepository.Update(lamp);
                await _context.SaveChangesAsync();

                return (true, "Lamp deleted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting lamp: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ToggleManualOverrideAsync(int id, bool enable, int? state = null)
        {
            try
            {
                var lamp = await _lampRepository.GetByIdWithDetailsAsync(id);
                if (lamp == null)
                {
                    return (false, "Lamp not found.");
                }

                lamp.ManualOverride = enable;
                lamp.ManualOverrideState = state;
                lamp.UpdatedAt = DateTime.UtcNow;

                _lampRepository.Update(lamp);
                await _context.SaveChangesAsync();

                // If enabling manual override with a state, send command immediately
                if (enable && state.HasValue)
                {
                    bool turnOn = state.Value == 1;
                    bool sent = await _webSocketHandler.SendStateChangeAsync(lamp.DeviceID, turnOn);

                    if (!sent)
                    {
                        return (true, "Manual override enabled, but lamp is not connected via WebSocket.");
                    }
                }

                return (true, enable ? "Manual override enabled." : "Manual override disabled.");
            }
            catch (Exception ex)
            {
                return (false, $"Error toggling manual override: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> SendStateChangeAsync(int id, bool turnOn)
        {
            try
            {
                var lamp = await _lampRepository.GetByIdWithDetailsAsync(id);
                if (lamp == null)
                {
                    return (false, "Lamp not found.");
                }

                if (!lamp.IsConnected)
                {
                    return (false, "Lamp is not connected via WebSocket. Please ensure the ESP32 device is online.");
                }

                bool sent = await _webSocketHandler.SendStateChangeAsync(lamp.DeviceID, turnOn);

                if (sent)
                {
                    // Update state optimistically
                    lamp.CurrentState = turnOn ? 1 : 0;
                    lamp.LastStateChange = DateTime.UtcNow;
                    lamp.UpdatedAt = DateTime.UtcNow;

                    _lampRepository.Update(lamp);
                    await _context.SaveChangesAsync();

                    return (true, $"Command sent: Lamp turned {(turnOn ? "ON" : "OFF")}");
                }
                else
                {
                    return (false, "Failed to send command to lamp. Connection may have been lost.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error sending state change: {ex.Message}");
            }
        }

        public async Task<List<Branch>> GetBranchesAsync()
        {
            return (await _branchRepository.GetAllAsync())
                .OrderBy(b => b.Name)
                .ToList();
        }

        public async Task<List<Timetable>> GetTimetablesAsync()
        {
            return (await _timetableRepository.GetAllAsync())
                .OrderBy(t => t.Name)
                .ToList();
        }

        public async Task<bool> IsDeviceIdUniqueAsync(string deviceId, int? excludeLampId = null)
        {
            return !await _lampRepository.DeviceIdExistsAsync(deviceId, excludeLampId);
        }
    }
}
