using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Repositories
{
    public class PerformanceReportRepository : BaseRepository, IPerformanceReportRepository
    {

        public PerformanceReportRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            
        }

        public async Task AddPerformanceReportAsync(PerformanceReport performanceReport)
        {
            await this.GetDbSet<PerformanceReport>().AddAsync(performanceReport);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task UpdatePerformanceReportAsync(PerformanceReport performanceReport)
        {
            this.GetDbSet<PerformanceReport>().Update(performanceReport);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task<PerformanceReport> GetPerformanceReportByAgentIdAsync(string agentId)
            => await this.GetDbSet<PerformanceReport>().Where(r => r.UserId == agentId).FirstOrDefaultAsync();            
    }
}
