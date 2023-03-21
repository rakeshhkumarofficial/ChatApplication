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
            ConnectionId.Add(email, Context.ConnectionId);
            Clients.All.SendAsync("refreshChats");
            return base.OnConnectedAsync();
        }

        // Create Chat with Available Users
        public async Task CreateChat(string ReceiverEmail)
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            bool isExists = _dbContext.UserMappings.Where(x => x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail).Any();
            if (!isExists)
            {
                var mapWithReceiver = new UserMapping()
                {
                    MapId = Guid.NewGuid(),
                    SenderEmail = email,
                    ReceiverEmail = ReceiverEmail,
                    LastUpdated = DateTime.Now
                };
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
            }
            var receiver = _dbContext.UserMappings.Where(x => x.SenderEmail == User.Email && x.ReceiverEmail == ReceiverEmail).Select(x => x).FirstOrDefault();
            var username = _dbContext.Users.Where(x => x.Email == receiver.ReceiverEmail).Select(x => x.FirstName).First();
            await Clients.Caller.SendAsync("ChatCreatedWith", username);
        }

        // Send Message To ChatList Users
        public async Task SendMessage(string ReceiverEmail, string message)
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            bool isExists = _dbContext.UserMappings.Where(x => (x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail) || (x.SenderEmail == ReceiverEmail && x.ReceiverEmail == email)).Any();
            if (!isExists)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "First Create chat with this User ");
            }
            if (isExists)
            {
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
                await Clients.Clients(connId).SendAsync("ReceiveMessage", Sender, message);
                await Clients.Caller.SendAsync("ReceiveMessage", Sender, message);
            }
        }

        // Get Online Users from ChatList
        public async Task GetOnlineUsers()
        {
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
                await Clients.Caller.SendAsync("OnlineUsersList", names);
            }
            if (names.Count == 0)
            {
                await Clients.Caller.SendAsync("OnlineUsersList", "No one is online");
            }

        }

        // Get Chatlist Users
        public async Task GetChatList()
        {
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
            await Clients.Caller.SendAsync("ChatList", chatlist);
        }

        // Load Old Messages
        public async Task LoadMessages(string ReceiverEmail , int page)
        {
            var httpContext = Context.GetHttpContext();
            var user1 = httpContext.User;
            var email = user1.FindFirst(ClaimTypes.Name)?.Value;
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            var messages = _dbContext.ChatMessage.Where(x => (x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail) || (x.SenderEmail == ReceiverEmail && x.ReceiverEmail == email));
            var orderedmsgs = messages.OrderByDescending(m => m.TimeStamp).Select(x => x);     
            var msgslist = (orderedmsgs.Skip((page - 1) * 30).Take(30));
            var usersname = _dbContext.Users.Where(u => u.Email == email || u.Email == ReceiverEmail).Select(u => new { u.FirstName, u.LastName }).First();

            var messagelist = new { usersname, messages };
            await Clients.Caller.SendAsync("OldMessages", messagelist);

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
