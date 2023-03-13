using ChatApplication.Data;
using ChatApplication.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
            if(user1 == null && user2 == null)
            {
                response.StatusCode = 404;
                response.Message = "Not Available to Chat";
                response.Data = null;
                return response;
            }
            var map = new UserRoomMap()
            {
                MapId = Guid.NewGuid(),
                User1Id = User1,
                User2Id = User2,
            };
            var msg = new ChatMessage()
            {
                MessageId = Guid.NewGuid(),
                SenderId = User2,
                RecieverId = User1,
                Message = null,
                TimeStamp = DateTime.Now,
            };
            
            var ChatRoom = new ChatRoom()
            {
                ChatRoomId = Guid.NewGuid(),
                ChatRoomName = user1.FirstName +" "+ user1.LastName,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Members = new List<UserRoomMap> { map },
                Messages = new List<ChatMessage> { msg }
            };
            _dbContext.ChatRooms.Add(ChatRoom);
            _dbContext.SaveChanges();
            response.StatusCode = 200;
            response.Message = "ChatRoom Created";
            response.Data = new { ChatRoom.ChatRoomId , ChatRoom.ChatRoomName , ChatRoom.Members , ChatRoom.Messages};
            return response;

        }

        public Response DeleteRoom(string email, Guid roomId)
        {
            var obj = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            
            int len = obj == null ? 0 : 1;
            Response res = new Response();
            if (len == 0)
            {
                res.StatusCode = 404;
                res.Message = "Not Found";
                res.Data = null;
                return res;
            }
            var room = _dbContext.ChatRooms.FirstOrDefault(x => x.ChatRoomId == roomId);
            int roomlen = room == null ? 0 : 1;
            if (roomlen == 0)
            {
                res.StatusCode = 404;
                res.Message = "ChatRoom Not Found";
                res.Data = null;
                return res;
            }

            _dbContext.ChatRooms.Remove(room);        
            _dbContext.SaveChanges();
            res.StatusCode = 200;
            res.Message = "Chat room deleted";
            res.Data = new { room.ChatRoomName, room.ChatRoomId };
            return res;

        }
    }
}
