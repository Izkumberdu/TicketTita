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
                        .Where(t => !t.IsDeleted)
                        .Include(t => t.TeamMembers)
                            .ThenInclude(u => u.User)
                        .Include(t => t.TeamMembers)
                        .Include(t => t.TicketAssignments)
                            .ThenInclude(a => a.Agent)
                        .Include(t => t.Specialization);
        }

        public async Task<List<Team>> GetAllAsync() =>
            await GetTeamsWithIncludes().AsNoTracking().ToListAsync();

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
            return this.GetDbSet<Team>()
                        .Where(team => !team.IsDeleted)
                        .Select(team => new
                        {
                            team.TeamId,
                            team.Name,
                            TeamSpecialization = new CategoryType
                            {
                                CategoryTypeId = team.Specialization.CategoryTypeId,
                                CategoryName = team.Specialization.CategoryName
                            },
                            TeamMembers = team.TeamMembers
                                .Select(member => new
                                {
                                    member.UserId,
                                    member.User.Name
                                })
                        })
                        .AsEnumerable()
                        .Select(t => new Team
                        {
                            TeamId = t.TeamId,
                            Name = t.Name,
                            TeamMembers = t.TeamMembers.Select(m => new TeamMember
                            {
                                UserId = m.UserId,
                                User = new User
                                {
                                    Name = m.Name
                                }
                            })
                            .ToList(),
                            Specialization = t.TeamSpecialization
                        });
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
                            .ThenInclude(tm => tm.Team)
                        .Include(u => u.PerformanceReport)
                        .Include(u => u.ActivityLogs)
                        .Include(u => u.KnowledgeBaseArticles)
                        .Include(u => u.TicketAssignmentAgents)
                            .ThenInclude(ta => ta.Ticket)
                        .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<TeamMember> FindTeamMemberByIdAsync(string id) => 
            await this.GetDbSet<TeamMember>().Include(u => u.Team).FirstOrDefaultAsync(tm => tm.UserId == id);

        public async Task<bool> IsExistingTeamMember(string teamId, string agentId) =>
            await GetTeamsWithIncludes()
                .AnyAsync(t => t.TeamMembers.Any(tm => tm.UserId == agentId && tm.TeamId == teamId));

        public async Task<List<Ticket>> GetResolvedTicketsAssignedToTeamAsync(string teamId)
        {
            return await this.GetDbSet<Ticket>()
                        .Where(t => t.TicketAssignment.TeamId == teamId && t.StatusTypeId == "S3")
                        .Include(t => t.Feedback)
                            .ThenInclude(f => f.User)
                            .ThenInclude(u => u.PerformanceReport)
                        .ToListAsync();
        }

        public async Task<List<Ticket>> GetResolvedTicketsAssignedToAgentAsync(string agentId)
        {
            return await this.GetDbSet<Ticket>()
                .Include(t => t.TicketAssignment)
                .Include(t => t.Feedback)
                .Where(t => t.TicketAssignment != null && t.TicketAssignment.AgentId == agentId)
                .ToListAsync();
        }
    }
}
