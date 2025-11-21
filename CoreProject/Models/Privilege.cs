using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Models
{
    public class Privilege
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string PageURL { get; set; }
        public int UserType { get; set; }  // Will map to roles like Admin, HR, Employee
        public int? ParentID { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }

        // Navigation properties
        public ICollection<Privilege> ChildPrivileges { get; set; }  // Self-referencing for parent-child hierarchy
        public Privilege ParentPrivilege { get; set; }
    }

}
