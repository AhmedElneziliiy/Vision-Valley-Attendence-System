using CoreProject.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardStatsAsync();
    }
}
