using System;
using System.Collections.Generic;

namespace ASI.Basecode.Data.Models
{
    public partial class User
    {
        public User()
        {
            ActivityLogs = new HashSet<ActivityLog>();
            Comments = new HashSet<Comment>();
            Feedbacks = new HashSet<Feedback>();
            KnowledgeBaseArticles = new HashSet<KnowledgeBaseArticle>();
            TicketAssignmentAgents = new HashSet<TicketAssignment>();
            TicketAssignmentAssignedBies = new HashSet<TicketAssignment>();
            Tickets = new HashSet<Ticket>();
        }

        public string UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string RoleId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedTime { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedTime { get; set; }
        public string Preferences { get; set; }

        public virtual Admin CreatedByNavigation { get; set; }
        public virtual Role Role { get; set; }
        public virtual Admin UpdatedByNavigation { get; set; }
        public virtual PerformanceReport PerformanceReport { get; set; }
        public virtual TeamMember TeamMember { get; set; }
        public virtual ICollection<ActivityLog> ActivityLogs { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<Feedback> Feedbacks { get; set; }
        public virtual ICollection<KnowledgeBaseArticle> KnowledgeBaseArticles { get; set; }
        public virtual ICollection<TicketAssignment> TicketAssignmentAgents { get; set; }
        public virtual ICollection<TicketAssignment> TicketAssignmentAssignedBies { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}
