using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Services.IService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    public class LampAccessRequestService : ILampAccessRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly LampWebSocketHandler _lampWebSocketHandler;
        private readonly ILogger<LampAccessRequestService> _logger;

        public LampAccessRequestService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            LampWebSocketHandler lampWebSocketHandler,
            ILogger<LampAccessRequestService> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _lampWebSocketHandler = lampWebSocketHandler;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, LampAccessRequest? Request)> SubmitRequestAsync(
            int userId, int lampId, string? reason = null)
        {
            try
            {
                // 1. Get lamp and user
                var lamp = await _context.Lamps
                    .Include(l => l.Branch)
                    .Include(l => l.Timetable)
                    .FirstOrDefaultAsync(l => l.ID == lampId && l.IsActive);

                if (lamp == null)
                {
                    return (false, "Lamp not found.", null);
                }

                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return (false, "User not found or inactive.", null);
                }

                // 2. Validate user is in same branch as lamp
                if (user.BranchID != lamp.BranchID)
                {
                    return (false, "You can only request access to lamps in your branch.", null);
                }

                // 3. Check if user is currently outside schedule hours
                var branchLocalTime = DateTime.UtcNow.AddHours(lamp.Branch.TimeZone);
                bool shouldBeOn = lamp.ShouldBeOn(branchLocalTime);

                if (shouldBeOn)
                {
                    return (false, "The lamp is currently within scheduled hours. No access request needed.", null);
                }

                // 4. Rate limiting: Check pending request count
                var pendingCount = await _context.LampAccessRequests
                    .CountAsync(r => r.UserID == userId && r.Status == "Pending");

                if (pendingCount >= 3)
                {
                    return (false, "You have too many pending requests. Please wait for a response.", null);
                }

                // 5. Create request
                var request = new LampAccessRequest
                {
                    LampID = lampId,
                    UserID = userId,
                    Reason = reason,
                    RequestedAt = DateTime.UtcNow,
                    TimeoutAt = DateTime.UtcNow.AddMinutes(5),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.LampAccessRequests.Add(request);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Lamp access request created. RequestID: {RequestId}, UserID: {UserId}, LampID: {LampId}",
                    request.ID, userId, lampId);

                // 6. Get notification recipients (managers in same branch + CEO + TechnicalManager)
                var recipients = await GetNotificationRecipientsAsync(lamp.BranchID);

                if (recipients.Count == 0)
                {
                    _logger.LogWarning("No notification recipients found for branch {BranchId}", lamp.BranchID);
                }

                // 7. Send SignalR notifications to all managers
                var notificationData = new
                {
                    type = "LampAccessRequest",
                    requestId = request.ID,
                    lampId = lamp.ID,
                    lampName = lamp.Name,
                    requesterUserId = user.Id,
                    requesterName = user.DisplayName,
                    branchName = lamp.Branch.Name,
                    reason = reason ?? "",
                    requestedAt = request.RequestedAt.ToString("o"),
                    timeoutAt = request.TimeoutAt.ToString("o")
                };

                await _notificationService.SendWebSocketNotificationToMultipleUsersAsync(recipients, notificationData);

                _logger.LogInformation("Notifications sent to {Count} recipients for request {RequestId}",
                    recipients.Count, request.ID);

                return (true, "Request submitted successfully. Managers have been notified.", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting lamp access request. UserID: {UserId}, LampID: {LampId}", userId, lampId);
                return (false, "An error occurred while submitting your request.", null);
            }
        }

        public async Task<(bool Success, string Message)> ApproveRequestAsync(
            int requestId, int respondingUserId, string? notes = null)
        {
            try
            {
                var request = await _context.LampAccessRequests
                    .Include(r => r.Lamp)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.ID == requestId);

                if (request == null)
                {
                    return (false, "Request not found.");
                }

                // Race condition check: only allow if still pending
                if (request.Status != "Pending")
                {
                    return (false, $"Request has already been {request.Status.ToLower()}.");
                }

                // Timeout check
                if (request.TimeoutAt <= DateTime.UtcNow)
                {
                    request.Status = "Timeout";
                    request.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return (false, "Request has timed out.");
                }

                // Update request
                request.Status = "Approved";
                request.RespondedByUserID = respondingUserId;
                request.RespondedAt = DateTime.UtcNow;
                request.ApprovedUntil = DateTime.UtcNow.AddHours(1);
                request.ResponseNotes = notes;
                request.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Request {RequestId} approved by user {UserId}", requestId, respondingUserId);

                // Open lamp via WebSocket
                bool lampOpened = await _lampWebSocketHandler.SendStateChangeAsync(request.Lamp.DeviceID, true);

                if (!lampOpened)
                {
                    _logger.LogWarning("Failed to open lamp {DeviceID} - device may be offline", request.Lamp.DeviceID);
                }

                // Get responder details
                var responder = await _userManager.FindByIdAsync(respondingUserId.ToString());

                // Notify employee
                var employeeNotification = new
                {
                    type = "LampAccessApproved",
                    requestId = request.ID,
                    lampName = request.Lamp.Name,
                    approvedByName = responder?.DisplayName ?? "Manager",
                    approvedAt = request.RespondedAt?.ToString("o"),
                    approvedUntil = request.ApprovedUntil?.ToString("o"),
                    message = "Your request has been approved. Lamp will remain open for 1 hour."
                };

                await _notificationService.SendWebSocketNotificationAsync(request.UserID, employeeNotification);

                // Notify other managers that request is already handled
                var otherManagers = await GetNotificationRecipientsAsync(request.Lamp.BranchID);
                otherManagers.Remove(respondingUserId); // Don't notify the responder

                if (otherManagers.Count > 0)
                {
                    var alreadyHandledNotification = new
                    {
                        type = "LampAccessAlreadyHandled",
                        requestId = request.ID,
                        status = "Approved",
                        handledByName = responder?.DisplayName ?? "Manager",
                        message = $"This request was already approved by {responder?.DisplayName ?? "Manager"}."
                    };

                    await _notificationService.SendWebSocketNotificationToMultipleUsersAsync(otherManagers, alreadyHandledNotification);
                }

                return (true, "Request approved successfully. Lamp has been opened for 1 hour.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request {RequestId}", requestId);
                return (false, "An error occurred while approving the request.");
            }
        }

        public async Task<(bool Success, string Message)> DeclineRequestAsync(
            int requestId, int respondingUserId, string? notes = null)
        {
            try
            {
                var request = await _context.LampAccessRequests
                    .Include(r => r.Lamp)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.ID == requestId);

                if (request == null)
                {
                    return (false, "Request not found.");
                }

                // Race condition check
                if (request.Status != "Pending")
                {
                    return (false, $"Request has already been {request.Status.ToLower()}.");
                }

                // Timeout check
                if (request.TimeoutAt <= DateTime.UtcNow)
                {
                    request.Status = "Timeout";
                    request.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return (false, "Request has timed out.");
                }

                // Update request
                request.Status = "Declined";
                request.RespondedByUserID = respondingUserId;
                request.RespondedAt = DateTime.UtcNow;
                request.ResponseNotes = notes;
                request.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Request {RequestId} declined by user {UserId}", requestId, respondingUserId);

                // Get responder details
                var responder = await _userManager.FindByIdAsync(respondingUserId.ToString());

                // Notify employee
                var employeeNotification = new
                {
                    type = "LampAccessDeclined",
                    requestId = request.ID,
                    lampName = request.Lamp.Name,
                    declinedByName = responder?.DisplayName ?? "Manager",
                    responseNotes = notes ?? "",
                    message = "Your request has been declined."
                };

                await _notificationService.SendWebSocketNotificationAsync(request.UserID, employeeNotification);

                // Notify other managers
                var otherManagers = await GetNotificationRecipientsAsync(request.Lamp.BranchID);
                otherManagers.Remove(respondingUserId);

                if (otherManagers.Count > 0)
                {
                    var alreadyHandledNotification = new
                    {
                        type = "LampAccessAlreadyHandled",
                        requestId = request.ID,
                        status = "Declined",
                        handledByName = responder?.DisplayName ?? "Manager",
                        message = $"This request was already declined by {responder?.DisplayName ?? "Manager"}."
                    };

                    await _notificationService.SendWebSocketNotificationToMultipleUsersAsync(otherManagers, alreadyHandledNotification);
                }

                return (true, "Request declined successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining request {RequestId}", requestId);
                return (false, "An error occurred while declining the request.");
            }
        }

        public async Task<List<LampAccessRequest>> GetPendingRequestsForUserAsync(int userId)
        {
            // Check if user is a manager/CEO/TechnicalManager
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return new List<LampAccessRequest>();

            var userRoles = await _userManager.GetRolesAsync(user);

            if (userRoles.Contains("Manager") || userRoles.Contains("CEO") || userRoles.Contains("TechnicalManager"))
            {
                // Return pending requests for lamps in their branch (managers can approve)
                var requests = await _context.LampAccessRequests
                    .Include(r => r.Lamp)
                    .ThenInclude(l => l.Branch)
                    .Include(r => r.User)
                    .Where(r => r.Status == "Pending" && r.Lamp.BranchID == user.BranchID)
                    .OrderByDescending(r => r.RequestedAt)
                    .ToListAsync();

                return requests;
            }
            else
            {
                // Return their own pending requests
                var requests = await _context.LampAccessRequests
                    .Include(r => r.Lamp)
                    .Where(r => r.UserID == userId && r.Status == "Pending")
                    .OrderByDescending(r => r.RequestedAt)
                    .ToListAsync();

                return requests;
            }
        }

        public async Task<List<LampAccessRequest>> GetRequestHistoryAsync(int userId, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.LampAccessRequests
                .Include(r => r.Lamp)
                .Include(r => r.RespondedByUser)
                .Where(r => r.UserID == userId);

            if (from.HasValue)
            {
                query = query.Where(r => r.RequestedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(r => r.RequestedAt <= to.Value);
            }

            var requests = await query
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return requests;
        }

        public async Task<LampAccessRequest?> GetRequestByIdAsync(int requestId)
        {
            return await _context.LampAccessRequests
                .Include(r => r.Lamp)
                .ThenInclude(l => l.Branch)
                .Include(r => r.User)
                .Include(r => r.RespondedByUser)
                .FirstOrDefaultAsync(r => r.ID == requestId);
        }

        public async Task<List<int>> GetNotificationRecipientsAsync(int branchId)
        {
            var recipients = new List<int>();

            // 1. All managers in same branch
            var managers = await _userManager.GetUsersInRoleAsync("Manager");
            var branchManagerIds = managers
                .Where(u => u.BranchID == branchId && u.IsActive)
                .Select(u => u.Id)
                .ToList();

            recipients.AddRange(branchManagerIds);

            // 2. All CEOs (across all branches)
            var ceos = await _userManager.GetUsersInRoleAsync("CEO");
            var ceoIds = ceos
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToList();

            recipients.AddRange(ceoIds);

            // 3. All Technical Managers (across all branches)
            var techManagers = await _userManager.GetUsersInRoleAsync("TechnicalManager");
            var techManagerIds = techManagers
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToList();

            recipients.AddRange(techManagerIds);

            // Remove duplicates
            return recipients.Distinct().ToList();
        }
    }
}
