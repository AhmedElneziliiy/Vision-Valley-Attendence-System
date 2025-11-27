using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoreProject.Utilities.DTOs
{
    #region Request DTOs

    /// <summary>
    /// Login request model for mobile app authentication
    /// </summary>
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "UDID is required")]
        [MinLength(10, ErrorMessage = "Invalid UDID format")]
        public string UDID { get; set; } = null!;
    }

    /// <summary>
    /// Reset password request - resets UDID and password to default
    /// </summary>
    public class ResetRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "UDID is required")]
        [MinLength(10, ErrorMessage = "Invalid UDID format")]
        public string UDID { get; set; } = null!;
    }

    /// <summary>
    /// Change password request with token authentication
    /// </summary>
    public class ChangePasswordRequestDto
    {
        [Required(ErrorMessage = "Old password is required")]
        public string OldPassword { get; set; } = null!;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{6,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match")]
        public string ConfirmPassword { get; set; } = null!;
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Login response with user data and JWT token
    /// </summary>
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public UserDataDto? UserData { get; set; }
    }

    /// <summary>
    /// Generic response for reset and change password operations
    /// </summary>
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
    }

    /// <summary>
    /// Complete user data for mobile app
    /// </summary>
    public class UserDataDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Mobile { get; set; }
        public bool IsActive { get; set; }
        public int? VacationBalance { get; set; }
        public string? Address { get; set; }
        public char? Gender { get; set; }

        // Manager information
        public bool IsManager { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }

        // Organization structure
        public OrganizationDataDto Organization { get; set; } = null!;
        public BranchDataDto Branch { get; set; } = null!;
        public DepartmentDataDto Department { get; set; } = null!;

        // Timetable
        public TimetableDataDto? Timetable { get; set; }

        // Roles
        public List<string> Roles { get; set; } = new List<string>();

        // Face Verification
        public bool IsFaceVerificationRequired { get; set; }
        public bool HasFaceEnrollment { get; set; }
    }

    public class OrganizationDataDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class BranchDataDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public List<DeviceDataDto> AvailableDevices { get; set; } = new List<DeviceDataDto>();
    }

    public class DeviceDataDto
    {
        public int Id { get; set; }
        public string? DeviceID { get; set; }
        public string? DeviceType { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class DepartmentDataDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class TimetableDataDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public int? GracePeriodMinutes { get; set; }
    }

    #endregion
}
