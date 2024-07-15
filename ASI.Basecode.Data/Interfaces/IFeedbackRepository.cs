using ASI.Basecode.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Interfaces
{
    public interface IFeedbackRepository
    {
        Task<List<Feedback>> GetAllAsync();
        Task AddAsync(Feedback feedback);
        Task DeleteAsync(Feedback feedback);
        Task<Feedback> FindFeedbackByIdAsync(string id);
        Task<Feedback> FindFeedbackByTicketIdAsync(string id);
        Task<User> FindUserByIdAsync(string id);
    }
}
