using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using CoreProject.Services.IService;
using CoreProject.ViewModels;
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
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly IRepository<ApplicationUser> _userRepo;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config,
            IRepository<ApplicationUser> userRepo,
            ILogger<AuthService> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _userRepo = userRepo;
            _logger = logger;
            _context = context;
        }

        public async Task<LoginResponseViewModel> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Login attempt with empty email");
                throw new ArgumentException("Email is required", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Login attempt with empty password for email: {Email}", email);
                throw new ArgumentException("Password is required", nameof(password));
            }

            _logger.LogInformation("Login attempt for email: {Email}", email);

            // Ignore query filters during authentication to bypass branch filtering
            var user = await _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for email: {Email}", email);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Check if user account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: Inactive account for email: {Email}", email);
                throw new UnauthorizedAccessException("Your account has been deactivated. Please contact your administrator.");
            }

            // Verify password
            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                _logger.LogWarning("Login failed: Invalid password for email: {Email}", email);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Additional security check: ensure branch is active
            if (user.Branch != null && !user.Branch.IsActive)
            {
                _logger.LogWarning("Login failed: User's branch is inactive for email: {Email}", email);
                throw new UnauthorizedAccessException("Your branch is currently inactive. Please contact your administrator.");
            }

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            if (roles == null || !roles.Any())
            {
                _logger.LogWarning("Login failed: No roles assigned to user: {Email}", email);
                throw new UnauthorizedAccessException("Your account has no roles assigned. Please contact your administrator.");
            }

            // Build claims
            var claims = new List<Claim>
             {
                 new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                 new Claim(ClaimTypes.Email, user.Email!),
                 new Claim(ClaimTypes.Name, user.DisplayName),
                 new Claim("BranchID", user.BranchID.ToString()),
                 new Claim("DepartmentID", user.DepartmentID.ToString())
             };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Generate JWT token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenExpiry = DateTime.UtcNow.AddDays(7); // Changed from 100 days to 7 days for better security

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: tokenExpiry,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogInformation("Login successful for user: {Email}, UserId: {UserId}, Roles: {Roles}",
                email, user.Id, string.Join(", ", roles));

            return new LoginResponseViewModel
            {
                Token = tokenString,
                ExpiresAt = tokenExpiry,
                User = new UserInfoViewModel
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName,
                    Email = user.Email!,
                    BranchId = user.BranchID,
                    BranchName = user.Branch?.Name ?? "Unknown",
                    Roles = roles.ToList()
                }
            };
        }

        public async Task<bool> RegisterAsync(string email, string password, string displayName, int branchId, int deptId)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
                BranchID = branchId,
                DepartmentID = deptId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Employee");
                return true;
            }
            return false;
        }
    }
}
