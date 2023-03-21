using ChatApplication.Models;

namespace ChatApplication.Services
{
    public interface IPasswordService
    {
        public Response ForgetPassword(ForgetPasswordRequest fp);
        public Response ResetPassword(ResetPassword reset, string email);
    }
}
