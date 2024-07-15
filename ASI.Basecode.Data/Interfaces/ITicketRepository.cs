using ASI.Basecode.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Interfaces
{
    public interface ITicketRepository
    {
        Task<List<Ticket>> GetAllAsync();
        Task AddAsync(Ticket ticket);
        Task UpdateAsync(Ticket ticket);
        Task DeleteAsync(Ticket ticket);

        IQueryable<Ticket> RetrieveAll();
        Task AddAttachmentAsync(Attachment attachment);
        Task RemoveAttachmentAsync(Attachment attachment);
        Task AssignTicketAsync(TicketAssignment assignment);
        Task RemoveAssignmentAsync(TicketAssignment assignment);
        Task<Ticket> FindByIdAsync(string id);
        Task<IEnumerable<Ticket>> FindByUserIdAsync(string id);
        Task<Attachment> FindAttachmentByIdAsync(string id);
        Task<Attachment> FindAttachmentByTicketIdAsync(string id);
        Task<TicketAssignment> FindAssignmentByTicketIdAsync(string id);
        Task<Team> FindTeamByUserIdAsync(string id);
        Task<User> FindAgentByUserIdAsync(string id);
        Task<CategoryType> FindCategoryByIdAsync(string id);
        Task<PriorityType> FindPriorityByIdAsync(string id);
        Task<StatusType> FindStatusByIdAsync(string id);
        Task<IQueryable<Notification>> FindNotificationsByTicketIdAsync(string id);
        Task<IQueryable<CategoryType>> GetCategoryTypesAsync();
        Task<IQueryable<PriorityType>> GetPriorityTypesAsync();
        Task<IQueryable<StatusType>> GetStatusTypesAsync();
        Task<IQueryable<User>> GetSupportAgentsAsync();
        Task<IQueryable<TicketAssignment>> GetTicketAssignmentsAsync();

        Task<IQueryable<string>> GetUserIdsWithTicketsAsync();
        Task<IQueryable<User>> UserGetAllAsync();
        Task<User> UserFindByIdAsync(string id);
        Task FeedbackDeleteAsync(Feedback feedback);
        Task NotificationDeleteAsync(string id);
        Task<Feedback> FeedbackFindByTicketIdAsync(string id);
        Task<Admin> AdminFindByIdAsync(string id);
    }
}
