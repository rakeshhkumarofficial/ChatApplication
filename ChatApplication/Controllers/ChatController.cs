//using ChatApplication.Data;
//using ChatApplication.Hubs;
//using ChatApplication.Models;
//using ChatApplication.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.SignalR;
//using System;
//using System.Security.Claims;

//namespace ChatApplication.Controllers
//{
//    [Route("api/[controller]/[Action]")]
//    [ApiController]
//    public class ChatController : ControllerBase
//    {

//        private readonly IHubContext<ChatHub> _hubContext;

//        public ChatController(IHubContext<ChatHub> hubContext)
//        {
//            _hubContext = hubContext;
//        }
                                         
//        [HttpPost]
//        public IActionResult Send(Guid ReceiverId , string message)
//        {
//            _hubContext.Clients.All.SendAsync("ReceiveMessage", ReceiverId, message);
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
