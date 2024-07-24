using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
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
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.CategoryType)
                    .Include(t => t.PriorityType)
                    .Include(t => t.StatusType)
                    .Include(t => t.User)
                    .Include(t => t.Feedback)
                    .Include(t => t.Attachments)
                    .Include(t => t.ActivityLogs)
                        .ThenInclude(t => t.User)
                    .Include(t => t.Comments)
                        .ThenInclude(cu => cu.User)
                    .Include(t => t.Comments)
                        .ThenInclude(cp => cp.Parent)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.InverseParent)
                    .Include(t => t.TicketAssignment)
                        .ThenInclude(ta => ta.Agent)
                    .Include(t => t.TicketAssignment)
                        .ThenInclude(ta => ta.Team);
        }

        private IQueryable<Ticket> GetTicketsWithLimitedIncludes()
        {
            return this.GetDbSet<Ticket>()
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.CategoryType)
                    .Include(t => t.PriorityType)
                    .Include(t => t.StatusType)
                    .Include(t => t.User)
                    .Include(t => t.Feedback)
                    .Include (t => t.ActivityLogs)
                        .ThenInclude(t => t.User)
                    .Include(t => t.TicketAssignment)
                        .ThenInclude(ta => ta.Agent)
                    .Include(t => t.TicketAssignment)
                        .ThenInclude(ta => ta.Team);
        }

        /// <summary>
        /// Get all tickets
        /// </summary>
        /// <returns>List Ticket</returns>
        public async Task<List<Ticket>> GetAllAsync() =>
            await GetTicketsWithLimitedIncludes().AsNoTracking().ToListAsync();

        /// <summary>
        /// Gets all tickets including the deleted.
        /// </summary>
        /// <returns>List Ticket</returns>
        public async Task<int> CountAllAndDeletedTicketsAsync() =>
            await this.GetDbSet<Ticket>().AsNoTracking().CountAsync();

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
            ticket.IsDeleted = true;
            this.GetDbSet<Ticket>().Update(ticket);
            await UnitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Hard delete a ticket
        /// </summary>
        /// <param name="ticket">The ticket</param>
        public async Task DeleteHardAsync(Ticket ticket)
        {
            ticket.CategoryTypeId = null;
            ticket.PriorityTypeId = null;
            ticket.StatusTypeId = null;
            ticket.UserId = null;

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
        /// Updates a ticket assignment.
        /// </summary>
        /// <param name="assignment">The assignment.</param>
        public async Task UpdateAssignmentAsync(TicketAssignment assignment)
        {
            this.GetDbSet<TicketAssignment>().Update(assignment);
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

        #region Comment Service Methods        
        /// <summary>
        /// Adds the comment.
        /// </summary>
        /// <param name="comment">The comment.</param>
        public async Task AddCommentAsync(Comment comment)
        {
            this.GetDbSet<Comment>().Add(comment);
            await UnitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Updates the comment.
        /// </summary>
        /// <param name="comment">The comment.</param>
        public async Task UpdateCommentAsync(Comment comment)
        {
            this.GetDbSet<Comment>().Update(comment);
            await UnitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes the comment.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public async Task DeleteCommentAsync(string id)
        {
            var comment = await FindCommentByIdAsync(id);
            await DeleteCommentAndChildrenRecursive(comment);
            await UnitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes the comment and children recursively.
        /// </summary>
        /// <param name="comment">The comment.</param>
        private async Task DeleteCommentAndChildrenRecursive(Comment comment)
        {
            await Context.Entry(comment).Collection(c => c.InverseParent).LoadAsync();

            foreach (var childComment in comment.InverseParent.ToList())
            {
                await DeleteCommentAndChildrenRecursive(childComment);
                this.GetDbSet<Comment>().Remove(childComment);
            }

            this.GetDbSet<Comment>().Remove(comment);
        }

        /// <summary>
        /// Gets the comments with includes.
        /// </summary>
        /// <returns>IQueryable Comment</returns>
        public IQueryable<Comment> GetCommentsWithIncludesAsync()
        {
            var comments = this.GetDbSet<Comment>()
                                .Include(cu => cu.User)
                                .Include(cp => cp.Parent)
                                .Include(c => c.InverseParent);
            return comments;
        }

        /// <summary>
        /// Finds the comment by identifier asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Comment</returns>
        public async Task<Comment> FindCommentByIdAsync(string id) =>
            await GetCommentsWithIncludesAsync().FirstOrDefaultAsync(c => c.CommentId == id);
        #endregion

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
            => await Task.FromResult(this.GetDbSet<User>().Include(x => x.Tickets));

        /// <summary>
        /// Get all user identifiers with tickets submitted
        /// </summary>
        /// <returns>IQueryable string</returns>
        public async Task<IQueryable<string>> GetUserIdsWithTicketsAsync() 
            => await Task.FromResult(this.GetDbSet<Ticket>().Select(x => x.UserId).Distinct());
        #endregion Get Methods
    }
}
