using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.Services.Services;
using ASI.Basecode.WebApp.Authentication;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASI.Basecode.WebApp.Controllers
{
    public class TeamController : ControllerBase<TeamController>
    {
        private readonly ITeamService _teamService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamController"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="teamService">The team service.</param>
        /// <param name="tokenValidationParametersFactory">The token validation parameters factory.</param>
        /// <param name="tokenProviderOptionsFactory">The token provider options factory.</param>
        public TeamController(
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper,
            ITeamService teamService,
            TokenValidationParametersFactory tokenValidationParametersFactory,
            TokenProviderOptionsFactory tokenProviderOptionsFactory) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            this._teamService = teamService;
        }

        /// <summary>Show all feedback</summary>
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> ViewAll(string sortOrder)
        {
            return await HandleExceptionAsync(async () =>
            {
                var teams = await _teamService.GetAllAsync();

                return View(teams);
            }, "ViewAll");
        }

        #region GET Methods
        [HttpGet]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> ViewTeam(string id, string showModal = null)
        {
            return await HandleExceptionAsync(async () =>
            {
                var team = await _teamService.GetTeamByIdAsync(id);
                var agents = await _teamService.GetAgentsAsync();
                var teams = await _teamService.GetAllStrippedAsync();

                ViewBag.ShowModal = showModal;
                ViewBag.AssignedAgents = agents.Where(x => x.TeamMember?.TeamId == id).ToList();
                ViewBag.UnassignedAgents = agents.Where(x => x.TeamMember == null).ToList();
                ViewBag.Teams = teams.Where(x => x.TeamId != id).ToList();

                return View(team);
            }, "ViewTeam");
        }

        [HttpGet]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Create() => 
            await HandleExceptionAsync(async () => View(new TeamViewModel()), "Create");
        #endregion GET Methods

        #region POST Methods
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Create(TeamViewModel model)
        {
            return await HandleExceptionAsync(async () =>
            {
                await _teamService.AddAsync(model);

                TempData["CreateMessage"] = "Created Successfully";
                return RedirectToAction("ViewAll");
            }, "Create");
        }

        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Edit(TeamViewModel model)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (ModelState.IsValid)
                {
                    await _teamService.UpdateAsync(model);
                    TempData["SuccessMessage"] = "Team updated successfully!";
                    return Json(new { success = true });
                }
                TempData["ErrorMessage"] = "An error occurred while updating the team. Please try again.";
                return Json(new { success = false });
            }, "Edit");
        }

        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (ModelState.IsValid)
                {
                    await _teamService.DeleteAsync(id);
                    return Json(new { success = true });
                }
                return Json(new { success = false });
            }, "Delete");
        }


        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> AssignAgent(string teamId, string agentId)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (ModelState.IsValid)
                {
                    await _teamService.AddTeamMemberAsync(teamId, agentId);
                    TempData["SuccessMessage"] = "Agent assigned successfully!";
                    return Json(new { success = true });
                }
                TempData["ErrorMessage"] = "An error occurred while assigning the agent. Please try again.";
                return Json(new { success = false });
            }, "AssignAgent");
        }

        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> ReassignAgent(string oldTeamId, string newTeamId, string agentId)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (ModelState.IsValid)
                {
                    await _teamService.RemoveTeamMemberAsync(oldTeamId, agentId);
                    if (!string.IsNullOrEmpty(newTeamId))
                    {
                        await _teamService.AddTeamMemberAsync(newTeamId, agentId);
                    }

                    TempData["SuccessMessage"] = string.IsNullOrEmpty(newTeamId) ? 
                        "Agent unassigned successfully!" : "Agent reassigned successfully!";
                    return Json(new { success = true });
                }
                TempData["ErrorMessage"] = "An error occurred while reassigning the agent. Please try again.";
                return Json(new { success = false });
            }, "ReassignAgent");
        }
        #endregion POST Methods
    }
}
