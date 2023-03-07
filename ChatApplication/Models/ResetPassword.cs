using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class ResetPassword
    {
        [EmailAddress] 
        public string Email { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
