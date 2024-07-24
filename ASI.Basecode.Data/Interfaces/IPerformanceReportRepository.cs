using ASI.Basecode.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Interfaces
{
    public interface IPerformanceReportRepository
    {
        Task AddPerformanceReportAsync(PerformanceReport performanceReport);
        Task UpdatePerformanceReportAsync(PerformanceReport performanceReport);
        Task<PerformanceReport> GetPerformanceReportByAgentIdAsync(string agentId);
    }
}
