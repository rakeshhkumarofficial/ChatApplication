using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class ChatMessage
    {
        [Key]
        public Guid MessageId { get; set; }
        public Guid SenderId { get; set; }
        public Guid RecieverId { get; set; }
        public string Message { get; set; }
        public DateTime TimeStamp { get; set; } 
    }
}
