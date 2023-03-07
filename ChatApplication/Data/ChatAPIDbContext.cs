using ChatApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Data
{
    public class ChatAPIDbContext : DbContext
    {
        public ChatAPIDbContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<ForgetPassword> ForgetPasswords { get; set; }
    }
}
