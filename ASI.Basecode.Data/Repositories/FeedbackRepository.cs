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
    public class FeedbackRepository : BaseRepository, IFeedbackRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeedbackRepository"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        public FeedbackRepository(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        /// <summary>
        /// Gets all feedbacks with includes.
        /// </summary>
        /// <returns>A list of all feedbacks Feedback (IQueryable)</returns>
        private IQueryable<Feedback> GetFeedbacksWithIncludes()
        {
            return this.GetDbSet<Feedback>()
                        .Include(f => f.Ticket)
                        .Include(f => f.User);
        }

        public async Task<List<Feedback>> GetAllAsync() =>
            await GetFeedbacksWithIncludes().ToListAsync();

        public async Task AddAsync(Feedback feedback)
        {
            await this.GetDbSet<Feedback>().AddAsync(feedback);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(Feedback feedback)
        {
            this.GetDbSet<Feedback>().Remove(feedback);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task<Feedback> FindFeedbackByIdAsync(string id) =>
            await GetFeedbacksWithIncludes().FirstOrDefaultAsync(x => x.FeedbackId == id);

        public async Task<Feedback> FindFeedbackByTicketIdAsync(string id) =>
            await GetFeedbacksWithIncludes().FirstOrDefaultAsync(x => x.TicketId == id);

        private IQueryable<User> GetUsersWithIncludes() =>
            this.GetDbSet<User>().Include(u => u.Feedbacks)
                                 .Include(u => u.Tickets);

        public async Task<User> FindUserByIdAsync(string id) =>
            await GetUsersWithIncludes().FirstOrDefaultAsync(x => x.UserId == id);
    }
}
