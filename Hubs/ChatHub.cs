using Microsoft.AspNetCore.SignalR;

namespace EventsApp.Hubs
{
    public class ChatHub : Hub
    {
        public Task JoinConversation(string token)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, token);
        }

        public Task LeaveConversation(string token)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, token);
        }
    }
}
