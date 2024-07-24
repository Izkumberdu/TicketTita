using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Interfaces
{
    public interface ITicketService
    {
        Task AddAsync(TicketViewModel model, string userId);
        Task UpdateAsync(TicketViewModel model, int updateType);
        Task DeleteAsync(string id);
        Task<string> UpdateAssignmentAsync(TicketViewModel model);
        Task AddAttachmentAsync(Attachment attachment, Ticket ticket);
        Task AddCommentAsync(CommentViewModel model);
        Task UpdateCommentAsync(CommentViewModel model);
        Task DeleteCommentAsync(string commentId);
        Task<List<TicketViewModel>> GetAllAsync();
        Task<TicketViewModel> GetTicketByIdAsync(string id);
        Task<TicketViewModel> GetFilteredTicketByIdAsync(string id);
        Task<PaginatedList<TicketViewModel>> GetFilteredAndSortedTicketsAsync(string showOption, string sortBy, List<string> selectedFilters, string search, bool clearFilters, int pageIndex, int pageSize);
        Task<Attachment> GetAttachmentByTicketIdAsync(string id);
        Task<TicketAssignment> GetAssignmentByTicketIdAsync(string id);
        Task<Team> GetTeamByUserIdAsync(string id);
        Task<IEnumerable<CategoryType>> GetCategoryTypesAsync();
        Task<IEnumerable<PriorityType>> GetPriorityTypesAsync();
        Task<IEnumerable<StatusType>> GetStatusTypesAsync();
        Task<IEnumerable<User>> GetSupportAgentsAsync();
        IEnumerable<TicketViewModel> GetUnresolvedTicketsOlderThan(TimeSpan timeSpan);
        Task<IEnumerable<string>> GetUserIdsWithTicketsAsync();
        Task<IEnumerable<User>> UserGetAllAsync();
        Task UpdateTrackingAsync(TicketViewModel ticketV = null, Ticket ticketT = null);
        Task<IEnumerable<ActivityLog>> GetActivityLogsByTicketIdAsync(string ticketId);
    }
}
