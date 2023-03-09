using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class ForgetPassword
    {
        public Guid Id { get; set; }

        [EmailAddress]
        public string Email { get; set; }
        public string ResetPasswordToken { get; set; }
        public int OneTimePass { get; set; }    
        public DateTime ExpiresAt { get; set; }
    }
}
