using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _repository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedbackService"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="mapper">The mapper.</param>
        public FeedbackService(IFeedbackRepository repository,
                            ITicketRepository ticketRepository,
                            IMapper mapper, ILogger<FeedbackService> logger)
        {
            _mapper = mapper;
            _repository = repository;
            _ticketRepository = ticketRepository;
        }

        /// <summary>
        /// Adds a ticket feedback.
        /// </summary>
        /// <param name="feedback">The feedback.</param>
        public async Task AddAsync(FeedbackViewModel feedback)
        {
            var existingFeedback = await _repository.FindFeedbackByTicketIdAsync(feedback.TicketId);
            if (existingFeedback == null)
            {
                var ticket = await _ticketRepository.FindByIdAsync(feedback.TicketId);
                var user = await _repository.FindUserByIdAsync(feedback.UserId);

                if (ticket != null && user != null)
                {
                    var newFeedback = _mapper.Map<Feedback>(feedback);
                    newFeedback.FeedbackId = Guid.NewGuid().ToString();
                    newFeedback.CreatedDate = DateTime.Now;
                    newFeedback.TicketId = feedback.TicketId;
                    newFeedback.UserId = feedback.UserId;
                    newFeedback.Ticket = ticket;
                    newFeedback.User = user;

                    ticket.Feedback = newFeedback;
                    user.Feedbacks.Add(newFeedback);

                    await _repository.AddAsync(newFeedback);
                }
            }
        }

        /// <summary>
        /// Gets all feedbacks.
        /// </summary>
        /// <returns>IEnumerable FeedbackViewModel</returns>
        public async Task<IEnumerable<FeedbackViewModel>> GetAllAsync()
        {
            var feedbacks = await _repository.GetAllAsync();
            var feedbackViewModels = _mapper.Map<IEnumerable<FeedbackViewModel>>(feedbacks).ToList();

            return feedbackViewModels;
        }

        /// <summary>
        /// Calls the repository to get feedback by its identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>FeedbackViewModel</returns>
        public async Task<FeedbackViewModel> GetFeedbackByIdAsync(string id) =>
            _mapper.Map<FeedbackViewModel>(await _repository.FindFeedbackByIdAsync(id));

        /// <summary>
        /// Calls the repository to get feedback by ticket identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>FeedbackViewModel</returns>
        public async Task<FeedbackViewModel> GetFeedbackByTicketIdAsync(string id) =>
            _mapper.Map<FeedbackViewModel>(await _repository.FindFeedbackByTicketIdAsync(id));
    }
}
