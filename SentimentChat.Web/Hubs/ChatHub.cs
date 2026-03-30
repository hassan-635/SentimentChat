using Microsoft.AspNetCore.SignalR;
using SentimentChat.Web.Models;

namespace SentimentChat.Web.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(ChatMessage message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}
