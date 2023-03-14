using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApplication.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatAPIDbContext _dbContext;
        public ChatHub(ChatAPIDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        [Authorize(Roles ="Login")]
        public async Task SendMessageToReciever(Guid SenderId, Guid RecieverId, string message)
         {
            var msg = new ChatMessage()
            {
                MessageId = Guid.NewGuid(),
                SenderId = SenderId,
                RecieverId = RecieverId,
                Message = message,
                TimeStamp = DateTime.UtcNow
     
            };
            var Sender = from u in _dbContext.Users where u.UserId == SenderId select u.FirstName;         
            _dbContext.ChatMessages.Add(msg);
            _dbContext.SaveChanges();
            
            await Clients.User(RecieverId.ToString()).SendAsync("ReceiveMessage", Sender, message);
         }
    }
}
