using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using System.Threading.Tasks;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IPerformanceReportService
    {
        Task<PerformanceReport> ViewAgentPerformanceReportAsync(string agentId);
        Task<PerformanceReport> GenerateAgentPerformanceReportAsync(string agentId);
        Task<PerformanceReportViewModel> GetPerformanceReport(string userId);
    }
}
