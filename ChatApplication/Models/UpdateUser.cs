using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class UpdateUser
    {
        public string? FirstName { get; set; } = "string";
        public string? LastName { get; set; } = "string";

        [EmailAddress]
        public string? Email { get; set; } = "user@example.com";
        public long Phone { get; set; } = 0;
       public DateTime DateOfBirth { get; set; } = DateTime.Now;
    }
}
