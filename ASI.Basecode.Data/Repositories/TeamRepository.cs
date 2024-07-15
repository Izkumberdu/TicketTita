using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Repositories
{
    public class TeamRepository : BaseRepository, ITeamRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeamRepository"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        public TeamRepository(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        private IQueryable<Team> GetTeamsWithIncludes()
        {
            return this.GetDbSet<Team>()
                        .Where(team => !team.IsDeleted)
                        .Include(f => f.TeamMembers)
                            .ThenInclude(u => u.User)
                        .Include(f => f.TicketAssignments)
                            .ThenInclude(a => a.Admin);
        }

        public async Task<List<Team>> GetAllAsync() =>
            await GetTeamsWithIncludes().ToListAsync();

        public async Task AddAsync(Team team)
        {
            await this.GetDbSet<Team>().AddAsync(team);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task UpdateAsync(Team team)
        {
            this.GetDbSet<Team>().Update(team);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(Team team)
        {
            team.IsDeleted = true;
            this.GetDbSet<Team>().Update(team);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task AddTeamMemberAsync(TeamMember teamMember)
        {
            await this.GetDbSet<TeamMember>().AddAsync(teamMember);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task RemoveTeamMemberAsync(TeamMember teamMember)
        {
            this.GetDbSet<TeamMember>().Remove(teamMember);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<Team>> GetAllStrippedAsync()
        {
            return await this.GetDbSet<Team>()
                        .Where(team => !team.IsDeleted)
                        .Select(team => new Team
                        {
                            TeamId = team.TeamId,
                            Name = team.Name
                        }).ToListAsync();
        }

        public async Task<IEnumerable<User>> GetAgentsAsync()
        {
            return await this.GetDbSet<User>()
                        .Include(a => a.TeamMember)
                        .Where(a => a.RoleId.Equals("Support Agent"))
                        .Select(agent => new User
                        {
                            UserId = agent.UserId,
                            Name = agent.Name,
                            Email = agent.Email,
                            TeamMember = agent.TeamMember
                        }).ToListAsync();
        }


        public async Task<Team> FindByIdAsync(string id) =>
            await GetTeamsWithIncludes().FirstOrDefaultAsync(t => t.TeamId == id);

        public async Task<User> FindAgentByIdAsync(string id)
        {
            return await this.GetDbSet<User>()
                        .Where(u => u.RoleId.Equals("Support Agent"))
                        .Include(u => u.CreatedByNavigation)
                        .Include(u => u.Role)
                        .Include(u => u.UpdatedByNavigation)
                        .Include(u => u.TeamMember)
                        .Include(u => u.ActivityLogs)
                        .Include(u => u.KnowledgeBaseArticles)
                        .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<TeamMember> FindTeamMemberByIdAsync(string id) => 
            await this.GetDbSet<TeamMember>().FirstOrDefaultAsync(tm => tm.UserId == id);

        public async Task<bool> IsExistingTeamMember(string teamId, string agentId) =>
            await GetTeamsWithIncludes()
                .AnyAsync(t => t.TeamMembers.Any(tm => tm.UserId == agentId && tm.TeamId == teamId));
    }
}
