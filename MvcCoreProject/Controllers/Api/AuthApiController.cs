using CoreProject.Services.IService;
using CoreProject.Utilities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MvcCoreProject.Controllers.Api
{
    /// <summary>
    /// Authentication API Controller for Mobile App Integration
    /// Thin controller that delegates business logic to AuthApiService
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthApiService _authApiService;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(
            IAuthApiService authApiService,
            ILogger<AuthApiController> logger)
        {
            _authApiService = authApiService;
            _logger = logger;
        }

        /// <summary>
        /// Login endpoint for mobile app
        /// Validates credentials, binds device UDID, and returns JWT token with user data
        /// </summary>
        /// <param name="request">Login credentials including email, password, and UDID</param>
        /// <returns>JWT token and complete user data including organization, branch, department, and role info</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest(new LoginResponseDto
                {
                    Success = false,
                    Message = $"Validation failed: {errors}"
                });
            }

            // Delegate to service
            var response = await _authApiService.LoginAsync(request);

            // Return appropriate HTTP status
            if (!response.Success)
            {
                if (response.Message?.Contains("Invalid email or password") == true ||
                    response.Message?.Contains("different device") == true ||
                    response.Message?.Contains("deactivated") == true)
                {
                    return Unauthorized(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Reset endpoint - Resets password to default (Pass@123)
        /// Validates that the UDID matches the user's registered device
        /// Used when user forgets password but still has access to their registered device
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/authapi/reset
        ///     {
        ///         "email": "hussein.mohamed@visionvalley.net",
        ///         "udid": "6c72cf5c980bd5c1"
        ///     }
        ///
        /// The UDID must match the device registered to this user account.
        /// If successful, password is reset to "Pass@123"
        /// </remarks>
        /// <param name="request">Email and current device UDID</param>
        /// <returns>Success or error message</returns>
        [HttpPost("reset")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AuthResponseDto>> Reset([FromBody] ResetRequestDto? request)
        {
            // Handle null or malformed JSON
            if (request == null)
            {
                _logger.LogWarning("Reset attempt failed: Request body is null or malformed");
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid request. Please provide email and UDID."
                });
            }

            // Manual validation for cleaner error messages
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Email is required."
                });
            }

            if (!IsValidEmail(request.Email))
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email format."
                });
            }

            if (string.IsNullOrWhiteSpace(request.UDID))
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Device ID (UDID) is required."
                });
            }

            if (request.UDID.Length < 10)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid device ID format."
                });
            }

            _logger.LogInformation("Processing password reset for: {Email}", request.Email);

            // Delegate to service
            var response = await _authApiService.ResetPasswordAsync(request);

            // Return appropriate HTTP status
            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Change Password endpoint - Requires authentication via JWT token
        /// Allows authenticated users to change their password
        /// </summary>
        /// <param name="request">Old password, new password, and confirmation</param>
        /// <returns>Success or error message</returns>
        [HttpPost("change-password")]
        [Authorize] // Requires valid JWT token
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = $"Validation failed: {errors}"
                });
            }

            // Get user ID from JWT token claims
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("Change password attempt failed: No valid user ID in token");
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid authentication token"
                });
            }

            // Delegate to service
            var response = await _authApiService.ChangePasswordAsync(userId, request);

            // Return appropriate HTTP status
            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return Unauthorized(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Helper method to validate email format
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
