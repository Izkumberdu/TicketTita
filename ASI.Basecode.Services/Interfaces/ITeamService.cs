using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Interfaces
{
    public interface ITeamService
    {
        Task<IEnumerable<TeamViewModel>> GetAllAsync();
        Task AddAsync(TeamViewModel team);
        Task UpdateAsync(TeamViewModel team);
        Task DeleteAsync(string id);
        Task AddTeamMemberAsync(string teamId, string agentId);
        Task RemoveTeamMemberAsync(string teamId, string agentId);
        Task<TeamViewModel> GetTeamByIdAsync(string id);
        Task<IEnumerable<Team>> GetAllStrippedAsync();
        Task<IEnumerable<User>> GetAgentsAsync();
    }
}
