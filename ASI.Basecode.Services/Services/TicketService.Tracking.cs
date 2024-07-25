﻿using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ASI.Basecode.Services.Exceptions.TicketExceptions;
using static ASI.Basecode.Services.Exceptions.TeamExceptions;
using System.Security.Claims;

namespace ASI.Basecode.Services.Services
{
    public partial class TicketService : ITicketService
    {
        /// <summary>
        /// Updates the ticket tracking: StatusType and/or PriorityType.
        /// </summary>
        /// <param name="ticketV">The ticket as TicketViewModel.</param>
        /// <param name="ticketT">The ticket as Ticket.</param>
        public async Task UpdateTrackingAsync(TicketViewModel ticketV = null, Ticket ticketT = null)
        {
            var ticket = ticketV ?? _mapper.Map<TicketViewModel>(ticketT);
            var existingTicket = await _repository.FindByIdAsync(ticket.TicketId);
            if (existingTicket == null)
                throw new TicketException("Ticket was not found.");

            var currentStatus = existingTicket.StatusType.StatusName.ToLower();
            var closedStatusList = new List<string> { "resolved", "closed" };

            bool statusChanged = existingTicket.StatusTypeId != ticket.StatusTypeId;
            bool priorityChanged = existingTicket.PriorityTypeId != ticket.PriorityTypeId && ticket.PriorityTypeId != null;

            if (!statusChanged && !priorityChanged && ticketT == null)
                throw new TicketException("No changes were made to the ticket.", ticket.TicketId);

            if ((statusChanged && priorityChanged) && currentStatus == "closed" && ticketT == null)
                throw new TicketException($"Cannot update a {currentStatus} ticket.", ticket.TicketId);

            if (statusChanged)
            {
                if (currentStatus == "closed")
                    throw new TicketException($"Cannot change status of a {currentStatus} ticket.", ticket.TicketId);

                existingTicket.StatusTypeId = string.IsNullOrEmpty(ticket.StatusTypeId) ?
                    existingTicket.StatusTypeId : ticket.StatusTypeId;
            }

            if (priorityChanged)
            {
                if (closedStatusList.Contains(currentStatus))
                    throw new TicketException($"Cannot change priority of a {currentStatus} ticket.", ticket.TicketId);

                existingTicket.PriorityTypeId = string.IsNullOrEmpty(ticket.PriorityTypeId) ?
                    existingTicket.PriorityTypeId : ticket.PriorityTypeId;
            }

            existingTicket.UpdatedDate = DateTime.Now;
            await UpdateTicketDate(existingTicket);
            await _repository.UpdateAsync(existingTicket);

            if (statusChanged || priorityChanged)
            {
                await _activityLogService.LogActivityAsync(existingTicket, _httpContextAccessor.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier).Value, $"Ticket Update",
                    $"{(statusChanged ? "Status" : "")}{(statusChanged && priorityChanged ? " & " : "")}{(priorityChanged ? "Priority" : "")} modified");
                _notificationService.CreateNotification(existingTicket, statusChanged ? 3 : 2, null, existingTicket.TicketAssignment?.AgentId);
                _notificationService.CreateNotification(existingTicket, priorityChanged && !statusChanged ? 2 : 3, null, existingTicket.TicketAssignment?.TeamId);

                if (existingTicket.StatusTypeId == "S3" && existingTicket.TicketAssignment?.AgentId != null)
                    await _performanceReportService.GenerateAgentPerformanceReportAsync(existingTicket.TicketAssignment.AgentId);
            }
        }
    }
}
