using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Data.Repositories;
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
using static ASI.Basecode.Services.Exceptions.TicketExceptions;

namespace ASI.Basecode.Services.Services
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _repository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TicketService> _logger;
        private readonly INotificationService _notificationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TicketService"/> class.
        /// </summary>
        /// <param name="repository">The repository</param>
        /// <param name="mapper">The mapper</param>
        /// <param name="logger">The logger</param>
        /// <param name="httpContextAccessor">The HTTP context accessor</param>
        public TicketService(
            ITicketRepository repository,
            IMapper mapper,
            INotificationService notificationService,
            ILogger<TicketService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        #region Ticket CRUD Operations
        /// <summary>
        /// Calls the repository to add a new ticket.
        /// </summary>
        /// <param name="ticket">Ticket identifier</param>
        /// <param name="userId">User identifier</param>
        public async Task AddAsync(TicketViewModel ticket, string userId)
        {
            if (await IsDuplicateTicketAsync(ticket, userId))
            {
                LogError("AddAsync", "Duplicate ticket detected.");
                throw new DuplicateTicketException("A similar ticket already exists.");
            }

            if (ticket != null)
            {
                if (ticket.File != null)
                    await HandleAttachmentAsync(ticket);

                var newTicket = _mapper.Map<Ticket>(ticket);
                newTicket.CreatedDate = DateTime.Now;
                newTicket.UserId = userId;

                AssignTicketProperties(newTicket);

                if (ticket.File != null && ticket.Attachment != null)
                {
                    ticket.Attachment.TicketId = newTicket.TicketId;
                    await AddAttachmentAsync(ticket.Attachment, newTicket);
                }
                else
                {
                    await _repository.AddAsync(newTicket);
                    CreateNotification(newTicket, 1, null);
                }
            }
        }

        /// <summary>
        /// Calls the repository to update an existing ticket.
        /// </summary>
        /// <param name="ticket">The ticket</param>
        /// <param name="updateType">The update type</param>
        public async Task UpdateAsync(TicketViewModel ticket, int updateType)
        {
            if (ticket.File != null) await HandleAttachmentAsync(ticket);

            var existingTicket = await _repository.FindByIdAsync(ticket.TicketId);
            if (existingTicket != null)
            {
                bool hasChanges = existingTicket.CategoryTypeId != ticket.CategoryTypeId ||
                          existingTicket.PriorityTypeId != ticket.PriorityTypeId ||
                          existingTicket.IssueDescription != ticket.IssueDescription ||
                          existingTicket.StatusTypeId != ticket.StatusTypeId ||
                          existingTicket.Subject != ticket.Subject ||
                          (existingTicket.Attachments?.FirstOrDefault()?.AttachmentId != ticket.Attachment?.AttachmentId);

                if (!hasChanges)
                {
                    throw new NoChangesException("No changes were made to the ticket.", ticket.TicketId);
                }

                ticket.UpdatedDate = DateTime.Now;
                ticket.CategoryType = await _repository.FindCategoryByIdAsync(ticket.CategoryTypeId);
                ticket.PriorityType = await _repository.FindPriorityByIdAsync(ticket.PriorityTypeId);
                ticket.StatusType = await _repository.FindStatusByIdAsync(ticket.StatusTypeId);
                ticket.User = await _repository.UserFindByIdAsync(ticket.UserId);

                if (existingTicket.TicketAssignment != null)
                    ticket.TicketAssignment = existingTicket.TicketAssignment; 
                
                _mapper.Map(ticket, existingTicket);
                await UpdateTicketDate(existingTicket);
                
                if (ticket.Attachment != null && ticket.File != null)
                {
                    ticket.Attachment.TicketId = existingTicket.TicketId;
                    await AddAttachmentAsync(ticket.Attachment, existingTicket);
                    CreateNotification(existingTicket, updateType, null, ticket.Agent?.UserId);
                }
                else
                {
                    await _repository.UpdateAsync(existingTicket);
                    CreateNotification(existingTicket, updateType, null, ticket.Agent?.UserId);
                }
            }
            else
            {
                LogError("UpdateAsync", "Ticket does not exist.");
            }
        }

        /// <summary>
        /// Helper method to create a notification.
        /// </summary>
        /// <param name="ticket"></param>
        /// <param name="updateType"></param>
        /// <param name="isReassigned"></param>
        /// <param name="agentId"></param>
        private void CreateNotification(Ticket ticket, int? updateType, bool? isReassigned, string agentId = null)
        {
            var userId = ticket.UserId;
            var ticketId = ticket.TicketId;

            var (title, type, message) = updateType switch
            {
                1 => ("New Ticket Created Successfully", "1", $"Ticket #{ticketId} Successfully Added"),
                2 => ("Ticket Priority Updated", "2", $"Ticket #{ticketId} Priority has been updated."),
                3 => ("Ticket Status Updated", "3", $"Ticket #{ticketId} Status has been updated."),
                4 => ("Ticket Attachment Updated", "4", $"Ticket #{ticketId} Details have been updated."), // attachment to details, need to update db notif type
                5 => (string.Empty, string.Empty, string.Empty), // ticket assignment
                6 => (string.Empty, string.Empty, string.Empty), // ticket reassignment
                7 => ("Ticket Description Updated", "7", $"Ticket #{ticketId} Description has been updated."), // DANGER: no entry in NotificationType
                _ => (string.Empty, string.Empty, string.Empty)
            };

            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(type))
            {
                if (agentId != null && (updateType == 3 || updateType == 4 || updateType == 5))
                {
                    _notificationService.AddNotification(ticketId, title, type, agentId, message);
                }
                _notificationService.AddNotification(ticketId, title, type, userId, message);
            }
            else if (isReassigned.HasValue)
            {
                string agentNotificationTitle = "Ticket Assigned";
                string userNotificationTitle = isReassigned.Value ? "Ticket Reassigned to an Agent" : "Ticket Assigned to an Agent";
                string notificationType = isReassigned.Value ? "6" : "5";

                _notificationService.AddNotification(ticketId, agentNotificationTitle, "5", agentId, $"Ticket #{ticketId} has been assigned to you.");
                _notificationService.AddNotification(ticketId, userNotificationTitle, notificationType, userId, $"Ticket #{ticketId} has been assigned to an agent.");
            }
        }

        /// <summary>
        /// Calls the repository to check if a ticket is a duplicate.
        /// </summary>
        /// <param name="ticket">The new ticket</param>
        /// <param name="userId">User identifier</param>
        /// <returns>true or false</returns>
        private async Task<bool> IsDuplicateTicketAsync(TicketViewModel ticket, string userId)
        {
            var duplicateTickets = await _repository.FindByUserIdAsync(userId);
            return duplicateTickets.Any(t => t.Subject.ToLower() == ticket.Subject.ToLower() &&
                                             t.CategoryTypeId == ticket.CategoryTypeId);
        }

        /// <summary>
        /// Calls the repository to delete a ticket.
        /// Deletes all attachments, assignments, and feedback associated with the ticket.
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        public async Task DeleteAsync(string id)
        {
            var ticket = await _repository.FindByIdAsync(id);
            if (ticket != null)
            {
                await RemoveAttachmentByTicketIdAsync(ticket.TicketId);
                await RemoveAssignmentByTicketIdAsync(ticket.TicketId);
                await RemoveFeedbackByTicketIdAsync(ticket.TicketId);
                await RemoveNotificationByTicketIdAsync(ticket.TicketId);
                await _repository.DeleteAsync(ticket);
            }
            else
            {
                LogError("DeleteAsync", "Ticket does not exist.");
            }
        }
        #endregion Ticket CRUD Operations

        #region Ticket Attachment CRUD Operations
        /// <summary>
        /// Calls the repository to add a new attachment.
        /// </summary>
        /// <param name="attachment">The attachment</param>
        /// <param name="ticket">The ticket</param>
        public async Task AddAttachmentAsync(Attachment attachment, Ticket ticket)
        {
            if (attachment != null)
            {
                attachment.Ticket = ticket;
                
                if (attachment.Ticket != null && ticket.Attachments.Count <= 0)
                {
                    ticket.Attachments.Add(attachment);
                    await _repository.AddAttachmentAsync(attachment);
                }
                LogError("AddAttachmentAsync", "Ticket does not exist.");
            }
            LogError("AddAttachmentAsync", "No attachment passed.");
        }

        /// <summary>
        /// Calls the repository to remove an attachment.
        /// </summary>
        /// <param name="attachmentId"></param>
        public async Task RemoveAttachmentAsync(string attachmentId)
        {
            var attachment = await _repository.FindAttachmentByIdAsync(attachmentId);
            var ticket = await _repository.FindByIdAsync(attachment.TicketId);
            if (attachment != null)
            {
                ticket.Attachments.Clear();
                await _repository.RemoveAttachmentAsync(attachment);
            }
        }
        #endregion Ticket Attachment CRUD Operations

        #region Ticket Assignment CRUD Operations
        /// <summary>
        /// Calls the repository to add a new ticket assignment.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="isReassigned">True or false</param>
        public async Task AddTicketAssignmentAsync(TicketViewModel model, bool isReassigned)
        {
            var assignment = await GetAssignmentByTicketIdAsync(model.TicketId);
            if (assignment == null)
            {
                var ticket = await _repository.FindByIdAsync(model.TicketId);
                assignment = await CreateTicketAssignmentAsync(model);
                ticket.TicketAssignment = assignment;
                assignment.Ticket = ticket;
                await _repository.AssignTicketAsync(assignment);

                CreateNotification(ticket, null, isReassigned, model.Agent.UserId);
            }
            else
            {
                LogError("AddTicketAssignmentAsync", "Assignment already exists.");
            }
        }

        /// <summary>
        /// Calls the repository to remove a ticket assignment.
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        public async Task RemoveAssignmentAsync(string id)
        {
            var assignment = await GetAssignmentByTicketIdAsync(id);
            if (assignment != null)
            {
                var ticket = await _repository.FindByIdAsync(id);
                ticket.TicketAssignment = null;
                await _repository.RemoveAssignmentAsync(assignment);
            }
        }
        #endregion Ticket Assignment CRUD Operations

        #region Ticket Feedback CRUD Operations
        /// <summary>
        /// Calls the repository to remove a feedback.
        /// </summary>
        /// <param name="id">Feedback identifier</param>
        private async Task RemoveFeedbackByTicketIdAsync(string id)
        {
            var feedback = await GetFeedBackByIdAsync(id);
            if (feedback != null) await _repository.FeedbackDeleteAsync(feedback);
        }
        #endregion Ticket Feedback CRUD Operations

        #region Get Methods
        /// <summary>
        /// Calls the repository to get all tickets.
        /// </summary>
        /// <returns>IEnumerable TicketViewModel</returns>
        public async Task<IEnumerable<TicketViewModel>> GetAllAsync()
        {
            var tickets = await _repository.GetAllAsync();
            var ticketViewModels = _mapper.Map<IEnumerable<TicketViewModel>>(tickets).ToList();

            return ticketViewModels;
        }

        /// <summary>
        /// Calls the repository to get a ticket by its identifier.
        /// </summary>
        /// <param name="id">The identifier</param>
        /// <returns>TicketViewModel</returns>
        public async Task<TicketViewModel> GetTicketByIdAsync(string id)
        {
            var ticket = await _repository.FindByIdAsync(id);
            var mappedTicket = _mapper.Map<TicketViewModel>(ticket);

            return mappedTicket;
        }

        /// <summary>
        /// Calls the repository to get all unresolved tickets.
        /// </summary>
        /// <returns>IEnumerable TicketViewModel</returns>
        public async Task<IEnumerable<TicketViewModel>> GetUnresolvedTicketsAsync() 
            => (await GetAllAsync()).Where(ticket => ticket.StatusType.StatusName == "Open" || ticket.StatusType.StatusName == "In Progress");

        /// <summary>
        /// Calls GetUnresolvedTicketsAsync and filters the result based on status: "assigned" or "unassigned".
        /// </summary>
        /// <param name="status">The status</param>
        /// <param name="assignedTicketIds">The list of ticketIds with an assignment</param>
        /// <returns>IEnumerable TicketViewModel</returns>
        public async Task<IEnumerable<TicketViewModel>> GetTicketsByAssignmentStatusAsync(string status, List<string> assignedTicketIds)
        {
            var tickets = await GetUnresolvedTicketsAsync();

            var filteredTickets = status.Equals("unassigned")
                ? tickets.Where(t => !assignedTicketIds.Contains(t.TicketId))
                : tickets.Where(t => assignedTicketIds.Contains(t.TicketId));

            return filteredTickets;
        }

        /// <summary>
        /// Calls GetAllAsync and filters the result based on sortBy, filterBy, and filterValue.
        /// </summary>
        /// <param name="sortBy">User defined sort order</param>
        /// <param name="filterBy">User defined filter category</param>
        /// <param name="filterValue">User defined filter value</param>
        /// <returns>IEnumerable TicketViewModel</returns>
        public async Task<IEnumerable<TicketViewModel>> GetFilteredAndSortedTicketsAsync(string sortBy, string filterBy, string filterValue)
        {
            var tickets = await GetAllAsync();
            var userRole = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userRole.Contains("Employee"))
            {
                tickets = tickets.Where(x => x.UserId == userId);
            }
            else if (userRole.Contains("Support Agent"))
            {
                tickets = tickets.Where(x => x.Agent != null && x.Agent.UserId == userId);
            }

            if (!string.IsNullOrEmpty(filterBy) && !string.IsNullOrEmpty(filterValue))
            {
                tickets = filterBy.ToLower() switch
                {
                    "priority" => tickets.Where(t => t.PriorityType.PriorityName == filterValue),
                    "status" => tickets.Where(t => t.StatusType.StatusName == filterValue),
                    "category" => tickets.Where(t => t.CategoryType.CategoryName == filterValue),
                    "user" => tickets.Where(t => t.User.Name == filterValue),
                    _ => tickets
                };
            }

            tickets = sortBy switch
            {
                "id_desc" => tickets.OrderByDescending(t => t.TicketId),
                "subject_desc" => tickets.OrderByDescending(t => t.Subject),
                "subject" => tickets.OrderBy(t => t.Subject),
                "status_desc" => tickets.OrderByDescending(t => t.StatusTypeId),
                "status" => tickets.OrderBy(t => t.StatusTypeId),
                "priority_desc" => tickets.OrderByDescending(t => t.PriorityTypeId),
                "priority" => tickets.OrderBy(t => t.PriorityTypeId),
                "category_desc" => tickets.OrderByDescending(t => t.CategoryType.CategoryName),
                "category" => tickets.OrderBy(t => t.CategoryType.CategoryName),
                "user_desc" => tickets.OrderByDescending(t => t.User.Name),
                "user" => tickets.OrderBy(t => t.User.Name),
                "created_desc" => tickets.OrderByDescending(t => t.CreatedDate),
                "created" => tickets.OrderBy(t => t.CreatedDate),
                "updated_desc" => tickets.OrderByDescending(t => t.UpdatedDate),
                "updated" => tickets.OrderBy(t => t.UpdatedDate),
                "resolved_desc" => tickets.OrderByDescending(t => t.ResolvedDate),
                "resolved" => tickets.OrderBy(t => t.ResolvedDate),
                _ => tickets.OrderBy(t => t.TicketId),
            };

            return tickets.AsQueryable();
        }

        /// <summary>
        /// Call GetTicketByIdAsync and return a subset of the ticket properties.
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        /// <returns>Json containing ticket details</returns>
        public async Task<object> GetTicketDetailsAsync(string id)
        {
            var ticket = await GetTicketByIdAsync(id);
            if (ticket != null)
            {
                return new
                {
                    ticket.TicketId,
                    ticket.Subject,
                    ticket.IssueDescription,
                    ticket.StatusTypeId,
                    ticket.CategoryTypeId,
                    ticket.PriorityTypeId,
                    AgentId = ticket.Agent?.UserId,
                };
            }
            return null;
        }

        /// <summary>
        /// Calls the repository to get an attachment by ticket identifier.
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        /// <returns>Attachment</returns>
        public async Task<Attachment> GetAttachmentByTicketIdAsync(string id) 
            => await _repository.FindAttachmentByTicketIdAsync(id);

        /// <summary>
        /// Calls the repository to get a ticket assignment by ticket identifier.
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        /// <returns>TicketAssignment</returns>
        public async Task<TicketAssignment> GetAssignmentByTicketIdAsync(string id) 
            => await _repository.FindAssignmentByTicketIdAsync(id);

        /// <summary>
        /// Calls the repository to get a team by user identifier.
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <returns>Team</returns>
        public async Task<Team> GetTeamByUserIdAsync(string id) 
            => await _repository.FindTeamByUserIdAsync(id);

        /// <summary>
        /// Calls the repository to get a user with role "Agent" by user identifier.
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <returns>User</returns>
        public async Task<User> GetAgentByIdAsync(string id) 
            => await _repository.FindAgentByUserIdAsync(id);

        /// <summary>
        /// Calls the repository to get all categories.
        /// </summary>
        /// <returns>IEnumerable CategoryType</returns>
        public async Task<IEnumerable<CategoryType>> GetCategoryTypesAsync() 
            => await _repository.GetCategoryTypesAsync();

        /// <summary>
        /// Calls the repository to get all priority types.
        /// </summary>
        /// <returns>IEnumerable PriorityType</returns>
        public async Task<IEnumerable<PriorityType>> GetPriorityTypesAsync() 
            => await _repository.GetPriorityTypesAsync();

        /// <summary>
        /// Calls the repository to get all status types.
        /// </summary>
        /// <returns>IEnumerable StatusType</returns>
        public async Task<IEnumerable<StatusType>> GetStatusTypesAsync() 
            => await _repository.GetStatusTypesAsync();

        /// <summary>
        /// Calls the repository to get all users with role "Support Aagent".
        /// </summary>
        /// <returns>IEnumerable User</returns>
        public async Task<IEnumerable<User>> GetSupportAgentsAsync() 
            => await _repository.GetSupportAgentsAsync();

        /// <summary>
        /// Calls the repository to get all ticket assignments.
        /// </summary>
        /// <returns>IEnumerable Ticket Assignment</returns>
        public async Task<IEnumerable<TicketAssignment>> GetTicketAssignmentsAsync() => await _repository.GetTicketAssignmentsAsync();


        public IEnumerable<TicketViewModel> GetUnresolvedTicketsOlderThan(TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now.Subtract(timeSpan);
            var unresolvedTickets = _repository.GetAllAsync().Result
                .Where(t => (t.ResolvedDate == null) && t.CreatedDate <= cutoffTime && (t.User.UserId != null))
                .ToList();

            return unresolvedTickets.Select(ticket => _mapper.Map<TicketViewModel>(ticket));
        }


        /// <summary>
        /// Get feedback by ticket identifier.
        /// </summary>
        /// <param name="id">The ticket identifier</param>
        /// <returns>Feedback</returns>
        private async Task<Feedback> GetFeedBackByIdAsync(string id)
            => await _repository.FeedbackFindByTicketIdAsync(id);

        /// <summary>
        /// Calls the repository to get all users with tickets.
        /// </summary>
        /// <returns>IEnumerable string</returns>
        public async Task<IEnumerable<string>> GetUserIdsWithTicketsAsync() 
            => await _repository.GetUserIdsWithTicketsAsync();

        /// <summary>
        /// Calls the repository to get all users.
        /// </summary>
        /// <returns>IEnumerable user</returns>
        public async Task<IEnumerable<User>> UserGetAllAsync() 
            => await _repository.UserGetAllAsync();

        /// <summary>
        /// Gets the current logged in admin.
        /// </summary>
        /// <returns>Admin</returns>
        private async Task<Admin> GetCurrentAdminAsync()
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext.User;
            if (claimsPrincipal == null || !claimsPrincipal.Identity.IsAuthenticated)
            {
                LogError("GetCurrentAdminAsync", "ClaimsPrincipal is null or not authenticated.");
                return null;
            }

            var adminId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminId))
            {
                LogError("GetCurrentAdminAsync", "AdminId is null or empty.");
                return null;
            }

            return await _repository.AdminFindByIdAsync(adminId);
        }
        #endregion Get Methods

        #region Utility Methods
        /// <summary>
        /// Initializes the TicketViewModel with the appropriate data based on the type.
        /// Types: "status", "assign", "priority", "reassign", or "default".
        /// </summary>
        /// <param name="type">The type of model to initialize</param>
        /// <returns>TicketViewModel</returns>
        public async Task<TicketViewModel> InitializeModelAsync(string type)
        {
            var assignedTicketIds = (await GetTicketAssignmentsAsync()).Select(ta => ta.TicketId).ToList();
            var tickets = type switch
            {
                "status" => (await GetAllAsync()).Where(ticket => !ticket.StatusType.StatusName.Contains("Resolved")),
                "assign" => (await GetTicketsByAssignmentStatusAsync("unassigned", assignedTicketIds)),
                "priority" => await GetUnresolvedTicketsAsync(),
                "reassign" => (await GetTicketsByAssignmentStatusAsync("assigned", assignedTicketIds)),
                _ => null
            };

            var userRole = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (tickets != null)
            {
                tickets = userRole.Contains("Support Agent") ? tickets.Where(x => x.Agent != null && x.Agent.UserId == userId)
                        : userRole.Contains("Employee") ? tickets.Where(x => x.UserId == userId) : tickets;
            }

            var statusTypes = await GetStatusTypesAsync();

            return new TicketViewModel
            {
                Tickets = !type.Contains("default") ? tickets : null,
                CategoryTypes = await GetCategoryTypesAsync(),
                PriorityTypes = await GetPriorityTypesAsync(),
                StatusTypes = !userRole.Contains("Employee") ? statusTypes : statusTypes.Where(x => !x.StatusName.Contains("Resolved")),
                Agents = !type.Contains("default") ? await GetSupportAgentsAsync() : null
            };
        }

        /// <summary>
        /// Helper method to assign ticket properties.
        /// </summary>
        /// <param name="ticket">The ticket</param>
        private void AssignTicketProperties(Ticket ticket)
        {
            var tickets = GetAllAsync().Result;
            string CC = ticket.CategoryTypeId;
            int CN = tickets.Count(t => t.CategoryTypeId == ticket.CategoryTypeId);
            int NN = tickets.Count();

            ticket.TicketId = $"{CC}-{CN + 1:00}-{NN + 1:00}";

            ticket.StatusTypeId ??= "S1";
            ticket.CategoryType = GetCategoryTypesAsync().Result.Single(x => x.CategoryTypeId == ticket.CategoryTypeId);
            ticket.PriorityType = GetPriorityTypesAsync().Result.Single(x => x.PriorityTypeId == ticket.PriorityTypeId);
            ticket.StatusType = GetStatusTypesAsync().Result.Single(x => x.StatusTypeId == ticket.StatusTypeId);
            ticket.User = _repository.UserFindByIdAsync(ticket.UserId).Result;
            ticket.User.Tickets.Add(ticket);
        }

        /// <summary>
        /// Extracts the agent identifier from the assignment identifier.
        /// </summary>
        /// <param name="assignmentId">The assignment identifier</param>
        /// <returns>string UserId</returns>
        public string ExtractAgentId(string assignmentId)
        {
            if (string.IsNullOrEmpty(assignmentId)) return string.Empty;

            string[] parts = assignmentId.Split('-');
            return parts.Length >= 5 ? string.Join("-", parts.Take(5)) : assignmentId;
        }

        /// <summary>
        /// Helper method to create a new ticket assignment.
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>TicketAssignment</returns>
        /// AssignmentId format: {AgentId}-{Timestamp}-{RandomNumber}
        private async Task<TicketAssignment> CreateTicketAssignmentAsync(TicketViewModel model)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var randomNumber = new Random().Next(1000, 9999);
            var assignmentId = $"{model.Agent.UserId}-{timestamp}-{randomNumber}";
            var currentAdmin = await GetCurrentAdminAsync();
            var teamId = await GetTeamByUserIdAsync(model.Agent.UserId);

            return new TicketAssignment
            {
                AssignmentId = assignmentId,
                TeamId = teamId.TeamId,
                TicketId = model.TicketId,
                AssignedDate = DateTime.Now,
                AdminId = currentAdmin.AdminId,
                Team = await _repository.FindTeamByUserIdAsync(model.Agent.UserId),
                Admin = currentAdmin
            };
        }

        /// <summary>
        /// Helper method to create a new ticket attachment.
        /// </summary>
        /// <param name="ticket">The ticket</param>
        private async Task HandleAttachmentAsync(TicketViewModel ticket)
        {
            _logger.LogInformation("=======Ticket Service : HandleAttachmentAsync Started=======");
            var allowedFileTypesString = Resources.Views.FileValidation.AllowedFileTypes;
            var allowedFileTypes = new HashSet<string>(allowedFileTypesString.Split(','), StringComparer.OrdinalIgnoreCase);
            long maxFileSize = Convert.ToInt32(Resources.Views.FileValidation.MaxFileSizeMB) * 1024 * 1024;

            if (ticket.File != null && ticket.File.Length > 0)
            {
                if (allowedFileTypes.Contains(ticket.File.ContentType) && ticket.File.Length <= maxFileSize)
                {
                    using (var stream = new MemoryStream())
                    {
                        await ticket.File.CopyToAsync(stream);
                        ticket.Attachment = new Attachment
                        {
                            AttachmentId = Guid.NewGuid().ToString(),
                            Name = ticket.File.FileName,
                            Content = stream.ToArray(),
                            Type = ticket.File.ContentType,
                            UploadedDate = DateTime.Now
                        };
                    }
                }
                else
                {
                    throw new InvalidFileException("File type not allowed or file size exceeds the limit.", ticket.TicketId);
                }
            }
            _logger.LogInformation("=======Ticket Service : HandleAttachmentAsync Ended=======");
        }

        /// <summary>
        /// Helper method to update the ticket resolved date based on status.
        /// </summary>
        /// <param name="ticket">The ticket</param>
        private async Task UpdateTicketDate(Ticket ticket)
        {
            var status = await _repository.FindStatusByIdAsync(ticket.StatusTypeId);

            if (status != null && (status.StatusName.Equals("Closed") || status.StatusName.Equals("Resolved")))
            {
                ticket.ResolvedDate = ticket.ResolvedDate ?? DateTime.Now;
            }
            else
            {
                ticket.ResolvedDate = null;
            }
        }
        
        /// <summary>
        /// Remove attachment by ticket identifier.
        /// </summary>
        /// <param name="id">The ticket identifier</param>
        private async Task RemoveAttachmentByTicketIdAsync(string id)
        {
            var attachment = await GetAttachmentByTicketIdAsync(id);
            if (attachment != null)
            {
                await _repository.RemoveAttachmentAsync(attachment);
            }
        }

        /// <summary>
        /// Remove assignment by ticket identifier.
        /// </summary>
        /// <param name="id">The ticket identifier</param>
        private async Task RemoveAssignmentByTicketIdAsync(string id)
        {
            var assignment = await GetAssignmentByTicketIdAsync(id);
            if (assignment != null)
            {
                await _repository.RemoveAssignmentAsync(assignment);
            }
        }

        private async Task RemoveNotificationByTicketIdAsync(string id)
        {
            await _repository.NotificationDeleteAsync(id);
        }
        #endregion Utility Methods

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
