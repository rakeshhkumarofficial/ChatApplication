using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class UpdateUser
    {
        public string? FirstName { get; set; } 
        public string? LastName { get; set; } 

        [EmailAddress]
        public string? Email { get; set; } 
        public long Phone { get; set; } = 0;
       public DateTime DateOfBirth { get; set; } = DateTime.Now;
    }
}
