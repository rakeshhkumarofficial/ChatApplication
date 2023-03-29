using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class Register
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public long Phone { get; set; }
        public string? Password { get; set; }
        public DateTime DateOfBirth { get; set; } = DateTime.Now;
    }
}
