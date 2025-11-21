using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.ViewModels
{
    public class UserInfoViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
    }
}
