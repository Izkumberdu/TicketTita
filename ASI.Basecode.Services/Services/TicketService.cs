﻿using ASI.Basecode.Data.Interfaces;
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
using System.Security.Claims;

namespace ASI.Basecode.Services.Services
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _repository;
        private readonly IAdminRepository _adminRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TicketService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TicketService"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="mapper">The mapper.</param>
        public TicketService(ITicketRepository repository, IAdminRepository adminRepository,
                            IUserRepository userRepository, IMapper mapper, 
                            ILogger<TicketService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _mapper = mapper;
            _repository = repository;
            _adminRepository = adminRepository;
            _userRepository = userRepository;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Ticket CRUD Operations
        /// <summary>
        /// Add a new ticket
        /// </summary>
        /// <param name="ticket">The ticket.</param>
        public void Add(TicketViewModel ticket, string userId)
        {
            if(ticket != null)
            {
                HandleAttachment(ticket);

                var newTicket = _mapper.Map<Ticket>(ticket);
                newTicket.CreatedDate = DateTime.Now;
                newTicket.UpdatedDate = null;
                newTicket.ResolvedDate = null;
                newTicket.UserId = userId;

                AssignTicketProperties(newTicket);

                string id = _repository.Add(newTicket);
                if (ticket.Attachment != null)
                {
                    ticket.Attachment.TicketId = id;
                    AddAttachment(ticket.Attachment);
                }
            }
            LogError("Add", "Invalid ticket passed.");
        }

        /// <summary>
        /// Update an existing ticket
        /// </summary>
        /// <param name="ticket">The ticket.</param>
        public void Update(TicketViewModel ticket)
        {
            HandleAttachment(ticket);

            var existingTicket = _repository.FindById(ticket.TicketId);
            if (existingTicket != null)
            {
                ticket.UpdatedDate = DateTime.Now;

                _mapper.Map(ticket, existingTicket);
                UpdateTicketDate(existingTicket);
                SetNavigationProperties(existingTicket);

                string id = _repository.Update(existingTicket);
                if (ticket.Attachment != null)
                {
                    ticket.Attachment.TicketId = id;
                    AddAttachment(ticket.Attachment);
                }
            }
            LogError("Update", "Ticket does not exist.");
        }

        /// <summary>
        /// Delete a ticket
        /// </summary>
        /// <param name="id">The identifier.</param>
        public void Delete(string id)
        {
            var ticket = _repository.FindById(id);
            if (ticket != null)
            {
                RemoveAttachmentByTicketId(ticket.TicketId);
                RemoveAssignmentByTicketId(ticket.TicketId);
                _repository.Delete(ticket);
            }
            LogError("Delete", "Ticket does not exist.");
        }
        #endregion Ticket CRUD Operations

        #region Ticket Attachment CRUD Operations
        /// <summary>
        /// Add a new attachment
        /// </summary>
        /// <param name="ticket">The attachment.</param>
        public void AddAttachment(Attachment attachment)
        {
            if (attachment != null) {
                attachment.Ticket = _repository.FindById(attachment.TicketId);

                if (attachment.Ticket != null) _repository.AddAttachment(attachment);
                LogError("AddAttachment", "Ticket does not exist.");
            }
            LogError("AddAttachment", "Invalid Attachment passed.");
        }

        /// <summary>
        /// Remove a ticket attachment.
        /// </summary>
        /// <param name="attachmentId">The attachment identifier.</param>
        public void RemoveAttachment(string attachmentId)
        {
            var attachment = _repository.FindAttachmentById(attachmentId);
            if (attachment != null) _repository.RemoveAttachment(attachment);

            LogError("RemoveAttachment", "Attachment does not exist.");
        }
        #endregion Ticket Attachment CRUD Operations

        #region Ticket Assignment CRUD Operations
        /// <summary>
        /// Assigns the ticket to agent.
        /// </summary>
        /// <param name="model">The model.</param>
        public void AddTicketAssignment(TicketViewModel model)
        {
            var assignment = GetAssignmentByTicketId(model.TicketId);
            if (assignment == null)
            {
                assignment = CreateTicketAssignment(model);
                _repository.AssignTicket(assignment);
            }
            LogError("AddTicketAssignment", "Assignment already exists.");
        }

        /// <summary>
        /// Removes a ticket assignment.
        /// </summary>
        /// <param name="id">The ticket identifier.</param>
        public void RemoveAssignment(string id)
        {
            var assignment = _repository.FindAssignmentByTicketId(id);
            if (assignment != null) _repository.RemoveAssignment(assignment);

            LogError("RemoveAssignment", "Assignment does not exist.");
        }
        #endregion Ticket Assignment CRUD Operations

        #region Get Methods
        /// <summary>
        /// Calls the repository to get all tickets.
        /// Maps the list of Ticket to a list of TicketViewModel.
        /// </summary>
        /// <returns>A list of TicketViewModel (IEnumerable)</returns>
        public IEnumerable<TicketViewModel> GetAll()
        {
            var tickets = _repository.GetAll().ToList();
            tickets.ForEach(ticket => SetNavigationProperties(ticket));

            var ticketViewModels = _mapper.Map<IEnumerable<TicketViewModel>>(tickets).ToList();
            foreach(var t in ticketViewModels)
            {
                t.Attachment = GetAttachmentByTicketId(t.TicketId);
                t.TicketAssignment = GetAssignmentByTicketId(t.TicketId);
                t.Agent = GetAgentById(ExtractAgentId(t.TicketAssignment?.AssignmentId));
            }

            return ticketViewModels;
        }

        /// <summary>
        /// Calls the repository to get a ticket by its id.
        /// Maps the Ticket to the TicketViewModel.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>TicketViewModel</returns>
        public TicketViewModel GetTicketById(string id)
        {
            var ticket = _repository.FindById(id);
            SetNavigationProperties(ticket);

            var mappedTicket = _mapper.Map<TicketViewModel>(ticket);
            mappedTicket.Attachment = GetAttachmentByTicketId(id);
            mappedTicket.TicketAssignment = GetAssignmentByTicketId(id);
            if (mappedTicket.TicketAssignment != null && !string.IsNullOrEmpty(mappedTicket.TicketAssignment.AssignmentId))
            {
                mappedTicket.Agent = GetAgentById(ExtractAgentId(mappedTicket.TicketAssignment.AssignmentId));
            }

            return mappedTicket;
        }

        /// <summary>
        /// Calls the repository to get unresolved tickets (Open, In Progress).
        /// </summary>
        /// <returns>A list of TicketViewModel (IEnumerable)</returns>
        public IEnumerable<TicketViewModel> GetUnresolvedTickets()
        {
            var tickets = GetAll()
                          .Where(ticket => ticket.StatusType.StatusName == "Open" || ticket.StatusType.StatusName == "In Progress")
                          .ToList();
            return _mapper.Map<IEnumerable<TicketViewModel>>(tickets);
        }

        /// <summary>
        /// Gets the tickets by assignment status.
        /// </summary>
        /// <param name="status">The status, "assigned" or "unassigned"</param>
        /// <returns>A list of TicketViewModel (IEnumerable)</returns>
        public IEnumerable<TicketViewModel> GetTicketsByAssignmentStatus(string status)
        {
            var assignedTicketIds = GetTicketAssignments().Select(ta => ta.TicketId).ToList();
            var tickets = _repository.GetTickets(status, assignedTicketIds).ToList();
            tickets.ForEach(ticket => SetNavigationProperties(ticket));

            return _mapper.Map<IEnumerable<TicketViewModel>>(tickets);
        }

        /// <summary>
        /// Retrieves ticket details for a specific ticket.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Json</returns>
        public Object GetTicketDetails(string id)
        {
            var ticket = GetTicketById(id);
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
                    ticket.Agent
                };
            }
            return null;
        }

        /// <summary>
        /// Calls the repository to get an attachment by its ticket id.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Attachment</returns>
        public Attachment GetAttachmentByTicketId(string id) => _repository.FindAttachmentByTicketId(id);

        /// <summary>
        /// Calls the repository to get an assignment by its ticket id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>TicketAssignment</returns>
        public TicketAssignment GetAssignmentByTicketId(string id) => _repository.FindAssignmentByTicketId(id);

        /// <summary>
        /// Calls the repository to get a Team through TeamMember userId.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Team</returns>
        public Team GetTeamByUserId(string id) => _repository.FindTeamByUserId(id);

        /// <summary>
        /// Calls the repository to get an agent by its user id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A User with "Support Agent" role</returns>
        public User GetAgentById(string id) => _repository.FindAgentByUserId(id);

        /// <summary>
        /// Calls the repository to get all category types.
        /// </summary>
        /// <returns>A list of CategoryType (IEnumerable)</returns>
        public IEnumerable<CategoryType> GetCategoryTypes() => _repository.GetCategoryTypes();

        /// <summary>
        /// Calls the repository to get all priority types.
        /// </summary>
        /// <returns>A list of PriorityType (IEnumerable)</returns>
        public IEnumerable<PriorityType> GetPriorityTypes() => _repository.GetPriorityTypes();

        /// <summary>
        /// Calls the repository to get all status types.
        /// </summary>
        /// <returns>A list of StatusType (IEnumerable)</returns>
        public IEnumerable<StatusType> GetStatusTypes() => _repository.GetStatusTypes();

        /// <summary>
        /// Calls the repository to get all support agents.
        /// </summary>
        /// <returns>A list of User (IEnumerable) with "Support Agent" role</returns>
        public IEnumerable<User> GetSupportAgents() => _repository.GetSupportAgents();

        /// <summary>
        /// Calls the repository to get all ticket assignments.
        /// </summary>
        /// <returns>A list of TicketAssignment (IEnumerable)</returns>
        public IEnumerable<TicketAssignment> GetTicketAssignments() => _repository.GetTicketAssignments();
        #endregion Get Methods

        #region Utility Methods
        /// <summary>
        /// Initializes a TicketViewModel to display.
        /// </summary>
        /// <returns>TicketViewModel</returns>
        public TicketViewModel InitializeModel(string type)
        {
            var tickets = type switch
            {
                "status" => GetAll().Where(ticket => !ticket.StatusType.StatusName.Contains("Resolved")),
                "assign" => GetTicketsByAssignmentStatus("unassigned"),
                "assigned" => GetTicketsByAssignmentStatus("reassign"),
                _ => GetUnresolvedTickets(),
            };

            return new TicketViewModel
            {
                Tickets = tickets,
                CategoryTypes = GetCategoryTypes(),
                PriorityTypes = GetPriorityTypes(),
                StatusTypes = GetStatusTypes(),
                Agents = GetSupportAgents()
            };
        }

        /// <summary>
        /// Extracts the agent identifier from assignment identifier.
        /// </summary>
        /// <param name="assignmentId">The assignment identifier.</param>
        /// <returns>Support agent identifier string</returns>
        public string ExtractAgentId(string assignmentId)
        {
            if (assignmentId == null)
                return string.Empty;

            string[] parts = assignmentId.Split('-');

            if (parts.Length >= 5)
            {
                var str = string.Join("-", parts.Take(5));
                return str;
            }
            else
            {
                return assignmentId;
            }
        }

        /// <summary>
        /// Handles the attachment by converting the File to an Attachment 
        /// and assigning it to the ticket.
        /// </summary>
        /// <param name="ticket">The ticket</param>
        private void HandleAttachment(TicketViewModel ticket)
        {
            _logger.LogInformation("=======Ticket Service : HandleAttachment Started=======");
            var allowedFileTypesString = Resources.Views.FileValidation.AllowedFileTypes;
            var allowedFileTypes = new HashSet<string>(allowedFileTypesString.Split(','), StringComparer.OrdinalIgnoreCase);
            long maxFileSize = Convert.ToInt32(Resources.Views.FileValidation.MaxFileSizeMB) * 1024 * 1024;

            if (ticket.File != null && ticket.File.Length > 0)
            {
                /// Check if the file type is allowed and the file size is within the limit
                if (allowedFileTypes.Contains(ticket.File.ContentType) && !(ticket.File.Length > maxFileSize))
                {
                    using (var stream = new MemoryStream())
                    {
                        ticket.File.CopyTo(stream);
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
                _logger.LogError("File type not allowed or file size exceeds the limit.");
            }
            _logger.LogInformation("=======Ticket Service : HandleAttachment Ended=======");
        }

        /// <summary>
        /// Updates the ticket resolved date based on the status.
        /// </summary>
        /// <param name="ticket"></param>
        /// <exception cref="ArgumentException"></exception>
        private void UpdateTicketDate(Ticket ticket)
        {
            var status = _repository.FindStatusById(ticket.StatusTypeId);

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
        /// Assigns the following ticket properties: TicketId, CategoryType, PriorityType, StatusType
        /// </summary>
        /// <param name="ticket">The ticket.</param>
        /// 
        /// Ticket ID format: CC-CN-NN
        /// CC = Category Type ID
        /// CN = Category Number (how many tickets have been created for a category)
        /// NN = Ticket Number (total number of tickets created)
        private void AssignTicketProperties(Ticket ticket)
        {
            /// TicketId is reused if an entry is deleted and a new one has the same category as deleted entry
            /// not scalable if database has many entries
            // TODO: revisit this logic
            int CC = Convert.ToInt32(ticket.CategoryTypeId);
            int CN = GetAll().Count(t => t.CategoryTypeId == ticket.CategoryTypeId);
            int NN = GetAll().Count();

            ticket.TicketId = $"{CC:00}-{CN + 1:00}-{NN + 1:00}";

            ticket.StatusTypeId ??= "1";
            SetNavigationProperties(ticket);
        }

        /// <summary>
        /// Sets the navigation properties for a ticket: CategoryType, PriorityType, StatusType
        /// </summary>
        /// <param name="ticket">The ticket.</param>
        private void SetNavigationProperties(Ticket ticket)
        {
            if (ticket == null) LogError("SetNavigationProperties", "Ticket is null.");

            ticket.CategoryType = GetCategoryTypes().Single(x => x.CategoryTypeId == ticket.CategoryTypeId);
            ticket.PriorityType = GetPriorityTypes().Single(x => x.PriorityTypeId == ticket.PriorityTypeId);
            ticket.StatusType = GetStatusTypes().Single(x => x.StatusTypeId == ticket.StatusTypeId);
            ticket.User = _userRepository.FindById(ticket.UserId);
        }

        /// <summary>
        /// Creates a new ticket assignment.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>TicketAssignment</returns>
        /// AssignmentId format: {AgentId}-{Timestamp}-{RandomNumber}
        private TicketAssignment CreateTicketAssignment(TicketViewModel model)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var randomNumber = new Random().Next(1000, 9999);
            var assignmentId = $"{model.Agent.UserId}-{timestamp}-{randomNumber}";
            var currentAdmin = GetCurrentAdmin();

            return new TicketAssignment
            {
                AssignmentId = assignmentId,
                TeamId = GetTeamByUserId(model.Agent.UserId).TeamId,
                TicketId = model.TicketId,
                AssignedDate = DateTime.Now,
                AdminId = currentAdmin.AdminId
            };
        }

        /// <summary>
        /// Removes the attachment by ticket identifier.
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        private void RemoveAttachmentByTicketId(string id)
        {
            var attachment = GetAttachmentByTicketId(id);
            if (attachment == null) 
                LogError("RemoveAttachmentByTicketId", "Attachment is null.");
            
           _repository.RemoveAttachment(attachment);
        }

        /// <summary>
        /// Removes the assignment by ticket identifier.
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        private void RemoveAssignmentByTicketId(string id)
        {
            var assignment = GetAssignmentByTicketId(id);
            if (assignment == null) 
                LogError("RemoveAssignmentByTicketId", "Assignment is null.");

            _repository.RemoveAssignment(assignment);
        }

        /// <summary>
        /// Gets the current admin.
        /// </summary>
        /// <returns></returns>
        private Admin GetCurrentAdmin()
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext.User;
            if (claimsPrincipal == null || !claimsPrincipal.Identity.IsAuthenticated)
            {
                LogError("GetCurrentAdmin", "ClaimsPrincipal is null or not authenticated.");
                return null;
            }

            var adminId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminId))
            {
                LogError("GetCurrentAdmin", "AdminId is null or empty.");
                return null;
            } 

            return _adminRepository.FindById(adminId);
        }
        #endregion Utility Methods

        #region Logging methods
        /// <summary>
        /// Log the error message.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="errorMessage">The error message.</param>
        public void LogError(string methodName, string errorMessage)
        {
            _logger.LogError($"Ticket Service {methodName} : {errorMessage}");
        }
        #endregion
    }
}