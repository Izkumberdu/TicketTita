using ASI.Basecode.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public class PerformanceReportViewModel
    {
        public string ReportId { get; set; }
        public string UserId { get; set; }
        public int ResolvedTickets { get; set; }
        public double AverageResolutionTime { get; set; }
        public DateTime AssignedDate { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<TeamMember> TeamMembers { get; set; }
    }
}
