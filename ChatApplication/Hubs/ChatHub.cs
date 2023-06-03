using ChatApplication.Data;
using ChatApplication.Models;
using MailKit.Search;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenAI_API.Completions;
using OpenAI_API;
using System.Security.Claims;
using static Azure.Core.HttpHeader;

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
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            User.IsActive = true;
            _dbContext.SaveChanges();
            /*if(ConnectionId.Keys.Contains(email))
            {
                Clients.Caller.SendAsync("AlreadyLogined");
                return base.OnConnectedAsync();
            }*/
            ConnectionId.Add(email, Context.ConnectionId);
            Clients.Caller.SendAsync("Connected");
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
                response.Data = mapWithReceiver;
                response.StatusCode = 200;
                response.Message = "Chat Created";
                await Clients.Caller.SendAsync("ChatCreatedWith", response);      
                return response;
            }
            var obj = _dbContext.UserMappings.Where(x => x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail).Select(x => x).FirstOrDefault();
            response.Data = obj;
            response.StatusCode = 200;
            response.Message = "Chat Already Created";
            await Clients.Caller.SendAsync("ChatCreatedWith", response);
            return response;               
            
        }

        // Send Message To ChatList Users
        public async Task<Response> SendMessage(string ReceiverEmail, string message , int messageType)
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
                MessageType = 1,
                TimeStamp = DateTime.UtcNow
            };
            if (ReceiverEmail == "chat@gmail.com")
            {
                if (messageType == 2 || messageType==3)
                {
                    msg.Messages = "Do not support Images and pdf";
                    response.Data = msg;
                    response.StatusCode = 200;
                    response.Message = "Error Message";
                    await Clients.Caller.SendAsync("ReceiveMessage", response);
                    return response;
                }
                
                // OpenAI API key
                string APIKey = " type your api key";
                string answer = string.Empty;

                // Create an instance of the OpenAIAPI class with the API key
                var openai = new OpenAIAPI(APIKey);

                // Create a new CompletionRequest object with the input prompt and other parameters
                CompletionRequest completion = new CompletionRequest();
                completion.Prompt = message;
                completion.Model = OpenAI_API.Models.Model.DavinciText;
                completion.MaxTokens = 500;

                // Call the CreateCompletionAsync method of the openai.Completions object to send the API request
                var result = openai.Completions.CreateCompletionAsync(completion);

                // Extract the response text from the CompletionResponse object and assign it to the 'answer' variable
                foreach (var item in result.Result.Completions)
                {
                    answer = item.Text;
                }
                var SenderBot = User.FirstName;
                _dbContext.ChatMessage.Add(msg);
                var msgbot = new Message()
                {
                    MessageId = Guid.NewGuid(),
                    SenderEmail = ReceiverEmail,
                    ReceiverEmail = email,
                    Messages = answer,
                    MessageType = 1,
                    TimeStamp = DateTime.UtcNow
                };
                _dbContext.ChatMessage.Add(msgbot);
                // await SendMessage(email, answer, 1);
                var updateMapBot = _dbContext.UserMappings.Where(x => (x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail) || (x.SenderEmail == ReceiverEmail && x.ReceiverEmail == email));
                foreach (var map in updateMapBot)
                {
                    map.LastUpdated = DateTime.Now;
                }

                _dbContext.SaveChanges();
                var connIdBot = ConnectionId.Where(x => x.Key == ReceiverEmail).Select(x => x.Value);
                await Clients.All.SendAsync("refreshChats");
                response.Data = msg;
                response.StatusCode = 200;
                response.Message = "Message Received";
                await Clients.Clients(connIdBot).SendAsync("ReceiveMessage", response);
                response.Message = "Message Sent";
                await Clients.Caller.SendAsync("ReceiveMessage", response);
                return response;
            }
            if(messageType == 2)
            {
                msg.MessageType = 2;
            }
            if(messageType == 3) {
                msg.MessageType = 3;
            }
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
            response.Message = "Message Received";
            await Clients.Clients(connId).SendAsync("ReceiveMessage", response);
            response.Message = "Message Sent";
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
                var usernames = _dbContext.Users.Where(x => x.Email == e).Select(x => new { x.FirstName, x.LastName, x.Email }).First();
                bool ChatExists = _dbContext.UserMappings.Where(x => (x.SenderEmail == email && x.ReceiverEmail == e) || (x.SenderEmail == e && x.ReceiverEmail == email)).Any();
                if (ChatExists)
                {
                    names.Add(usernames);
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
            List<object> chatlist = new List<object>();
            if (ReceiverEmails != null)
            {
                foreach (var e in ReceiverEmails)
                {
                    var usernames = _dbContext.Users.Where(u => u.Email == e).Select(u => new { u.FirstName, u.LastName, u.ProfilePic , u.Gender,u.Phone,u.Email, u.IsActive}).First();
                    chatlist.Add(usernames);
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
            var existemail = _dbContext.UserMappings.Where(x => (x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail) || (x.SenderEmail == ReceiverEmail && x.ReceiverEmail == email)).Any();
            if(!existemail)
            {
                response.StatusCode = 200;
                response.Message = "Email Not exists";
                response.Data = null;
                await Clients.Caller.SendAsync("OldMessages", response);
                return response;
            }
            var prevMessages = _dbContext.ChatMessage.Where(x => (x.SenderEmail == email && x.ReceiverEmail == ReceiverEmail) || (x.SenderEmail == ReceiverEmail && x.ReceiverEmail == email));
            var orderedmsgs = prevMessages.OrderByDescending(m => m.TimeStamp).Select(x => x);     
            var msgslist = (orderedmsgs.Skip((page - 1) * 20).Take(20));
            var usersname = _dbContext.Users.Where(u => u.Email == email || u.Email == ReceiverEmail).Select(u => new { u.FirstName, u.LastName,u.Email,u.ProfilePic }).First();

            var messages = msgslist.Select(x => x).Reverse();
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
            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            User.IsActive = false;
            _dbContext.SaveChanges();
            ConnectionId.Remove(email);
            Clients.All.SendAsync("refreshChats");
            return base.OnDisconnectedAsync(exception);
        }         
    }
}
