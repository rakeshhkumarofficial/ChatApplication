using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class ForgetPasswordRequest
    {
        public string URL { get; set; }
        public string Email { get; set; }
    }
}
