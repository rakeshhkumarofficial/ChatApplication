using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

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
       
        // Create connectionId of login User
        public override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value; 
            if(ConnectionId.Keys.Contains(email))
            {
                Clients.Caller.SendAsync("AlreadyLogined");
                return base.OnConnectedAsync();
            }
            ConnectionId.Add(email, Context.ConnectionId);
            Clients.All.SendAsync("refreshChats");
            return base.OnConnectedAsync();
        }

        // Create Chat with Available Users
        public async Task<Response> CreateChat(string ReceiverEmail)
        {
            Response response = new Response();
            var mapWithReceiver = new UserMapping();
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            bool isExists = _dbContext.UserMappings.Where(x => x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail).Any();
           
            if (!isExists)
            {
                //var mapWithReceiver = new UserMapping()

                mapWithReceiver.MapId = Guid.NewGuid();
                mapWithReceiver.SenderEmail = email;
                mapWithReceiver.ReceiverEmail = ReceiverEmail;
                mapWithReceiver.LastUpdated = DateTime.Now;
                
                _dbContext.UserMappings.Add(mapWithReceiver);

                var mapWithSender = new UserMapping()
                {
                    MapId = Guid.NewGuid(),
                    SenderEmail = ReceiverEmail,
                    ReceiverEmail = email,
                    LastUpdated = DateTime.Now
                };
                _dbContext.UserMappings.Add(mapWithSender);
                _dbContext.SaveChanges();
                await Clients.Caller.SendAsync("ChatCreatedWith", mapWithReceiver);
                response.Data = mapWithReceiver;
                response.StatusCode = 200;
                response.Message = "Chat Created";
                return response;
            }
            var obj = _dbContext.UserMappings.Where(x => x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail).Select(x => x).FirstOrDefault();
            await Clients.Caller.SendAsync("ChatCreatedWith", obj);
            response.Data = obj;
            response.StatusCode = 200;
            response.Message = "Chat Already Created";
            return response;               
            
        }

        // Send Message To ChatList Users
        public async Task<Response> SendMessage(string ReceiverEmail, string message)
        {
            Response response = new Response();
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            bool isExists = _dbContext.UserMappings.Where(x => (x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail) || (x.SenderEmail == ReceiverEmail && x.ReceiverEmail == email)).Any();
            if (!isExists)
            {
                response.Data = null;
                response.StatusCode = 200;
                response.Message = "First Create Chat with this User";
                await Clients.Caller.SendAsync("ReceiveMessage", response);
                return response;
            }
            var msg = new Message()
            {
                MessageId = Guid.NewGuid(),
                SenderEmail = email,
                ReceiverEmail = ReceiverEmail,
                Messages = message,
                TimeStamp = DateTime.UtcNow
            };
            var Sender = User.FirstName;
            _dbContext.ChatMessage.Add(msg);
            var updateMap = _dbContext.UserMappings.Where(x => (x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail) || (x.SenderEmail == ReceiverEmail && x.ReceiverEmail == email));
            foreach (var map in updateMap)
            {
                map.LastUpdated = DateTime.Now;
            }

            _dbContext.SaveChanges();
            var connId = ConnectionId.Where(x => x.Key == ReceiverEmail).Select(x => x.Value);
            await Clients.All.SendAsync("refreshChats");
            response.Data = msg;
            response.StatusCode = 200;
            response.Message = "Message Sent";
            await Clients.Clients(connId).SendAsync("ReceiveMessage", response);
            await Clients.Caller.SendAsync("ReceiveMessage", response); 
            return response;
        }

        // Get Online Users from ChatList
        public async Task<Response> GetOnlineUsers()
        {
            Response response = new Response();
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            var emails = ConnectionId.Where(x => x.Key != email).Select(x => x.Key);

            List<object> names = new List<object>();

            foreach (var e in emails)
            {
                var usernames = _dbContext.Users.Where(x => x.Email == e).Select(x => x).First();
                bool ChatExists = _dbContext.UserMappings.Where(x => (x.SenderEmail == email && x.ReceiverEmail == e) || (x.SenderEmail == e && x.ReceiverEmail == email)).Any();
                if (ChatExists)
                {
                    names.Add(usernames.FirstName + " " + usernames.LastName);
                }
            }
            if (names.Count > 0)
            {
                response.Data = names;
                response.StatusCode = 200;
                response.Message = "Online Users";
                await Clients.Caller.SendAsync("OnlineUsersList", response);
                return response;
            }

            response.Data = null;
            response.StatusCode = 200;
            response.Message = "No one is online";
            await Clients.Caller.SendAsync("OnlineUsersList", response);
            return response;

        }

        // Get Chatlist Users
        public async Task<Response> GetChatList()
        {
            Response response = new Response();
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var Receivers = _dbContext.UserMappings.Where(u => u.SenderEmail == email);
            var ReceiverEmails = Receivers.OrderByDescending(x=>x.LastUpdated).Select(x=>x.ReceiverEmail).ToList();
            List<string> chatlist = new List<string>();
            if (ReceiverEmails != null)
            {
                foreach (var e in ReceiverEmails)
                {
                    var usernames = _dbContext.Users.Where(u => u.Email == e).Select(u => u).First();
                    chatlist.Add(usernames.FirstName + " " + usernames.LastName);
                }
            }
            response.Data = chatlist;
            response.StatusCode = 200;
            response.Message = "Chat List";
            await Clients.Caller.SendAsync("ChatList", response);
          
            return response;
        }

        // Load Old Messages
        public async Task<Response> LoadMessages(string ReceiverEmail , int page)
        {
            Response response = new Response();
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            var messages = _dbContext.ChatMessage.Where(x => (x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail) || (x.SenderEmail == ReceiverEmail && x.ReceiverEmail == email));
            var orderedmsgs = messages.OrderByDescending(m => m.TimeStamp).Select(x => x);     
            var msgslist = (orderedmsgs.Skip((page - 1) * 30).Take(30));
            var usersname = _dbContext.Users.Where(u => u.Email == email || u.Email == ReceiverEmail).Select(u => new { u.FirstName, u.LastName }).First();

            var messagelist = new { usersname, messages };
            response.StatusCode = 200;
            response.Message = "old Messages";
            response.Data = messagelist;
            await Clients.Caller.SendAsync("OldMessages", response);
            
            return response;

        }

        // Remove ConnectionId when User Logout
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
