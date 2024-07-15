using ASI.Basecode.Services.ServiceModels;
using System;
using System.Collections.Generic;

namespace ASI.Basecode.Services.Interfaces
{
    public interface INotificationService
    {
        IEnumerable<NotificationViewModel> RetrieveAll(string userId);
        void MarkNotificationAsRead(string notificationId);
        void MarkAllNotificationsAsRead(string UserId);
        void AddNotification(string ticketId, string description, string notificationTypeId,string UserId, string title);
        void GetUnreadNotifications(string userId);
        public void MarkAllNotificationsAsUnread(string UserId);
        void MarkNotificationAsUnread(string notificationId);
        bool HasUnreadNotifications(string userId);
        void DeleteNotification(string notificationId);
    }
}
