using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Services
{
    public class ChatService : IChatService
    {
        private readonly ChatAPIDbContext _dbContext;
        private readonly IConfiguration _configuration;
        public ChatService(ChatAPIDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }
        public Response ChatRoom(Guid User1, Guid User2)
        {
            Response response = new Response();
            var user1 = _dbContext.Users.Where(u => u.UserId == User1).FirstOrDefault();
            var user2 = _dbContext.Users.Where(u => u.UserId == User2).FirstOrDefault();
            if(user1 != null && user2 != null)
            {
                response.StatusCode = 404;
                response.Message = "Not Available to Chat";
                response.Data = null;
                return response;
            }
           
            var ChatRoom = new ChatRoom()
            {
                ChatRoomId = Guid.NewGuid(),
                ChatRoomName = user1.FirstName +" "+ user1.LastName,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Members = new List<User> { user1, user2 },
                Messages = new List<ChatMessage> { null }
            };
            _dbContext.ChatRooms.Add(ChatRoom);
            _dbContext.SaveChanges();
            response.StatusCode = 200;
            response.Message = "ChatRoom Created";
            response.Data = new { ChatRoom.ChatRoomId , ChatRoom.ChatRoomName};
            return response;

        }
    }
}
