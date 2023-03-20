using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class UpdateUser
    {
        public string? FirstName { get; set; } 
        public string? LastName { get; set; } 
        public string? Email { get; set; }
        public long Phone { get; set; } = -1;
        public DateTime DateOfBirth { get; set; } = DateTime.MinValue;
    }
}
