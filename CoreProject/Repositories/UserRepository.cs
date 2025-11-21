using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Repositories
{
    public class UserRepository : Repository<ApplicationUser>, IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersByBranchAsync(int branchId)
        {
            return await _context.Users
                .Include(u => u.Branch)
                .Include(u => u.Department)
                .Where(u => u.BranchID == branchId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string role)
        {
            var users = await _context.Users.ToListAsync();
            var result = new List<ApplicationUser>();

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, role))
                    result.Add(user);
            }

            return result;
        }

        public async Task<IEnumerable<string>> GetAllRolesAsync()
        {
            return await _context.Roles.Select(r => r.Name!).ToListAsync();
        }

    }

}
