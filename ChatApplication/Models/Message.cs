using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class Message
    {
        [Key]
        public Guid MessageId { get; set; }
        public string SenderEmail { get; set; }
        public string ReceiverEmail { get; set; }
        public string Messages { get; set; }
        public int MessageType { get; set; } = 1;
        public DateTime TimeStamp { get; set; }
    }
}
