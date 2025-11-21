using CoreProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<ApplicationUser>
    {
        Task<IEnumerable<ApplicationUser>> GetUsersByBranchAsync(int branchId);
        Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string role);
        Task<IEnumerable<string>> GetAllRolesAsync();
    }
}
