using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ChatApplication.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatAPIDbContext _dbContext;
        public ChatHub(ChatAPIDbContext dbContext)
        {
            _dbContext = dbContext;
        }
       

       // [Authorize(Roles ="Login")]
        public async Task SendMessage(Guid ReceiverId, string message)
         {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;  
            var User = _dbContext.Users.FirstOrDefault(u=>u.Email== email);
            var msg = new ChatMessage()
            {
                MessageId = Guid.NewGuid(),
                SenderId = User.UserId,
                RecieverId = ReceiverId,
                Message = message,
                TimeStamp = DateTime.UtcNow
            };
            var Sender = from u in _dbContext.Users where u.Email == email select new {u.FirstName, u.LastName};         
            _dbContext.ChatMessages.Add(msg);
            _dbContext.SaveChanges();
            
            await Clients.User(ReceiverId.ToString()).SendAsync("ReceiveMessage", Sender, message, Context.ConnectionId);
         }
        
       
    }
}
