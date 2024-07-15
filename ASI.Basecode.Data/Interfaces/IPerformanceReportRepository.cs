using ASI.Basecode.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Interfaces
{
    public interface IPerformanceReportRepository
    {
        Task<List<PerformanceReport>> GetAllAsync();
        Task<PerformanceReport> FindByUserIdAsync(string id);
        PerformanceReport FindByUserId(string id);
        void Add(PerformanceReport performanceReport);
        Task UpdateAsync(PerformanceReport performanceReport);
        Task DeleteAsync(PerformanceReport performanceReport);

        /*void DeleteById(string userId);*/
    }
}
