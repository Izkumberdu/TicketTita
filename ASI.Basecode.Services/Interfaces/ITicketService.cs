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

        Task AddAsync(TicketViewModel ticket, string userId);
        Task UpdateAsync(TicketViewModel ticket,  int UpdateType);
        Task DeleteAsync(string id);
        Task AddAttachmentAsync(Attachment attachment, Ticket ticket);
        Task RemoveAttachmentAsync(string attachmentId);
        Task AddTicketAssignmentAsync(TicketViewModel model,  bool Reassign);
        Task RemoveAssignmentAsync(string id);
        Task<IEnumerable<TicketViewModel>> GetAllAsync();
        Task<TicketViewModel> GetTicketByIdAsync(string id);
        Task<IEnumerable<TicketViewModel>> GetUnresolvedTicketsAsync();
        Task<IEnumerable<TicketViewModel>> GetTicketsByAssignmentStatusAsync(string status, List<string> assignedTicketIds);
        Task<object> GetTicketDetailsAsync(string id);
        Task<Attachment> GetAttachmentByTicketIdAsync(string id);
        Task<TicketAssignment> GetAssignmentByTicketIdAsync(string id);
        Task<Team> GetTeamByUserIdAsync(string id);
        Task<User> GetAgentByIdAsync(string id);
        Task<IEnumerable<CategoryType>> GetCategoryTypesAsync();
        Task<IEnumerable<PriorityType>> GetPriorityTypesAsync();
        Task<IEnumerable<StatusType>> GetStatusTypesAsync();
        Task<IEnumerable<User>> GetSupportAgentsAsync();
        Task<IEnumerable<TicketAssignment>> GetTicketAssignmentsAsync();
        Task<TicketViewModel> InitializeModelAsync(string type);
        string ExtractAgentId(string id);
        IEnumerable<TicketViewModel> GetUnresolvedTicketsOlderThan(TimeSpan timeSpan);
        Task<IEnumerable<string>> GetUserIdsWithTicketsAsync();
        Task<IEnumerable<User>> UserGetAllAsync();

        Task<IEnumerable<TicketViewModel>> GetFilteredAndSortedTicketsAsync(string sortBy, string filterBy, string filterValue);
    }
}
