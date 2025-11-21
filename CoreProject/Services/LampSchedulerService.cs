using CoreProject.Context;
using CoreProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    /// <summary>
    /// Background service that automatically controls lamps based on timetable schedules
    /// Checks every 60 seconds and sends state change commands to lamps via WebSocket
    /// </summary>
    public class LampSchedulerService : BackgroundService
    {
        private readonly ILogger<LampSchedulerService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly LampWebSocketHandler _webSocketHandler;

        // Check interval: every 60 seconds
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

        public LampSchedulerService(
            ILogger<LampSchedulerService> logger,
            IServiceScopeFactory serviceScopeFactory,
            LampWebSocketHandler webSocketHandler)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _webSocketHandler = webSocketHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LampSchedulerService started");

            // Wait 10 seconds before first check to allow app to fully start
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndUpdateLampsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in LampSchedulerService");
                }

                // Wait for next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("LampSchedulerService stopped");
        }

        /// <summary>
        /// Check all lamps and update their states based on timetable schedules
        /// </summary>
        private async Task CheckAndUpdateLampsAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Get all active lamps with their branch and timetable info
            var lamps = await context.Lamps
                .IgnoreQueryFilters()
                .Where(l => l.IsActive)
                .Include(l => l.Branch)
                .Include(l => l.Timetable)
                .ToListAsync();

            if (lamps.Count == 0)
            {
                _logger.LogDebug("No active lamps found");
                return;
            }

            _logger.LogInformation("Checking {Count} active lamps", lamps.Count);

            foreach (var lamp in lamps)
            {
                try
                {
                    await CheckLampAsync(lamp, context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking lamp {DeviceID}", lamp.DeviceID);
                }
            }
        }

        /// <summary>
        /// Check a single lamp and update its state if needed
        /// </summary>
        private async Task CheckLampAsync(Lamp lamp, ApplicationDbContext context)
        {
            // Calculate branch local time using timezone offset
            var utcNow = DateTime.UtcNow;
            var timezoneOffsetHours = lamp.Branch.TimeZone;
            var branchLocalTime = utcNow.AddHours(timezoneOffsetHours);

            _logger.LogDebug("Checking lamp {DeviceID} for branch {BranchName} at local time {LocalTime}",
                lamp.DeviceID, lamp.Branch.Name, branchLocalTime.ToString("HH:mm:ss"));

            // Determine if lamp should be ON based on timetable (with grace periods)
            bool shouldBeOn = lamp.ShouldBeOn(branchLocalTime, graceHoursBefore: 1, graceHoursAfter: 1);

            _logger.LogDebug("Lamp {DeviceID}: Current state={CurrentState}, Should be={ShouldBe}",
                lamp.DeviceID,
                lamp.CurrentState == 1 ? "ON" : "OFF",
                shouldBeOn ? "ON" : "OFF");

            // Convert boolean to integer (1 = ON, 0 = OFF)
            int desiredState = shouldBeOn ? 1 : 0;

            // Check if state change is needed
            if (lamp.CurrentState != desiredState)
            {
                _logger.LogInformation("Lamp {DeviceID} state change needed: {OldState} -> {NewState}",
                    lamp.DeviceID,
                    lamp.CurrentState == 1 ? "ON" : "OFF",
                    desiredState == 1 ? "ON" : "OFF");

                // Send state change command via WebSocket
                bool success = await _webSocketHandler.SendStateChangeAsync(lamp.DeviceID, shouldBeOn);

                if (success)
                {
                    _logger.LogInformation("State change command sent successfully to lamp {DeviceID}", lamp.DeviceID);

                    // Update lamp state in database immediately (optimistic update)
                    // The actual state will be confirmed when we receive ACK from ESP32
                    lamp.CurrentState = desiredState;
                    lamp.LastStateChange = DateTime.UtcNow;
                    lamp.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning("Failed to send state change command to lamp {DeviceID} - device may be disconnected", lamp.DeviceID);
                }
            }
            else
            {
                _logger.LogDebug("Lamp {DeviceID} is already in correct state: {State}",
                    lamp.DeviceID,
                    lamp.CurrentState == 1 ? "ON" : "OFF");
            }
        }
    }
}
