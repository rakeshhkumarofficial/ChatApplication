using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Security.Claims;
using static Azure.Core.HttpHeader;
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
        public async Task CreateChat(Guid Receiver)
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            bool isExists = _dbContext.UserChatMaps.Where(x => x.SenderId == User.UserId && x.ReceiverId == Receiver).Any();
            if (!isExists)
            {
                var chatmap = new ChatMap()
                {
                    MapId = Guid.NewGuid(),
                    SenderId = User.UserId,
                    ReceiverId = Receiver
                };
                _dbContext.UserChatMaps.Add(chatmap);
                _dbContext.SaveChanges();
            }
            var obj = _dbContext.UserChatMaps.Where(x => x.SenderId == User.UserId && x.ReceiverId == Receiver).Select(x => x).FirstOrDefault();
            var username = _dbContext.Users.Where(x => x.UserId == obj.ReceiverId).Select(x => x.FirstName).First();
            await Clients.Caller.SendAsync("ChatCreatedWith", username);
        }              
        public async Task SendMessage(string receiver,string message)
        {
            var ReceiverId = new Guid(receiver);
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            var msg = new ChatMessage()
            {
                MessageId = Guid.NewGuid(),
                SenderId = User.UserId,
                ReceiverId = ReceiverId,
                Message = message,
                TimeStamp = DateTime.UtcNow
            };
            var Sender = User.FirstName;
            _dbContext.ChatMessages.Add(msg);
            _dbContext.SaveChanges();
            var recievermail = _dbContext.Users.FirstOrDefault(u => u.UserId == ReceiverId);
            var connId = ConnectionId.Where(x => x.Key == recievermail.Email).Select(x => x.Value);
            await Clients.Clients(connId).SendAsync("ReceiveMessage",Sender,message);
            await Clients.Caller.SendAsync("ReceiveMessage",Sender,message);
            
        } 
          
        public async Task GetOnlineUsers()
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var obj = ConnectionId.Where(x => x.Key != email).Select(x => x.Key);

            List<object> names = new List<object>();
                    
            foreach (var obj2 in obj)
            {
                var usernames = _dbContext.Users.Where(x => x.Email == obj2).Select(x => x).First();
                names.Add(usernames.FirstName + " " + usernames.LastName);              
            }         
            await Clients.Caller.SendAsync("OnlineUsersList", names);          
        }
        
        public async Task GetChats()
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            var ReceiverIds = _dbContext.UserChatMaps.Where(u => u.SenderId == User.UserId).Select(u => u.ReceiverId).ToList();
            List<string> chatlist = new List<string>();
            if (ReceiverIds != null)
            {
                
                foreach (var receiverid in ReceiverIds)
                {
                    var obj = _dbContext.Users.Where(u => u.UserId == receiverid).Select(u => u).First();
                    chatlist.Add(obj.FirstName+" "+obj.LastName);
                }            
            }
            await Clients.Caller.SendAsync("ChatList", chatlist);
        }

        public async Task LoadMessages(Guid ReceiverId)
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            var msgs = _dbContext.ChatMessages.Where(x => x.SenderId == User.UserId && x.ReceiverId == ReceiverId || x.SenderId == ReceiverId && x.ReceiverId == User.UserId);
            var orderedmsg = msgs.OrderByDescending(m => m.TimeStamp).Select(x => new {x.SenderId , x.Message , x.TimeStamp});
            var messages = orderedmsg.Take(30);
            var usersname = _dbContext.Users.Where(u => u.UserId == User.UserId || u.UserId == ReceiverId).Select(u => new { u.FirstName, u.LastName }).First();

            var messagelist = new { usersname , messages };
            await Clients.Caller.SendAsync("oldMessages", messagelist);

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
