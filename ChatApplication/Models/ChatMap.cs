using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class ChatMap
    {
        [Key]
        public Guid MapId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
    }
}
