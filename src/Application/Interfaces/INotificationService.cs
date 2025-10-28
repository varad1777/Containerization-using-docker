using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendToAllAsync(string message, string createdBy);
    }
}
