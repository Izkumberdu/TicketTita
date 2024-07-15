using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASI.Basecode.Services.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IMapper _mapper;
        private readonly INotificationRepository _notificationRepository;
        private readonly ITicketRepository _ticketRepository;

        public NotificationService(IMapper mapper, INotificationRepository notificationRepository, ITicketRepository ticketRepository)
        {
            _notificationRepository = notificationRepository;
            _ticketRepository = ticketRepository;
            _mapper = mapper;
        }
        /// <summary>
        /// Retrieves all.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns></returns>
        public IEnumerable<NotificationViewModel> RetrieveAll(string userId)
        {
            var notification = _notificationRepository.RetrieveAll().Where(x => x.UserId == userId).ToList();
            var data = notification.Select(s => new NotificationViewModel
            {
                NotificationId = s.NotificationId,
                NotificationDate = s.NotificationDate,
                NotificationTypeId = s.NotificationTypeId,
                TicketId = s.TicketId,
                Title = s.Title,
                UserId = s.UserId,
                Description = s.Description,
                IsRead = s.IsRead
            });

            return data;
        }
        /// <summary>
        /// Marks the notification as read.
        /// </summary>
        /// <param name="notificationId">The notification identifier.</param>
        public void MarkNotificationAsRead(string notificationId)
        {
            var notification = _notificationRepository.FindById(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                _notificationRepository.Update(notification);
            }
        }
        /// <summary>
        /// Marks the notification as unread.
        /// </summary>
        /// <param name="notificationId">The notification identifier.</param>
        public void MarkNotificationAsUnread(string notificationId)
        {
            var notification = _notificationRepository.FindById(notificationId);
            if (notification != null)
            {
                notification.IsRead = false;
                _notificationRepository.Update(notification);
            }
        }
        /// <summary>
        /// Marks all notifications as unread.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        public void MarkAllNotificationsAsUnread(string userId)
        {
            var notifications = _notificationRepository.RetrieveAll().Where(n => n.UserId == userId).ToList();
            foreach (var notification in notifications)
            {
                notification.IsRead = false;
                _notificationRepository.Update(notification);
            }
        }
        /// <summary>
        /// Marks all notifications as read.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        public void MarkAllNotificationsAsRead(string userId)
        {
            var notifications = _notificationRepository.RetrieveAll().Where(n => n.UserId == userId).ToList();
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                _notificationRepository.Update(notification);
            }
        }
        /// <summary>
        /// Gets the unread notifications.
        /// </summary>
        /// <param name="UserId">The user identifier.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void GetUnreadNotifications(string UserId)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Adds the notification.
        /// </summary>
        /// <param name="ticketId">The ticket identifier.</param>
        /// <param name="description">The description.</param>
        /// <param name="notificationTypeId">The notification type identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="title">The title.</param>
        public void AddNotification(string ticketId, string description, string notificationTypeId, string userId, string title)
        {
                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    NotificationDate = DateTime.Now,
                    TicketId = ticketId,
                    Description = description,
                    Title = title,
                    UserId = userId,
                    NotificationTypeId = notificationTypeId
                };
                _notificationRepository.Add(notification);
           
        }
        /// <summary>
        /// Determines whether [has unread notifications] [the specified user identifier].
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>
        ///   <c>true</c> if [has unread notifications] [the specified user identifier]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasUnreadNotifications(string userId)
        {
            return _notificationRepository.RetrieveAll().Any(n => n.UserId == userId && !n.IsRead);
        }
        /// <summary>
        /// Deletes the notification.
        /// </summary>
        /// <param name="notificationId">The notification identifier.</param>
        public void DeleteNotification(string notificationId)
        {
            _notificationRepository.Delete(notificationId);
        }
    }
}
