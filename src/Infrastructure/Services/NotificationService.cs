using Microsoft.AspNetCore.SignalR;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.RTC;
using System.Linq;
using System.Threading.Tasks;

namespace MyApp.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task SendToAllAsync(string message, string createdBy)
        {
            // 1️⃣ Create main notification
            var notification = new Notification
            {
                Message = message,
                CreatedBy = createdBy,

            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // 2️⃣ Create tracking entries for all users
            var userIds = _context.Users.Select(u => u.Id).ToList(); // Adjust if your User table name differs
            var userNotifications = userIds.Select(uid => new UserNotification
            {
                UserId = uid,
                NotificationId = notification.Id,
                IsRead = false
            }).ToList();

            _context.UserNotifications.AddRange(userNotifications);
            await _context.SaveChangesAsync();

            // 3️⃣ Broadcast via SignalR
            await _hubContext.Clients.All.SendAsync(
                "ReceiveNotification",
                notification.Id,
                notification.CreatedBy,
                notification.Message,
                notification.CreatedAt
            );
        }
    }
}
