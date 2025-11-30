using CoreProject.Hubs;
using CoreProject.Services.IService;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendWebSocketNotificationAsync(int userId, object notificationData)
        {
            try
            {
                var groupName = $"user_{userId}";
                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", notificationData);

                _logger.LogInformation("WebSocket notification sent to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WebSocket notification to user {UserId}", userId);
            }
        }

        public async Task SendWebSocketNotificationToMultipleUsersAsync(List<int> userIds, object notificationData)
        {
            try
            {
                var groupNames = userIds.Select(id => $"user_{id}").ToList();

                foreach (var groupName in groupNames)
                {
                    await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", notificationData);
                }

                _logger.LogInformation("WebSocket notification sent to {Count} users", userIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WebSocket notifications to multiple users");
            }
        }
    }
}
