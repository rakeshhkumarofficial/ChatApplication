//using ChatApplication.Data;
//using ChatApplication.Hubs;
//using ChatApplication.Models;
//using ChatApplication.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Net.Http;
//using System.Security.Claims;

//namespace ChatApplication.Controllers
//{
//    [Route("api/[controller]/[Action]")]
//    [ApiController]
//    public class ChatController : ControllerBase
//    {

//        private readonly IHubContext<ChatHub> _hubContext;
//        private readonly ChatAPIDbContext _dbContext;

//        public ChatController(IHubContext<ChatHub> hubContext, ChatAPIDbContext dbContext)
//        {
//            _hubContext = hubContext;
//            _dbContext = dbContext;
//        }

//        [HttpPost,Authorize(Roles ="Login")]
//        public async Task<IActionResult> SendMessage([FromQuery] Guid RecieverId, [FromQuery] string message)
//        {
//            var user = HttpContext.User;
//            var email = user.FindFirst(ClaimTypes.Name)?.Value;
//            var User = _dbContext.Users.FirstOrDefault(u => u.Email == email);
//            await _hubContext.Clients.Users(RecieverId.ToString()).SendAsync("ReceiveMessage",User.UserId, message);
//            return Ok();
//        }



































//        /*
//        private readonly ChatAPIDbContext _dbContext;
//        public readonly IConfiguration _configuration;
//        public ChatController(ChatAPIDbContext dbContext, IConfiguration configuration)
//        {
//            _dbContext = dbContext;
//            _configuration = configuration;

//        }

//        [HttpPost,Authorize(Roles ="Login")]
//        public IActionResult ChatRoom(ChatRequest chat)
//        {
//            var user = HttpContext.User;
//            var email = user.FindFirst(ClaimTypes.Name)?.Value;
//            var dbuser = _dbContext.Users.FirstOrDefault(x => x.Email == email);
//            IChatService chatService = new ChatService(_dbContext,_configuration);
//            var res = chatService.ChatRoom( chat.UserId, dbuser.UserId);
//            return Ok(res);          
//        }

//        [HttpDelete,Authorize(Roles ="Login")]
//        public IActionResult DeleteRoom(Guid roomId)
//        {
//            var user = HttpContext.User;
//            var email = user.FindFirst(ClaimTypes.Name)?.Value;  
//            IChatService chatService = new ChatService(_dbContext, _configuration);
//            var res = chatService.DeleteRoom(email,roomId);
//            return Ok(res);
//        }
//        */
//    }
//}
