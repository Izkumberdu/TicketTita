using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Data.Repositories;
using ASI.Basecode.Services.Exceptions;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ASI.Basecode.Services.Exceptions.TeamExceptions;
using static ASI.Basecode.Resources.Constants.Enums;
using System.Collections.Generic;

namespace ASI.Basecode.Services.Services
{
    public class PerformanceReportService : IPerformanceReportService
    {
        private readonly IPerformanceReportRepository _performanceReportRepository;
        private readonly ITeamRepository _teamRepository;

        public PerformanceReportService(IPerformanceReportRepository performanceReportRepository, ITeamRepository teamRepository)
        {
            _performanceReportRepository = performanceReportRepository;
            _teamRepository = teamRepository;
        }

        public async Task<PerformanceReport> ViewAgentPerformanceReportAsync(string agentId)
        {
            if (string.IsNullOrEmpty(agentId)) throw new TeamException("Agent does not exist.");

            var report = await _performanceReportRepository.GetPerformanceReportByAgentIdAsync(agentId);
            if(report == null)
            {
                var performanceReport = new PerformanceReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    ResolvedTickets = 0,
                    AverageResolutionTime = 0.0,
                    AssignedDate = DateTime.UtcNow,
                    UserId = agentId
                };
                await _performanceReportRepository.AddPerformanceReportAsync(performanceReport);
            }
            return report;
        }


        public async Task<PerformanceReport> GenerateAgentPerformanceReportAsync(string agentId)
        {
            var agent = await _teamRepository.FindAgentByIdAsync(agentId);
            if (agent != null)
            {
                var performanceReport = agent.PerformanceReport;
                var resolvedTickets = await _teamRepository.GetResolvedTicketsAssignedToAgentAsync(agentId);

                performanceReport.ResolvedTickets = resolvedTickets.Count;
                var resolutionTime = new List<double>();
                foreach(var ticket in resolvedTickets)
                {
                    resolutionTime.Add((ticket.ResolvedDate.Value - ticket.CreatedDate).TotalMinutes);
                }
                performanceReport.AverageResolutionTime = resolutionTime.Average();
                return performanceReport;
            }
            return await ViewAgentPerformanceReportAsync(agentId);
        }

        public async Task<PerformanceReportViewModel> GetPerformanceReport(string userId)
        {
            var user = await _teamRepository.FindAgentByIdAsync(userId);
            if (user != null)
            {
                var performanceReport = user.PerformanceReport;
                var tickets = await _teamRepository.GetResolvedTicketsAssignedToAgentAsync(userId);
                if (performanceReport != null)
                {
                    return new PerformanceReportViewModel
                    {
                        ReportId = performanceReport.ReportId,
                        ResolvedTickets = performanceReport.ResolvedTickets,
                        AverageResolutionTime = performanceReport.AverageResolutionTime,
                        AssignedDate = performanceReport.AssignedDate,
                        Name = user.Name,
                        AverageRating = tickets.Select(t => t.Feedback.FeedbackRating).Average(),
                        Feedbacks = tickets.Select(t => t.Feedback).ToList()
                    };
                }
            }
            return null;
        }
    }
}
