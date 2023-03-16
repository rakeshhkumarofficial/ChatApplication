using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
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
        public object CreateChat(Guid Receiver)
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
        public Response GetOnlineUsers()
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var obj = ConnectionId.Where(x => x.Key != email).Select(x => x.Key).ToList();
            List<object> usernames = new List<object>(); 
            foreach ( var obj2 in obj)
            {
                var users = _dbContext.Users.Where(x => x.Email.ToLower() == obj2.ToLower()).Select(u=>new {u.FirstName , u.LastName});
                usernames.Add(users);
            }        
            Response res = new Response();
            res.StatusCode = 200;
            res.Message = "online Users";
            res.Data = usernames;
            return res;
        }
        public Response GetChats()
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            var ReceiverIds = _dbContext.UserChatMaps.Where(u => u.SenderId == User.UserId).Select(u => u.ReceiverId).ToList();
            Response res = new Response();
            if (ReceiverIds != null)
            {
                List<object> chatlist = new List<object>();
                foreach (var receiverid in ReceiverIds) {
                    var obj = _dbContext.Users.Where(u => u.UserId == receiverid).Select(u=>  new { u.FirstName, u.LastName });
                    chatlist.Add(obj);
                 }
                res.StatusCode = 200;
                res.Message = "chats List";
                res.Data = chatlist;
                return res;
            }
            res.StatusCode = 404;
            res.Message = "Chat doesn't Exist";
            res.Data = null;
            return res;
        }
        public Response LoadMessages(Guid ReceiverId)
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            var msgs = _dbContext.ChatMessages.Where(x => x.SenderId == User.UserId && x.ReceiverId == ReceiverId);
            var orderedmsg = msgs.OrderByDescending(m => m.TimeStamp).Select(x => x);
            int pages = 10;
            int records = 30;
            var pageRecords = (orderedmsg.Skip((pages - 1) * records).Take(records));
            var usersname = _dbContext.Users.Where(u => u.UserId == User.UserId || u.UserId == ReceiverId).Select(u => new { u.FirstName, u.LastName });
            Response res = new Response();
            res.StatusCode = 200;
            res.Message = "Previous messages";
            res.Data = new {pageRecords , usersname};
            return res;            
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
