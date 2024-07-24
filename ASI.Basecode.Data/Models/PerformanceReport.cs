using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class PerformanceReport
    {
        public string ReportId { get; set; }
        public int ResolvedTickets { get; set; }
        public double AverageResolutionTime { get; set; }
        public DateTime AssignedDate { get; set; }
        public string UserId { get; set; }

        public virtual User User { get; set; }
    }
}
