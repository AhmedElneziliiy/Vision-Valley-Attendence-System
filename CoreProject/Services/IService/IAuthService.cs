using CoreProject.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface IAuthService
    {
        Task<LoginResponseViewModel> LoginAsync(string email, string password);
        Task<bool> RegisterAsync(string email, string password, string displayName, int branchId, int deptId);
    }
}
