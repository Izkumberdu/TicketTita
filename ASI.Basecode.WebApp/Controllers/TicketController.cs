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
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASI.Basecode.WebApp.Controllers
{
    public class TicketController : ControllerBase<TicketController>
    {
        private readonly ITicketService _ticketService;
        private readonly INotificationService _notificationService;

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
            INotificationService notificationService,
            TokenValidationParametersFactory tokenValidationParametersFactory,
            TokenProviderOptionsFactory tokenProviderOptionsFactory) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            this._ticketService = ticketService;
            this._notificationService = notificationService;
        }

        #region GET methods
        /// <summary>
        /// Shows all tickets
        /// </summary>
        /// <param name="sortBy">User defined, taken from the page</param>
        /// <param name="filterBy">User defined, taken from the page</param>
        /// <param name="filterValue">User defined, taken from the page</param>
        /// <returns>ViewAll page</returns>
        [Authorize]
        public async Task<IActionResult> ViewAll(string sortBy, string filterBy, string filterValue)
        {
            return await HandleExceptionAsync(async () =>
            {
                await PopulateViewBagAsync();
                var tickets = await _ticketService.GetFilteredAndSortedTicketsAsync(sortBy, filterBy, filterValue);

                ViewData["FilterBy"] = filterBy;
                ViewData["FilterValue"] = filterValue;
                ViewData["SortBy"] = sortBy;

                return View(tickets);
            }, "ViewAll");
        }

        /// <summary>
        /// Shows a specific ticket
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        /// <returns>ViewTicket page</returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ViewTicket(string id, string notificationId, string showModal = null)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (string.IsNullOrEmpty(id)) return RedirectToAction("ViewAll");
                var ticket = await _ticketService.GetTicketByIdAsync(id);
                if (ticket == null) return RedirectToAction("ViewAll");

                if (!string.IsNullOrEmpty(notificationId))
                {
                    _notificationService.MarkNotificationAsRead(notificationId);
                }

                ViewBag.ShowModal = showModal;
                return View(ticket);
            }, "ViewTicket");
        }


        /// <summary>
        /// Shows the page to create a ticket
        /// </summary>
        /// <returns>Create page</returns>
        [HttpGet]
        [Authorize(Policy = "Employee")]
        public async Task<IActionResult> Create() =>
            await HandleExceptionAsync(async () => View(await _ticketService.InitializeModelAsync("default")), "Create");

        /// <summary>
        /// Shows the page to update the status of a ticket
        /// </summary>
        /// <returns>UpdateStatus page</returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UpdateStatus() =>
            await HandleExceptionAsync(async () => View(await _ticketService.InitializeModelAsync("status")), "UpdateStatus");

        /// <summary>
        /// Shows the page to update the priority of a ticket
        /// </summary>
        /// <returns>UpdatePriority page</returns>
        [HttpGet]
        [Authorize(Policy = "AdminOrAgent")]
        public async Task<IActionResult> UpdatePriority() =>
            await HandleExceptionAsync(async () => View(await _ticketService.InitializeModelAsync("priority")), "UpdatePriority");

        /// <summary>
        /// Shows the page to assign a ticket
        /// </summary>
        /// <returns>AssignTicket page</returns>
        [HttpGet]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> AssignTicket() =>
            await HandleExceptionAsync(async () => View(await _ticketService.InitializeModelAsync("assign")), "AssignTicket");

        /// <summary>
        /// Shows the page to reassign a ticket
        /// </summary>
        /// <returns>ReassignTicket page</returns>
        [HttpGet]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> ReassignTicket() =>
            await HandleExceptionAsync(async () => View(await _ticketService.InitializeModelAsync("reassign")), "ReassignTicket");

        /// <summary>
        /// Shows the page to edit a ticket
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Edit page</returns>
        [HttpGet]
        [Authorize(Policy = "Employee")]
        public async Task<IActionResult> Edit(string id)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (string.IsNullOrEmpty(id)) return RedirectToAction("ViewAll");
                var ticket = await _ticketService.GetTicketByIdAsync(id);
                if (ticket == null) return RedirectToAction("ViewAll");
                return View(ticket);
            }, "Edit");
        }

        /// <summary>
        /// Fetches the ticket details to be used in the view
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Json containing ticket details</returns>
        [HttpGet]
        [Authorize]
        public async Task<JsonResult> GetTicketDetails(string id)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (string.IsNullOrEmpty(id)) return Json(null);
                var ticketDetails = await _ticketService.GetTicketDetailsAsync(id);
                if (ticketDetails == null) return Json(null);
                return Json(ticketDetails);
            }, "GetTicketDetails");
        }

        /// <summary>
        /// Allows the user to download the attachment
        /// </summary>
        /// <param name="id">Attachment identifier</param>
        /// <returns>The file to download</returns>
        [HttpGet]
        [Authorize]
        public async Task<FileResult> DownloadAttachment(string id)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (string.IsNullOrEmpty(id)) return null;
                var attachment = await _ticketService.GetAttachmentByTicketIdAsync(id);
                if (attachment == null) return null;
                return File(attachment.Content, "application/octet-stream", attachment.Name);
            }, "DownloadAttachment");
        }
        #endregion GET methods

        #region POST methods
        /// <summary>
        /// Allows the user to create a ticket
        /// </summary>
        /// <param name="model"></param>
        /// <returns>ViewAll page</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(TicketViewModel model)
        {
            return await HandleExceptionAsync(async () =>
            {
                string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                if (string.IsNullOrEmpty(userId)) return RedirectToAction("ViewAll");

                await _ticketService.AddAsync(model, userId);

                TempData["CreateMessage"] = "Created Successfully";
                return RedirectToAction("ViewAll");
            }, "Create");
        }

        /// <summary>
        /// Allows the user to edit a ticket
        /// </summary>
        /// <param name="model"></param>
        /// <returns>ViewAll page</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(TicketViewModel model)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (model == null) return RedirectToAction("ViewAll");

                await _ticketService.UpdateAsync(model,4);
                return RedirectToAction("ViewAll");
            }, "Edit");
        }

        /// <summary>
        /// Allows the user to update the ticket status
        /// </summary>
        /// <param name="model"></param>
        /// <returns>ViewTicket page</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateStatus(TicketViewModel model)
        {
            return await HandleExceptionAsync(async () =>
            {
                var ticket = await _ticketService.GetTicketByIdAsync(model.TicketId);
                if (ticket == null) return RedirectToAction("ViewAll");

                ticket.StatusTypeId = model.StatusTypeId;
                await _ticketService.UpdateAsync(ticket, 3);
                return RedirectToAction("ViewTicket", new { id = model.TicketId });
            }, "UpdateStatus");
        }

        /// <summary>
        /// Allows the user to update the ticket priority
        /// </summary>
        /// <param name="model"></param>
        /// <returns>ViewTicket page</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdatePriority(TicketViewModel model)
        {
            return await HandleExceptionAsync(async () =>
            {
                var ticket = await _ticketService.GetTicketByIdAsync(model.TicketId);
                if (ticket == null) return RedirectToAction("ViewAll");

                ticket.PriorityTypeId = model.PriorityTypeId;
                await _ticketService.UpdateAsync(ticket, 2);
                return RedirectToAction("ViewTicket", new { id = model.TicketId });
            }, "UpdatePriority");
        }

        /// <summary>
        /// Allows the user to delete a ticket
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Json success status</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (string.IsNullOrEmpty(id)) return Json(new { success = false });
                await _ticketService.DeleteAsync(id);
                return Json(new { success = true });
            }, "Delete");
        }

        /// <summary>
        /// Allows the user to remove an attachment from a ticket
        /// </summary>
        /// <param name="ticketId">Ticket identifier</param>
        /// <param name="attachmentId">Attachment identifier</param>
        /// <returns>Edit page</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveAttachment(string ticketId, string attachmentId)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (string.IsNullOrEmpty(ticketId) || string.IsNullOrEmpty(attachmentId)) return RedirectToAction("ViewAll");
                var ticket = await _ticketService.GetTicketByIdAsync(ticketId);
                if (ticket == null) return RedirectToAction("ViewAll");

                await _ticketService.RemoveAttachmentAsync(attachmentId);
                ticket.Attachment = null;

                await _ticketService.UpdateAsync(ticket, 4);
                return RedirectToAction("Edit", new { id = ticketId });
            }, "RemoveAttachment");
        }

        /// <summary>
        /// Allows the user to assign a ticket to another user
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>ViewTicket page</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AssignTicket(TicketViewModel model)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (model == null || model.Agent == null) return RedirectToAction("ViewAll");
                await _ticketService.AddTicketAssignmentAsync(model, false);
                return RedirectToAction("ViewTicket", new { id = model.TicketId });
            }, "AssignTicket");
        }

        /// <summary>
        /// Allows the user to reassign a ticket to another user
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>ViewTicket page</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ReassignTicket(TicketViewModel model)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (model == null) return RedirectToAction("ViewAll");
                var assignment = await _ticketService.GetAssignmentByTicketIdAsync(model.TicketId);
                if (assignment != null && model.Agent.UserId != _ticketService.ExtractAgentId(assignment.AssignmentId))
                {
                    await _ticketService.RemoveAssignmentAsync(assignment.TicketId);
                    if (model.Agent != null && !model.Agent.UserId.Equals("remove"))
                    {
                        await _ticketService.AddTicketAssignmentAsync(model, true);
                    }
                }
                return RedirectToAction("ViewTicket", new { id = model.TicketId });
            }, "ReassignTicket");
        }
        #endregion POST methods

        /// <summary>
        /// Populates the view bag with the priority, status, category types and users
        /// Used for dropdowns in the view
        /// </summary>
        private async Task PopulateViewBagAsync()
        {
            var usersWithTickets = await _ticketService.GetUserIdsWithTicketsAsync();
            ViewBag.PriorityTypes = (await _ticketService.GetPriorityTypesAsync())
                                    .OrderBy(pt => pt.PriorityTypeId).Select(pt => pt.PriorityName)
                                    .ToList();
            ViewBag.StatusTypes = (await _ticketService.GetStatusTypesAsync())
                                    .OrderBy(st => st.StatusTypeId).Select(st => st.StatusName)
                                    .ToList();
            ViewBag.CategoryTypes = (await _ticketService.GetCategoryTypesAsync())
                                    .OrderBy(ct => ct.CategoryTypeId).Select(ct => ct.CategoryName)
                                    .ToList();
            ViewBag.Users = (await _ticketService.UserGetAllAsync())
                                    .Where(u => u.RoleId == "Employee" && usersWithTickets.Contains(u.UserId))
                                    .OrderBy(u => u.Name).Select(u => u.Name).Distinct().ToList();
        }
    }
}
