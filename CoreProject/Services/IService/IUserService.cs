using CoreProject.Models;
using CoreProject.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface IUserService
    {
        Task<UserCreateViewModel?> GetCreateUserViewModelAsync();
        Task<bool> CreateUserAsync(UserCreateViewModel model);
        Task<UserCreateViewModel> GetCreateUserViewModelAsync(int? selectedBranchId = null);

        Task<IEnumerable<UserViewModel>> GetFilteredUsersAsync(int? branchId, string? role, ClaimsPrincipal currentUser);
        Task<IEnumerable<Branch>> GetBranchesAsync();
        Task<IEnumerable<string>> GetRolesAsync();

        // View Details
        Task<UserDetailsViewModel?> GetUserDetailsAsync(int userId);

        // Edit User
        Task<UserEditViewModel?> GetEditUserViewModelAsync(int userId);
        Task<bool> UpdateUserAsync(UserEditViewModel model);

        // Delete User
        Task<bool> DeleteUserAsync(int userId);

        // Reset Password
        Task<bool> ResetUserPasswordAsync(int userId);
    }
}
