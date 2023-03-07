﻿using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class Register
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }
        public long Phone { get; set; }
        
        [Required]
        public string? Password { get; set; }
        public DateTime DateOfBirth { get; set; } = DateTime.UtcNow;
    }
}
