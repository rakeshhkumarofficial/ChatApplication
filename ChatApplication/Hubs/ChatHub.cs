using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ChatApplication.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        private readonly ChatAPIDbContext _dbContext;
        private static Dictionary<string, string> ConnectionId = new Dictionary<string, string>();
        public ChatHub(ChatAPIDbContext dbContext)
        {
            _dbContext = dbContext;
        }      
        public override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            ConnectionId.Add(email, Context.ConnectionId);
            return base.OnConnectedAsync();
        }
        public async Task<object> CreateChat(Guid Receiver)
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            bool isExists = _dbContext.UserChatMaps.Where(x=>x.SenderId==User.UserId && x.ReceiverId == Receiver).Any();
            Response res = new Response();
            if(!isExists)
            {
                var chatmap = new ChatMap()
                {
                    MapId = Guid.NewGuid(),
                    SenderId = User.UserId,
                    ReceiverId = Receiver
                };
                res.Message = "Chat Created";
                res.Data = chatmap;
                res.StatusCode = 200;
                _dbContext.UserChatMaps.Add(chatmap);
                _dbContext.SaveChanges();
                return res;
            }
            var obj = _dbContext.UserChatMaps.Where(x => x.SenderId == User.UserId && x.ReceiverId == Receiver).Select(x => x).FirstOrDefault();
            res.Message = "Chat Created";
            res.Data = obj;
            res.StatusCode = 200;
            return res;
        }
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
                ReceiverId = ReceiverId,
                Message = message,
                TimeStamp = DateTime.UtcNow
            };
            var Sender = from u in _dbContext.Users where u.Email == email select new {u.FirstName, u.LastName};         
            _dbContext.ChatMessages.Add(msg);
            _dbContext.SaveChanges();
            var recievermail = _dbContext.Users.FirstOrDefault(u => u.UserId == ReceiverId);
            var connId = ConnectionId.Where(x => x.Key == recievermail.Email).Select(x => x.Value).First();
            await Clients.User(connId).SendAsync("ReceiveMessage", Sender, message);
            await Clients.Caller.SendAsync("ReceiveMessage", Sender, message);
        }
        public async Task GetOnlineUsers()
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var obj = ConnectionId.Where(x => x.Key != email).Select(x => x.Key);
            var connId = ConnectionId.Where(x => x.Key == email).Select(x => x.Value).First();
            await Clients.User(connId).SendAsync("RecieveOnlineUsers", obj);
        }
        public object GetChats()
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            var chats = _dbContext.UserChatMaps.Where(u => u.SenderId == User.UserId).Select(u => u);
            return chats;
        }
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            ConnectionId.Remove(email);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
