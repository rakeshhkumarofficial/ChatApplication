using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class Register
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        [Required(ErrorMessage ="Email is required")]
        [EmailAddress]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        public long Phone { get; set; }
        
        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }
        public DateTime DateOfBirth { get; set; } = DateTime.UtcNow;
    }
}
