using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Domain.Entities
{
    public class UserNotification
    {

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid NotificationId { get; set; }
        public Notification Notification { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

    }
}
