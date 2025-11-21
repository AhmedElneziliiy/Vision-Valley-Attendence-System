using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreProject.ViewModels
{
    public class DepartmentCreateViewModel
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public int BranchId { get; set; }

        public bool IsActive { get; set; } = true;

        // Dropdown lists
        public IEnumerable<SelectListItem> Branches { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
