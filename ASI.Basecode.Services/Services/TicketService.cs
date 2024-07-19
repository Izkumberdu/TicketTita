﻿using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Data.Repositories;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading.Tasks;
using ZXing;
using static ASI.Basecode.Services.Exceptions.TicketExceptions;
using static ASI.Basecode.Services.Exceptions.TeamExceptions;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Services
{
    public partial class TicketService : ITicketService
    {
        private readonly ITicketRepository _repository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        private readonly IPerformanceReportRepository _performanceReportRepository;
        private readonly ITeamRepository _teamRepository;

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
            IHttpContextAccessor httpContextAccessor,
            IPerformanceReportRepository performanceReportRepository,
            ITeamRepository teamRepository)
        {
            _repository = repository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _performanceReportRepository = performanceReportRepository;
            _teamRepository = teamRepository;
        }

        #region Ticket CRUD Operations
        /// <summary>
        /// Calls the repository to add a new ticket.
        /// </summary>
        /// <param name="model">Ticket identifier</param>
        /// <param name="userId">User identifier</param>
        public async Task AddAsync(TicketViewModel model, string userId)
        {
            if (await IsDuplicateTicketAsync(model, userId))
                throw new TicketException("A similar ticket already exists.");

            if (model != null)
            {
                if(model.Subject.Length > 100)
                    throw new TicketException("Subject has exceeded maximum allowed characters of 100.");
                if (model.IssueDescription.Length > 800)
                    throw new TicketException("Description has exceeded maximum allowed characters of 800.");

                if (model.File != null)
                    await HandleAttachmentAsync(model);

                var newTicket = _mapper.Map<Ticket>(model);
                newTicket.CreatedDate = DateTime.Now;
                newTicket.UserId = userId;

                AssignTicketProperties(newTicket);

                if (model.File != null && model.Attachment.AttachmentId != null)
                {
                    model.Attachment.TicketId = newTicket.TicketId;
                    await AddAttachmentAsync(model.Attachment, newTicket);
                }
                else
                {
                    await _repository.AddAsync(newTicket);   
                }
                CreateNotification(newTicket, 1, null);
            }
        }

        /// <summary>
        /// Calls the repository to update an existing ticket.
        /// </summary>
        /// <param name="model">The ticket</param>
        /// <param name="updateType">The update type</param>
        public async Task UpdateAsync(TicketViewModel model, int updateType)
        {
            var ticket = await _repository.FindByIdAsync(model.TicketId);
            if (await IsDuplicateTicketAsync(model, model.UserId) && ticket.Subject != model.Subject)
                throw new TicketException("A similar ticket already exists.");

            if (ticket != null)
            {
                if (model.Subject.Length > 100)
                    throw new TicketException("Subject has exceeded maximum allowed characters of 100.");
                if (model.IssueDescription.Length > 800)
                    throw new TicketException("Description has exceeded maximum allowed characters of 800.");

                bool hasChanges = ticket.IssueDescription != model.IssueDescription ||
                                ticket.Subject != model.Subject ||
                                (!ticket.Attachments.Any() && model.File != null);
                bool hasAttachmentChanges = ticket.Attachments.Any() && (model.File != null || 
                                (model.File == null && model.Attachment == null));

                if ((hasChanges || hasAttachmentChanges) && ticket.ResolvedDate != null)
                    throw new TicketException("Cannot make changes to resolved tickets.", model.TicketId);
                if (!hasChanges && !hasAttachmentChanges)
                    throw new TicketException("No changes were made to the ticket.", model.TicketId);

                if (hasAttachmentChanges)
                {
                    await RemoveAttachmentByTicketIdAsync(ticket.TicketId);
                    ticket.Attachments.Clear();
                }
                if (model.File != null) await HandleAttachmentAsync(model);

                ticket.Subject = model.Subject != null ? model.Subject : ticket.Subject;
                ticket.IssueDescription = model.IssueDescription != null ? model.IssueDescription : ticket.IssueDescription;
                if(model.File != null) ticket.Attachments.Add(model.Attachment);

                model.UpdatedDate = DateTime.Now;
                await UpdateTicketDate(ticket);
                
                if (model.File != null && model.Attachment.AttachmentId != null)
                {
                    model.Attachment.TicketId = ticket.TicketId;
                    await AddAttachmentAsync(model.Attachment, ticket);
                }
                else
                {
                    await _repository.UpdateAsync(ticket);
                }
                CreateNotification(ticket, updateType, null, model.Agent?.UserId);

            }
            else
            {
                throw new TicketException("Ticket does not exist.");
            }
        }

        /// <summary>
        /// Calls the repository to delete a ticket.
        /// Deletes all associated data (attachments, assignments, feedback, notifications, and comments).
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        public async Task DeleteAsync(string id)
        {
            var ticket = await _repository.FindByIdAsync(id);
            if (ticket != null) await _repository.DeleteAsync(ticket);
            else throw new TicketException("Ticket does not exist.");
        }
        #endregion Ticket CRUD Operations

        #region Utility Methods
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
        /// Helper method to assign ticket properties.
        /// </summary>
        /// <param name="ticket">The ticket</param>
        private void AssignTicketProperties(Ticket ticket)
        {
            var tickets = _repository.GetAllAndDeletedAsync().Result;
            string CC = ticket.CategoryTypeId;
            int NN = tickets.Count;

            ticket.TicketId = $"{CC}-{NN + 1:0000}";

            ticket.StatusTypeId ??= "S1";
            ticket.CategoryType = GetCategoryTypesAsync().Result.Single(x => x.CategoryTypeId == ticket.CategoryTypeId);
            ticket.PriorityType = GetPriorityTypesAsync().Result.Single(x => x.PriorityTypeId == ticket.PriorityTypeId);
            ticket.StatusType = GetStatusTypesAsync().Result.Single(x => x.StatusTypeId == ticket.StatusTypeId);
            ticket.User = _repository.UserFindByIdAsync(ticket.UserId).Result;
            ticket.User.Tickets.Add(ticket);
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
        #endregion Utility Methods

        #region Performance Report Methods
        private async Task UpdateTeamPerformanceReportsAsync(Ticket existingTicket)
        {
            if (existingTicket.TicketAssignment != null)
            {
                var team = await _teamRepository.FindByIdAsync(existingTicket.TicketAssignment.TeamId);
                if (team != null)
                {
                    foreach (var teamMember in team.TeamMembers)
                    {
                        var performanceReport = teamMember.Report;
                        if (performanceReport != null)
                        {
                            performanceReport.ResolvedTickets++;
                            var resolutionTime = (existingTicket.UpdatedDate.Value - existingTicket.CreatedDate).TotalMinutes;
                            performanceReport.AverageResolutionTime = ((performanceReport.AverageResolutionTime * (performanceReport.ResolvedTickets - 1)) + resolutionTime) / performanceReport.ResolvedTickets;

                            await _performanceReportRepository.UpdatePerformanceReportAsync(performanceReport);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
