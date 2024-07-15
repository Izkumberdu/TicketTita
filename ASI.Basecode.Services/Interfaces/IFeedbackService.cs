using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IFeedbackService
    {
        Task AddAsync(FeedbackViewModel feedback);
        Task<IEnumerable<FeedbackViewModel>> GetAllAsync();
        Task<FeedbackViewModel> GetFeedbackByIdAsync(string id);
        Task<FeedbackViewModel> GetFeedbackByTicketIdAsync(string id);
    }
}
