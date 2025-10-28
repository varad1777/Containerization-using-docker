using Microsoft.AspNetCore.SignalR;

namespace MyApp.Infrastructure.RTC
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            var userId = Context.UserIdentifier; 
    Console.WriteLine($"User connected: {userId}, connection: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

    }
}
