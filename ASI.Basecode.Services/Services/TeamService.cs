using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TeamService> _logger;
        private readonly INotificationService _notificationService;

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
            ILogger<TeamService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        public async Task AddAsync(TeamViewModel team)
        {
            var teams = await _repository.GetAllAsync();
            if (teams.Any(t => t.Name.ToLower() == team.Name.ToLower()))
            {
                LogError("AddAsync", "Team name already exists.");
                throw new TeamNameAlreadyExistsException("Team name already exists.");
            }

            if (team != null)
            {
                var newTeam = new Team
                {
                    TeamId = Guid.NewGuid().ToString(),
                    Name = team.Name,
                    Description = team.Description,
                };

                await _repository.AddAsync(newTeam);
            }
        }

        public async Task UpdateAsync(TeamViewModel team)
        {
            var existingTeam = await _repository.FindByIdAsync(team.TeamId);
            if (existingTeam != null)
            {
                bool hasChanges = existingTeam.Name != team.Name ||
                                  existingTeam.Description != team.Description;

                if (!hasChanges)
                {
                    throw new NoChangesException("No changes were made to the team.", team.TeamId);
                }

                var teamMembers = existingTeam.TeamMembers;
                var ticketAssignments = existingTeam.TicketAssignments;

                _mapper.Map(team, existingTeam);

                existingTeam.TeamMembers = teamMembers;
                existingTeam.TicketAssignments = ticketAssignments;

                await _repository.UpdateAsync(existingTeam);
            }
        }

        public async Task DeleteAsync(string id)
        {
            var team = await _repository.FindByIdAsync(id);
            var teamMembers = team.TeamMembers.ToList();
            var ticketAssignments = team.TicketAssignments.ToList();

            if(ticketAssignments.Any(t => t.Ticket.ResolvedDate == null))
            {
                var message = "Cannot delete team with unresolved tickets, reassign or unassign tickets before deleting.";
                LogError("DeleteAsync", message);
                throw new TeamHasUnresolvedTicketsException(message);
            }
            if (teamMembers.Any())
            {
                var message = "Cannot delete team with members, unassign or reassign them before deleting.";
                LogError("DeleteAsync", message);
                throw new TeamHasMembersException(message);
            }

            await _repository.DeleteAsync(team);
        }

        public async Task AddTeamMemberAsync(string teamId, string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                _logger.LogError("No agent selected.");
                throw new NoAgentSelectedException("Please select an agent to add to the team.", teamId);
            }

            bool existingTeamMember = await _repository.IsExistingTeamMember(teamId, agentId);
            if (!existingTeamMember)
            {
                var team = await _repository.FindByIdAsync(teamId);
                var agent = await _repository.FindAgentByIdAsync(agentId);
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

        public async Task RemoveTeamMemberAsync(string teamId, string agentId)
        {
            bool existingTeamMember = await _repository.IsExistingTeamMember(teamId, agentId);
            if (existingTeamMember)
            {
                var team = await _repository.FindByIdAsync(teamId);
                var agent = await _repository.FindAgentByIdAsync(agentId);
                var teamMember = await _repository.FindTeamMemberByIdAsync(agentId);

                agent.TeamMember = null;
                team.TeamMembers.Remove(teamMember);
                await _repository.RemoveTeamMemberAsync(teamMember);
            }
        }

        #region Get Methods
        public async Task<IEnumerable<TeamViewModel>> GetAllAsync()
        {
            var teams = await _repository.GetAllAsync();
            var teamViewModels = _mapper.Map<IEnumerable<TeamViewModel>>(teams.OrderBy(t => t.Name));

            return teamViewModels;
        }

        public async Task<IEnumerable<Team>> GetAllStrippedAsync() =>
            await _repository.GetAllStrippedAsync();


        public async Task<TeamViewModel> GetTeamByIdAsync(string id)
        {
            var team = await _repository.FindByIdAsync(id);
            var teamViewModel = _mapper.Map<TeamViewModel>(team);

            return teamViewModel;
        }

        public async Task<IEnumerable<User>> GetAgentsAsync() =>
            await _repository.GetAgentsAsync();
        #endregion Get Methods

        #region Logging methods
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="methodName">The method where this error was logged</param>
        /// <param name="errorMessage">The error message</param>
        private void LogError(string methodName, string errorMessage)
        {
            _logger.LogError($"Ticket Service {methodName} : {errorMessage}");
        }
        #endregion
    }
}
