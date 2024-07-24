using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading.Tasks;
using static ASI.Basecode.Services.Exceptions.TeamExceptions;
using static ASI.Basecode.Services.Exceptions.TicketExceptions;

namespace ASI.Basecode.Services.Services
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _repository;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly ITicketService _ticketService;
        private readonly IPerformanceReportRepository _performanceReportRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamService"/> class.
        /// </summary>
        /// <param name="repository">The repository</param>
        /// <param name="mapper">The mapper</param>
        /// <param name="logger">The logger</param>
        /// <param name="httpContextAccessor">The HTTP context accessor</param>
        public TeamService(
            ITeamRepository repository,
            IMapper mapper,
            INotificationService notificationService,
            ITicketService ticketService,
            ILogger<TeamService> logger,
            IHttpContextAccessor httpContextAccessor,
            IPerformanceReportRepository performanceReportRepository)
        {
            _repository = repository;
            _mapper = mapper;
            _notificationService = notificationService;
            _ticketService = ticketService;
            _performanceReportRepository = performanceReportRepository;
        }

        /// <summary>
        /// Calls the repository to add a new team.
        /// </summary>
        /// <param name="team">The team.</param>
        public async Task AddAsync(TeamViewModel team)
        {
            string[] validSpecializationIds = { "C1", "C2", "C3", "C4" };
            if (!validSpecializationIds.Contains(team.SpecializationId))
                throw new TeamException("Invalid specialization.", team.TeamId);

            var teams = await _repository.GetAllAsync();
            if (teams.Any(t => t.Name.ToLower() == team.Name.ToLower()))
                throw new TeamException("Team name already exists.");

            if (team != null)
            {
                var newTeam = new Team
                {
                    TeamId = Guid.NewGuid().ToString(),
                    Name = team.Name,
                    Description = team.Description,
                    SpecializationId = team.SpecializationId
                };

                await _repository.AddAsync(newTeam);
            }
        }

        /// <summary>
        /// Calls the repository to update a team.
        /// </summary>
        /// <param name="team">The team.</param>
        public async Task UpdateAsync(TeamViewModel team)
        {
            string[] validSpecializationIds = { "C1", "C2", "C3", "C4" };
            if (!validSpecializationIds.Contains(team.SpecializationId))
                throw new TeamException("Invalid specialization.", team.TeamId);

            var existingTeam = await _repository.FindByIdAsync(team.TeamId);
            if (existingTeam != null)
            {
                bool hasChanges = existingTeam.Name != team.Name ||
                                  existingTeam.Description != team.Description ||
                                  existingTeam.SpecializationId != team.SpecializationId;

                if (!hasChanges)
                    throw new TeamException("No changes were made to the team.", team.TeamId); 

                var teamMembers = existingTeam.TeamMembers;
                var ticketAssignments = existingTeam.TicketAssignments;
                var specialization = existingTeam.Specialization;

                _mapper.Map(team, existingTeam);

                existingTeam.TeamMembers = teamMembers;
                existingTeam.TicketAssignments = ticketAssignments;
                existingTeam.Specialization = specialization;

                await _repository.UpdateAsync(existingTeam);
            }
        }

        /// <summary>
        /// Calls the repository to delete a team.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public async Task DeleteAsync(string id)
        {
            var team = await _repository.FindByIdAsync(id);
            var teamMembers = team.TeamMembers.ToList();
            var ticketAssignments = team.TicketAssignments.ToList();

            if(ticketAssignments.Any(t => t.Ticket.ResolvedDate == null))
            {
                var message = "Cannot delete team with unresolved tickets, reassign or unassign tickets before deleting.";
                throw new TeamException(message);
            }
            if (teamMembers.Any())
            {
                var message = "Cannot delete team with members, unassign or reassign them before deleting.";
                throw new TeamException(message);
            }

            await _repository.DeleteAsync(team);
        }

        /// <summary>
        /// Adds a team member to team.
        /// </summary>
        /// <param name="teamId">The team identifier.</param>
        /// <param name="agentId">The agent identifier.</param>
        public async Task AddTeamMemberAsync(string teamId, string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
                throw new TeamException("Please select an agent to add to the team.", teamId);

            bool existingTeamMember = await _repository.IsExistingTeamMember(teamId, agentId);
            if (!existingTeamMember)
            {
                var team = await _repository.FindByIdAsync(teamId);
                var agent = await _repository.FindAgentByIdAsync(agentId);

                var model = new TicketViewModel();
                foreach (var ticketAssignment in agent.TicketAssignmentAgents)
                {
                    if (ticketAssignment.Ticket.StatusTypeId == "S3") continue;
                    model.AgentId = agentId;
                    model.TicketId = ticketAssignment.TicketId;
                    model.TeamId = teamId;
                    await _ticketService.UpdateAssignmentAsync(model);
                }

                var teamMember = new TeamMember
                {
                    TeamId = team.TeamId,
                    UserId = agentId,
                    Team = team,
                    User = agent
                };

                team.TeamMembers.Add(teamMember);
                await _repository.AddTeamMemberAsync(teamMember);
            }
        }

        /// <summary>
        /// Removes a team member from a team.
        /// </summary>
        /// <param name="teamId">The team identifier.</param>
        /// <param name="agentId">The agent identifier.</param>
        public async Task RemoveTeamMemberAsync(string teamId, string agentId)
        {
            bool existingTeamMember = await _repository.IsExistingTeamMember(teamId, agentId);
            if (existingTeamMember)
            {
                var team = await _repository.FindByIdAsync(teamId);
                var agent = await _repository.FindAgentByIdAsync(agentId);
                var teamMember = await _repository.FindTeamMemberByIdAsync(agentId);

                var model = new TicketViewModel();
                foreach(var ticketAssignment in agent.TicketAssignmentAgents)
                {
                    if (ticketAssignment.Ticket.StatusTypeId == "S3") continue;
                    model.AgentId = "no_agent";
                    model.TicketId = ticketAssignment.TicketId;
                    model.TeamId = ticketAssignment.TeamId;
                    await _ticketService.UpdateAssignmentAsync(model);
                }

                agent.TeamMember = null;
                team.TeamMembers.Remove(teamMember);
                await _repository.RemoveTeamMemberAsync(teamMember);
            }
        }

        #region Get Methods        
        /// <summary>
        /// Calls the repository to get all teams.
        /// </summary>
        /// <returns>IEnumerable TeamViewModel</returns>
        public async Task<PaginatedList<TeamViewModel>> GetAllAsync(string sortBy, string filterBy, int pageIndex, int pageSize)
        {
            var teams = _mapper.Map<List<TeamViewModel>>(await _repository.GetAllAsync());

            if (!string.IsNullOrEmpty(filterBy))
            {
                teams = teams.Where(team => team.Name.Contains(filterBy, StringComparison.OrdinalIgnoreCase) ||
                                   (team.Specialization.CategoryName.Contains(filterBy, StringComparison.OrdinalIgnoreCase)))
                             .ToList();
            }
            
            teams = sortBy switch
            {
                "name_desc" => teams.OrderByDescending(t => t.Name).ToList(),
                "specialization_desc" => teams.OrderByDescending(t => t.Specialization.CategoryName).ToList(),
                "specialization" => teams.OrderBy(t => t.Specialization.CategoryName).ToList(),
                "agents_desc" => teams.OrderByDescending(t => t.TeamMembers?.Count() ?? 0).ToList(),
                "agents" => teams.OrderBy(t => t.TeamMembers?.Count() ?? 0).ToList(),
                "active_desc" => teams.OrderByDescending(t => t.TicketAssignments?.Count(ta => ta.Ticket?.ResolvedDate == null) ?? 0).ToList(),
                "active" => teams.OrderBy(t => t.TicketAssignments?.Count(ta => ta.Ticket?.ResolvedDate == null) ?? 0).ToList(),
                "inactive_desc" => teams.OrderByDescending(t => t.TicketAssignments?.Count(ta => ta.Ticket?.ResolvedDate != null) ?? 0).ToList(),
                "inactive" => teams.OrderBy(t => t.TicketAssignments?.Count(ta => ta.Ticket?.ResolvedDate != null) ?? 0).ToList(),
                //"completion_desc" => teams.OrderByDescending().ToList(), // TODO: completion time
                //"completion" => teams.OrderBy().ToList(), // TODO: completion time
                //"rating_desc" => teams.OrderByDescending().ToList(), // TODO: rating
                //"rating" => teams.OrderBy().ToList(), // TODO: rating
                _ => teams.OrderBy(t => t.Name).ToList(),
            };

            var count = teams.Count;
            var items = teams.Skip((pageIndex - 1) * pageSize).Take(pageSize);

            return new PaginatedList<TeamViewModel>(items, count, pageIndex, pageSize);
        }

        /// <summary>
        /// Calls the repository to get all teams with just the necessary attributes.
        /// </summary>
        /// <returns>IEnumerable Team</returns>
        public async Task<IEnumerable<Team>> GetAllStrippedAsync() =>
            await _repository.GetAllStrippedAsync();

        /// <summary>
        /// Calls the repository to find a team by its identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>TeamViewModel</returns>
        public async Task<TeamViewModel> GetTeamByIdAsync(string id)
        {
            var team = await _repository.FindByIdAsync(id);
            var teamViewModel = _mapper.Map<TeamViewModel>(team);

            return teamViewModel;
        }

        /// <summary>
        /// Calls the repository to get all agents.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<User>> GetAgentsAsync() =>
            await _repository.GetAgentsAsync();
        #endregion Get Methods
    }
}
