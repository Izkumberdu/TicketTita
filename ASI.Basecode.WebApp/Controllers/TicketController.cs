using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.WebApp.Authentication;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.WebApp.Controllers
{
    [Route("ticket")]
    public partial class TicketController : ControllerBase<TicketController>
    {
        private readonly ITicketService _ticketService;
        private readonly IFeedbackService _feedbackService;
        private readonly INotificationService _notificationService;
        private readonly ITeamService _teamService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TicketController"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="ticketService">The ticket service.</param>
        /// <param name="tokenValidationParametersFactory">The token validation parameters factory.</param>
        /// <param name="tokenProviderOptionsFactory">The token provider options factory.</param>
        public TicketController(
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper,
            ITicketService ticketService,
            IFeedbackService feedbackService,
            ITeamService teamService,
            INotificationService notificationService,
            IUserPreferencesService userPreferences,
            TokenValidationParametersFactory tokenValidationParametersFactory,
            TokenProviderOptionsFactory tokenProviderOptionsFactory) : base(httpContextAccessor, loggerFactory, configuration, mapper, userPreferences)
        {
            this._ticketService = ticketService;
            this._feedbackService = feedbackService;
            this._notificationService = notificationService;
            this._teamService = teamService;
        }

        #region GET methods
        /// <summary>
        /// Shows all tickets
        /// </summary>
        /// <param name="sortBy">User defined, taken from the page</param>
        /// <param name="filterBy">User defined, taken from the page</param>
        /// <param name="filterValue">User defined, taken from the page</param>
        /// <returns>GetAll page</returns>
        [Authorize]
        [HttpGet]
        [Route("all")]
        public async Task<IActionResult> GetAll(string showOption, string sortBy, List<string> selectedFilters, string search, bool clearFilters = false, int pageIndex = 1, int pageSize = 5)
        {
            return await HandleExceptionAsync(async () =>
            {
                var tickets = await _ticketService.GetFilteredAndSortedTicketsAsync(showOption, sortBy, selectedFilters, search, clearFilters, pageIndex, pageSize);

                ViewData["Search"] = search;
                ViewData["SortBy"] = string.IsNullOrEmpty(sortBy) && !clearFilters ? UserTicketSortPreference : sortBy;
                ViewData["ShowOption"] = string.IsNullOrEmpty(showOption) && !clearFilters ? UserTicketViewPreference : showOption;
                ViewData["PageSize"] = pageSize;
                await PopulateViewBagAsync(selectedFilters);

                return View("ViewAll", tickets);
            }, "GetAll");
        }

        /// <summary>
        /// Shows a specific ticket
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        /// <returns>GetTicket page</returns>
        [HttpGet]
        [Authorize]
        [Route("view/{id}")]
        public async Task<IActionResult> GetTicket(string id, string notificationId, string showModal = null)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Ticket ID is invalid!";
                    return RedirectToAction("GetAll");
                }

                var ticket = await _ticketService.GetFilteredTicketByIdAsync(id);
                ticket.ActivityLogs = await _ticketService.GetActivityLogsByTicketIdAsync(id);
                if (ticket == null)
                {
                    TempData["ErrorMessage"] = "Ticket not found!";
                    return RedirectToAction("GetAll");
                }
                if (ticket != null)
                {
                    ViewBag.ShowModal = showModal;
                    ViewBag.UserId = UserId;
                    if (!string.IsNullOrEmpty(notificationId))
                    {
                        _notificationService.MarkNotificationAsRead(notificationId);
                    }
                    return View("ViewTicket", ticket);
                }
                return RedirectToAction("GetAll");
            }, "GetTicket");
        }
        #endregion GET methods

        #region POST methods
        /// <summary>
        /// Allows the user to create a ticket
        /// </summary>
        /// <param name="model"></param>
        /// <returns>GetAll page</returns>
        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> Create(TicketViewModel model)
        {
            return await HandleExceptionAsync(async () =>
            {
                if(model != null || string.IsNullOrEmpty(UserId))
                {
                    await _ticketService.AddAsync(model, UserId);
                    TempData["SuccessMessage"] = "New ticket created successfully!";
                    return Json(new { success = true });
                }
                TempData["ErrorMessage"] = "An error occurred while creating a new ticket. Please try again.";
                return Json(new { success = false });
            }, "Create");
        }

        /// <summary>
        /// Allows the user to update a ticket
        /// </summary>
        /// <param name="model"></param>
        /// <returns>GetAll page</returns>
        [HttpPost]
        [Authorize]
        [Route("update")]
        public async Task<IActionResult> Update(TicketViewModel model)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (model != null)
                {
                    await _ticketService.UpdateAsync(model,4);
                    TempData["SuccessMessage"] = "Ticket updated successfully!";
                    return Json(new { success = true });
                }
                TempData["ErrorMessage"] = "An error occurred while updating the ticket. Please try again.";
                return Json(new { success = false });
            }, "Update");
        }

        /// <summary>
        /// Allows the user to delete a ticket
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Json success status</returns>
        [HttpPost]
        [Authorize]
        [Route("delete")]
        public async Task<IActionResult> Delete(string id)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (!string.IsNullOrEmpty(id))
                {
                    await _ticketService.DeleteAsync(id);
                    TempData["SuccessMessage"] = "Ticket deleted successfully!";
                    return Json(new { success = true });
                }
                TempData["ErrorMessage"] = "An error occurred while deleting the ticket. Please try again.";
                return Json(new { success = false });
            }, "Delete");
        }
        #endregion POST methods

        /// <summary>
        /// Populates the view bag with the priority, status, category types and users
        /// Used for dropdowns in the view
        /// </summary>
        private async Task PopulateViewBagAsync(List<string> selectedFilters)
        {
            var priorityTypes = await _ticketService.GetPriorityTypesAsync();
            var statusTypes = await _ticketService.GetStatusTypesAsync();
            var categoryTypes = await _ticketService.GetCategoryTypesAsync();
            var users = await _ticketService.UserGetAllAsync();
            var teams = await _teamService.GetAllStrippedAsync();

            ViewBag.PriorityTypes = priorityTypes.OrderBy(pt => pt.PriorityTypeId).ToList();
            ViewBag.StatusTypes = statusTypes.OrderBy(st => st.StatusTypeId).ToList();
            ViewBag.CategoryTypes = categoryTypes.OrderBy(ct => ct.CategoryTypeId).ToList();
            ViewBag.Users = users.Where(u => u.RoleId == "Employee" && u.Tickets.Any()).OrderBy(u => u.Name).Distinct().ToList();
            ViewBag.Agents = users.Where(u => u.RoleId == "Support Agent").OrderBy(u => u.Name).Distinct().ToList();
            ViewBag.Teams = teams.OrderBy(t => t.Name).ToList();
            ViewBag.SelectedFilters = selectedFilters;
        }
    }
}
