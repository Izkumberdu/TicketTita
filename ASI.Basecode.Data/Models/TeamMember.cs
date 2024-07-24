using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class TeamMember
    {
        public string TeamId { get; set; }
        public string UserId { get; set; }

        public virtual Team Team { get; set; }
        public virtual User User { get; set; }
    }
}
