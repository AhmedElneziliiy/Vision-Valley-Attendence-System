using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoreProject.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Add this connection to user-specific group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation("User {UserId} connected to NotificationHub with ConnectionId {ConnectionId}",
                    userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("Connection {ConnectionId} attempted to connect without valid user ID",
                    Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("User {UserId} disconnected from NotificationHub. ConnectionId: {ConnectionId}",
                    userId, Context.ConnectionId);
            }

            if (exception != null)
            {
                _logger.LogError(exception, "User {UserId} disconnected with error", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
