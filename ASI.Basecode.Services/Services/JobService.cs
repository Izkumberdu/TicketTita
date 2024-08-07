﻿using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Services
{
    public class JobService: Quartz.IJob
    {
        private readonly INotificationService _notificationService;
        private readonly ITicketService _ticketService;
        private readonly ILogger<JobService> _logger;

        public JobService(INotificationService notificationService, ITicketService ticketService, ILogger<JobService> logger)
        {
            _notificationService = notificationService;
            _ticketService = ticketService;
            _logger = logger;
        }
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.ITrigger" />
        /// fires that is associated with the <see cref="T:Quartz.IJob" />.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns></returns>
        /// <remarks>
        /// The implementation may wish to set a  result object on the
        /// JobExecutionContext before this method exits.  The result itself
        /// is meaningless to Quartz, but may be informative to
        /// <see cref="T:Quartz.IJobListener" />s or
        /// <see cref="T:Quartz.ITriggerListener" />s that are watching the job's
        /// execution.
        /// </remarks>
        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Executing ReminderJob...");

            var unresolvedTickets = _ticketService.GetUnresolvedTicketsOlderThan(TimeSpan.FromMinutes(5));

            foreach (var ticket in unresolvedTickets)
            {
                if(ticket.Agent != null)
                {
                    _notificationService.AddNotification(
                        ticketId: ticket.TicketId,
                        description: "This ticket has been unresolved for over 30 minutes.",
                        notificationTypeId: "8",
                        UserId: ticket.Agent.UserId,
                        title: $"Reminder: Ticket #{ticket.TicketId} Unresolved"
                    );
                }
            }

            return Task.CompletedTask;
        }

    }
}
