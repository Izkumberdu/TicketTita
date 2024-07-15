using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class Team
    {
        public Team()
        {
            TeamMembers = new HashSet<TeamMember>();
            TicketAssignments = new HashSet<TicketAssignment>();
        }

        public string TeamId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDeleted { get; set; }

        public virtual ICollection<TeamMember> TeamMembers { get; set; }
        public virtual ICollection<TicketAssignment> TicketAssignments { get; set; }
    }
}
