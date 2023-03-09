using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class ResetPassword
    {
        public int OneTimePassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
