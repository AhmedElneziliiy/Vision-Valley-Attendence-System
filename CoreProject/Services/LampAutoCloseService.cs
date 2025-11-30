using CoreProject.Context;
using CoreProject.Services.IService;
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
    /// Background service that checks for approved lamp access requests every 60 seconds
    /// and automatically closes lamps after the 1-hour approval period expires
    /// </summary>
    public class LampAutoCloseService : BackgroundService
    {
        private readonly ILogger<LampAutoCloseService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

        public LampAutoCloseService(
            ILogger<LampAutoCloseService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LampAutoCloseService started");

            // Wait 10 seconds before first check
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAutoCloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in LampAutoCloseService");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("LampAutoCloseService stopped");
        }

        private async Task CheckAutoCloseAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var lampWebSocketHandler = scope.ServiceProvider.GetRequiredService<LampWebSocketHandler>();

            var now = DateTime.UtcNow;

            var requestsToClose = await context.LampAccessRequests
                .Include(r => r.Lamp)
                .Where(r => r.Status == "Approved"
                         && !r.IsAutoClosed
                         && r.ApprovedUntil <= now)
                .ToListAsync();

            if (requestsToClose.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Auto-closing {Count} lamps (1-hour approval period expired)", requestsToClose.Count);

            foreach (var request in requestsToClose)
            {
                // Close lamp via WebSocket
                bool closed = await lampWebSocketHandler.SendStateChangeAsync(request.Lamp.DeviceID, false);

                if (!closed)
                {
                    _logger.LogWarning("Failed to close lamp {DeviceID} - device may be offline", request.Lamp.DeviceID);
                }
                else
                {
                    _logger.LogInformation("Lamp {DeviceID} auto-closed for request {RequestId}", request.Lamp.DeviceID, request.ID);
                }

                request.IsAutoClosed = true;
                request.AutoClosedAt = DateTime.UtcNow;
                request.UpdatedAt = DateTime.UtcNow;

                // Notify employee
                var employeeNotification = new
                {
                    type = "LampAccessAutoClose",
                    requestId = request.ID,
                    lampName = request.Lamp.Name,
                    message = "Lamp auto-closed after 1 hour."
                };

                await notificationService.SendWebSocketNotificationAsync(request.UserID, employeeNotification);
            }

            await context.SaveChangesAsync();

            _logger.LogInformation("Completed auto-closing {Count} lamps", requestsToClose.Count);
        }
    }
}
