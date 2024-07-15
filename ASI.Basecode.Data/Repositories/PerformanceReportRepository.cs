using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Repositories
{
    public class PerformanceReportRepository : BaseRepository, IPerformanceReportRepository
    {
        public PerformanceReportRepository(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        private IQueryable<PerformanceReport> GetPerformanceReportsWithIncludes()
        {
            return this.GetDbSet<PerformanceReport>()
                        .Include(pr => pr.TeamMembers)
                        .ThenInclude(tm => tm.Team)
                        .Include(pr => pr.TeamMembers)
                        .ThenInclude(tm => tm.User);
        }

        public async Task<List<PerformanceReport>> GetAllAsync()
        {
            return await GetPerformanceReportsWithIncludes().ToListAsync();
        }

        public async Task<PerformanceReport> FindByUserIdAsync(string id)
        {
            return await GetPerformanceReportsWithIncludes().FirstOrDefaultAsync(pr => pr.UserId == id);
        }

        public PerformanceReport FindByUserId(string id)
        {
            return GetPerformanceReportsWithIncludes().FirstOrDefault(pr => pr.UserId == id);
        }

        public void Add(PerformanceReport performanceReport)
        {
            this.GetDbSet<PerformanceReport>().Add(performanceReport);
            UnitOfWork.SaveChanges();
        }

        public async Task UpdateAsync(PerformanceReport performanceReport)
        {
            this.GetDbSet<PerformanceReport>().Update(performanceReport);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(PerformanceReport performanceReport)
        {
            this.GetDbSet<PerformanceReport>().Remove(performanceReport);
            await UnitOfWork.SaveChangesAsync();
        }

        /*public void DeleteById(string userId)
        {
            var reportToDelete = this.GetDbSet<PerformanceReport>().FirstOrDefault(x => x.UserId.Equals(userId));
            this.GetDbSet<PerformanceReport>().Remove(reportToDelete);
        }*/

    }
}
