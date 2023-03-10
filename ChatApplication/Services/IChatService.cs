using ChatApplication.Models;

namespace ChatApplication.Services
{
    public interface IChatService
    {
        public Response ChatRoom(Guid User1 , Guid User2);
        public Response DeleteRoom(string email, Guid roomId);
    }
}
