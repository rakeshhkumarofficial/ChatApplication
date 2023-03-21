using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class UserMapping
    {
        [Key]
        public Guid MapId { get; set; }
        public string SenderEmail { get; set; }
        public string ReceiverEmail { get; set; }
        public DateTime LastUpdated { get; set;} = DateTime.Now;
    }
}
