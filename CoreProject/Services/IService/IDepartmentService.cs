using CoreProject.ViewModels;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentViewModel>> GetAllDepartmentsAsync(ClaimsPrincipal currentUser);
        Task<DepartmentDetailsViewModel?> GetDepartmentDetailsAsync(int departmentId);
        Task<DepartmentCreateViewModel> GetCreateDepartmentViewModelAsync(ClaimsPrincipal currentUser);
        Task<bool> CreateDepartmentAsync(DepartmentCreateViewModel model);
        Task<DepartmentEditViewModel?> GetEditDepartmentViewModelAsync(int departmentId, ClaimsPrincipal currentUser);
        Task<bool> UpdateDepartmentAsync(DepartmentEditViewModel model);
        Task<bool> DeleteDepartmentAsync(int departmentId);
    }
}
