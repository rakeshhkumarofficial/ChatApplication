using ChatApplication.Models;

namespace ChatApplication.Services
{
    public interface IUserService
    {
        public Response AddUser(Register user);
        public Response Login(Login login, IConfiguration _configuration);
    }
}
