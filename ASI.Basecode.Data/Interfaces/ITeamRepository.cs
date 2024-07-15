using ASI.Basecode.Data.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Interfaces
{
    public interface ITeamRepository
    {
        Task<List<Team>> GetAllAsync();
        Task<IEnumerable<Team>> GetAllStrippedAsync();
        Task AddAsync(Team team);
        Task UpdateAsync(Team team);
        Task DeleteAsync(Team team);
        Task AddTeamMemberAsync(TeamMember teamMember);
        Task RemoveTeamMemberAsync(TeamMember teamMember);
        Task<IEnumerable<User>> GetAgentsAsync();
        Task<Team> FindByIdAsync(string id);
        Task<User> FindAgentByIdAsync(string id);
        Task<TeamMember> FindTeamMemberByIdAsync(string id);
        Task<bool> IsExistingTeamMember(string teamId, string agentId);
    }
}
