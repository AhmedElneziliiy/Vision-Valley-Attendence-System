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
    /// Background service that checks for timed-out lamp access requests every 60 seconds
    /// and automatically rejects requests that have exceeded the 5-minute timeout
    /// </summary>
    public class RequestTimeoutService : BackgroundService
    {
        private readonly ILogger<RequestTimeoutService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

        public RequestTimeoutService(
            ILogger<RequestTimeoutService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RequestTimeoutService started");

            // Wait 10 seconds before first check
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckTimeoutsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in RequestTimeoutService");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("RequestTimeoutService stopped");
        }

        private async Task CheckTimeoutsAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTime.UtcNow;

            var expiredRequests = await context.LampAccessRequests
                .Include(r => r.Lamp)
                .Where(r => r.Status == "Pending" && r.TimeoutAt <= now)
                .ToListAsync();

            if (expiredRequests.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Processing {Count} expired lamp access requests", expiredRequests.Count);

            foreach (var request in expiredRequests)
            {
                request.Status = "Timeout";
                request.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Request {RequestId} timed out (no manager response within 5 minutes)", request.ID);

                // Notify employee
                var employeeNotification = new
                {
                    type = "LampAccessTimeout",
                    requestId = request.ID,
                    lampName = request.Lamp.Name,
                    message = "Request timed out. No manager responded within 5 minutes."
                };

                await notificationService.SendWebSocketNotificationAsync(request.UserID, employeeNotification);
            }

            await context.SaveChangesAsync();

            _logger.LogInformation("Completed processing {Count} expired requests", expiredRequests.Count);
        }
    }
}
