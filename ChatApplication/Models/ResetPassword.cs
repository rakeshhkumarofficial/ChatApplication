﻿using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class ResetPassword
    {
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
