using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Services.IService;
using CoreProject.Utilities.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    /// <summary>
    /// Service implementation for mobile app authentication API
    /// Contains all business logic for login, reset, and password change operations
    /// </summary>
    public class AuthApiService : IAuthApiService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthApiService> _logger;
        private readonly ApplicationDbContext _context;

        public AuthApiService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthApiService> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            try
            {
                // Find user by email with all related data
                var user = await _userManager.Users
                    .Include(u => u.Department)
                    .Include(u => u.Branch)
                        .ThenInclude(b => b.Organization)
                    .Include(u => u.Timetable)
                    .Include(u => u.Manager)
                    .Include(u => u.Subordinates)
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed: User not found - {Email}", request.Email);
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Login attempt for inactive user: {Email}", request.Email);
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Your account has been deactivated. Please contact your administrator."
                    };
                }

                // Verify password
                var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
                if (!passwordCheck.Succeeded)
                {
                    _logger.LogWarning("Login attempt failed: Invalid password - {Email}", request.Email);
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Check UDID binding
                if (!string.IsNullOrEmpty(user.UDID) && user.UDID != request.UDID)
                {
                    _logger.LogWarning("Login attempt failed: UDID mismatch - {Email}, Expected: {Expected}, Got: {Actual}",
                        request.Email, user.UDID, request.UDID);
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "This account is registered to a different device. Please use the Reset Password option to register this device."
                    };
                }

                // Bind UDID if not set
                if (string.IsNullOrEmpty(user.UDID))
                {
                    user.UDID = request.UDID;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("UDID bound for user: {Email}", request.Email);
                }

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                // Check if user is a manager
                bool isManager = user.Subordinates.Any() ||
                                 roles.Contains("Manager") ||
                                 roles.Contains("Admin") ||
                                 roles.Contains("HR");

                // Get active devices for the user's branch
                var branchDevices = await _context.Devices
                    .Where(d => d.BranchID == user.BranchID && d.IsActive)
                    .Select(d => new DeviceDataDto
                    {
                        Id = d.ID,
                        DeviceID = d.DeviceID,
                        DeviceType = d.DeviceType.HasValue
                            ? d.DeviceType.Value == 'I' ? "In Device"
                            : d.DeviceType.Value == 'O' ? "Out Device"
                            : d.DeviceType.Value == 'B' ? "Both (In/Out)"
                            : "Unknown"
                            : "Not Set",
                        Description = d.Description,
                        IsActive = d.IsActive
                    })
                    .ToListAsync();

                // Generate JWT token
                var token = GenerateJwtToken(user, roles.ToList());

                // Build response with complete user data
                var response = new LoginResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    UserData = new UserDataDto
                    {
                        Id = user.Id,
                        DisplayName = user.DisplayName,
                        Email = user.Email!,
                        Mobile = user.Mobile,
                        IsActive = user.IsActive,
                        VacationBalance = user.VacationBalance,
                        Address = user.Address,
                        Gender = user.Gender,
                        IsManager = isManager,
                        ManagerId = user.ManagerID,
                        ManagerName = user.Manager?.DisplayName,
                        Organization = new OrganizationDataDto
                        {
                            Id = user.Branch.Organization.ID,
                            Name = user.Branch.Organization.Name,
                            Description = null
                        },
                        Branch = new BranchDataDto
                        {
                            Id = user.Branch.ID,
                            Name = user.Branch.Name,
                            Address = null,
                            Phone = null,
                            AvailableDevices = branchDevices
                        },
                        Department = new DepartmentDataDto
                        {
                            Id = user.Department.ID,
                            Name = user.Department.Name,
                            Description = null
                        },
                        Timetable = user.Timetable != null ? new TimetableDataDto
                        {
                            Id = user.Timetable.ID,
                            Name = user.Timetable.Name,
                            CheckInTime = !string.IsNullOrEmpty(user.Timetable.WorkingDayStartingHourMinimum)
                                ? TimeSpan.Parse(user.Timetable.WorkingDayStartingHourMinimum)
                                : null,
                            CheckOutTime = !string.IsNullOrEmpty(user.Timetable.WorkingDayEndingHour)
                                ? TimeSpan.Parse(user.Timetable.WorkingDayEndingHour)
                                : null,
                            GracePeriodMinutes = null
                        } : null,
                        Roles = roles.ToList(),

                        // Face Verification flags
                        IsFaceVerificationRequired = user.Branch.IsFaceVerificationEnabled && user.IsFaceVerificationEnabled,
                        HasFaceEnrollment = user.FaceEmbedding != null && user.FaceEmbedding.Length > 0
                    }
                };

                _logger.LogInformation("User logged in successfully: {Email}, Roles: {Roles}",
                    request.Email, string.Join(", ", roles));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login. Please try again later."
                };
            }
        }

        public async Task<AuthResponseDto> ResetPasswordAsync(ResetRequestDto request)
        {
            try
            {
                // Find user by email
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("Reset attempt failed: User not found - {Email}", request.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Reset attempt for inactive user: {Email}", request.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Your account has been deactivated. Please contact your administrator."
                    };
                }

                // Validate UDID matches the user's registered device
                if (string.IsNullOrEmpty(user.UDID))
                {
                    _logger.LogWarning("Reset attempt failed: User has no registered device - {Email}", request.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "No device is registered for this account. Please contact your administrator."
                    };
                }

                if (user.UDID != request.UDID)
                {
                    _logger.LogWarning("Reset attempt failed: UDID mismatch - {Email}, Expected: {Expected}, Got: {Actual}",
                        request.Email, user.UDID, request.UDID);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "This device is not authorized for this account. Please use your registered device or contact your administrator."
                    };
                }

                // Reset password to default "Pass@123" (DO NOT reset UDID)
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                if (!removePasswordResult.Succeeded)
                {
                    _logger.LogError("Failed to remove password for user: {Email}, Errors: {Errors}",
                        request.Email, string.Join(", ", removePasswordResult.Errors.Select(e => e.Description)));
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Failed to reset password. Please contact your administrator."
                    };
                }

                var addPasswordResult = await _userManager.AddPasswordAsync(user, "Pass@123");
                if (!addPasswordResult.Succeeded)
                {
                    _logger.LogError("Failed to set default password for user: {Email}, Errors: {Errors}",
                        request.Email, string.Join(", ", addPasswordResult.Errors.Select(e => e.Description)));
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Failed to reset password. Please contact your administrator."
                    };
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Password reset successful for user: {Email} from device {UDID}", request.Email, request.UDID);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Password has been reset to 'Pass@123'. You can now login with the new password."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reset for email: {Email}", request.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during reset. Please try again later."
                };
            }
        }

        public async Task<AuthResponseDto> ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
        {
            try
            {
                // Find user
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("Change password attempt failed: User not found - ID: {UserId}", userId);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Verify old password
                var passwordCheck = await _userManager.CheckPasswordAsync(user, request.OldPassword);
                if (!passwordCheck)
                {
                    _logger.LogWarning("Change password attempt failed: Incorrect old password - User: {Email}", user.Email);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Current password is incorrect"
                    };
                }

                // Check if new password is same as old password
                if (request.OldPassword == request.NewPassword)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "New password must be different from the current password"
                    };
                }

                // Change password
                var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Change password failed for user: {Email}, Errors: {Errors}", user.Email, errors);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = $"Password change failed: {errors}"
                    };
                }

                // Update timestamp
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Password changed successfully for user: {Email}", user.Email);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Password changed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change for user ID: {UserId}", userId);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred while changing password. Please try again later."
                };
            }
        }

        /// <summary>
        /// Generates JWT token with user claims
        /// </summary>
        private string GenerateJwtToken(ApplicationUser user, List<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UDID", user.UDID ?? string.Empty)
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["JWT:DurationInDays"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
