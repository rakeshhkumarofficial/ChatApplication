using ChatApplication.Data;
using ChatApplication.Models;
using ChatApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ChatAPIDbContext _dbContext;
        public readonly IConfiguration _configuration;
        public ChatController(ChatAPIDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;

        }

        [HttpPost,Authorize(Roles ="Login")]
        public IActionResult ChatRoom(ChatRequest chat)
        {
            var user = HttpContext.User;
            var email = user.FindFirst(ClaimTypes.Name)?.Value;
            var dbuser = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            IChatService chatService = new ChatService(_dbContext,_configuration);
            var res = chatService.ChatRoom( chat.UserId, dbuser.UserId);
            return Ok(res);          
        }

    }
}
