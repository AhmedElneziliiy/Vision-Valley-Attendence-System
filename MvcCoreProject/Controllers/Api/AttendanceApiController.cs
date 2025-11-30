using CoreProject.Services.IService;
using CoreProject.Utilities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MvcCoreProject.Controllers.Api
{
    /// <summary>
    /// API controller for mobile app attendance operations (check-in/check-out)
    /// </summary>
    [Route("api/attendance")]
    [ApiController]
    [Authorize] // All attendance endpoints require authentication
    public class AttendanceApiController : ControllerBase
    {
        private readonly IAttendanceApiService _attendanceApiService;
        private readonly ILogger<AttendanceApiController> _logger;

        public AttendanceApiController(
            IAttendanceApiService attendanceApiService,
            ILogger<AttendanceApiController> logger)
        {
            _attendanceApiService = attendanceApiService;
            _logger = logger;
        }

        /// <summary>
        /// Process attendance action (check-in or check-out)
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/attendance/action
        ///     {
        ///         "username": "hussein.mohamed@visionvalley.net",
        ///         "udid": "6c72cf5c980bd5c1",
        ///         "deviceID": "100000000",
        ///         "actionType": "CheckIn"
        ///     }
        ///
        /// ActionType must be either "CheckIn" or "CheckOut"
        ///
        /// User must be authenticated (requires JWT token in Authorization header)
        ///
        /// Validations performed:
        /// - User exists and is active
        /// - UDID matches user's registered device
        /// - Device exists and is active
        /// - User is assigned to the same branch as the device
        /// - Timezone handling for branch location (Egypt, Dubai, etc.)
        /// - Check-out requires previous check-in on the same day
        ///
        /// Returns:
        /// - Success/failure status
        /// - Attendance data including check-in time, check-out time, duration, status (on time/late)
        /// - Timezone-adjusted times
        /// </remarks>
        /// <param name="request">Attendance action request</param>
        /// <returns>Attendance action response with attendance data</returns>
        /// <response code="200">Action processed successfully</response>
        /// <response code="400">Invalid request (validation errors)</response>
        /// <response code="401">User not authorized</response>
        [HttpPost("action")]
        [ProducesResponseType(typeof(AttendanceActionResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<AttendanceActionResponseDto>> ProcessAttendanceAction([FromBody] AttendanceActionRequestDto request)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid attendance action request: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                return BadRequest(new AttendanceActionResponseDto
                {
                    Success = false,
                    Message = "Invalid request. Please check your input.",
                    Data = null
                });
            }

            _logger.LogInformation("Processing attendance action: User={Username}, Device={DeviceID}, Action={ActionType}",
                request.Username, request.DeviceID, request.ActionType);

            // Delegate to service
            var response = await _attendanceApiService.ProcessAttendanceActionAsync(request);

            // Return appropriate HTTP status based on response
            if (!response.Success)
            {
                // Check specific error messages for appropriate status codes
                if (response.Message.Contains("not found") ||
                    response.Message.Contains("No check-in found"))
                {
                    return BadRequest(response);
                }

                if (response.Message.Contains("inactive") ||
                    response.Message.Contains("deactivated") ||
                    response.Message.Contains("not registered") ||
                    response.Message.Contains("not assigned"))
                {
                    return BadRequest(response);
                }

                // Generic error
                return BadRequest(response);
            }

            // Success
            _logger.LogInformation("Attendance action successful: User={Username}, Action={ActionType}, AttendanceId={AttendanceId}",
                request.Username, request.ActionType, response.Data?.AttendanceId);

            return Ok(response);
        }

        /// <summary>
        /// Get attendance report for the authenticated user
        /// </summary>
        /// <remarks>
        /// Sample requests:
        ///
        ///     GET /api/attendance/report
        ///     (Returns current month report)
        ///
        ///     GET /api/attendance/report?dateFrom=2025-01-01&amp;dateTo=2025-01-31
        ///     (Returns report for specified date range)
        ///
        /// User must be authenticated (requires JWT token in Authorization header)
        /// User ID is extracted from the JWT token automatically
        ///
        /// Date parameters are optional:
        /// - If not provided, defaults to current month (1st day to today)
        /// - dateFrom: Start date for the report
        /// - dateTo: End date for the report (cannot be in the future)
        ///
        /// Returns:
        /// - Daily attendance records with check-in, check-out, duration, and status
        /// - Vacation days from branch settings (weekends and national holidays)
        /// - Summary statistics (total working days, present days, absent days, etc.)
        /// - Expected check-in/check-out times from user's timetable
        /// </remarks>
        /// <param name="request">Optional date range filter</param>
        /// <returns>Attendance report with daily records and statistics</returns>
        /// <response code="200">Report generated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authorized</response>
        [HttpGet("report")]
        [ProducesResponseType(typeof(AttendanceReportResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<AttendanceReportResponseDto>> GetAttendanceReport([FromQuery] AttendanceReportRequestDto request)
        {
            try
            {
                // Get user ID from JWT token claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("Failed to extract user ID from JWT token");
                    return Unauthorized(new AttendanceReportResponseDto
                    {
                        Success = false,
                        Message = "Invalid authentication token"
                    });
                }

                _logger.LogInformation("Generating attendance report for user: {UserId}, DateFrom={DateFrom}, DateTo={DateTo}",
                    userId, request.DateFrom, request.DateTo);

                // Delegate to service
                var response = await _attendanceApiService.GetUserAttendanceReportAsync(userId, request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                _logger.LogInformation("Attendance report generated successfully for user: {UserId}, Records={RecordCount}",
                    userId, response.Data?.DailyRecords.Count ?? 0);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attendance report");
                return BadRequest(new AttendanceReportResponseDto
                {
                    Success = false,
                    Message = "An error occurred while generating the report"
                });
            }
        }

        /// <summary>
        /// Process passthrough access control verification
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/attendance/passthrough
        ///     {
        ///         "username": "hussein.mohamed@visionvalley.net",
        ///         "udid": "6c72cf5c980bd5c1",
        ///         "deviceID": "100000000"
        ///     }
        ///
        /// User must be authenticated (requires JWT token in Authorization header)
        ///
        /// Validations performed:
        /// - User exists and is active
        /// - UDID matches user's registered device
        /// - Device exists and is active
        /// - User is assigned to the same branch as the device
        /// - Sets AccessControlState to 1 for the device
        ///
        /// Returns:
        /// - Access granted/denied status
        /// - Current AccessControlState value
        /// - User and branch information
        /// </remarks>
        /// <param name="request">Passthrough request</param>
        /// <returns>Passthrough response with access granted/denied status</returns>
        /// <response code="200">Request processed successfully</response>
        /// <response code="400">Invalid request (validation errors)</response>
        /// <response code="401">User not authorized</response>
        [HttpPost("passthrough")]
        [ProducesResponseType(typeof(PassthroughResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<PassthroughResponseDto>> Passthrough([FromBody] PassthroughRequestDto request)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid passthrough request: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                return BadRequest(new PassthroughResponseDto
                {
                    Success = false,
                    Message = "Invalid request. Please check your input.",
                    AccessGranted = false
                });
            }

            _logger.LogInformation("Processing passthrough: User={Username}, Device={DeviceID}",
                request.Username, request.DeviceID);

            // Delegate to service
            var response = await _attendanceApiService.ProcessPassthroughAsync(request);

            // Return appropriate HTTP status based on response
            if (!response.Success)
            {
                _logger.LogWarning("Passthrough failed: User={Username}, Reason={Message}",
                    request.Username, response.Message);
                return BadRequest(response);
            }

            // Success
            _logger.LogInformation("Passthrough successful: User={Username}, AccessGranted={AccessGranted}, AccessControlState={State}",
                request.Username, response.AccessGranted, response.AccessControlState);

            return Ok(response);
        }

        /// <summary>
        /// Check the current attendance status of the authenticated user
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/attendance/status
        ///     Authorization: Bearer {your-jwt-token}
        ///
        /// User must be authenticated (requires JWT token in Authorization header)
        ///
        /// Returns:
        /// - Current status: "CheckedIn", "CheckedOut", or "NotCheckedIn"
        /// - Today's attendance details (if exists)
        /// - Expected check-in/check-out times from timetable
        /// - Duration of work if checked out
        /// </remarks>
        /// <returns>User attendance status for today</returns>
        /// <response code="200">Status retrieved successfully</response>
        /// <response code="401">User not authorized</response>
        [HttpGet("status")]
        [ProducesResponseType(typeof(UserStatusResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<UserStatusResponseDto>> CheckUserStatus()
        {
            try
            {
                // Get user ID from JWT token claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("Failed to extract user ID from JWT token");
                    return Unauthorized(new UserStatusResponseDto
                    {
                        Success = false,
                        Message = "Invalid authentication token"
                    });
                }

                _logger.LogInformation("Checking attendance status for user: {UserId}", userId);

                // Delegate to service
                var response = await _attendanceApiService.CheckUserStatusAsync(userId);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                _logger.LogInformation("Attendance status retrieved for user: {UserId}, Status={Status}",
                    userId, response.Data?.Status);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user attendance status");
                return BadRequest(new UserStatusResponseDto
                {
                    Success = false,
                    Message = "An error occurred while checking user status"
                });
            }
        }

        /// <summary>
        /// Get the current user profile data (same as login response but without token)
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/attendance/profile
        ///     Authorization: Bearer {your-jwt-token}
        ///
        /// User must be authenticated (requires JWT token in Authorization header)
        ///
        /// Returns the same user data structure as the login endpoint, including:
        /// - User information (name, email, mobile, etc.)
        /// - Organization details
        /// - Branch details with available devices
        /// - Department details
        /// - Timetable information
        /// - User roles
        ///
        /// This endpoint is useful for refreshing user data without re-authenticating
        /// </remarks>
        /// <returns>Complete user profile data</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="401">User not authorized</response>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(UserProfileResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<UserProfileResponseDto>> GetUserProfile()
        {
            try
            {
                // Get user ID from JWT token claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("Failed to extract user ID from JWT token");
                    return Unauthorized(new UserProfileResponseDto
                    {
                        Success = false,
                        Message = "Invalid authentication token"
                    });
                }

                _logger.LogInformation("Retrieving profile for user: {UserId}", userId);

                // Delegate to service
                var response = await _attendanceApiService.GetUserProfileAsync(userId);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                _logger.LogInformation("Profile retrieved successfully for user: {UserId}", userId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return BadRequest(new UserProfileResponseDto
                {
                    Success = false,
                    Message = "An error occurred while retrieving user profile"
                });
            }
        }

        /// <summary>
        /// Verify user's face against their enrolled photo
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/attendance/verify-face
        ///     Authorization: Bearer {your-jwt-token}
        ///     {
        ///         "faceImage": "base64-encoded-image-string"
        ///     }
        ///
        /// User must be authenticated (requires JWT token in Authorization header)
        /// User ID is extracted from the JWT token automatically
        ///
        /// The endpoint:
        /// - Verifies the submitted face image against the user's enrolled face photo in the database
        /// - Returns whether the face is verified (similarity >= 85%)
        /// - Returns the similarity percentage
        ///
        /// Returns:
        /// - IsVerified: true if similarity >= 85%, false otherwise
        /// - Similarity: percentage match (0-100)
        /// - User information
        /// </remarks>
        /// <param name="request">Face verification request with base64 encoded image</param>
        /// <returns>Face verification response with verification result and similarity</returns>
        /// <response code="200">Verification processed successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authorized</response>
        [HttpPost("verify-face")]
        [ProducesResponseType(typeof(VerifyFaceResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<VerifyFaceResponseDto>> VerifyFace([FromBody] VerifyFaceRequestDto request)
        {
            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid verify face request: {Errors}",
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                    return BadRequest(new VerifyFaceResponseDto
                    {
                        Success = false,
                        Message = "Invalid request. Please check your input."
                    });
                }

                // Get user ID from JWT token claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("Failed to extract user ID from JWT token");
                    return Unauthorized(new VerifyFaceResponseDto
                    {
                        Success = false,
                        Message = "Invalid authentication token"
                    });
                }

                _logger.LogInformation("Verifying face for user: {UserId}", userId);

                // Delegate to service
                var response = await _attendanceApiService.VerifyUserFaceAsync(userId, request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                _logger.LogInformation("Face verification completed for user: {UserId}, IsVerified={IsVerified}, Similarity={Similarity}",
                    userId, response.Data?.IsVerified, response.Data?.Similarity);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying face");
                return BadRequest(new VerifyFaceResponseDto
                {
                    Success = false,
                    Message = "An error occurred while verifying face"
                });
            }
        }

        /// <summary>
        /// Get action status for the authenticated user (face verification requirements)
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/attendance/action-status
        ///     Authorization: Bearer {your-jwt-token}
        ///
        /// User must be authenticated (requires JWT token in Authorization header)
        /// User ID is extracted from the JWT token automatically
        ///
        /// Returns:
        /// - isFaceVerificationRequired: true if face verification is required for this user
        /// - hasFaceEnrollment: true if user has enrolled their face photo
        ///
        /// Face verification is required when both:
        /// - The branch has face verification enabled
        /// - The user has face verification enabled
        /// </remarks>
        /// <returns>Action status with face verification settings</returns>
        /// <response code="200">Status retrieved successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authorized</response>
        [HttpGet("action-status")]
        [ProducesResponseType(typeof(ActionStatusResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ActionStatusResponseDto>> GetActionStatus()
        {
            try
            {
                // Get user ID from JWT token claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("Failed to extract user ID from JWT token");
                    return Unauthorized(new ActionStatusResponseDto
                    {
                        Success = false,
                        Message = "Invalid authentication token"
                    });
                }

                _logger.LogInformation("Getting action status for user: {UserId}", userId);

                // Delegate to service
                var response = await _attendanceApiService.GetActionStatusAsync(userId);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                _logger.LogInformation("Action status retrieved for user: {UserId}", userId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting action status");
                return BadRequest(new ActionStatusResponseDto
                {
                    Success = false,
                    Message = "An error occurred while getting action status"
                });
            }
        }
    }
}
