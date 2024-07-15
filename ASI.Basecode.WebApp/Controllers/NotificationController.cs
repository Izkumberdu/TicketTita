using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Services;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;

namespace ASI.Basecode.WebApp.Controllers
{
    public class NotificationController : ControllerBase<NotificationController>
    {
        private readonly INotificationService _notificationService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationController(INotificationService notificationService,
                                      IHttpContextAccessor httpContextAccessor,
                                      ILoggerFactory loggerFactory,
                                      IConfiguration configuration,
                                      INotificationRepository notificationRepository,
                                      IMapper mapper = null) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _notificationService = notificationService;
            _httpContextAccessor = httpContextAccessor;
            _notificationRepository = notificationRepository;
        }

        public IActionResult Index()
        {

            var userId = _httpContextAccessor.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var notifications = _notificationService.RetrieveAll(userId).OrderByDescending(x => x.NotificationDate);

            return View(notifications);
        }

        public IActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var notification = _notificationRepository.FindById(id);
            if (notification == null)
            {
                return NotFound();
            }

            _notificationRepository.Delete(notification.NotificationId);

            return Ok();
        }
        #region Mark As Read

        /// <summary>
        /// Marks as read.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public IActionResult MarkAsRead(string id)
        {
            _notificationService.MarkNotificationAsRead(id);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Marks all as read.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public IActionResult MarkAllAsRead()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _notificationService.MarkAllNotificationsAsRead(userId);
            return RedirectToAction("Index");
        }
        #endregion

        #region Mark As Unread
        /// <summary>
        /// Marks as unread.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public IActionResult MarkAsUnread(string id)
        {
            _notificationService.MarkNotificationAsUnread(id);
            return RedirectToAction("Index");
        }
      
        /// <summary>
        /// Marks all as unread.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public IActionResult MarkAllAsUnread()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _notificationService.MarkAllNotificationsAsUnread(userId);
            return RedirectToAction("Index");
        }

        #endregion  
    }
}
