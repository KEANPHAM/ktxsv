using KTXSV.Models;
using System;

namespace KTXSV.Services
{
    public class NotificationService
    {
        protected readonly KTXSVEntities _db; // protected để lớp con truy cập

        public NotificationService(KTXSVEntities dbContext)
        {
            _db = dbContext;
        }

        /// <summary>
        /// Lưu Notification vào cơ sở dữ liệu
        /// </summary>
        public void SaveNotification(Notification noti)
        {
            if (noti.CreatedAt == null)
                noti.CreatedAt = DateTime.Now;

            if (string.IsNullOrEmpty(noti.TargetRole))
                noti.TargetRole = "Unknown";

            _db.Notifications.Add(noti);
            _db.SaveChanges();
        }
    }
}
