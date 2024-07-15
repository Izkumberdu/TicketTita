using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.WebApp.Authentication;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASI.Basecode.WebApp.Controllers
{
    public class FeedbackController : ControllerBase<FeedbackController>
    {
        private readonly IFeedbackService _feedbackService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedbackController"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="feedbackService">The feedback service.</param>
        /// <param name="tokenValidationParametersFactory">The token validation parameters factory.</param>
        /// <param name="tokenProviderOptionsFactory">The token provider options factory.</param>
        public FeedbackController(
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper,
            IFeedbackService feedbackService,
            TokenValidationParametersFactory tokenValidationParametersFactory,
            TokenProviderOptionsFactory tokenProviderOptionsFactory) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            this._feedbackService = feedbackService;
        }

        /// <summary>
        /// Allows the user to provide a feedback.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Success result</returns>
        [HttpPost]
        [Authorize(Policy = "Employee")]
        public async Task<IActionResult> ProvideFeedback(FeedbackViewModel model)
        {
            return await HandleExceptionAsync(async () =>
            {
                if (ModelState.IsValid)
                {
                    if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.TicketId))
                        return RedirectToAction("ViewAll");

                    await _feedbackService.AddAsync(model);

                    TempData["SuccessMessage"] = "Thank you for your feedback!";
                    return Json(new { success = true });
                }
                TempData["ErrorMessage"] = "An error occurred while submitting your feedback. Please try again.";
                return Json(new { success = false });
            }, "ProvideFeedback");
        }
    }
}
