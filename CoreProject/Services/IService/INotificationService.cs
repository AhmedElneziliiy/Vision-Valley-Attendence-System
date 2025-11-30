using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface INotificationService
    {
        /// <summary>
        /// Sends a WebSocket/SignalR notification to a single user
        /// </summary>
        Task SendWebSocketNotificationAsync(int userId, object notificationData);

        /// <summary>
        /// Sends a WebSocket/SignalR notification to multiple users
        /// </summary>
        Task SendWebSocketNotificationToMultipleUsersAsync(List<int> userIds, object notificationData);
    }
}
