using ChatApplication.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;

namespace ChatApplication.Data
{
    public class ChatAPIDbContext : DbContext
    {
        public ChatAPIDbContext(DbContextOptions options) : base(options)
        {
        }
        // User Table 
        public DbSet<User> Users { get; set; }

        // ForgetPassword Table
        public DbSet<ForgetPassword> ForgetPasswords { get; set; }

        // ChatMessage Table
        public DbSet<ChatMessage> ChatMessages { get; set; }

        // ChatMap Table - one on one chat Mapping
        public DbSet<ChatMap> UserChatMaps { get; set;}      
    }
}
