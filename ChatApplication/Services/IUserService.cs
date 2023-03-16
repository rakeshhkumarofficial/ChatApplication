using ChatApplication.Models;

namespace ChatApplication.Services
{
    public interface IUserService
    {
        public Response AddUser(Register user);
        public Response Login(Login login, IConfiguration _configuration);    
        public Response UpdateUser(UpdateUser update,string email);
        public Response DeleteUser(Guid UserId);
        public Response UploadProfileImage(FileUpload upload, string email);
        public Response ChangePassword(ChangePassword pass,string email);
        public Response Search(string Name,string email);
    }
}
