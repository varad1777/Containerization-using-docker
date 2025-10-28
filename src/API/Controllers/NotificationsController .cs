using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Infrastructure.Data;
using System.Security.Claims;


namespace MyApp.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // Get all notifications for this user
        [HttpGet]
        public IActionResult GetAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var notifications = _context.UserNotifications
                .Where(un => un.UserId == userId)
                .Select(un => new
                {
                    un.Notification.Id,
                    un.Notification.Message,
                    un.Notification.CreatedBy,
                    un.Notification.CreatedAt,
                    un.IsRead
                })
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return Ok(notifications);
        }

        // Mark all as read
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var unread = _context.UserNotifications
                .Where(un => un.UserId == userId && !un.IsRead)
                .ToList();

            foreach (var un in unread)
            {
                un.IsRead = true;
                un.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }


        [HttpPut("mark-read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            // Find the specific UserNotification
            var userNotification = await _context.UserNotifications
                .FirstOrDefaultAsync(un => un.UserId == userId && un.NotificationId == notificationId);

            if (userNotification == null)
                return NotFound("Notification not found");

            // Mark it as read
            userNotification.IsRead = true;
            userNotification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok();
        }


        // Get unread count
        [HttpGet("unread-count")]
        public IActionResult GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var count = _context.UserNotifications.Count(un => un.UserId == userId && !un.IsRead);
            return Ok(count);
        }
    }
}
