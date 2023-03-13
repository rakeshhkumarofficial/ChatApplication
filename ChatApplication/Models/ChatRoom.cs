using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class ChatRoom
    {
        [Key]
        public Guid ChatRoomId { get; set; }
        public string ChatRoomName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }      
        public virtual List<UserRoomMap> Members { get; set; }
        public virtual List<ChatMessage> Messages { get; set; } 
    }
}
