namespace ChatApplication.Models
{
    public class ChatRoom
    {
        public Guid ChatRoomId { get; set; }
        public string ChatRoomName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public virtual List<User> Members { get; set; }
        public virtual List<ChatMessage> Messages { get; set; }
    }
}
