using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Services
{
    public  class PerformanceReportService : IPerformanceReportService
    {
        private readonly IPerformanceReportRepository _performanceReportRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IMapper _mapper;

        public PerformanceReportService(IPerformanceReportRepository performanceReportRepository, ITicketRepository ticketRepository, IMapper mapper)
        {
            _mapper = mapper;
            _performanceReportRepository = performanceReportRepository;
            _ticketRepository = ticketRepository;
        }

        public async Task UpdatePerformanceReportAsync(Team team)
        {
            foreach (var user in team.TeamMembers)
            {
                var tickets = await _ticketRepository.GetResolvedTicketsByUserIdAsync(user.UserId);
                var resolvedTickets = tickets.Count();
                var averageResolutionTime = tickets.Any() ? tickets.Average(t => (t.ResolvedDate - t.CreatedDate).Value.TotalMinutes) : 0;

                var performanceReport = await _performanceReportRepository.FindByUserIdAsync(user.UserId);

                if (performanceReport != null)
                {
                    performanceReport.ResolvedTickets = resolvedTickets;
                    performanceReport.AverageResolutionTime = averageResolutionTime;
                    await _performanceReportRepository.UpdateAsync(performanceReport);
                }
            }
        }

        public PerformanceReportViewModel GetPerformanceReportByUserId(string userId)
        {
            var performanceReport = _performanceReportRepository.FindByUserId(userId);
            return _mapper.Map<PerformanceReportViewModel>(performanceReport);
        }

    }
}
