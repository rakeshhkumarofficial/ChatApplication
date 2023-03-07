using ChatApplication.Models;

namespace ChatApplication.Services
{
    public interface IUserService
    {
        public Response AddUser(Register user);
        public Response Login(Login login, IConfiguration _configuration);
        public Response GetUser(Guid UserId, string? FirstName, string? LastName, long Phone, int sort, int pageNumber, int records);
        public Response UpdateUser(Guid UserId, UpdateUser update);
    }
}
