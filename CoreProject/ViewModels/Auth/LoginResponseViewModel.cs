using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.ViewModels
{
    public class LoginResponseViewModel
    {
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public UserInfoViewModel User { get; set; } = null!;
    }
}
