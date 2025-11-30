using CoreProject.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface ILampAccessRequestService
    {
        /// <summary>
        /// Employee submits a lamp access request
        /// </summary>
        Task<(bool Success, string Message, LampAccessRequest? Request)> SubmitRequestAsync(int userId, int lampId, string? reason = null);

        /// <summary>
        /// Manager approves a lamp access request
        /// </summary>
        Task<(bool Success, string Message)> ApproveRequestAsync(int requestId, int respondingUserId, string? notes = null);

        /// <summary>
        /// Manager declines a lamp access request
        /// </summary>
        Task<(bool Success, string Message)> DeclineRequestAsync(int requestId, int respondingUserId, string? notes = null);

        /// <summary>
        /// Get pending requests for a user (manager sees requests they can approve, employee sees their own pending requests)
        /// </summary>
        Task<List<LampAccessRequest>> GetPendingRequestsForUserAsync(int userId);

        /// <summary>
        /// Get request history for a user
        /// </summary>
        Task<List<LampAccessRequest>> GetRequestHistoryAsync(int userId, DateTime? from = null, DateTime? to = null);

        /// <summary>
        /// Get a specific request by ID
        /// </summary>
        Task<LampAccessRequest?> GetRequestByIdAsync(int requestId);

        /// <summary>
        /// Get list of user IDs who should receive notifications for a given branch
        /// (All managers in branch + CEO + TechnicalManager)
        /// </summary>
        Task<List<int>> GetNotificationRecipientsAsync(int branchId);
    }
}
