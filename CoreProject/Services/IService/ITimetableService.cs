using CoreProject.ViewModels;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface ITimetableService
    {
        Task<IEnumerable<TimetableViewModel>> GetAllTimetablesAsync(ClaimsPrincipal currentUser, int? branchId = null);
        Task<TimetableDetailsViewModel?> GetTimetableDetailsAsync(int timetableId);
        Task<TimetableCreateViewModel> GetCreateTimetableViewModelAsync(ClaimsPrincipal currentUser);
        Task<bool> CreateTimetableAsync(TimetableCreateViewModel model);
        Task<TimetableEditViewModel?> GetEditTimetableViewModelAsync(int timetableId);
        Task<bool> UpdateTimetableAsync(TimetableEditViewModel model);
        Task<bool> DeleteTimetableAsync(int timetableId);

        // Configuration management
        Task<bool> AddConfigurationToTimetableAsync(int timetableId, int configurationId, string value);
        Task<bool> UpdateConfigurationAsync(int timetableConfigurationId, string newValue);
        Task<bool> RemoveConfigurationFromTimetableAsync(int timetableConfigurationId);
    }
}
