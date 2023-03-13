using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Models
{
    public class UserRoomMap
    {
        [Key]
        public Guid MapId { get; set; } 
        public Guid User1Id { get; set; }
        public Guid User2Id { get; set; }
    }
}
