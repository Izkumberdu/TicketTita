using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Repositories
{
    public class TicketRepository : BaseRepository, ITicketRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TicketRepository"/> class.
        /// </summary>
        /// <param name="unitOfWork"></param>
        public TicketRepository(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        #region Ticket Service Methods
        /// <summary>
        /// Get all tickets with includes to related entities
        /// </summary>
        /// <returns>IQueryable Ticket</returns>
        private IQueryable<Ticket> GetTicketsWithIncludes()
        {
            return this.GetDbSet<Ticket>()
                    .Include(t => t.CategoryType)
                    .Include(t => t.PriorityType)
                    .Include(t => t.StatusType)
                    .Include(t => t.User)
                    .Include(t => t.Feedback)
                    .Include(t => t.Attachments)
                    .Include(t => t.TicketAssignment)
                        .ThenInclude(ta => ta.Team)
                        .ThenInclude(team => team.TeamMembers)
                        .ThenInclude(tm => tm.User);
        }

        /// <summary>
        /// Get all tickets
        /// </summary>
        /// <returns>List Ticket</returns>
        public async Task<List<Ticket>> GetAllAsync()
        {
            var tickets = await GetTicketsWithIncludes().ToListAsync();
            return tickets;
        }
        /// <summary>
        /// Retrieves all Non Asycn.
        /// </summary>
        /// <returns></returns>
        public IQueryable<Ticket> RetrieveAll() { 
            var tickets = GetTicketsWithIncludes();
            return tickets;
        }
        public async Task<List<Ticket>> GetAllAsync() => 
            await GetTicketsWithIncludes().ToListAsync();

        /// <summary>
        /// Add a ticket
        /// </summary>
        /// <param name="ticket">The ticket</param>
        public async Task AddAsync(Ticket ticket)
        {
            await this.GetDbSet<Ticket>().AddAsync(ticket);
            await UnitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Update a ticket
        /// </summary>
        /// <param name="ticket">The ticket</param>
        public async Task UpdateAsync(Ticket ticket)
        {
            this.GetDbSet<Ticket>().Update(ticket);
            await UnitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Delete a ticket
        /// </summary>
        /// <param name="ticket">The ticket</param>
        public async Task DeleteAsync(Ticket ticket)
        {
            this.GetDbSet<Ticket>().Remove(ticket);
            await UnitOfWork.SaveChangesAsync();
        }
        #endregion Ticket Service Methods

        #region Attachment Service Methods
        /// <summary>
        /// Add an attachment
        /// </summary>
        /// <param name="attachment">The attachment</param>
        public async Task AddAttachmentAsync(Attachment attachment)
        {
            await this.GetDbSet<Attachment>().AddAsync(attachment);
            await UnitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Remove an attachment
        /// </summary>
        /// <param name="attachment">The attachment</param>
        public async Task RemoveAttachmentAsync(Attachment attachment)
        {
            this.GetDbSet<Attachment>().Remove(attachment);
            await UnitOfWork.SaveChangesAsync();
        }
        #endregion Attachment Service Methods

        #region Ticket Assignment Service Methods  
        /// <summary>
        /// Add a ticket assignment
        /// </summary>
        /// <param name="assignment">The assignment</param>
        public async Task AssignTicketAsync(TicketAssignment assignment)
        {
            await this.GetDbSet<TicketAssignment>().AddAsync(assignment);
            await UnitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Remove a ticket assignment
        /// </summary>
        /// <param name="assignment">The assignment</param>
        public async Task RemoveAssignmentAsync(TicketAssignment assignment)
        {
            this.GetDbSet<TicketAssignment>().Remove(assignment);
            await UnitOfWork.SaveChangesAsync();
        }
        #endregion Ticket Assignment Service Methods

        #region Feedback Service Methods
        public async Task FeedbackDeleteAsync(Feedback feedback)
        {
            this.GetDbSet<Feedback>().Remove(feedback);
            await UnitOfWork.SaveChangesAsync();
        }
        #endregion Feedback Service Methods

        #region Notification Service Methods
        public async Task NotificationDeleteAsync(string id)
        {
            var notifications = this.GetDbSet<Notification>().Where(n => n.TicketId == id);
            this.GetDbSet<Notification>().RemoveRange(notifications);
            await UnitOfWork.SaveChangesAsync();
        }
        #endregion Notification Service Methods

        #region Find Methods
        /// <summary>
        /// Find a ticket by id
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        /// <returns>Ticket</returns>
        public async Task<Ticket> FindByIdAsync(string id) 
            => await GetTicketsWithIncludes().FirstOrDefaultAsync(t => t.TicketId == id);

        /// <summary>
        /// Find a ticket by user id
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <returns>Ticket</returns>
        public async Task<IEnumerable<Ticket>> FindByUserIdAsync(string id) =>
            await GetTicketsWithIncludes().Where(t => t.UserId == id).ToListAsync();

        /// <summary>
        /// Find an attachment by its identifier
        /// </summary>
        /// <param name="id">Attachment identifier</param>
        /// <returns>Attachment</returns>
        public async Task<Attachment> FindAttachmentByIdAsync(string id) 
            => await this.GetDbSet<Attachment>().FirstOrDefaultAsync(x => x.AttachmentId == id);

        /// <summary>
        /// Find an attachment by ticket identifier
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        /// <returns>Attachment</returns>
        public async Task<Attachment> FindAttachmentByTicketIdAsync(string id) 
            => await this.GetDbSet<Attachment>().FirstOrDefaultAsync(x => x.TicketId == id);

        /// <summary>
        /// Find a ticket assignment by ticket identifier
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        /// <returns>TicketAssignment</returns>
        public async Task<TicketAssignment> FindAssignmentByTicketIdAsync(string id) 
            => await this.GetDbSet<TicketAssignment>().FirstOrDefaultAsync(x => x.TicketId == id);

        /// <summary>
        /// Find a team by a member's user identifier
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <returns>Team</returns>
        public async Task<Team> FindTeamByUserIdAsync(string id)
        {
            var memberOf = await this.GetDbSet<TeamMember>().FirstOrDefaultAsync(x => x.UserId == id);
            return await this.GetDbSet<Team>().FirstOrDefaultAsync(x => x.TeamId == memberOf.TeamId);
        }

        /// <summary>
        /// Find an agent by user identifier
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <returns>User</returns>
        public async Task<User> FindAgentByUserIdAsync(string id) 
            => await this.GetDbSet<User>().FirstOrDefaultAsync(x => x.UserId == id);

        /// <summary>
        /// Find a category by its identifier
        /// </summary>
        /// <param name="id">CategoryType identifier</param>
        /// <returns>CategoryType</returns>
        public async Task<CategoryType> FindCategoryByIdAsync(string id) 
            => await this.GetDbSet<CategoryType>().FirstOrDefaultAsync(x => x.CategoryTypeId == id);

        /// <summary>
        /// Find a priority by its identifier
        /// </summary>
        /// <param name="id">PriorityType identifier</param>
        /// <returns>PriorityType</returns>
        public async Task<PriorityType> FindPriorityByIdAsync(string id) 
            => await this.GetDbSet<PriorityType>().FirstOrDefaultAsync(x => x.PriorityTypeId == id);

        /// <summary>
        /// Find a status by its identifier
        /// </summary>
        /// <param name="id">StatusType identifier</param>
        /// <returns>StatusType</returns>
        public async Task<StatusType> FindStatusByIdAsync(string id) 
            => await this.GetDbSet<StatusType>().FirstOrDefaultAsync(x => x.StatusTypeId == id);

        /// <summary>
        /// Find a user by its identifier
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <returns>User</returns>
        public async Task<User> UserFindByIdAsync(string id) 
            => await this.GetDbSet<User>().FirstOrDefaultAsync(x => x.UserId == id);
        
        /// <summary>
        /// Find a feedback by ticket identifier
        /// </summary>
        /// <param name="id">Ticket identifier</param>
        /// <returns>Feedback</returns>
        public async Task<Feedback> FeedbackFindByTicketIdAsync(string id) 
            => await this.GetDbSet<Feedback>().FirstOrDefaultAsync(x => x.TicketId == id);
        
        /// <summary>
        /// Find an admin by its identifier
        /// </summary>
        /// <param name="id">Admin identifier</param>
        /// <returns>Admin</returns>
        public async Task<Admin> AdminFindByIdAsync(string id) 
            => await this.GetDbSet<Admin>().FirstOrDefaultAsync(x => x.AdminId == id);

        public async Task<IQueryable<Notification>> FindNotificationsByTicketIdAsync(string id)
            => await Task.FromResult(this.GetDbSet<Notification>().Where(x => x.TicketId == id));
        #endregion Find Methods

        #region Get Methods
        /// <summary>
        /// Get all category types
        /// </summary>
        /// <returns>IQueryable CategoryType</returns>
        public async Task<IQueryable<CategoryType>> GetCategoryTypesAsync() 
            => await Task.FromResult(this.GetDbSet<CategoryType>());

        /// <summary>
        /// Get all priority types
        /// </summary>
        /// <returns>IQueryable PriorityType</returns>
        public async Task<IQueryable<PriorityType>> GetPriorityTypesAsync() 
            => await Task.FromResult(this.GetDbSet<PriorityType>());

        /// <summary>
        /// Get all status types
        /// </summary>
        /// <returns>IQueryable StatusType</returns>
        public async Task<IQueryable<StatusType>> GetStatusTypesAsync() 
            => await Task.FromResult(this.GetDbSet<StatusType>());

        /// <summary>
        /// Get all support agents
        /// </summary>
        /// <returns>IQueryable User</returns>
        public async Task<IQueryable<User>> GetSupportAgentsAsync() 
            => await Task.FromResult(this.GetDbSet<User>().Where(x => x.RoleId == "Support Agent"));

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns>IQueryable User</returns>
        public async Task<IQueryable<User>> UserGetAllAsync() 
            => await Task.FromResult(this.GetDbSet<User>());

        /// <summary>
        /// Get all ticket assignments
        /// </summary>
        /// <returns>IQueryable TicketAssignment</returns>
        public async Task<IQueryable<TicketAssignment>> GetTicketAssignmentsAsync() 
            => await Task.FromResult(this.GetDbSet<TicketAssignment>());

        /// <summary>
        /// Get all user identifiers with tickets submitted
        /// </summary>
        /// <returns>IQueryable string</returns>
        public async Task<IQueryable<string>> GetUserIdsWithTicketsAsync() 
            => await Task.FromResult(this.GetDbSet<Ticket>().Select(x => x.UserId).Distinct());
        #endregion Get Methods
    }
}
