using CoreProject.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface IBranchService
    {
        Task<IEnumerable<BranchViewModel>> GetAllBranchesAsync();
        Task<BranchDetailsViewModel?> GetBranchDetailsAsync(int branchId);
        Task<BranchCreateViewModel> GetCreateBranchViewModelAsync();
        Task<bool> CreateBranchAsync(BranchCreateViewModel model);
        Task<BranchEditViewModel?> GetEditBranchViewModelAsync(int branchId);
        Task<bool> UpdateBranchAsync(BranchEditViewModel model);
        Task<bool> DeleteBranchAsync(int branchId);

        // Department management
        Task<bool> AddDepartmentToBranchAsync(int branchId, string departmentName);
        Task<bool> RemoveDepartmentFromBranchAsync(int departmentId);
        Task<bool> UpdateDepartmentAsync(int departmentId, string newName, bool isActive);
    }
}
