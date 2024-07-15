using System;
using System.Collections.Generic;
using ASI.Basecode.Data.Models;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IPerformanceReportService
    {
        Task UpdatePerformanceReportAsync(Team team);
        public PerformanceReportViewModel GetPerformanceReportByUserId(string userId);
    }
}
