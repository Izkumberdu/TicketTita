using ASI.Basecode.Data.Interfaces;
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
    public class JobService: IJob
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

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Executing ReminderJob...");

            var unresolvedTickets = _ticketService.GetUnresolvedTicketsOlderThan(TimeSpan.FromMinutes(30));

            foreach (var ticket in unresolvedTickets)
            {
                _notificationService.AddNotification(
                    ticketId: ticket.TicketId,
                    description: "This ticket has been unresolved for over 30 minutes.",
                    notificationTypeId: "7",
                    UserId: ticket.User.UserId,
                    title: $"Reminder: Ticket #{ticket.TicketId} Unresolved"
                );
            }

            return Task.CompletedTask;
        }

    }
}
