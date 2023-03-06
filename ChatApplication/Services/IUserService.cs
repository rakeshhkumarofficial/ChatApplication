using ChatApplication.Models;

namespace ChatApplication.Services
{
    public interface IUserService
    {
        public Response AddUser(Register user);
        public Response Login(string Email, string Password, IConfiguration _configuration);
    }
}
