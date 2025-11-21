using CoreProject.Utilities.DTOs;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    /// <summary>
    /// Service interface for mobile app authentication API
    /// Handles login, password reset, and password change operations
    /// </summary>
    public interface IAuthApiService
    {
        /// <summary>
        /// Authenticates user with email, password, and device UDID
        /// Binds device on first login, verifies on subsequent logins
        /// </summary>
        /// <param name="request">Login credentials including UDID</param>
        /// <returns>Login response with JWT token and user data, or error message</returns>
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

        /// <summary>
        /// Resets user password to default and clears UDID binding
        /// Allows user to login from a new device
        /// </summary>
        /// <param name="request">Email and new device UDID</param>
        /// <returns>Success or error response</returns>
        Task<AuthResponseDto> ResetPasswordAsync(ResetRequestDto request);

        /// <summary>
        /// Changes password for authenticated user
        /// </summary>
        /// <param name="userId">User ID from JWT token</param>
        /// <param name="request">Old password, new password, and confirmation</param>
        /// <returns>Success or error response</returns>
        Task<AuthResponseDto> ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
    }
}
