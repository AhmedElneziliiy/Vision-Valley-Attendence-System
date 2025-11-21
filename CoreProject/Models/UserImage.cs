using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Models
{
    public class UserImage
    {
        public int UserID { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public string ImageUrl { get; set; } = null!;
    }
}
