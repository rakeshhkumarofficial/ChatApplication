using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class ForgetPasswordRequest
    {
        [EmailAddress]
        public string Email { get; set; }
    }
}
